using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.AppUI.UI;

namespace Unity.Industry.Viewer.Streaming
{
    public class StreamToolsUIControllerBase : MonoBehaviour
    {
        public class StreamToolData
        {
            private StreamingToolAsset toolAsset { get; set; }

            public StreamToolData(StreamingToolAsset toolAsset)
            {
                this.toolAsset = toolAsset;
            }

            public void OnButtonPress()
            {
                StreamToolsController.ToolSelected?.Invoke(toolAsset);
            }
        }
        
        public static Action<StreamingToolAsset, GameObject, bool> UpdateToolPanel;
        
        public Dictionary<StreamingToolAsset, IPressable> ToolButtons => m_toolButtons;
        
        protected Dictionary<StreamingToolAsset, IPressable> m_toolButtons;
    }
}
