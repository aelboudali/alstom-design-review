using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
//Enable this if want to use Oculus Loader
//using Unity.XR.Oculus;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;

namespace Unity.Industry.Viewer.VR.Editor
{
    public static class LoaderControl
    {
        static void GetXRManagerSettings(BuildTarget buildTarget, out BuildTargetGroup buildTargetGroup, out XRManagerSettings manager)
        {
            buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (xrSettings == null)
            {
                manager = null;
                return;
            }

            manager = xrSettings.Manager;
        }

        public static bool IsLoaderEnabled(BuildTarget buildTarget, Type loaderType)
        {
            GetXRManagerSettings(buildTarget, out _, out var manager);
            if (manager == null)
                return false;

            var activeLoaders = manager.activeLoaders;
            return activeLoaders != null && activeLoaders.Any(x => x.GetType() == loaderType);
        }

        public static void EnableLoader(BuildTarget buildTarget, Type loaderType)
        {
            GetXRManagerSettings(buildTarget, out var buildTargetGroup, out var manager);
            if(manager == null) return;

            string loaderName = string.Empty;
            
            switch (loaderType)
            {
                case not null when loaderType == typeof(ARCoreLoader):
                    // Add any specific logic for ARCoreLoader here
                    loaderName = typeof(ARCoreLoader).FullName;
                    break;
                
                //Enable this if want to use Oculus Loader
                /*case not null when loaderType == typeof(OculusLoader):
                    loaderName = k_OculusLoader;
                    break;*/
                
                case not null when loaderType == typeof(OpenXRLoader):
                    loaderName = typeof(OpenXRLoader).FullName;
                    break;
            }
            
            if (string.IsNullOrEmpty(loaderName))
                return;

            if (!XRPackageMetadataStore.AssignLoader(manager, loaderName, buildTargetGroup)) return;
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();
        }

        public static void EnableFeatureGroup(BuildTargetGroup buildTarget, string id)
        {
            var featureSets = OpenXRFeatureSetManager.FeatureSetsForBuildTarget(buildTarget);
            foreach (var featureSet in featureSets.Where(featureSet => featureSet.featureSetId == id).Where(featureSet => featureSet.isInstalled))
            {
                featureSet.isEnabled = true;
            }
        }

        public static void DisableFeatureGroup(BuildTargetGroup buildTarget, string id)
        {
            var featureSets = OpenXRFeatureSetManager.FeatureSetsForBuildTarget(buildTarget);
            foreach (var featureSet in featureSets.Where(featureSet => featureSet.featureSetId == id).Where(featureSet => featureSet.isInstalled))
            {
                featureSet.isEnabled = false;
            }
        }
        
        public static void DisableLoader(BuildTarget buildTarget, Type loaderType)
        {
            GetXRManagerSettings(buildTarget, out var buildTargetGroup, out var manager);
            if(manager == null) return;

            string loaderName = string.Empty;
            
            switch (loaderType)
            {
                case not null when loaderType == typeof(ARCoreLoader):
                    // Add any specific logic for ARCoreLoader here
                    loaderName = typeof(ARCoreLoader).FullName;
                    break;
                
                //Enable this if want to use Oculus Loader
                /*case not null when loaderType == typeof(OculusLoader):
                    loaderName = k_OculusLoader;
                    break;*/
                
                case not null when loaderType == typeof(OpenXRLoader):
                    loaderName = typeof(OpenXRLoader).FullName;
                    break;
            }
            
            if (string.IsNullOrEmpty(loaderName))
                return;
            
            if(!XRPackageMetadataStore.RemoveLoader(manager, loaderName, buildTargetGroup))
                Debug.LogError("Failed to remove loader from XR Manager Settings");
        }
    }
}
