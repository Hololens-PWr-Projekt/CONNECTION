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
            string filePath = GetFilePath(fileName);

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"File not found at path: {filePath}");
                    return null;
                }

                string jsonContent = File.ReadAllText(filePath);

                return Deserialize<Dictionary<string, object>>(jsonContent);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load JSON file: {e.Message}");
                return null;
            }
        }

        // Serialize data into JSON format, keeping original property names
        public static string Serialize(object data)
        {
            try
            {
                return JsonConvert.SerializeObject(data, Formatting.None);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to serialize object. Exception: {e.Message}");
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
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize object. Exception: {e.Message}");
                return default;
            }
        }

        // Helper function to get the file path
        private static string GetFilePath(string fileName)
        {
            string DataPath = Path.Combine(Application.dataPath, "Scripts", "data");

            return Path.Combine(DataPath, $"{fileName}.json");
        }
    }
}