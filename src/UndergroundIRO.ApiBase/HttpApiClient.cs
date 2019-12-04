using ApiBase.Models;
using ApiBase.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ApiBase
{
    public partial class HttpApiClient
    {
        public string Id { get; }
        public int TotalRequestsCount { get; private set; }
        public string BasePath => Configuration.BasePath;
        protected Configuration Configuration { get; }
        protected JsonSerializerSettings JsonSettings { get; set; } = new JsonSerializerSettings();
        protected ILogger Log { get; }
        protected LogLevel CurrentLogLevel => Configuration.LogLevel;
        protected HttpClient HttpClient { get; } = new HttpClient();
        readonly IExceptionsFactory _exceptionsFactory;

        public HttpApiClient(Configuration conf)
        {
            Id = Guid.NewGuid().ToString().Remove(6);
            Configuration = conf ?? throw new ArgumentNullException(nameof(conf));
            if (conf.LoggerFactory == null)
            {
                throw new NullReferenceException("conf.LoggerFactory");
            }
            Log = conf.LoggerFactory.CreateLogger(GetType());
            _timeoutQueue = conf.TimeoutQueue ?? throw new NullReferenceException("conf.TimeoutQueue");
            _exceptionsFactory = conf.ExceptionsFactory ?? throw new NullReferenceException("conf.ExceptionsFactory");
            UseTimeoutQueue = conf.UseTimeoutQueue;
        }

        protected async Task<HttpResponseMessage> CallApiAsync(
            string relativeUrl,
            HttpMethod method,
            string contentType = null,
            IDictionary<string, string> queryParams = null,
            string postBody = null,
            IDictionary<string, string> headerParams = null,
            IDictionary<string, string> formParams = null,
            IDictionary<string, string> pathParams = null
            )
        {
            var absoluteUrl = RelativeUrlToAbsolute(relativeUrl);
            var request = PrepareRequest(
                absoluteUrl,
                method,
                contentType,
                queryParams,
                postBody,
                headerParams,
                formParams,
                pathParams
                );
            InterceptRequest(request);
            TotalRequestsCount++;
            //Logging.
            if (CurrentLogLevel <= LogLevel.Information)
            {
                var logStr = $"{nameof(HttpApiClient)} '{Id}' will send request '{TotalRequestsCount}':\n";
                var requestLogDto = new RequestLogSerializeDto(
                    absoluteUrl,
                    method,
                    contentType,
                    queryParams,
                    postBody,
                    headerParams,
                    formParams,
                    pathParams
                    );
                logStr += requestLogDto.ToString();
                Log.LogInformation(logStr);
            }
            if (CurrentLogLevel <= LogLevel.Information)
            {

                var logStr = $"Request number '{TotalRequestsCount}' parameters is :\n";
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
            ThrowIfResponseError(request, response);
            InterceptResponse(request, response);
            return response;
        }

        /// <summary>
        /// Will throw exception if can't.
        /// </summary>
        protected virtual async Task<ApiResponse<T>> ResolveApiResponse<T>(HttpResponseMessage httpResponse, JsonSerializerSettings settings = null)
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
                var jToken = Deserialize(content, settings);
                ThrowIfResponseError(jToken);
                var data = jToken.ToObject<T>();
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

        protected string RelativeUrlToAbsolute(string relativeUrl)
        {
            if (relativeUrl.StartsWith("https://") || relativeUrl.StartsWith("http://"))
            {
                return relativeUrl;
            }
            if (BasePath.EndsWith("/") && relativeUrl.StartsWith("/"))
            {
                relativeUrl = relativeUrl.Substring(1);
            }
            return BasePath + relativeUrl;
        }

        protected virtual JToken Deserialize(string str, JsonSerializerSettings settings = null)
        {
            settings = settings ?? JsonSettings;
            return JsonConvert.DeserializeObject<JToken>(str, settings);
        }

        protected virtual string Serialize(object obj, JsonSerializerSettings settings = null)
        {
            settings = settings ?? JsonSettings;
            return JsonConvert.SerializeObject(obj, settings);
        }

        protected IDictionary<string, string> ParametersToDict(object obj, JsonSerializerSettings settings = null)
        {
            var dict = new Dictionary<string, string>();
            var jObject = (obj as JObject) ?? JObject.FromObject(obj);
            foreach (var prop in jObject)
            {
                dict[prop.Key] = ParameterToString(prop, settings);
            }
            return dict;
        }

        protected string ParameterToString(object obj, JsonSerializerSettings settings = null)
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

        protected void ThrowIfResponseError(HttpRequestMessage request, HttpResponseMessage response)
        {
            var ex = _exceptionsFactory.CheckHttpResponse(request, response);
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

        protected void ThrowIfResponseError(JToken responseJToken)
        {
            var ex = _exceptionsFactory.CheckJTokenResponse(responseJToken);
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

        protected string RemoveJsonStringBrackets(string json)
        {
            if (json.StartsWith("\"") && json.EndsWith("\""))
            {
                json = json.Substring(1);
                json = json.Remove(json.Length - 1);
            }
            return json;
        }

        #region Timeout queue.
        readonly ITimeoutQueue _timeoutQueue;

        public TimeSpan RequestsTimeout => _timeoutQueue.Timeout;

        /// <summary>
        /// If true - will execute each request in timeout queue.
        /// </summary>
        public bool UseTimeoutQueue { get; }

        /// <summary>
        /// For each request in TimeoutQueue with position bigger than this value will be throwed exception.
        /// </summary>
        public int QueuePendingRequestLimit => _timeoutQueue.QueuePendingItemLimit;

        protected virtual async Task InQueue(Func<Task> func)
        {
            await _timeoutQueue.Execute(func);
        }

        protected virtual async Task<TResult> InQueue<TResult>(Func<Task<TResult>> func)
        {
            return await _timeoutQueue.Execute<TResult>(func);
        }

        /// <summary>
        /// If <see cref="UseTimeoutQueue"/> is false - execute without queue.
        /// </summary>
        protected virtual async Task InQueueIfNeed(Func<Task> func)
        {
            if (UseTimeoutQueue)
            {
                await _timeoutQueue.Execute(func);
            }
            else
            {
                await func();
            }
        }

        /// <summary>
        /// If <see cref="UseTimeoutQueue"/> is false - execute without queue.
        /// </summary>
        protected virtual async Task<TResult> InQueueIfNeed<TResult>(Func<Task<TResult>> func)
        {
            if (UseTimeoutQueue)
            {
                return await _timeoutQueue.Execute<TResult>(func);
            }
            else
            {
                return await func();
            }
        }
        #endregion
    }
}
