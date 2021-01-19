using System;
using System.Collections.Generic;
using System.Text;
namespace HttpRest
{
    public enum HttpRestResult
    {
        Success,
        Cancel,
        RequestError,
        HttpError,
        SerializeError,
        Unknown
    }
}