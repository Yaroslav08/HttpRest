using System;
using System.Net;
namespace HttpRest
{
    public interface IHttpRestResponse
    {
        HttpRestResult RestResult { get; }

        HttpStatusCode StatusCode { get; }

        Exception? InnerException { get; }
    }

    public interface IHttpRestResponse<out T> : IHttpRestResponse
    {
        T? Content { get; }
    }

    public sealed class HttpRestResponse<T> : IHttpRestResponse<T>
    {
        public HttpRestResult RestResult { get; }

        public HttpStatusCode StatusCode { get; }

        public Exception? InnerException { get; }

        public T? Content { get; }

        public HttpRestResponse(HttpRestResult restResult, HttpStatusCode statusCode, Exception? innerException, T? content)
        {
            RestResult = restResult;
            StatusCode = statusCode;
            InnerException = innerException;
            Content = content;
        }
    }

    public static class HttpResponseExtensions
    {
        public static bool IsSuccess(this IHttpRestResponse response) => response.RestResult == HttpRestResult.Success;
    }
}