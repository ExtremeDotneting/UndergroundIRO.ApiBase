using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UndergroundIRO.ApiBase.Models;
using UndergroundIRO.ApiBase.Services;

namespace UndergroundIRO.ApiBase
{
    public partial class HttpApiClient : IHttpApiClient
    {
        public string Id { get; }
        public int TotalRequestsCount { get; private set; }
        public HttpConfiguration Configuration { get; }
        public JsonSerializerSettings JsonSettings { get; set; } = new JsonSerializerSettings();

        protected ILogger Log { get; }
        protected LogLevel CurrentLogLevel => Configuration.LogLevel;
        protected HttpClient HttpClient { get; } = new HttpClient();

        protected IExceptionsFactory ExceptionsFactory;

        public HttpApiClient(
            HttpConfiguration conf = null,
            ITimeoutQueue timeoutQueue = null,
            IExceptionsFactory exceptionsFactory = null,
            ILoggerFactory loggerFactory = null
            )
        {
            Id = Guid.NewGuid().ToString().Remove(6);
            Configuration = conf ?? new HttpConfiguration();
            loggerFactory ??= Services.LoggerFactory.CreateDefaultLoggerFactory();
            Log = loggerFactory.CreateLogger(GetType());
            _timeoutQueue = timeoutQueue ?? new TimeoutQueue();
            ExceptionsFactory = exceptionsFactory ?? new ExceptionsFactory();
        }

        #region CallApi.
        public async Task<HttpResponseMessage> CallApiAsync(
            string absoluteUrl,
            HttpMethod method,
            IDictionary<string, string> headerParams = null
        )
        {
            return await CallApiAsync_Privat(
                absoluteUrl: absoluteUrl,
                method: method,
                headerParams: headerParams,
                formParams: null,
                textContentEncoding: null,
                mediaType: null,
                stringsBody: null,
                httpContent: null
            );
        }

        public async Task<HttpResponseMessage> CallApiAsync(
            string absoluteUrl,
            HttpMethod method,
            string stringsBody,
            string mediaType = "application/json",
            Encoding textContentEncoding = null,
            IDictionary<string, string> headerParams = null
        )
        {
            return await CallApiAsync_Privat(
                absoluteUrl: absoluteUrl,
                method: method,
                headerParams: headerParams,
                formParams: null,
                textContentEncoding: textContentEncoding,
                mediaType: mediaType,
                stringsBody: stringsBody,
                httpContent: null
            );
        }

        public async Task<HttpResponseMessage> CallApiAsync(
            string absoluteUrl,
            HttpMethod method,
            IDictionary<string, string> formParams,
            IDictionary<string, string> headerParams 
        )
        {
            return await CallApiAsync_Privat(
                absoluteUrl: absoluteUrl,
                method: method,
                headerParams: headerParams,
                formParams: formParams,
                textContentEncoding: null,
                mediaType: null,
                stringsBody: null,
                httpContent: null
            );
        }

        public async Task<HttpResponseMessage> CallApiAsync(
            string absoluteUrl,
            HttpMethod method,
            IDictionary<string, string> headerParams,
            HttpContent httpContent
        )
        {
            return await CallApiAsync_Privat(
                absoluteUrl: absoluteUrl,
                method: method,
                headerParams: headerParams,
                formParams: null,
                textContentEncoding: null,
                mediaType: null,
                stringsBody: null,
                httpContent: httpContent
            );
        }

        async Task<HttpResponseMessage> CallApiAsync_Privat(
            string absoluteUrl,
            HttpMethod method,
            IDictionary<string, string> headerParams,
            IDictionary<string, string> formParams,
            Encoding textContentEncoding,
            string mediaType,
            string stringsBody,
            HttpContent httpContent
        )
        {
            var request = PrepareRequest(
                url: absoluteUrl,
                method: method,
                headerParams: headerParams,
                formParams: formParams,
                textContentEncoding: textContentEncoding,
                mediaType: mediaType,
                stringsBody: stringsBody,
                httpContent: httpContent
            );
            InterceptRequest(request);
            TotalRequestsCount++;
            //Logging.
            if (CurrentLogLevel <= LogLevel.Information)
            {
                var logStr = $"{nameof(HttpApiClient)} '{Id}' will send request '{TotalRequestsCount}':\n";
                var requestLogDto = new RequestLogSerializeDto(
                    url: absoluteUrl,
                    method: method,
                    headerParams: headerParams,
                    formParams: formParams,
                    textContentEncoding: textContentEncoding,
                    mediaType: mediaType,
                    stringsBody: stringsBody,
                    httpContent: httpContent
                    );
                logStr += requestLogDto.ToString();
                Log.LogInformation(logStr);
            }
            if (CurrentLogLevel <= LogLevel.Information)
            {

                var logStr = $"Request number '{TotalRequestsCount}' parameters is:\n";
                logStr += JsonConvert.SerializeObject(
                    request,
                    Formatting.Indented,
                    new StringEnumConverter()
                    );
                Log.LogInformation(logStr);
            }

            var response = await InQueueIfNeed(async () =>
                await HttpClient.SendAsync(request)
                );
            if (CurrentLogLevel <= LogLevel.Information)
            {
                try
                {
                    var respText = await response.Content.ReadAsStringAsync();
                    var logStr = $"Response number '{TotalRequestsCount}' body:\n{respText}";
                    Log.LogInformation(logStr);
                }
                catch { }
            }
            ThrowIfResponseError(request, response);
            InterceptResponse(request, response);
            return response;
        }
        #endregion

        /// <summary>
        /// Will throw exception if can't.
        /// </summary>
        public virtual async Task<ApiResponse<T>> ResolveApiResponse<T>(HttpResponseMessage httpResponse, JsonSerializerSettings settings = null)
        {
            settings = settings ?? JsonSettings;
            try
            {
                var headers = new Dictionary<string, IEnumerable<string>>();
                foreach (var item in httpResponse.Headers)
                {
                    headers[item.Key] = item.Value;
                }
                var content = await httpResponse.Content.ReadAsStringAsync();
                var jToken = Deserialize<JToken>(content, settings);
                ThrowIfResponseError(jToken);
                var data = Deserialize<T>(content, settings);
                var apiResponse = new ApiResponse<T>(
                    (int)httpResponse.StatusCode,
                    headers,
                    data
                    );
                //Logging.
                if (CurrentLogLevel <= LogLevel.Information)
                {
                    var logStr = $"{nameof(HttpApiClient)} '{Id}' resolved ApiResponse:\n";
                    logStr += apiResponse.ToString();
                    Log.LogInformation(logStr);
                }
                return apiResponse;
            }
            catch (Exception ex)
            {
                throw new Exception("Can't deserialize api response.", ex);
            }
        }

        #region Helpers.

        public void AddDefaultHeader(string key, string value)
        {
            Configuration.DefaultHeaders.Add(key, value);
        }

        public string RelativeUrlToAbsolute(string basePath, string relativeUrl)
        {
            if (relativeUrl.StartsWith("https://") || relativeUrl.StartsWith("http://"))
            {
                return relativeUrl;
            }
            if (basePath.EndsWith("/") && relativeUrl.StartsWith("/"))
            {
                relativeUrl = relativeUrl.Substring(1);
            }
            return basePath + relativeUrl;
        }

        public virtual T Deserialize<T>(string str, JsonSerializerSettings settings = null)
        {
            settings = settings ?? JsonSettings;
            return JsonConvert.DeserializeObject<T>(str, settings);
        }

        public virtual string SerializeAsType<T>(T obj, JsonSerializerSettings settings = null)
        {
            settings = settings ?? JsonSettings;
            return JsonConvert.SerializeObject(obj, typeof(T), settings);
        }

        public virtual string Serialize(object obj, JsonSerializerSettings settings = null)
        {
            settings = settings ?? JsonSettings;
            return JsonConvert.SerializeObject(obj, settings);
        }

        public IDictionary<string, string> ParametersToDict(object obj, JsonSerializerSettings settings = null)
        {
            var dict = new Dictionary<string, string>();
            var jObject = (obj as JObject) ?? JObject.FromObject(obj);
            foreach (var prop in jObject)
            {
                dict[prop.Key] = ParameterToString(prop, settings);
            }
            return dict;
        }

        public string ParameterToString(object obj, JsonSerializerSettings settings = null)
        {
            settings = settings ?? this.JsonSettings;
            if (obj == null)
            {
                return "null";
            }
            else if (obj is string)
            {
                return (string)obj;
            }
            else if (obj is IConvertible)
            {
                var res = Serialize(obj, settings);
                return RemoveJsonStringBrackets(res);
            }
            else if (obj is IEnumerable enumerable)
            {
                var flattenedstring = new StringBuilder();
                foreach (var param in enumerable)
                {
                    if (flattenedstring.Length > 0)
                        flattenedstring.Append(",");
                    flattenedstring.Append(param);
                }
                return flattenedstring.ToString();
            }
            else
            {
                var res = Serialize(obj, settings);
                return RemoveJsonStringBrackets(res);
            }
        }

        public string RemoveJsonStringBrackets(string json)
        {
            if (json.StartsWith("\"") && json.EndsWith("\""))
            {
                json = json.Substring(1);
                json = json.Remove(json.Length - 1);
            }
            return json;
        }
        #endregion

        void ThrowIfResponseError(HttpRequestMessage request, HttpResponseMessage response)
        {
            var ex = ExceptionsFactory.CheckHttpResponse(request, response);
            if (ex != null)
            {
                //Logging.
                if (CurrentLogLevel <= LogLevel.Error)
                {
                    Log.LogError($"{nameof(HttpApiClient)} '{Id}' RestResponse error:\n{ex}");
                }
                throw ex;
            }
        }

        void ThrowIfResponseError(JToken responseJToken)
        {
            var ex = ExceptionsFactory.CheckJTokenResponse(responseJToken);
            if (ex != null)
            {
                //Logging.
                if (CurrentLogLevel <= LogLevel.Error)
                {
                    Log.LogError($"{nameof(HttpApiClient)} '{Id}' JToken error:\n{ex}");
                }
                throw ex;
            }
        }

        #region Timeout queue.
        readonly ITimeoutQueue _timeoutQueue;

        /// <summary>
        /// If <see cref="HttpConfiguration.UseTimeoutQueue"/> is false - execute without queue.
        /// </summary>
        async Task InQueueIfNeed(Func<Task> func)
        {
            if (Configuration.UseTimeoutQueue)
            {
                await _timeoutQueue.Execute(func);
            }
            else
            {
                await func();
            }
        }

        /// <summary>
        /// If <see cref="HttpConfiguration.UseTimeoutQueue"/> is false - execute without queue.
        /// </summary>
        async Task<TResult> InQueueIfNeed<TResult>(Func<Task<TResult>> func)
        {
            if (Configuration.UseTimeoutQueue)
            {
                return await _timeoutQueue.Execute<TResult>(func);
            }
            else
            {
                return await func();
            }
        }
        #endregion

        #region Standart.
        public virtual async Task<TResult> GetRequest<TResult>(string absoluteUrl)
        {
            var restResponse = await CallApiAsync(
                absoluteUrl,
                HttpMethod.Get
            );
            var apiResponse = await ResolveApiResponse<TResult>(restResponse);
            return apiResponse.Data;
        }

        /// <summary>
        /// </summary>
        /// <param name="requestData">Serialized as json.</param>
        public virtual async Task<TResult> PostRequest<TRequest, TResult>(string absoluteUrl, TRequest requestData)
        {
            var body = Serialize(requestData);
            var restResponse = await CallApiAsync(
                absoluteUrl,
                HttpMethod.Post,
                stringsBody:body
            );
            var apiResponse = await ResolveApiResponse<TResult>(restResponse);
            return apiResponse.Data;
        }

        /// <summary>
        /// </summary>
        /// <param name="requestData">Serialized as json.</param>
        public virtual async Task<TResult> PutRequest<TRequest, TResult>(string absoluteUrl, TRequest requestData)
        {
            var body = Serialize(requestData);
            var restResponse = await CallApiAsync(
                absoluteUrl,
                HttpMethod.Put,
                stringsBody: body
            );
            var apiResponse = await ResolveApiResponse<TResult>(restResponse);
            return apiResponse.Data;
        }

        /// <summary>
        /// </summary>
        /// <param name="requestData">Serialized as json.</param>
        public virtual async Task<TResult> HeadRequest<TRequest, TResult>(string absoluteUrl, TRequest requestData)
        {
            var body = Serialize(requestData);
            var restResponse = await CallApiAsync(
                absoluteUrl,
                HttpMethod.Head,
                stringsBody: body
            );
            var apiResponse = await ResolveApiResponse<TResult>(restResponse);
            return apiResponse.Data;
        }

        /// <summary>
        /// </summary>
        /// <param name="requestData">Serialized as json.</param>
        public virtual async Task<TResult> DeleteRequest<TRequest, TResult>(string absoluteUrl, TRequest requestData)
        {
            var body = Serialize(requestData);
            var restResponse = await CallApiAsync(
                absoluteUrl,
                HttpMethod.Delete,
                stringsBody: body
            );
            var apiResponse = await ResolveApiResponse<TResult>(restResponse);
            return apiResponse.Data;
        }

        /// <summary>
        /// </summary>
        /// <param name="requestData">Serialized as json.</param>
        public virtual async Task<TResult> OptionsRequest<TRequest, TResult>(string absoluteUrl, TRequest requestData)
        {
            var body = Serialize(requestData);
            var restResponse = await CallApiAsync(
                absoluteUrl,
                HttpMethod.Options,
                stringsBody: body
            );
            var apiResponse = await ResolveApiResponse<TResult>(restResponse);
            return apiResponse.Data;
        }
        #endregion
    }
}
