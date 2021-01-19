using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        private static RestResponse<T> ErrorResponse<T>(Exception e, HttpStatusCode statusCode)
        {
            return e switch
            {
                HttpRequestException hre => new RestResponse<T>(HttpRestResult.RequestError, statusCode, hre, default),
                WebException we => new RestResponse<T>(HttpRestResult.HttpError, (we.Response as HttpWebResponse)?.StatusCode ?? statusCode, we, default),
                TaskCanceledException tce => new RestResponse<T>(HttpRestResult.Cancel, statusCode, tce, default),
                _ => new RestResponse<T>(HttpRestResult.Unknown, statusCode, e, default)
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
                    return new RestResponse<T>(HttpRestResult.HttpError, response.StatusCode, null, default);
                }

                try
                {
                    var obj = await config.Serializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), cancel).ConfigureAwait(false);

                    return new RestResponse<T>(HttpRestResult.Success, response.StatusCode, null, obj);
                }
                catch (Exception e)
                {
                    return new RestResponse<T>(HttpRestResult.SerializeError, response.StatusCode, e, default);
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
                    return new RestResponse<object>(HttpRestResult.SerializeError, 0, e, default);
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
                    return new RestResponse<object>(HttpRestResult.HttpError, response.StatusCode, null, default);
                }

                return new RestResponse<object>(HttpRestResult.Success, response.StatusCode, null, default);
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
                    return new RestResponse<T>(HttpRestResult.SerializeError, 0, e, default);
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
                    return new RestResponse<T>(HttpRestResult.HttpError, response.StatusCode, null, default);
                }
                try
                {
                    var obj = await config.Serializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), cancel).ConfigureAwait(false);
                    return new RestResponse<T>(HttpRestResult.Success, response.StatusCode, null, obj);
                }
                catch (Exception e)
                {
                    return new RestResponse<T>(HttpRestResult.SerializeError, response.StatusCode, e, default);
                }
            }
            catch (Exception e)
            {
                return ErrorResponse<T>(e, response?.StatusCode ?? 0);
            }
        }
        #endregion

    }
}