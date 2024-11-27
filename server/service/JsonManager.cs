using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Server.Service
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

        // Serialize data into JSON format, keeping original property names
        public static string Serialize<T>(T data)
        {
            try
            {
                return JsonConvert.SerializeObject(data, Formatting.None);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to serialize object. Exception: {e.Message}");
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
                Console.WriteLine($"Failed to deserialize object. Exception: {e.Message}");
                return default;
            }
        }
    }
}