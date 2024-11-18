using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Manager.Json
{
    public static class JsonManager
    {
        public static Dictionary<string, object> LoadFromFile(string fileName)
        {
            string filePath = Path.Combine(Application.dataPath, "Scripts", "data", fileName + ".json");

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"JsonManager - File not found at path: {filePath}");
                    return null;
                }

                string jsonContent = File.ReadAllText(filePath);

                // Parse the JSON content into a dictionary
                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
                if (result == null)
                {
                    Debug.LogError("JsonManager - Failed to parse JSON content.");
                }


                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"JsonManager - Failed to load JSON file: {e.Message}");
                return null;
            }
        }

        public static T FromJson<T>(string jsonData)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"JsonManager - Failed to serialize object. Exception: {ex.Message}");
                return default(T);
            }
        }

        // Convert an object to a JSON string.
        public static string ToJson<T>(T data)
        {
            try
            {
                return JsonConvert.SerializeObject(data, Formatting.None);
            }
            catch (Exception ex)
            {
                Debug.LogError($"JsonManager - Failed to serialize object. Exception: {ex.Message}");
                return null;
            }
        }


    }
}
