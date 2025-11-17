using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Cloud.Assets;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Text;

namespace Unity.Industry.Viewer.Assets
{
    // This script provides functionality for downloading textures in Unity.
    // It includes methods for downloading texture thumbnails from URLs and caching them.
    // The script uses UnityWebRequest for downloading textures asynchronously.
    // It manages a cache of downloaded textures to avoid redundant downloads.
    // The script also handles invoking callbacks once the texture download is complete.
    public static class TextureDownload
    {
        static readonly int k_TimeoutDelay = 10000;

        private class TextureDownloadEntry
        {
            public bool IsDownloading;
            public Texture2D Texture2D;
            public string versionId;
            public string Url;
            public readonly List<Action<Texture2D>> Listeners = new();
        }

        private static Dictionary<int, TextureDownloadEntry> s_TextureCache = new();

        public static void ClearCache()
        {
            s_TextureCache.Clear();
        }

        public static async Task DownloadThumbnail(IAsset asset, Action<Texture2D> actionCallBack)
        {
            var downloadKey = asset.Descriptor.AssetId.GetHashCode();
            var versionId = asset.Descriptor.AssetVersion.ToString();
            Uri url = null;
            bool updateUrl = false;
            if (s_TextureCache.TryGetValue(downloadKey, out var entry))
            {
                if (entry.versionId != versionId)
                {
                    url = await asset.GetPreviewUrlAsync(CancellationToken.None);
                    updateUrl = true;
                }
                else
                {
                    url = new Uri(entry.Url);
                }
            }
            else
            {
                url = await asset.GetPreviewUrlAsync(CancellationToken.None);
                
                if (url == null)
                {
                    actionCallBack?.Invoke(null);
                    return;
                }
            }

            if (updateUrl)
            {
                s_TextureCache.Remove(downloadKey);
            }

            _ = Download(downloadKey, versionId, url.ToString(), actionCallBack);
        }

        static async Task Download(int key, string versionId, string url, Action<Texture2D> actionCallBack)
        {
            if(!s_TextureCache.TryGetValue(key, out var entry))
            {
                entry = new TextureDownloadEntry
                {
                    IsDownloading = true,
                    Url = url,
                    versionId = versionId
                };
                
                lock (entry.Listeners)
                {
                    entry.Listeners.Add(actionCallBack);
                }
                
                s_TextureCache.Add(key, entry);

                try
                {
                    var taskTexture = DownloadTexture(url.ToString());
                    if (await Task.WhenAny(taskTexture) == taskTexture)
                    {
                        entry.Texture2D = taskTexture.Result;
                        entry.IsDownloading = false;
                    }
                    else
                    {
                        //Get Preset
                        entry.IsDownloading = false;
                        actionCallBack?.Invoke(null);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    OnTextureDownloaded(entry);
                    actionCallBack?.Invoke(null);
                }
                
            } else if (entry.IsDownloading)
            {
                lock (entry.Listeners)
                {
                    entry.Listeners.Add(actionCallBack);
                }
                return;
            }
            OnTextureDownloaded(entry);
            actionCallBack?.Invoke(entry.Texture2D);
        }
        
        static async Task<Texture2D> DownloadTexture(string url)
        {
            using var uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            uwr.downloadHandler = new DownloadHandlerTexture();

            var operation = uwr.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }
            return DownloadHandlerTexture.GetContent(uwr);
        }
        
        static void OnTextureDownloaded(TextureDownloadEntry entry)
        {
            entry.IsDownloading = false;

            lock (entry.Listeners)
            {
                foreach (var listener in entry.Listeners)
                {
                    listener?.Invoke(entry.Texture2D);
                }
            }
        }

        public static Task DownloadThumbnail(int assetId, string versionId, string url, Action<Texture2D> actionCallBack)
        {
            var key = assetId;
            var sb = new StringBuilder(url);
            #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_IOS
            if (!url.StartsWith("file://"))
            {
                sb.Insert(0, "file://");
            }
            #else
            if (!url.StartsWith("file:///"))
            {
                sb.Insert(0, "file:///");
            }
            #endif
            url = sb.ToString();
            
            _ = Download(key, versionId, url, actionCallBack);
            return Task.CompletedTask;
        }
    }
}
