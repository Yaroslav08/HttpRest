using HttpRest.Serializers;
using HttpRest.Transport;
using System;
using System.Diagnostics.CodeAnalysis;
namespace HttpRest
{
    public class HttpRestConfig
    {
        public static HttpRestConfig Default { get; } = new();

        [AllowNull]
        public ISerializer Serializer { get; set; }

        public ContentEncoding ContentEncoding { get; set; } = ContentEncoding.Gzip;

        public int TransferBufferSize { get; set; } = 16 * 1024;

        public Func<ILengthResolveContext, long?>? LengthResolver { get; set; }
    }
}