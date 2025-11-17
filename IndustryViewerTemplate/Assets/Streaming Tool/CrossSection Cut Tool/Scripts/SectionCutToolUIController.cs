using System.Linq;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Toggle = Unity.AppUI.UI.Toggle;
using RuntimeGizmos;

namespace Unity.Industry.Viewer.Streaming.CrossSection
{
    public class SectionCutToolUIController : StreamToolUIBase
    {
        private const string k_MoveButton = "MoveButton";
        private const string k_RotateButton = "RotateButton";
        private const string k_ScaleButton = "ScaleButton";
        private const string k_FlipToggle = "FlipToggle";
        private const string k_ResetButton = "ResetButton";
        private const string k_GizmoToggle = "GizmoToggle";
        private const string k_SectionBoxToggle = "SectionBoxToggle";
        
        private ActionGroup m_ActionGroup;
        private ActionButton m_MoveButton;
        private ActionButton m_RotateButton;
        private ActionButton m_ScaleButton;
        private ActionButton m_ResetButton;
        private Toggle m_FlipToggle;
        private Toggle m_GizmoToggle;
        private Toggle m_SectionBoxToggle;
        
        private SectionCutToolController m_SectionCutToolController;
        
        public override void InitializeUI(UIDocument uiDocument, VisualElement parent, GameObject controller)
        {
            m_SectionCutToolController = controller.GetComponent<SectionCutToolController>();

            if (m_SectionCutToolController == null)
            {
                parent.Clear();
                return;
            }
                
            m_ActionGroup = parent.Q<ActionGroup>();
            m_ActionGroup.SetSelectionWithoutNotify(new []{0});
            m_MoveButton = parent.Q<ActionButton>(k_MoveButton);
            m_MoveButton.clicked += SetToMoveMode;
            
            m_RotateButton = parent.Q<ActionButton>(k_RotateButton);
            m_RotateButton.clicked += SetToRotateMode;
            
            m_ScaleButton = parent.Q<ActionButton>(k_ScaleButton);
            m_ScaleButton.clicked += SetScaleMode;
            
            m_ResetButton = parent.Q<ActionButton>(k_ResetButton);
            m_ResetButton.clicked += ResetSectionCut;
            
            m_FlipToggle = parent.Q<Toggle>(k_FlipToggle);
            m_FlipToggle.RegisterValueChangedCallback(OnFlipToggleChanged);
            m_FlipToggle.SetValueWithoutNotify(false);
            
            m_GizmoToggle = parent.Q<Toggle>(k_GizmoToggle);
            m_GizmoToggle.RegisterValueChangedCallback(OnGizmoToggleChanged);
            m_GizmoToggle.SetValueWithoutNotify(true);
            
            m_SectionBoxToggle = parent.Q<Toggle>(k_SectionBoxToggle);
            m_SectionBoxToggle.RegisterValueChangedCallback(OnSectionBoxToggleChanged);
            m_SectionBoxToggle.SetValueWithoutNotify(true);
        }

        private void OnSectionBoxToggleChanged(ChangeEvent<bool> evt)
        {
            m_SectionCutToolController?.ShowSectionBox(evt.newValue);
        }

        private void OnGizmoToggleChanged(ChangeEvent<bool> evt)
        {
            m_SectionCutToolController?.ShowGizmo(evt.newValue, m_ActionGroup.selectedIds.First());
        }

        private void OnFlipToggleChanged(ChangeEvent<bool> evt)
        {
            Shader.SetGlobalFloat(SectionCutToolController.FlippedShaderId, evt.newValue ? 1 : 0);
        }

        private void ResetSectionCut()
        {
            m_FlipToggle.SetValueWithoutNotify(false);
            m_GizmoToggle.SetValueWithoutNotify(true);
            m_SectionBoxToggle.SetValueWithoutNotify(true);
            m_SectionCutToolController.ResetSectionCut((TransformType)m_ActionGroup.selectedIds.First());
        }

        private void SetScaleMode()
        {
            m_ActionGroup.SetSelectionWithoutNotify(new []{2});
            m_SectionCutToolController.SetGizmoMode(TransformType.Scale);
        }

        private void SetToRotateMode()
        {
            m_ActionGroup.SetSelectionWithoutNotify(new []{1});
            m_SectionCutToolController.SetGizmoMode(TransformType.Rotate);
        }

        private void SetToMoveMode()
        {
            m_ActionGroup.SetSelectionWithoutNotify(new []{0});
            m_SectionCutToolController.SetGizmoMode(TransformType.Move);
        }

        public override void UninitializeUI()
        {
            
        }
        
        
    }
}
