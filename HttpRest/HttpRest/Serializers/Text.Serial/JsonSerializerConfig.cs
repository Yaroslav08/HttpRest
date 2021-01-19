using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HttpRest.Serializers.Text.Serial
{
    public class JsonSerializerConfig
    {
        public string ContentType { get; set; } = "application/json";

        public JsonSerializerOptions Options { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
