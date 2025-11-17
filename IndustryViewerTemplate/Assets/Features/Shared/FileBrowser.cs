#if UNITY_STANDALONE || UNITY_EDITOR
using SFB;
#endif
using System;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace Unity.Industry.Viewer.Shared
{
    public static class FileBrowser
    {
        public const string SupportedStreamingFileExtensions =
            "pxz,3ds,sat,sab,dwg,dxf,fbx,ipt,iam,nwd,nwc,rvt,rfa,catpart,catproduct,catshape,cgr,3dxml,dae" +
            ",asm,neu,prt,xas,xpr,pvs,pvz,gltf,glb,gds,ifc,igs,iges,obj,plmxml,prc,3dm,rvm,par,pwd,psm" +
            ",sldasm,sldprt,stp,step,stpz,stepz,stpx,stpxz,stl,vda,wrl,vrml";

        public const string DefaultImageFileExtensions = "png,jpg,jpeg,bmp,tiff,tif,gif";

        private static HashSet<string> m_SupportedStreamingFileExtensionsHashSet =
            SupportedStreamingFileExtensions.Split(',').ToHashSet();

        private static HashSet<string> m_DefaultImageFileExtensionsHashSet =
            DefaultImageFileExtensions.Split(',').ToHashSet();

        private static string PrepareFileExtensionForComapision(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return string.Empty;
            }

            return extension.Trim(' ', '*', '.').ToLowerInvariant();
        }

        public static bool IsSupportedStreamingFileExtension(string extension)
        {
            return m_SupportedStreamingFileExtensionsHashSet.Contains(PrepareFileExtensionForComapision(extension));
        }

        public static bool IsDefaultImageFileExtension(string extension)
        {
            return m_DefaultImageFileExtensionsHashSet.Contains(PrepareFileExtensionForComapision(extension));
        }

        
        public static void OpenFile(string title, string defaultFolder, string extension, Action<string> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

#if UNITY_EDITOR
            callback(EditorUtility.OpenFilePanel(title, defaultFolder, extension));
            return;
#endif

#if UNITY_STANDALONE// || UNITY_WEBGL
            ExtensionFilter[] convertedExtensions = null;
            if (!string.IsNullOrEmpty(extension))
            {
                convertedExtensions = new[]
                {
                    new ExtensionFilter("Supported Files", extension.Split(','))
                };
            }

            StandaloneFileBrowser.OpenFilePanelAsync(title, defaultFolder, convertedExtensions, false, (paths) =>
            {
                callback(paths?.Length > 0 ? paths[0] : string.Empty);
            });

            return;
#endif

            
#if UNITY_IOS || UNITY_ANDROID
            if( NativeFilePicker.IsFilePickerBusy() )
                return;
            string[] convertedMobileExtensions = null;
#if UNITY_IOS
            // NOTE: custom file extensions are supported on iOS only.
            // https://github.com/yasirkula/UnityNativeFilePicker?tab=readme-ov-file#unity-native-file-picker-plugin
            if (!string.IsNullOrEmpty(extension))
            {
                convertedMobileExtensions = extension.Split(",")
                    .Select(extension => NativeFilePicker.ConvertExtensionToFileType(extension))
                    .ToArray();
            }
#endif
            NativeFilePicker.PickFile((path) => { callback(path); }, convertedMobileExtensions);
            return;
#endif

            throw new PlatformNotSupportedException("File browser is not supported on this platform.");
        }

        public static void OpenFiles(string title, string defaultFolder, string extension, Action<string[]> callback)
        {
            if(callback == null) throw new ArgumentNullException(nameof(callback));
            
#if UNITY_EDITOR || UNITY_STANDALONE
            ExtensionFilter[] convertedExtensions = null;
            if (!string.IsNullOrEmpty(extension))
            {
                convertedExtensions = new[]
                {
                    new ExtensionFilter("Supported Files", extension.Split(','))
                };
            }
            
            StandaloneFileBrowser.OpenFilePanelAsync(title, defaultFolder, convertedExtensions, true, callback);      
            return;
#endif
            
            
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if( NativeFilePicker.IsFilePickerBusy() )
                return;
            string[] convertedMobileExtensions = null;
#if UNITY_IOS
            // NOTE: custom file extensions are supported on iOS only.
            // https://github.com/yasirkula/UnityNativeFilePicker?tab=readme-ov-file#unity-native-file-picker-plugin
            if (!string.IsNullOrEmpty(extension))
            {
                convertedMobileExtensions = extension.Split(",")
                    .Select(extension => NativeFilePicker.ConvertExtensionToFileType(extension))
                    .ToArray();
            }
#endif
            
            NativeFilePicker.PickMultipleFiles((files) => { callback(files); }, convertedMobileExtensions);
            return;
#endif
            
            throw new PlatformNotSupportedException("File browser is not supported on this platform.");
        }

        /* Summary: Opens a save file dialog.
         * title: The title of the dialog.
         * defaultFolder: The default folder to open.
         * fileNameWithExtension: The default file name with extension.
         * extension: The extension filter (e.g. "png", "jpg", "txt").
         * callback: The callback to invoke with the selected file path or an empty string if cancelled.
         */
#if UNITY_EDITOR ||UNITY_STANDALONE// || UNITY_WEBGL
        public static void SaveFile(string title, string defaultFolder, string fileNameWithExtension, string extension,
            Action<string> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
#if UNITY_EDITOR
            callback(EditorUtility.SaveFilePanel(title, defaultFolder, fileNameWithExtension, extension));
            return;
#endif
            
#if UNITY_STANDALONE// || UNITY_WEBGL
            StandaloneFileBrowser.SaveFilePanelAsync(title, defaultFolder, fileNameWithExtension, extension, callback);
            return;
#endif
        }
#endif

#if UNITY_IOS || UNITY_ANDROID
        public static void ExportFile(string filePath, Action<bool> callback)
        {
            NativeFilePicker.ExportFile(filePath, ExportCallback);
            return;

            void ExportCallback(bool success)
            {
                callback(success);
            }
        }
#endif
    }
}
