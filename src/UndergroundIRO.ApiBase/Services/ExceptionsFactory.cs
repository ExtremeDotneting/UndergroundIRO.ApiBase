using ApiBase.Exceptions;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;

namespace ApiBase.Services
{
    public class ExceptionsFactory : IExceptionsFactory
    {
        public virtual Exception CheckHttpResponse(HttpRequestMessage request, HttpResponseMessage response)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            var status = (int)response.StatusCode;
            var content = response.Content.ReadAsStringAsync().Result;
            if (status >= 400)
            {

                return new ApiException(
                    status,
                    $"Error calling '{request.RequestUri.AbsoluteUri}' method.",
                    content
                    );
            }
            if (status == 0)
            {
                return new ApiException(
                    status,
                    $"Error calling '{request.RequestUri.AbsoluteUri}' method.",
                    content
                    );
            }
            return null;
        }

        public virtual Exception CheckJTokenResponse(JToken jToken)
        {
            return null;
        }
    }
}