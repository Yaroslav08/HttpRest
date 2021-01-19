using System;
using System.Net;
namespace HttpRest
{
    public interface IHttpRestResponse
    {
        RestResult RestResult { get; }

        HttpStatusCode StatusCode { get; }

        Exception? InnerException { get; }
    }

    public interface IHttpRestResponse<out T> : IHttpRestResponse
    {
        T? Content { get; }
    }

    public sealed class RestResponse<T> : IHttpRestResponse<T>
    {
        public RestResult RestResult { get; }

        public HttpStatusCode StatusCode { get; }

        public Exception? InnerException { get; }

        public T? Content { get; }

        public RestResponse(RestResult restResult, HttpStatusCode statusCode, Exception? innerException, T? content)
        {
            RestResult = restResult;
            StatusCode = statusCode;
            InnerException = innerException;
            Content = content;
        }
    }

    public static class HttpResponseExtensions
    {
        public static bool IsSuccess(this IRestResponse response) => response.RestResult == RestResult.Success;
    }
}