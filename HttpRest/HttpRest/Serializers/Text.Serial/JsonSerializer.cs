﻿using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HttpRest.Serializers.Text.Serial
{
    public sealed class JsonSerializer : ISerializer
    {
        public static JsonSerializer Default { get; } = new(new JsonSerializerConfig());

        private readonly JsonSerializerOptions options;

        public string ContentType { get; }

        public JsonSerializer(JsonSerializerConfig config)
        {
            options = config.Options;
            ContentType = config.ContentType;
        }

        public async ValueTask SerializeAsync<T>(Stream stream, T obj, CancellationToken cancel)
        {
            await System.Text.Json.JsonSerializer.SerializeAsync(stream, obj, obj!.GetType(), options, cancel).ConfigureAwait(false);
        }

        public async ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancel)
        {
            return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream, options, cancel).ConfigureAwait(false);
        }
    }
}
