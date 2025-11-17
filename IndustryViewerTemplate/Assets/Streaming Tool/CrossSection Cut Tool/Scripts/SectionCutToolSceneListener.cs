using UnityEngine;
using Unity.AppUI.UI;
using System.Collections;
#if !VR_MODE
using Unity.Industry.Viewer.Navigation.MobileAR;
using ARState = Unity.Industry.Viewer.Navigation.MobileAR.ARState;
#else
using Unity.Industry.Viewer.VR.CameraPassThrough;
using ARState = Unity.Industry.Viewer.VR.CameraPassThrough.ARState;
#endif
using UnityEngine.UIElements;

namespace Unity.Industry.Viewer.Streaming.CrossSection
{
    public class SectionCutToolSceneListener : MonoBehaviour
    {
        [SerializeField] private StreamingToolAsset m_ToolAsset;
        
        IPressable m_SectionCutToolButton;
        
        private void Awake()
        {
            Shader.SetGlobalFloat(SectionCutToolController.EnableClippingShaderId, 0f);
            StreamToolSubmenuController.InitializeTools += OnInitializeTools;
        }
        
        private void OnDestroy()
        {
            StreamToolSubmenuController.InitializeTools -= OnInitializeTools;
        }

        private void OnInitializeTools(StreamingToolAsset[] obj)
        {
            StartCoroutine(WaitForUICompleted());
            return;
            
            IEnumerator WaitForUICompleted()
            {
                yield return new WaitForEndOfFrame();
                bool disableInCurrentState = false;
#if !VR_MODE
                var mobileAR = FindFirstObjectByType<MobileARController>(FindObjectsInactive.Exclude);
                if (mobileAR != null)
                {
                    disableInCurrentState = mobileAR.CurrentARState != ARState.ConfirmPosition;
                    if (disableInCurrentState)
                    {
                        MobileARController.ARStateChange += ARStateChange;

                        void ARStateChange(ARState arState)
                        {
                            if (arState != ARState.ConfirmPosition) return;
                            MobileARController.ARStateChange -= ARStateChange;
                            ButtonStatus(true);
                        }
                    }
                }
#else
                var passthroughAR = FindFirstObjectByType<CameraPassThroughController>(FindObjectsInactive.Exclude);
                if (passthroughAR != null)
                {
                    disableInCurrentState = passthroughAR.State != ARState.ConfirmPosition;
                    if (disableInCurrentState)
                    {
                        CameraPassThroughController.OnStateChange += ARStateChange;

                        void ARStateChange(ARState arState)
                        {
                            if (arState != ARState.ConfirmPosition) return;
                            CameraPassThroughController.OnStateChange -= ARStateChange;
                            ButtonStatus(true);
                        }
                    }
                }
#endif

                if (disableInCurrentState)
                {
                    ButtonStatus(false);
                }
            }
        }

        private void ButtonStatus(bool state)
        {
            var streamToolUIControllers =
                FindObjectsByType<StreamToolsUIControllerBase>(FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            foreach (var streamToolsUIControllerBase in streamToolUIControllers)
            {
                if(streamToolsUIControllerBase.ToolButtons == null) continue;
                if(streamToolsUIControllerBase.ToolButtons.TryGetValue(m_ToolAsset, out m_SectionCutToolButton))
                {
                    var toolButton = m_SectionCutToolButton as VisualElement;
                    toolButton?.SetEnabled(state);
                }
            }
        }
    }
}
