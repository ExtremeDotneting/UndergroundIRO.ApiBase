using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace UndergroundIRO.ApiBase.Services
{
    public interface IExceptionsFactory
    {
        Exception CheckHttpResponse(HttpRequestMessage request, HttpResponseMessage response);

        Exception CheckJTokenResponse(JToken jToken);
    }
}
