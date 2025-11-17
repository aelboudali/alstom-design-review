using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Cloud.Common;
using Unity.Cloud.DataStreaming.Runtime;
using Unity.Cloud.DataStreaming.Metadata;
using UnityEngine;
using System.Threading;
using System.Linq;

namespace Unity.Industry.Viewer.Streaming.Hierarchy
{
    public class HierarchyModifier : InstanceModifier
    {
        const int k_InitialAncestorQueryChunkSize = 5_000;

        static readonly OptionalData k_OptionalData = new(OptionalData.Fields.Id | OptionalData.Fields.AncestorIds);
        static readonly Color32 k_DefaultColor = Color.clear;

        readonly Dictionary<ModelStreamId, Tracker> m_Trackers = new();
        readonly HashSet<ModelStreamId> m_MissingTrackerWarningLogged = new();

        public void AddMetadataRepository(ModelStreamId modelStreamId, IMetadataRepository metadataRepository,
            CancellationToken cancellationToken)
        {
            m_Trackers.Add(modelStreamId, new Tracker(modelStreamId, metadataRepository, cancellationToken));
        }

        public void RemoveMetadataRepository(ModelStreamId modelStreamId)
        {
            if (!TryGetTracker(modelStreamId, out var tracker))
                return;

            tracker.Dispose();
            m_Trackers.Remove(modelStreamId);
        }

        async Task ExecuteInitialAncestorQuery(Tracker tracker)
        {
            for (var i = 0; i < tracker.InstancesToQueryAncestors.Count; i += k_InitialAncestorQueryChunkSize)
            {
                var chunk = tracker.InstancesToQueryAncestors.Skip(i).Take(k_InitialAncestorQueryChunkSize);
                await ExecuteAncestorQuery(tracker.ModelStreamId, chunk);
            }

            tracker.InstancesToQueryAncestors.Clear();
        }

        async Task ExecuteAncestorQuery(ModelStreamId modelStreamId, IEnumerable<InstanceId> instances)
        {
            if (!TryGetTracker(modelStreamId, out var tracker))
                return;

            if (tracker.CancellationToken.IsCancellationRequested)
                return;

            var query = tracker.MetadataRepository
                .Query()
                .Select(MetadataPathCollection.None, k_OptionalData)
                .WhereInstanceEquals(instances)
                .WithCancellation(tracker.CancellationToken);

            await foreach (var metadataInstance in query)
            {
                if (tracker.CancellationToken.IsCancellationRequested)
                    return;

                if (!tracker.AncestorsByInstance.ContainsKey(metadataInstance.Id))
                {
                    var ancestors = new List<InstanceId>();
                    ancestors.AddRange(metadataInstance.AncestorIds);
                    // add the instance itself as its closest ancestor
                    ancestors.Add(metadataInstance.Id);
                    tracker.AncestorsByInstance.Add(metadataInstance.Id, ancestors);
                    tracker.InstancesToModify.Add(metadataInstance.Id);
                }
            }

            ApplyHighlight(tracker, tracker.InstancesToModify);
            ApplyVisibility(tracker, tracker.InstancesToModify);

            tracker.InstancesToModify.Clear();
        }

        public override async Task LoadAsync(ModelStreamId modelStreamId,
            IEnumerable<InstanceGeometricErrorState> states)
        {
            if (!TryGetTracker(modelStreamId, out var tracker))
                return;

            if (tracker.IsPendingInitialQuery)
            {
                tracker.InstancesToQueryAncestors.UnionWith(states.Select(x => x.InstanceId));
                return;
            }

            await ExecuteAncestorQuery(modelStreamId, states.Select(x => x.InstanceId));
        }

        public override Task UnloadAsync(ModelStreamId modelStreamId, IEnumerable<InstanceGeometricErrorState> states)
        {
            if (!TryGetTracker(modelStreamId, out var tracker))
                return Task.CompletedTask;

            if (tracker.IsPendingInitialQuery)
            {
                tracker.InstancesToQueryAncestors.ExceptWith(states.Select(x => x.InstanceId));
                return Task.CompletedTask;
            }

            foreach (var id in states.Select(x => x.InstanceId))
            {
                if (tracker.AncestorsByInstance.TryGetValue(id, out var ancestors))
                {
                    ancestors?.Clear();
                    tracker.AncestorsByInstance.Remove(id);
                }
            }

            return Task.CompletedTask;
        }

        public async Task SetModifiers(
            ModelStreamId modelStreamId,
            IDictionary<Color, List<InstanceId>> highlighted,
            IEnumerable<InstanceId> hidden,
            InstanceId isolated)
        {
            if (!TryGetTracker(modelStreamId, out var tracker))
                return;

            tracker.Highlighted.Clear();
            foreach (var (color, instances) in highlighted)
                tracker.Highlighted.Add(color, instances.ToHashSet());

            tracker.Hidden.Clear();
            tracker.Hidden.UnionWith(hidden);

            tracker.Isolated = isolated;

            if (tracker.IsPendingInitialQuery)
            {
                tracker.IsPendingInitialQuery = false;
                await ExecuteInitialAncestorQuery(tracker);
            }
            else
            {
                ApplyHighlight(tracker, tracker.AncestorsByInstance.Keys);
                ApplyVisibility(tracker, tracker.AncestorsByInstance.Keys);
            }
        }

        bool TryGetTracker(ModelStreamId modelStreamId, out Tracker tracker)
        {
            if (!m_Trackers.TryGetValue(modelStreamId, out tracker))
            {
                if (!m_MissingTrackerWarningLogged.Contains(modelStreamId))
                {
                    var logger = LoggerProvider.GetLogger<HierarchyModifier>();
                    logger.LogWarning(
                        $"Cannot find {nameof(IMetadataRepository)} for {modelStreamId}. Subsequent warnings for this model will be ignored.",
                        this);
                    m_MissingTrackerWarningLogged.Add(modelStreamId);
                }

                return false;
            }

            return true;
        }

        void ApplyHighlight(Tracker tracker, IEnumerable<InstanceId> instances)
        {
            if (!tracker.Highlighted.Any())
            {
                InstanceUpdater.SetHighlight(tracker.ModelStreamId, instances, k_DefaultColor);
                return;
            }

            var allAncestors = tracker.Highlighted.Values.SelectMany(x => x);

            var closestAncestorByInstance = instances.ToDictionary(
                x => x,
                x => tracker.AncestorsByInstance[x].AsEnumerable()
                    .LastOrDefault(ancestor => allAncestors.Contains(ancestor)));

            var notHighlighted = instances.Except(closestAncestorByInstance
                .Where(x => x.Value != default)
                .Select(x => x.Key));

            InstanceUpdater.SetHighlight(tracker.ModelStreamId, notHighlighted, k_DefaultColor);

            foreach (var (color, ancestors) in tracker.Highlighted)
            {
                var highlighted = closestAncestorByInstance
                    .Where(x => ancestors.Contains(x.Value))
                    .Select(x => x.Key);

                InstanceUpdater.SetHighlight(tracker.ModelStreamId, highlighted, color);
            }
        }

        void ApplyVisibility(Tracker tracker, IEnumerable<InstanceId> instances)
        {
            var hidden = tracker.AncestorsByInstance
                .Where(x => instances.Contains(x.Key) && IsHidden(tracker, x.Key))
                .Select(x => x.Key);

            var visible = instances
                .Except(hidden);

            InstanceUpdater.SetVisibility(tracker.ModelStreamId, hidden, false);
            InstanceUpdater.SetVisibility(tracker.ModelStreamId, visible, true);
        }

        static bool IsHidden(Tracker tracker, InstanceId instance)
        {
            var ancestors = tracker.AncestorsByInstance[instance];

            if (tracker.Isolated == InstanceId.None)
            {
                return tracker.Hidden
                    .Intersect(ancestors.AsEnumerable())
                    .Any();
            }

            foreach (var ancestor in ancestors.AsEnumerable().Reverse())
            {
                if (ancestor == tracker.Isolated)
                    return false;

                if (tracker.Hidden.Contains(ancestor))
                    return true;
            }

            // no hidden or isolated ancestors found, but since there is at least one isolated instance the default state is hidden
            return true;
        }
    }
    
    class Tracker : IDisposable
    {
        public readonly ModelStreamId ModelStreamId;
        public readonly IMetadataRepository MetadataRepository;
        public readonly CancellationToken CancellationToken;

        public readonly Dictionary<InstanceId, List<InstanceId>> AncestorsByInstance = new();
        public readonly HashSet<InstanceId> InstancesToQueryAncestors = new();
        public readonly HashSet<InstanceId> InstancesToModify = new();

        public readonly Dictionary<Color, HashSet<InstanceId>> Highlighted = new();
        public readonly HashSet<InstanceId> Hidden = new();
        public InstanceId Isolated = InstanceId.None;

        public bool IsPendingInitialQuery { get; set; } = true;

        public Tracker(ModelStreamId modelStreamId, IMetadataRepository metadataRepository, CancellationToken cancellationToken)
        {
            ModelStreamId = modelStreamId;
            MetadataRepository = metadataRepository;
            CancellationToken = cancellationToken;
        }

        public void Dispose()
        {
            foreach (var pooledList in AncestorsByInstance)
            {
                pooledList.Value.Clear();
            }
        }
    }
}
