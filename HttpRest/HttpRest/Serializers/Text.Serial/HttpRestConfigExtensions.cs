using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpRest.Serializers.Text.Serial
{
    public static class HttpRestConfigExtensions
    {
        public static HttpRestConfig UseJsonSerializer(this HttpRestConfig config)
        {
            config.Serializer = JsonSerializer.Default;
            return config;
        }

        public static HttpRestConfig UseJsonSerializer(this HttpRestConfig config, Action<System.Text.Json.JsonSerializerOptions> action)
        {
            var serializerConfig = new JsonSerializerConfig();
            action(serializerConfig.Options);
            config.Serializer = new JsonSerializer(serializerConfig);
            return config;
        }
    }
}
