using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AppUI.UI;
using Unity.Cloud.Common;
using Unity.Industry.Viewer.Assets;
using RuntimeGizmos;
using Unity.Industry.Viewer.Shared;
using UnityEngine.Localization;

namespace Unity.Industry.Viewer.Streaming.Interaction
{
    public class InteractionToolUIController : StreamToolUIBase
    {
        private const string k_InteractButtonName = "InteractButton";
        private const string k_InteractableItemListName = "InteractableItemList";
        private const string k_TransformActionGroupName = "TransformActionGroup";
        private const string k_TransformContainerName = "TransformContainer";
        private const string k_MoveButton = "MoveButton";
        private const string k_RotateButton = "RotateButton";
        private const string k_ScaleButton = "ScaleButton";
        private const string k_GlobalLocalActionGroupName = "GlobalLocalActionGroup";
        private const string k_GlobalButton = "GlobalButton";
        private const string k_LocalButton = "LocalButton";
        private const string k_ResetButton = "ResetTransformButton";

        private VisualElement m_TransformContainer;
        private ActionGroup m_TransformActionGroup;
        private ActionButton m_MoveButton;
        private ActionButton m_RotateButton;
        private ActionButton m_ScaleButton;

        private ActionGroup m_GlobalLocalActionGroup;
        private ActionButton m_GlobalButton;
        private ActionButton m_LocalButton;

        private ActionButton m_ResetButton;

        private ActionButton m_InteractButton;
        private ListView m_interactableItemList;
        private InteractionToolController m_InteractionToolController;
        
        private Text m_OfflineWarningText;
        
        [SerializeField]
        private LocalizedString m_offlineWarningLocalizedString;
        
        private Text m_offlineModelWarningText;
        
        [SerializeField]
        private LocalizedString m_offlineModelLocalizedString;

        private void Start()
        {
            InteractionToolController.InstanceSelected += OnInstanceSelected;
            InteractionToolController.GeoGameObjectCreated += OnGeoGameObjectCreated;
            InteractionToolController.OfflineAssetSelected += OfflineAssetSelected;
            NetworkDetector.OnNetworkStatusChanged += OnNetworkStatusChanged;
        }

        private void OnDestroy()
        {
            InteractionToolController.InstanceSelected -= OnInstanceSelected;
            InteractionToolController.GeoGameObjectCreated -= OnGeoGameObjectCreated;
            InteractionToolController.OfflineAssetSelected -= OfflineAssetSelected;
            NetworkDetector.OnNetworkStatusChanged -= OnNetworkStatusChanged;
        }
        
        private void OfflineAssetSelected()
        {
            m_InteractButton.SetEnabled(false);
            if (m_offlineModelWarningText == null)
            {
                ShowOfflineModelWarning();
            }
        }

        private async void ShowOfflineModelWarning()
        {
            m_offlineModelWarningText = new Text
            {
                text = await m_offlineModelLocalizedString.GetTitleLocalizedStringForAppUIAsync(),
                style =
                {
                    paddingBottom = new Length(15f, LengthUnit.Pixel),
                }
            };
            var indexOfButton = m_InteractButton.parent.IndexOf(m_InteractButton);
            m_InteractButton.parent.Insert(indexOfButton, m_offlineModelWarningText);
        }

        private void OnNetworkStatusChanged(bool isConnected)
        {
            if (!isConnected)
            {
                m_InteractButton.SetEnabled(false);
                if (m_OfflineWarningText == null)
                {
                    OfflineWarning();
                }
            }
            else
            {
                m_OfflineWarningText?.RemoveFromHierarchy();
            }
        }

        private void OnGeoGameObjectCreated(GeoGameObjectDetails? objectDetails)
        {
            LoadingUIPanel.HideLoadingPanel.Invoke(null);
            if (!objectDetails.HasValue)
            {
                return;
            }

            var currentSource = m_interactableItemList.itemsSource as List<GeoGameObjectDetails>;
            if (currentSource == null)
            {
                currentSource = new List<GeoGameObjectDetails> { objectDetails.Value };
            }
            else
            {
                currentSource.Add(objectDetails.Value);
            }

            m_interactableItemList.itemsSource = currentSource;
            m_interactableItemList.Rebuild();
            StartCoroutine(WaitForUIUpdate());
            m_InteractButton?.SetEnabled(false);
            return;

            IEnumerator WaitForUIUpdate()
            {
                yield return new WaitForEndOfFrame();
                m_interactableItemList.selectedIndex = currentSource.Count - 1;
                m_interactableItemList.Rebuild();
            }
        }

        private void OnInstanceSelected(InstanceId? instanceId)
        {
            m_offlineModelWarningText?.RemoveFromHierarchy();
            m_InteractButton?.SetEnabled(instanceId.HasValue && instanceId.Value != InstanceId.None);
            m_TransformContainer?.SetEnabled(false);
            m_interactableItemList.ClearSelection();
        }

        public override void InitializeUI(UIDocument uiDocument, VisualElement parent, GameObject controller)
        {
            m_PanelDocument = uiDocument;
            m_InteractButton = parent.Q<ActionButton>(k_InteractButtonName);
            m_InteractButton.clicked += OnInteractButtonClicked;
            m_interactableItemList = parent.Q<ListView>(k_InteractableItemListName);
            m_interactableItemList.bindItem = InteractableItemListBindItem;
            m_interactableItemList.selectedIndicesChanged += OnInteractableItemListIndicesChanged;
            if (controller.TryGetComponent(out m_Controller))
            {
                //m_Controller.ToolOpened += OnToolOpened;
                //m_Controller.ToolClosed += OnToolClosed;
            }

            m_InteractionToolController = controller.GetComponent<InteractionToolController>();
            m_InteractButton.SetEnabled(false);

            m_TransformContainer = parent.Q<VisualElement>(k_TransformContainerName);

            m_TransformActionGroup = parent.Q<ActionGroup>(k_TransformActionGroupName);
            m_TransformActionGroup.SetSelectionWithoutNotify(new[] { 0 });
            m_MoveButton = parent.Q<ActionButton>(k_MoveButton);
            m_MoveButton.clicked += SetToMoveMode;

            m_RotateButton = parent.Q<ActionButton>(k_RotateButton);
            m_RotateButton.clicked += SetToRotateMode;

            m_ScaleButton = parent.Q<ActionButton>(k_ScaleButton);
            m_ScaleButton.clicked += SetScaleMode;

            m_GlobalLocalActionGroup = parent.Q<ActionGroup>(k_GlobalLocalActionGroupName);
            m_GlobalLocalActionGroup.SetSelectionWithoutNotify(new[] { 0 });
            m_GlobalButton = parent.Q<ActionButton>(k_GlobalButton);
            m_GlobalButton.clicked += SetToGlobalSpace;
            m_LocalButton = parent.Q<ActionButton>(k_LocalButton);
            m_LocalButton.clicked += SetToLocalSpace;

            m_ResetButton = parent.Q<ActionButton>(k_ResetButton);
            m_ResetButton.clicked += OnResetButtonClicked;

            m_TransformContainer?.SetEnabled(false);

            if (NetworkDetector.IsOffline)
            {
                OfflineWarning();
            }
        }

        private async void OfflineWarning()
        {
            m_OfflineWarningText = new Text
            {
                text = await m_offlineWarningLocalizedString.GetTitleLocalizedStringForAppUIAsync(),
                style =
                {
                    paddingBottom = new Length(15f, LengthUnit.Pixel),
                }
            };
            var indexOfButton = m_InteractButton.parent.IndexOf(m_InteractButton);
            m_InteractButton.parent.Insert(indexOfButton, m_OfflineWarningText);
        }

        private void OnResetButtonClicked()
        {
            m_InteractionToolController?.ResetTransform();
        }

        private void SetToGlobalSpace()
        {
            m_GlobalLocalActionGroup.SetSelectionWithoutNotify(new[] { 0 });
            m_InteractionToolController.SetTransformSpace(TransformSpace.Global);
        }

        private void SetToLocalSpace()
        {
            m_GlobalLocalActionGroup.SetSelectionWithoutNotify(new[] { 1 });
            m_InteractionToolController.SetTransformSpace(TransformSpace.Local);
        }

        //Commenting out the scale mode for now
        private void SetScaleMode()
        {
            m_TransformActionGroup.SetSelectionWithoutNotify(new[] { 2 });
            m_InteractionToolController.SetGizmoMode(TransformType.Scale);
        }

        private void SetToRotateMode()
        {
            m_TransformActionGroup.SetSelectionWithoutNotify(new[] { 1 });
            m_InteractionToolController.SetGizmoMode(TransformType.Rotate);
        }

        private void SetToMoveMode()
        {
            m_TransformActionGroup.SetSelectionWithoutNotify(new[] { 0 });
            m_InteractionToolController.SetGizmoMode(TransformType.Move);
        }

        private void OnInteractableItemListIndicesChanged(IEnumerable<int> obj)
        {
            m_InteractButton?.SetEnabled(false);
            if (obj == null || !obj.Any())
            {
                m_TransformContainer?.SetEnabled(false);
                return;
            }

            m_TransformContainer?.SetEnabled(true);
            var selectedIndex = obj.First();
            var currentSource = m_interactableItemList.itemsSource as List<GeoGameObjectDetails>;
            if (currentSource == null || selectedIndex < 0 || selectedIndex >= currentSource.Count) return;
            var selectedObjectDetails = currentSource[selectedIndex];
            TransformType selectedTransformType;
            
            switch (m_TransformActionGroup.selectedIndices.First())
            {
                case 0:
                    selectedTransformType = TransformType.Move;
                    break;
                case 1:
                    selectedTransformType = TransformType.Rotate;
                    break;
                case 2:
                    selectedTransformType = TransformType.Scale;
                    break;
                default:
                    selectedTransformType = TransformType.Move;
                    break;
            }

            TransformSpace selectedTransformSpace;

            switch (m_GlobalLocalActionGroup.selectedIndices.First())
            {
                case 0:
                    selectedTransformSpace = TransformSpace.Global;
                    break;
                case 1:
                    selectedTransformSpace = TransformSpace.Local;
                    break;
                default:
                    selectedTransformSpace = TransformSpace.Global;
                    break;
            }
            m_InteractionToolController.OnItemSelected(selectedObjectDetails.InSceneGameObject.transform,
                selectedTransformType, selectedTransformSpace);
        }

        private void InteractableItemListBindItem(VisualElement element, int index)
        {
            if (m_interactableItemList.itemsSource is not List<GeoGameObjectDetails> currentSource) return;
            var item = currentSource[index];
            var text = element.Q<Text>();
            text.text = item.InSceneGameObject.name;

            var iconButton = element.Q<IconButton>();
            if (iconButton != null)
            {
                iconButton.userData = item;
                iconButton.RegisterCallback<ClickEvent>(OnBinButtonClicked);
            }
        }

        private void OnBinButtonClicked(ClickEvent evt)
        {
            var b = (GeoGameObjectDetails)(evt.currentTarget as IconButton).userData;
            m_InteractionToolController.RemoveGeoGameObject(b, out bool isCurrentSelected);
            if (isCurrentSelected)
            {
                m_TransformContainer?.SetEnabled(false);
            }

            var currentSource = m_interactableItemList.itemsSource as List<GeoGameObjectDetails>;
            if (currentSource == null) return;
            currentSource.Remove(b);
            if (currentSource.Count == 0)
            {
                m_TransformContainer?.SetEnabled(false);
            }

            m_interactableItemList.itemsSource = currentSource.Count == 0 ? null : currentSource;
            m_interactableItemList.Rebuild();
        }

        private void OnInteractButtonClicked()
        {
            LoadingUIPanel.ShowLoadingPanel.Invoke(null);
            m_InteractButton.SetEnabled(false);
            m_TransformContainer?.SetEnabled(false);
            m_InteractionToolController?.MakeItInteractable();
        }

        public override void UninitializeUI()
        {
            m_InteractButton.clicked -= OnInteractButtonClicked;
            m_interactableItemList.bindItem -= InteractableItemListBindItem;
            m_MoveButton.clicked -= SetToMoveMode;
            m_RotateButton.clicked -= SetToRotateMode;
            m_ScaleButton.clicked -= SetScaleMode;

            m_GlobalButton.clicked -= SetToGlobalSpace;
            m_LocalButton.clicked -= SetToLocalSpace;
            m_ResetButton.clicked -= OnResetButtonClicked;
        }
    }
}