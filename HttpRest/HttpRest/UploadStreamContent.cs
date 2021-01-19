using HttpRest.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpRest
{
    public sealed class UploadStreamContent : HttpContent
    {
        private readonly Stream source;

        private readonly Func<Stream, Stream, Func<Stream, Stream, ValueTask>, ValueTask>? filter;

        private readonly int bufferSize;

        private readonly Action<long>? progress;

        private readonly CancellationToken cancel;

        public UploadStreamContent(UploadEntry entry, int bufferSize, Action<long>? progress, CancellationToken cancel)
        {
            source = entry.Stream;
            filter = entry.Filter;
            this.bufferSize = bufferSize;
            this.progress = progress;
            this.cancel = cancel;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                source.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override bool TryComputeLength(out long length)
        {
            if ((filter is null) && source.CanSeek)
            {
                length = source.Length;
                return true;
            }

            length = 0;
            return false;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            if (progress is null)
            {
                if (filter is null)
                {
                    await source.CopyToAsync(stream, bufferSize, cancel).ConfigureAwait(false);
                    return;
                }

                await filter(source, stream, async (s, d) => await s.CopyToAsync(d, bufferSize, cancel).ConfigureAwait(false)).ConfigureAwait(false);
                return;
            }

            if (filter is null)
            {
                await CopyAsync(source, stream, bufferSize, progress, cancel).ConfigureAwait(false);
                return;
            }

            await filter(source, stream, (s, d) => CopyAsync(s, d, bufferSize, progress, cancel)).ConfigureAwait(false);
        }

        private static async ValueTask CopyAsync(Stream source, Stream destination, int bufferSize, Action<long> progress, CancellationToken cancel)
        {
            var buffer = new byte[bufferSize];
            int read;
            while ((read = await source.ReadAsync(buffer, cancel).ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancel).ConfigureAwait(false);
                progress(read);
            }

            await destination.FlushAsync(cancel).ConfigureAwait(false);
        }
    }
}
