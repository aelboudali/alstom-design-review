using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.XR.ARCore;
//Enable this if want to use Oculus Loader
//using Unity.XR.Oculus;
using UnityEngine.XR.OpenXR;

namespace Unity.Industry.Viewer.VR.Editor
{
    public class BuildAutomation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform is not BuildTarget.Android) return;
            
#if VR_MODE
            if (LoaderControl.IsLoaderEnabled(BuildTarget.Android, typeof(ARCoreLoader)))
            {
                LoaderControl.DisableLoader(BuildTarget.Android, typeof(ARCoreLoader));
            }
                    
            //Enable this if want to use Oculus Loader
            /*if (!LoaderControl.IsLoaderEnabled(BuildTarget.Android, typeof(OculusLoader)))
            {
                LoaderControl.EnableLoader(BuildTarget.Android, typeof(OculusLoader));
            }*/
            
            if (!LoaderControl.IsLoaderEnabled(BuildTarget.Android, typeof(OpenXRLoader)))
            {
                LoaderControl.EnableLoader(BuildTarget.Android, typeof(OpenXRLoader));
            }
            
            
#else
            if (!LoaderControl.IsLoaderEnabled(BuildTarget.Android, typeof(ARCoreLoader)))
            {
                LoaderControl.EnableLoader(BuildTarget.Android, typeof(ARCoreLoader));
            }
        
            //Enable this if want to use Oculus Loader
            /*if (LoaderControl.IsLoaderEnabled(BuildTarget.Android, typeof(OculusLoader)))
            {
                LoaderControl.DisableLoader(BuildTarget.Android, typeof(OculusLoader));
            }*/
            
            if (LoaderControl.IsLoaderEnabled(BuildTarget.Android, typeof(OpenXRLoader)))
            {
                LoaderControl.DisableLoader(BuildTarget.Android, typeof(OpenXRLoader));
            }
#endif
        }
    }
}
