using UnityEngine;
using System.Globalization;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.Industry.Viewer.Streaming.Measurement
{
    public static class MeasureUnit
    {
        public static string GetDistanceFormattedString(float distanceInMeters, MeasureFormat format)
        {
            return format switch
            {
                MeasureFormat.Meters => $"{distanceInMeters:F2} m",
                MeasureFormat.Centimeters => $"{distanceInMeters * 100f:F2} cm",
                MeasureFormat.Feet => $"{distanceInMeters * 3.28084f:F2} ft",
                MeasureFormat.Inches => $"{distanceInMeters * 39.3701f:F1} in",
                MeasureFormat.FeetAndInches => FormatFeetAndInches(), // Simple call to the local function
                _ => $"{distanceInMeters:F2} m"
            };
            
            string FormatFeetAndInches()
            {
                float totalInches = distanceInMeters * 39.3701f;
                int feet = (int)Math.Floor(totalInches / 12f);
                float inches = (float)Math.Round(totalInches - feet * 12f, 1);
                if (inches >= 12f)
                {
                    feet++;
                    inches = 0f;
                }
                return $"{feet} ft {inches:F1} in";
            }
        }

        public static MeasureFormat GetSystemUnit()
        {
            var region = new RegionInfo(CultureInfo.CurrentCulture.Name);
            return region.IsMetric ? MeasureFormat.Meters : MeasureFormat.Feet;
        }
        
        /// <summary>
        /// Deletes the directory containing the given file path, and then its parent directories one by one,
        /// but only if each is empty. Stops at the first non-empty directory or root.
        /// </summary>
        /// <param name="filePath">The file path whose directory tree to clean up.</param>
        public static void DeleteFileAndEmptyParents(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            string dir = Path.GetDirectoryName(filePath);
            while (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir);
                var dirs = Directory.GetDirectories(dir);

                // Check for any non-hidden, non-system, non-dot files
                bool hasVisibleFiles = false;
                foreach (var file in files)
                {
                    var name = Path.GetFileName(file);
                    var attrs = File.GetAttributes(file);
                    if (!name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase) &&
                        (attrs & FileAttributes.Hidden) != FileAttributes.Hidden &&
                        (attrs & FileAttributes.System) != FileAttributes.System &&
                        !name.StartsWith("."))
                    {
                        hasVisibleFiles = true;
                        break;
                    }
                }

                if (dirs.Length > 0 || hasVisibleFiles)
                    break;

                // Delete all hidden/system/dot files before deleting the directory
                foreach (var file in files)
                {
                    var name = Path.GetFileName(file);
                    var attrs = File.GetAttributes(file);
                    if (name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase) ||
                        (attrs & FileAttributes.Hidden) == FileAttributes.Hidden ||
                        (attrs & FileAttributes.System) == FileAttributes.System ||
                        name.StartsWith("."))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }

                Directory.Delete(dir);
                dir = Path.GetDirectoryName(dir);
            }
        }
        
        public static async void DeleteMeasurement(MeasureLineData measureLineData, Action<bool, List<MeasureLineData>> callback)
        {
            try
            {
                var collection = await ReadCollections();
                if (collection == null || !collection.Contains(measureLineData))
                {
                    callback?.Invoke(false, null);
                    return;
                }
                collection.Remove(measureLineData);

                if (collection.Count == 0)
                {
                    File.Delete(MeasurementToolController.StorageUri);
                    DeleteFileAndEmptyParents(MeasurementToolController.StorageUri);
                }
                else
                {
                    var json = JsonConvert.SerializeObject(collection, Formatting.Indented);
                    await File.WriteAllTextAsync(MeasurementToolController.StorageUri, json);
                }
                callback?.Invoke(true, collection);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                callback?.Invoke(false, null);
            }
        }
        
        public static async Task<List<MeasureLineData>> ReadCollections()
        {
            if (!File.Exists(MeasurementToolController.StorageUri)) return null;
            //Read existing lines
            List<MeasureLineData> collections = null;
            var stringData = await File.ReadAllTextAsync(MeasurementToolController.StorageUri);
            collections = JsonConvert.DeserializeObject<List<MeasureLineData>>(stringData);
            return collections;
        }
    }
}
