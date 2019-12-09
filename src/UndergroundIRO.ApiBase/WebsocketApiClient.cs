using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace UndergroundIRO.ApiBase
{
    public class WebsocketApiClient : IDisposable
    {
        public string Id { get; }
        public string BasePath => Configuration.BasePath;
        public WebsocketClient Client { get; }
        public int TotalRequestsCount { get; private set; }

        protected WebsocketConfiguration Configuration { get; }
        protected JsonSerializerSettings JsonSettings { get; set; } = new JsonSerializerSettings();
        protected ILogger Log { get; }
        protected LogLevel CurrentLogLevel => Configuration.LogLevel;

        public event Action<WebsocketApiClient, ResponseMessage, Exception> ReceivedMessageHandlerExceptionRised;
        public event Action<WebsocketApiClient, ResponseMessage> ReceivedMessage;

        protected WebsocketApiClient(WebsocketConfiguration conf)
        {
            Id = Guid.NewGuid().ToString().Remove(6);
            Configuration = conf ?? throw new ArgumentNullException(nameof(conf));
            if (conf.LoggerFactory == null)
            {
                throw new NullReferenceException("conf.LoggerFactory");
            }
            Log = conf.LoggerFactory.CreateLogger(GetType());
            Client = new WebsocketClient(new Uri(Configuration.BasePath))
            {
                ErrorReconnectTimeoutMs = Configuration.ErrorConnectionTimeoutMS
            };
            Client.MessageReceived.Subscribe(BasicMessageReceiver);
        }

        public async Task Start()
        {
            await Client.Start();
        }
        public async Task Reconnect()
        {
            await Client.Reconnect();
        }

        public async Task Stop()
        {
            await Client.Stop(WebSocketCloseStatus.NormalClosure, "disconnected");
        }

        /// <summary>
        /// Override this to receive messages.
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void ReceivedMessageHandler(ResponseMessage msg)
        {
        }

        void BasicMessageReceiver(ResponseMessage msg)
        {
            try
            {
                var currentMsgNum = TotalRequestsCount++;
                if (CurrentLogLevel <= LogLevel.Information)
                {
                    var logDict = new Dictionary<string, object>()
                    {
                        {"Type", msg.MessageType},
                        {"Text", msg.Text}
                    };
                    var logJson = JsonConvert.SerializeObject(logDict, Formatting.Indented);
                    Log.LogInformation($"WebsocketMsg #{currentMsgNum}: Message received. {logJson}");
                }
                ReceivedMessage?.Invoke(this, msg);
                ReceivedMessageHandler(msg);
            }
            catch (Exception ex)
            {
                Log.LogError($"Error handling websocket received message: '{ex}'.");
                ReceivedMessageHandlerExceptionRised?.Invoke(this, msg, ex);
            }
        }

        public void Dispose()
        {
            Client.Dispose();
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

        protected string RemoveJsonStringBrackets(string json)
        {
            if (json.StartsWith("\"") && json.EndsWith("\""))
            {
                json = json.Substring(1);
                json = json.Remove(json.Length - 1);
            }
            return json;
        }
    }
}
