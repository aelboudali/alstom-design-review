using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.XR.ARFoundation;
using System;
using UnityEngine.Localization;
using UnityEngine.UIElements;
using Unity.AppUI.UI;

namespace Unity.Industry.Viewer.Streaming.Measurement
{
    public class MeasurementToolSceneListener: MonoBehaviour
    {
        [SerializeField]
        LocalizedString[] m_NavigationOptionsLeadsDisability = Array.Empty<LocalizedString>();
        
        IPressable m_MeasurementToolButton;
        
        [SerializeField] private StreamingToolAsset m_ToolAsset;
        private bool disallowInNavigationMode = false;

        private void Start()
        {
            StreamToolSubmenuController.InitializeTools += OnInitializeTools;
            NavigationController.OnNavigationOptionChanged += OnNavigationOptionChanged;
        }

        private void OnDestroy()
        {
            StreamToolSubmenuController.InitializeTools -= OnInitializeTools;
            NavigationController.OnNavigationOptionChanged -= OnNavigationOptionChanged;
        }

        private void OnInitializeTools(StreamingToolAsset[] obj)
        {
            StartCoroutine(WaitForUICompleted());
            return;

            IEnumerator WaitForUICompleted()
            {
                yield return new WaitForEndOfFrame();
                if (disallowInNavigationMode)
                {
                    DisableUI();
                }
            }
        }

        private void DisableUI()
        {
            var streamToolUIControllers =
                FindObjectsByType<StreamToolsUIControllerBase>(FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            foreach (var streamToolsUIControllerBase in streamToolUIControllers)
            {
                if(streamToolsUIControllerBase.ToolButtons == null) continue;
                if(streamToolsUIControllerBase.ToolButtons.TryGetValue(m_ToolAsset, out m_MeasurementToolButton))
                {
                    var toolButton = m_MeasurementToolButton as VisualElement;
                    toolButton?.SetEnabled(false);
                }
            }
        }

        private void OnNavigationOptionChanged(NavigationOption newNavigation)
        {
            StartCoroutine(WaitForToCheckARSession());

            IEnumerator WaitForToCheckARSession()
            {
                yield return null;
                
                // Check if ARSession is enabled
                ARSession arSession = FindFirstObjectByType<ARSession>(FindObjectsInactive.Exclude);
                if(arSession != null && arSession.enabled)
                {
                    if (m_NavigationOptionsLeadsDisability.Any(x =>
                            x.TableReference.Equals(newNavigation.NavigationName.TableReference) &&
                            x.TableEntryReference.Equals(newNavigation.NavigationName.TableEntryReference)))
                    {
                        disallowInNavigationMode = true;
                        // Disable the measurement tool button when in AR mode and using a navigation option that leads to disability
                        DisableUI();
                    }
                }
                else
                {
                    disallowInNavigationMode = false;
                    if (m_MeasurementToolButton != null)
                    {
                        var toolButton = m_MeasurementToolButton as VisualElement;
                        toolButton?.SetEnabled(true);
                    }
                }
            }
        }
    }
}
