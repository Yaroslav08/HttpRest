using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace HttpRest.Serializers.Json.Net
{
    public class JsonSerializerConfig
    {
        public string ContentType { get; set; } = "application/json";

        public JsonSerializerSettings Settings { get; } = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
    }
}
