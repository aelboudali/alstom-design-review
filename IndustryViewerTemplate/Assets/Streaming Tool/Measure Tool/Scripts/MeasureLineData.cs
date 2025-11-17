using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.Industry.Viewer.Streaming.Measurement
{
    public enum MeasureFormat
    {
        Meters = 0,
        Centimeters = 1,
        Feet = 2,
        Inches = 3,
        FeetAndInches = 4
    }
    
    public enum MeasureMode
    {
        TwoPoint = 0,
        Orthogonal = 1
    }

    [Serializable]
    public class Anchor
    {
        [JsonIgnore]
        public Vector3 Position;
        [JsonIgnore]
        public Vector3 Normal;

        [JsonProperty("Position")]
        public float[] PositionArray
        {
            get => new float[] { Position.x, Position.y, Position.z };
            set
            {
                if (value != null && value.Length == 3)
                    Position = new Vector3(value[0], value[1], value[2]);
            }
        }

        [JsonProperty("Normal")]
        public float[] NormalArray
        {
            get => new float[] { Normal.x, Normal.y, Normal.z };
            set
            {
                if (value != null && value.Length == 3)
                    Normal = new Vector3(value[0], value[1], value[2]);
            }
        }
        
        public Anchor(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }
        // Parameterless constructor for deserialization
        public Anchor() { }
    }
    
    [Serializable]
    public class MeasureLineData
    {
        public string Id { get; set; }

        public string Name { get; set; }

        [JsonIgnore]
        public Color Color { get; set; } = new(0.78f, 0.42f, 0.0f);

        [JsonProperty("Color")]
        public float[] ColorRGBA
        {
            get => new float[] { Color.r, Color.g, Color.b, Color.a };
            set
            {
                if (value != null && value.Length == 4)
                    Color = new Color(value[0], value[1], value[2], value[3]);
            }
        }
        
        public MeasureMode MeasureMode { get; set; } = MeasureMode.TwoPoint;
        
        public MeasureFormat MeasureFormat { get; set; }

        public bool HasMeasureFormatOverride { get; set; }
        
        public float DistanceInMeters
        {
            get
            {
                if (m_IsDirty)
                {
                    m_CachedDistance = ComputeDistanceInMeters();
                    m_IsDirty = false;
                }

                return m_CachedDistance;
            }
        }

        public IReadOnlyList<Anchor> Anchors => m_Anchors;

        public bool IsEditable { get; set; } = true;

        static readonly string k_DefaultName = "Measure Line";
        readonly List<Anchor> m_Anchors;

        bool m_IsDirty = true;
        float m_CachedDistance;

        public MeasureLineData()
        {
            Id = Guid.NewGuid().ToString();
            Name = k_DefaultName;
            m_Anchors = new List<Anchor>();
        }
        
        public MeasureLineData(MeasureFormat measureFormat, bool hasMeasureFormatOverride)
        {
            Id = Guid.NewGuid().ToString();
            Name = k_DefaultName;
            m_Anchors = new List<Anchor>();
            MeasureFormat = measureFormat;
            HasMeasureFormatOverride =  hasMeasureFormatOverride;
        }

        public MeasureLineData(IEnumerable<Anchor> anchors)
        {
            Id = Guid.NewGuid().ToString();
            m_Anchors = new List<Anchor>(anchors);
        }

        public static MeasureLineData Clone(MeasureLineData other)
        {
            var clone = new MeasureLineData(other.Id, other.Anchors, other.MeasureFormat, other.HasMeasureFormatOverride)
            {
                Name = other.Name,
                Color = other.Color,
                IsEditable = other.IsEditable
            };
            return clone;
        }

        MeasureLineData(string id, IEnumerable<Anchor> anchors, MeasureFormat measureFormat, bool hasMeasureFormatOverride)
        {
            m_Anchors = new List<Anchor>(anchors);
            Id = id;
            MeasureFormat = measureFormat;
            HasMeasureFormatOverride = hasMeasureFormatOverride;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (MeasureLineData)obj;

            if (Id != other.Id)
            {
                return false;
            }

            if (Anchors == null && other.Anchors == null)
            {
                return true;
            }

            if (Anchors == null || other.Anchors == null || Anchors.Count != other.Anchors.Count)
            {
                return false;
            }

            return true;
        }

        public float ComputeDistanceInMeters()
        {
            if (m_Anchors is not { Count: > 1 })
                return 0.0f;

            var total = 0.0f;

            for (var i = 0; i < m_Anchors.Count - 1; i++)
            {
                total += (m_Anchors[i].Position - m_Anchors[i + 1].Position).magnitude;
            }

            return total;
        }

        public string GetFormattedDistanceString(MeasureFormat format)
        {
            return MeasureUnit.GetDistanceFormattedString(DistanceInMeters, format);
        }

        public void SetAnchor(int index, Anchor anchor)
        {
            var current = m_Anchors[index];
            m_Anchors[index] = anchor;

            if (!m_IsDirty)
            {
                m_IsDirty = current.Position != anchor.Position;
            }
        }

        public void AddAnchor(Anchor anchor)
        {
            m_Anchors.Add(anchor);
            m_IsDirty = true;
        }

        public void RemoveAnchor(Anchor anchor)
        {
            m_Anchors.Remove(anchor);
            m_IsDirty = true;
        }

        public void RemoveAnchorAt(int index)
        {
            m_Anchors.RemoveAt(index);
            m_IsDirty = true;
        }

        public void RemoveAnchors()
        {
            m_Anchors.Clear();
            m_IsDirty = true;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Id.GetHashCode();
            hash = hash * 23 + Name.GetHashCode();

            if (m_Anchors != null)
            {
                foreach (var anchor in m_Anchors)
                {
                    hash = hash * 23 + anchor.GetHashCode();
                }
            }

            return hash;
        }
    }
}
