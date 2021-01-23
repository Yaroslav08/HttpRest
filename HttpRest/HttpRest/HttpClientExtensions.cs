using HttpRest;
using HttpRest.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
namespace HttpRest
{
    public static partial class HttpClientExtensions
    {
        #region Common
        private static HttpRestResponse<T> ErrorResponse<T>(Exception e, HttpStatusCode statusCode)
        {
            return e switch
            {
                HttpRequestException hre => new HttpRestResponse<T>(HttpRestResult.RequestError, statusCode, hre, default),
                WebException we => new HttpRestResponse<T>(HttpRestResult.HttpError, (we.Response as HttpWebResponse)?.StatusCode ?? statusCode, we, default),
                TaskCanceledException tce => new HttpRestResponse<T>(HttpRestResult.Cancel, statusCode, tce, default),
                _ => new HttpRestResponse<T>(HttpRestResult.Unknown, statusCode, e, default)
            };
        }

        private static void ProcessHeaders(HttpRequestMessage request, IDictionary<string, object>? headers)
        {
            if (headers is null)
            {
                return;
            }

            foreach (var header in headers)
            {
                switch (header.Value)
                {
                    case IEnumerable<string> ies:
                        request.Headers.Add(header.Key, ies);
                        break;
                    case IEnumerable<object> ie:
                        request.Headers.Add(header.Key, ie.Select(x => x.ToString()));
                        break;
                    default:
                        request.Headers.Add(header.Key, header.Value.ToString());
                        break;
                }
            }
        }
        #endregion

        #region HttpGet
        public static ValueTask<IHttpRestResponse<T>> GetAsync<T>(this HttpClient client, string path, IDictionary<string, object>? headers = null, CancellationToken cancel = default)
        {
            return GetAsync<T>(client, HttpRestConfig.Default, path, headers, cancel);
        }

        public static async ValueTask<IHttpRestResponse<T>> GetAsync<T>(this HttpClient client, HttpRestConfig config, string path, IDictionary<string, object>? headers = null, CancellationToken cancel = default)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);

                ProcessHeaders(request, headers);

                response = await client.SendAsync(request, cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new HttpRestResponse<T>(HttpRestResult.HttpError, response.StatusCode, null, default);
                }

                try
                {
                    var obj = await config.Serializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), cancel).ConfigureAwait(false);

                    return new HttpRestResponse<T>(HttpRestResult.Success, response.StatusCode, null, obj);
                }
                catch (Exception e)
                {
                    return new HttpRestResponse<T>(HttpRestResult.SerializeError, response.StatusCode, e, default);
                }
            }
            catch (Exception e)
            {
                return ErrorResponse<T>(e, response?.StatusCode ?? 0);
            }
        }
        #endregion

        #region HttpPost
        public static ValueTask<IHttpRestResponse> PostAsync(this HttpClient client, string path, object parameter, IDictionary<string, object>? headers = null, bool compress = false, CancellationToken cancel = default)
        {
            return PostAsync(client, HttpRestConfig.Default, path, parameter, headers, compress, cancel);
        }

        public static async ValueTask<IHttpRestResponse> PostAsync(this HttpClient client, HttpRestConfig config, string path, object parameter, IDictionary<string, object>? headers = null, bool compress = false, CancellationToken cancel = default)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, path);
                await using var stream = new MemoryStream();

                ProcessHeaders(request, headers);

                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, cancel).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    return new HttpRestResponse<object>(HttpRestResult.SerializeError, 0, e, default);
                }

                stream.Seek(0, SeekOrigin.Begin);
                var content = (HttpContent)new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
                if (compress)
                {
                    content = new CompressedContent(content, config.ContentEncoding);
                }

                request.Content = content;

                response = await client.SendAsync(request, cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new HttpRestResponse<object>(HttpRestResult.HttpError, response.StatusCode, null, default);
                }

                return new HttpRestResponse<object>(HttpRestResult.Success, response.StatusCode, null, default);
            }
            catch (Exception e)
            {
                return ErrorResponse<object>(e, response?.StatusCode ?? 0);
            }
        }

        public static ValueTask<IHttpRestResponse<T>> PostAsync<T>(this HttpClient client, string path, object parameter, IDictionary<string, object>? headers = null, bool compress = false, CancellationToken cancel = default)
        {
            return PostAsync<T>(client, HttpRestConfig.Default, path, parameter, headers, compress, cancel);
        }

        public static async ValueTask<IHttpRestResponse<T>> PostAsync<T>(this HttpClient client, HttpRestConfig config, string path, object parameter, IDictionary<string, object>? headers = null, bool compress = false, CancellationToken cancel = default)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, path);
                await using var stream = new MemoryStream();
                ProcessHeaders(request, headers);
                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, cancel).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    return new HttpRestResponse<T>(HttpRestResult.SerializeError, 0, e, default);
                }
                stream.Seek(0, SeekOrigin.Begin);
                var content = (HttpContent)new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
                if (compress)
                {
                    content = new CompressedContent(content, config.ContentEncoding);
                }
                request.Content = content;
                response = await client.SendAsync(request, cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new HttpRestResponse<T>(HttpRestResult.HttpError, response.StatusCode, null, default);
                }
                try
                {
                    var obj = await config.Serializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), cancel).ConfigureAwait(false);
                    return new HttpRestResponse<T>(HttpRestResult.Success, response.StatusCode, null, obj);
                }
                catch (Exception e)
                {
                    return new HttpRestResponse<T>(HttpRestResult.SerializeError, response.StatusCode, e, default);
                }
            }
            catch (Exception e)
            {
                return ErrorResponse<T>(e, response?.StatusCode ?? 0);
            }
        }
        #endregion

        #region Download
        public static ValueTask<IHttpRestResponse> DownloadAsync(this HttpClient client, string path, string filename, IDictionary<string, object>? headers = null, Action<long, long, long>? progress = null, CancellationToken cancel = default)
        {
            return DownloadAsync(client, HttpRestConfig.Default, path, filename, headers, progress, cancel);
        }

        public static async ValueTask<IHttpRestResponse> DownloadAsync(this HttpClient client, HttpRestConfig config, string path, string filename, IDictionary<string, object>? headers = null, Action<long, long, long>? progress = null, CancellationToken cancel = default)
        {
            var delete = true;
            try
            {
                await using var stream = new FileStream(filename, FileMode.Create);
                var result = await DownloadAsync(client, config, path, stream, headers, progress, cancel).ConfigureAwait(false);
                if (result.IsSuccess())
                {
                    delete = false;
                }
                return result;
            }
            finally
            {
                if (delete)
                {
                    File.Delete(filename);
                }
            }
        }

        public static ValueTask<IHttpRestResponse> DownloadAsync(this HttpClient client, string path, Stream stream, IDictionary<string, object>? headers = null, Action<long, long, long>? progress = null, CancellationToken cancel = default)
        {
            return DownloadAsync(client, HttpRestConfig.Default, path, stream, headers, progress, cancel);
        }

        public static async ValueTask<IHttpRestResponse> DownloadAsync(this HttpClient client, HttpRestConfig config, string path, Stream stream, IDictionary<string, object>? headers = null, Action<long, long, long>? progress = null, CancellationToken cancel = default)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                ProcessHeaders(request, headers);
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new HttpRestResponse<object>(HttpRestResult.HttpError, response.StatusCode, null, default);
                }
                await using (var input = await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false))
                {
                    if (progress is not null)
                    {
                        var totalSize = response.Content.Headers.ContentLength ??
                                        config.LengthResolver?.Invoke(new LengthResolveContext(response));
                        if (totalSize.HasValue)
                        {
                            var buffer = new byte[config.TransferBufferSize];
                            var totalProcessed = 0L;
                            int read;
                            while ((read = await input.ReadAsync(buffer, cancel).ConfigureAwait(false)) > 0)
                            {
                                await stream.WriteAsync(buffer.AsMemory(0, read), cancel).ConfigureAwait(false);

                                totalProcessed += read;
                                //(processed * 100) / total
                                progress(totalProcessed, totalSize.Value, (totalProcessed * 100) / totalSize.Value);
                            }
                        }
                        else
                        {
                            await input.CopyToAsync(stream, config.TransferBufferSize, cancel).ConfigureAwait(false);
                            await stream.FlushAsync(cancel).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await input.CopyToAsync(stream, config.TransferBufferSize, cancel).ConfigureAwait(false);
                        await stream.FlushAsync(cancel).ConfigureAwait(false);
                    }
                }
                return new HttpRestResponse<object>(HttpRestResult.Success, response.StatusCode, null, default);
            }
            catch (Exception e)
            {
                return ErrorResponse<object>(e, response?.StatusCode ?? 0);
            }
        }
        #endregion

        #region Upload
        public static ValueTask<IHttpRestResponse> UploadAsync(this HttpClient client, string path, Stream stream, string name, string filename, Func<Stream, Stream, Func<Stream, Stream, ValueTask>, ValueTask>? filter = null, IDictionary<string, object>? parameters = null, IDictionary<string, object>? headers = null, Action<long, long, long>? progress = null, CancellationToken cancel = default)
        {
            return UploadAsync(client, HttpRestConfig.Default, path, stream, name, filename, filter, parameters, headers, progress, cancel);
        }

        public static ValueTask<IHttpRestResponse> UploadAsync( this HttpClient client, HttpRestConfig config, string path, Stream stream, string name, string filename, Func<Stream, Stream, Func<Stream, Stream, ValueTask>, ValueTask>? filter = null, IDictionary<string, object>? parameters = null, IDictionary<string, object>? headers = null, Action<long, long, long>? progress = null, CancellationToken cancel = default)
        {
            return UploadAsync(client, config, path, new[] { new UploadEntry(stream, name, filename) { Filter = filter } }, parameters, headers, progress, cancel);
        }

        public static ValueTask<IHttpRestResponse> UploadAsync( this HttpClient client, string path, string name, string filename, Func<Stream, Stream, Func<Stream, Stream, ValueTask>, ValueTask>? filter = null, IDictionary<string, object>? parameters = null, IDictionary<string, object>? headers = null, Action<long, long, long>? progress = null, CancellationToken cancel = default)
        {
            return UploadAsync(client, HttpRestConfig.Default, path, name, filename, filter, parameters, headers, progress, cancel);
        }

        public static async ValueTask<IHttpRestResponse> UploadAsync(this HttpClient client, HttpRestConfig config, string path, string name, string filename, Func<Stream, Stream, Func<Stream, Stream, ValueTask>, ValueTask>? filter = null, IDictionary<string, object>? parameters = null, IDictionary<string, object>? headers = null, Action<long, long, long>? progress = null, CancellationToken cancel = default)
        {
            var fi = new FileInfo(filename);
            await using var stream = fi.OpenRead();
            return await UploadAsync(client, config, path, new[] { new UploadEntry(stream, name, fi.Name) { Filter = filter } }, parameters, headers, progress, cancel).ConfigureAwait(false);
        }

        public static ValueTask<IHttpRestResponse> UploadAsync(this HttpClient client, string path, IList<UploadEntry> entries, IDictionary<string, object>? parameters = null, IDictionary<string, object>? headers = null, Action<long, long, long>? progress = null, CancellationToken cancel = default)
        {
            return UploadAsync(client, HttpRestConfig.Default, path, entries, parameters, headers, progress, cancel);
        }

        public static async ValueTask<IHttpRestResponse> UploadAsync(this HttpClient client, HttpRestConfig config, string path, IList<UploadEntry> entries, IDictionary<string, object>? parameters = null, IDictionary<string, object>? headers = null, Action<long, long, long>? progress = null, CancellationToken cancel = default)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, path);
                using var multipart = new MultipartFormDataContent();
                ProcessHeaders(request, headers);
                if (parameters is not null)
                {
                    foreach (var parameter in parameters)
                    {
                        multipart.Add(new StringContent(parameter.Value.ToString() ?? string.Empty), parameter.Key);
                    }
                }
                var progressProxy = default(Action<long>);
                if (progress is not null)
                {
                    var totalSize = CalcTotalSize(entries);
                    if (totalSize.HasValue)
                    {
                        var totalProcessed = 0L;
                        progressProxy = processed =>
                        {
                            totalProcessed += processed;
                            progress(totalProcessed, totalSize.Value, (totalProcessed * 100) / totalSize.Value);
                        };
                    }
                }
                foreach (var upload in entries)
                {
                    multipart.Add(new UploadStreamContent(upload, config.TransferBufferSize, progressProxy, cancel), upload.Name, upload.FileName);
                }
                request.Content = multipart;
                response = await client.SendAsync(request, cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new HttpRestResponse<object>(HttpRestResult.HttpError, response.StatusCode, null, default);
                }
                return new HttpRestResponse<object>(HttpRestResult.Success, response.StatusCode, null, default);
            }
            catch (Exception e)
            {
                return ErrorResponse<object>(e, response?.StatusCode ?? 0);
            }
        }

        private static long? CalcTotalSize(IList<UploadEntry> entries)
        {
            var total = 0L;
            foreach (var upload in entries)
            {
                if (!upload.Stream.CanSeek)
                {
                    return null;
                }

                total += upload.Stream.Length;
            }
            return total;
        }
        #endregion
    }
}