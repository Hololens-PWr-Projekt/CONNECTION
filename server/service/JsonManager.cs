using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Server.Service
{
    public static class JsonManager
    {
        public static void SetSerializerSettings()
        {
            JsonConvert.DefaultSettings = () =>
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                };
        }

        // Serialize data into JSON format, keeping original property names
        public static string Serialize<T>(T data)
        {
            if (data == null)
            {
                Console.WriteLine("Failed to serialize object: data is null.");
                return string.Empty;
            }

            try
            {
                return JsonConvert.SerializeObject(data, Formatting.None);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to serialize object. Exception: {e.Message}");
                return string.Empty;
            }
        }

        // Deserialize JSON string into an object, respecting original property names
        public static T Deserialize<T>(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<T>(jsonData);

                if (data == null)
                {
                    Console.WriteLine("Failed to deserialize object: JSON data is null or empty.");
                    return default!;
                }

                return data;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to deserialize object. Exception: {e.Message}");
                return default!;
            }
        }
    }
}
