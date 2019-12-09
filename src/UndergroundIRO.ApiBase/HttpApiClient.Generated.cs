using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace UndergroundIRO.ApiBase
{
    //!STANDARD SWAGGER GENERATED CODE.
    /// <summary>
    /// API client is mainly responsible for making the HTTP call to the API backend.
    /// </summary>
    public partial class HttpApiClient
    {
        /// <summary>
        /// Allows for extending request processing for <see cref="HttpApiClient"/> generated code.
        /// </summary>
        /// <param name="request">The RestSharp request object</param>
        protected virtual void InterceptRequest(HttpRequestMessage request) { }

        /// <summary>
        /// Allows for extending response processing for <see cref="HttpApiClient"/> generated code.
        /// </summary>
        /// <param name="request">The RestSharp request object</param>
        /// <param name="response">The RestSharp response object</param>
        protected virtual void InterceptResponse(HttpRequestMessage request, HttpResponseMessage response) { }

        /// <summary>
        /// Creates and sets up a RestRequest prior to a call.
        /// </summary>
        protected HttpRequestMessage PrepareRequest(
            string url,
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
            if (pathParams != null)
            {
                foreach (var pathParam in pathParams)
                {
                    url = url.Replace("{" + pathParam.Key + "}", pathParam.Value);
                }
            }

            if (queryParams != null)
            {
                url += "?";
                // add query parameter, if any
                foreach (var param in queryParams)
                {
                    url += $"{UrlEncode(param.Key)}={UrlEncode(param.Value)}&";
                }
                url = url.Remove(url.Length - 1);
            }

            var request = new HttpRequestMessage(method, url);

            if (headerParams != null)
            {
                // add header parameter, if any
                foreach (var param in headerParams)
                {
                    request.Headers.Add(param.Key, param.Value);
                }
            }

            if (formParams != null)
            {
                request.Content = new FormUrlEncodedContent(formParams);
            }
            else if (postBody != null)
            {
                var stringContent = new StringContent(
                    postBody, 
                    textContentEncoding,
                    contentType?.MediaType ?? "text/plain"
                    );
                request.Content = stringContent;
            }

            if (contentType != null)
            {
                request.Content.Headers.ContentType = contentType;
            }
            return request;
        }

        /// <summary>
        /// Escape string (url-encoded).
        /// </summary>
        /// <param name="str">string to be escaped.</param>
        /// <returns>Escaped string.</returns>
        protected string EscapeString(string str)
        {
            return UrlEncode(str);
        }

        /// <summary>
        ///Check if the given MIME is a JSON MIME.
        ///JSON MIME examples:
        ///    application/json
        ///    application/json; charset=UTF8
        ///    APPLICATION/JSON
        ///    application/vnd.company+json
        /// </summary>
        /// <param name="mime">MIME</param>
        /// <returns>Returns True if MIME type is json.</returns>
        protected bool IsJsonMime(string mime)
        {
            var jsonRegex = new Regex("(?i)^(application/json|[^;/ \t]+/[^;/ \t]+[+]json)[ \t]*(;.*)?$");
            return mime != null && (jsonRegex.IsMatch(mime) || mime.Equals("application/json-patch+json"));
        }

        /// <summary>
        /// Select the Content-Type header's value from the given content-type array:
        /// if JSON type exists in the given array, use it;
        /// otherwise use the first one defined in 'consumes'
        /// </summary>
        /// <param name="contentTypes">The Content-Type array to select from.</param>
        /// <returns>The Content-Type header to use.</returns>
        protected string SelectHeaderContentType(string[] contentTypes)
        {
            if (contentTypes.Length == 0)
                return "application/json";

            foreach (var contentType in contentTypes)
            {
                if (IsJsonMime(contentType.ToLower()))
                    return contentType;
            }

            return contentTypes[0]; // use the first content type specified in 'consumes'
        }

        /// <summary>
        /// Select the Accept header's value from the given accepts array:
        /// if JSON exists in the given array, use it;
        /// otherwise use all of them (joining into a string)
        /// </summary>
        /// <param name="accepts">The accepts array to select from.</param>
        /// <returns>The Accept header to use.</returns>
        protected string SelectHeaderAccept(string[] accepts)
        {
            if (accepts.Length == 0)
                return null;

            if (accepts.Contains("application/json", StringComparer.OrdinalIgnoreCase))
                return "application/json";

            return string.Join(",", accepts);
        }

        /// <summary>
        /// Encode string in base64 format.
        /// </summary>
        /// <param name="text">string to be encoded.</param>
        /// <returns>Encoded string.</returns>
        protected string Base64Encode(string text)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Convert stream to byte array
        /// </summary>
        /// <param name="inputStream">Input stream to be converted</param>
        /// <returns>Byte array</returns>
        protected byte[] ReadAsBytes(Stream inputStream)
        {
            byte[] buf = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int count;
                while ((count = inputStream.Read(buf, 0, buf.Length)) > 0)
                {
                    ms.Write(buf, 0, count);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// URL encode a string
        /// Credit/Ref: https://github.com/restsharp/RestSharp/blob/master/RestSharp/Extensions/stringExtensions.cs#L50
        /// </summary>
        /// <param name="input">string to be URL encoded</param>
        /// <returns>Byte array</returns>
        protected string UrlEncode(string input)
        {
            const int maxLength = 32766;

            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (input.Length <= maxLength)
            {
                return Uri.EscapeDataString(input);
            }

            StringBuilder sb = new StringBuilder(input.Length * 2);
            int index = 0;

            while (index < input.Length)
            {
                int length = Math.Min(input.Length - index, maxLength);
                string substring = input.Substring(index, length);

                sb.Append(Uri.EscapeDataString(substring));
                index += substring.Length;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert params to key/value pairs. 
        /// Use collectionFormat to properly format lists and collections.
        /// </summary>
        /// <param name="name">Key name.</param>
        /// <param name="value">Value object.</param>
        /// <returns>A list of KeyValuePairs</returns>
        protected IEnumerable<KeyValuePair<string, string>> ParameterToKeyValuePairs(string collectionFormat, string name, object value)
        {
            var parameters = new List<KeyValuePair<string, string>>();

            if (IsCollection(value) && collectionFormat == "multi")
            {
                var valueCollection = value as IEnumerable;
                parameters.AddRange(from object item in valueCollection select new KeyValuePair<string, string>(name, ParameterToString(item)));
            }
            else
            {
                parameters.Add(new KeyValuePair<string, string>(name, ParameterToString(value)));
            }

            return parameters;
        }

        /// <summary>
        /// Check if generic object is a collection.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True if object is a collection type</returns>
        private static bool IsCollection(object value)
        {
            return value is IList || value is ICollection;
        }
    }
}
