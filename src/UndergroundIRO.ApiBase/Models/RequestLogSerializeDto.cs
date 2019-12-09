using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UndergroundIRO.ApiBase.Models
{
    class RequestLogSerializeDto
    {
        public RequestLogSerializeDto() { }

        public RequestLogSerializeDto(
            string relativePath,
            HttpMethod method,
            MediaTypeHeaderValue contentType,
            Encoding textContentEncoding,
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
            TextContentEncoding = textContentEncoding;
            QueryParams = queryParams;
            PostBody = postBody;
            HeaderParams = headerParams;
            FormParams = formParams;
            PathParams = pathParams;
        }

        public string RelativePath { get; set; }
        public HttpMethod Method { get; set; }
        public MediaTypeHeaderValue ContentType { get; set; }
        public Encoding TextContentEncoding { get; set; }
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
