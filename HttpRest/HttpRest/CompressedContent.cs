using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace HttpRest
{
    public sealed class CompressedContent : HttpContent
    {
        private readonly HttpContent content;

        private readonly ContentEncoding contentEncoding;

        public CompressedContent(HttpContent content, ContentEncoding contentEncoding)
        {
            this.content = content;
            this.contentEncoding = contentEncoding;
            foreach (var header in content.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            Headers.ContentEncoding.Add(contentEncoding.ToString().ToLowerInvariant());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                content.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            var compressedStream = contentEncoding == ContentEncoding.Gzip
                ? (Stream)new GZipStream(stream, CompressionMode.Compress, true)
                : new DeflateStream(stream, CompressionMode.Compress, true);
            return content.CopyToAsync(compressedStream, context)
                .ContinueWith(_ => compressedStream.Dispose());
        }
    }
}
