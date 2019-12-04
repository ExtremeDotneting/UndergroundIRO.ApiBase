using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;

namespace ApiBase.Services
{
    public interface IExceptionsFactory
    {
        Exception CheckHttpResponse(HttpRequestMessage request, HttpResponseMessage response);

        Exception CheckJTokenResponse(JToken jToken);
    }
}
