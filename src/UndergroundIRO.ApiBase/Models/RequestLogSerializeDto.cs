using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Net.Http;

namespace ApiBase.Models
{
    class RequestLogSerializeDto
    {
        public RequestLogSerializeDto() { }

        public RequestLogSerializeDto(
            string relativePath,
            HttpMethod method,
            string contentType,
            IDictionary<string, string> queryParams,
            string postBody,
            IDictionary<string, string> headerParams,
            IDictionary<string, string> formParams,
            IDictionary<string, string> pathParams
            )
        {
            RelativePath = relativePath;
            Method = method;
            ContentType = contentType;
            QueryParams = queryParams;
            PostBody = postBody;
            HeaderParams = headerParams;
            FormParams = formParams;
            PathParams = pathParams;
        }

        public string RelativePath { get; set; }
        public HttpMethod Method { get; set; }
        public string ContentType { get; set; }
        public IDictionary<string, string> QueryParams { get; set; }
        public string PostBody { get; set; }
        public IDictionary<string, string> HeaderParams { get; set; }
        public IDictionary<string, string> FormParams { get; set; }
        public IDictionary<string, string> PathParams { get; set; }

        public override string ToString()
        {
            var settings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
            };
            settings.Converters.Add(new StringEnumConverter());
            return JsonConvert.SerializeObject(
                this,
                settings
                );
        }
    }
}
