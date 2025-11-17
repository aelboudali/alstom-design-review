using UnityEngine;
using Unity.Cloud.DataStreaming.Runtime;

namespace Unity.Industry.Viewer.Streaming
{
    public abstract class StreamingMaterialFactoryObject : ScriptableObject
    {
        /// <summary>
        /// Create a new <see cref="StreamingMaterialFactory"/> based on this instance values.
        /// </summary>
        /// <returns>The new <see cref="StreamingMaterialFactory"/> instance.</returns>
        public abstract StreamingMaterialFactory Instantiate();
    }
}
