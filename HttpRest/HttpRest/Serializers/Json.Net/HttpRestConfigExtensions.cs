using System;

namespace HttpRest.Serializers.Json.Net
{
    public static class RestConfigExtensions
    {
        public static HttpRestConfig UseJsonSerializer(this HttpRestConfig config)
        {
            config.Serializer = JsonSerializer.Default;
            return config;
        }

        public static HttpRestConfig UseJsonSerializer(this HttpRestConfig config, Action<JsonSerializerConfig> action)
        {
            var serializerConfig = new JsonSerializerConfig();
            action(serializerConfig);
            config.Serializer = new JsonSerializer(serializerConfig);
            return config;
        }
    }
}
