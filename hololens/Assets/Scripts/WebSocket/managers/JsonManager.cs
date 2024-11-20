using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Manager.Json
// stworzyc pomocnia klase Logging(nazwa, content) -> JsonManager - {message}
{
    public static class JsonManager
    {
        private static readonly string DataPath = Path.Combine(Application.dataPath, "Scripts", "data");

        // Load JSON and deserilize its content into a dictionary
        public static Dictionary<string, object> LoadFromFile(string fileName)
        {
            string filePath = Path.Combine(Application.dataPath, "Scripts", "data", fileName + ".json");

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.Log($"File not found at path: {filePath}");
                    return null;
                }

                string jsonContent = File.ReadAllText(filePath);
                return Deserialize<Dictionary<string, object>>(jsonContent);
            }
            catch (Exception e)
            {
                Debug.LogError($"JsonManager - Failed to load JSON file: {e.Message}");
                return null;
            }
        }

        public static string Serialize<T>(T data)
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

        public static T Deserialize<T>(string jsonData)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"JsonManager - Failed to serialize object. Exception: {ex.Message}");
                return default;
            }
        }

        private static string GetFilePath(string fileName) => Path.Combine(DataPath, $"{fileName}.json");
    }

}
