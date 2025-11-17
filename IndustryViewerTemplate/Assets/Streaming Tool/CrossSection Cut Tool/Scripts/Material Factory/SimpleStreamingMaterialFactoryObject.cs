using UnityEngine;
using System;
using Unity.Cloud.DataStreaming.Runtime;

namespace Unity.Industry.Viewer.Streaming.CrossSection
{
    [Serializable]
    public struct MaterialObject
    {
        public Material Material;
        public MaterialLighting Lighting;
        public MaterialAlphaMode AlphaMode;
    }

    [CreateAssetMenu(fileName = "SimpleStreamingMaterialFactoryObject", menuName = "IVT/Streaming/Simple Streaming Material Factory Object")]
    public class SimpleStreamingMaterialFactoryObject : StreamingMaterialFactoryObject
    {
        public MaterialObject[] Materials;
        
        public override StreamingMaterialFactory Instantiate()
        {
            var crossSectionMaterialFactory = new CrossSectionMaterialFactory();
            crossSectionMaterialFactory.Initialize(Materials);
            return crossSectionMaterialFactory;
        }
    }
}