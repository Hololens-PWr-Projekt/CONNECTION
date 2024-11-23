using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Manager.Json
{

    public static class JsonManager
    {
        private static readonly string DataPath = Path.Combine(Application.dataPath, "Scripts", "data");

        public static void SetSerializerSettings()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

        }

        // Load JSON and deserialize its content into a dictionary
        public static Dictionary<string, object> LoadFromFile(string fileName)
        {
            string filePath = Path.Combine(DataPath, fileName + ".json");

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"JsonManager - File not found at path: {filePath}");
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

        // Serialize data into JSON format, keeping original property names
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

        // Deserialize JSON string into an object, respecting original property names
        public static T Deserialize<T>(string jsonData)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"JsonManager - Failed to deserialize object. Exception: {ex.Message}");
                return default;
            }
        }

        // Helper function to get the file path
        private static string GetFilePath(string fileName) => Path.Combine(DataPath, $"{fileName}.json");
    }
}