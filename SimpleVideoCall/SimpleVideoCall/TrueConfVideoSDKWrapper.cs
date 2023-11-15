using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SimpleVideoCall
{
    public delegate void VideoSDKEventHandler(object source, string response);

    [Serializable]
    public class VideoSDKException : Exception
    {

    }

    internal class TrueConfVideoSDKWrapper : TrueConfVideoSDKInterface
    {
        private string m_ip = "127.0.0.1";
        private int m_port = 0;
        private string m_pin = "";
        private int m_wsPort = 0;
        private int m_httpPort = 0;
        private bool m_debug = false;
        private bool m_Connected = false;

        // Events
        //private event EventHandler<string> m_OnEvent;

        private static object m_consoleLock = new object();
        private const int m_sendChunkSize = 10240;
        private const int m_receiveChunkSize = 10240;
        private const bool m_verbose = true;
        private static readonly TimeSpan m_delay = TimeSpan.FromMilliseconds(1000);

        private ClientWebSocket m_webSocket;
        private CancellationTokenSource m_cancellationTokenSource;

        public bool Connected
        {
            get { return m_Connected; }
        }

        public TrueConfVideoSDKWrapper(bool debug = false)
        {
            this.m_webSocket = new ClientWebSocket();

            //Implementation of timeout of 2000 ms
            this.m_cancellationTokenSource = new CancellationTokenSource();
            this.m_cancellationTokenSource.CancelAfter(2000);

            this.m_debug = debug;


            //Connect($"ws://{this.m_ip}:{this.m_wsPort}").Wait();

            // Events
        }

        ~TrueConfVideoSDKWrapper()
        {

        }

        public bool OpenSession(string ip, int port, string pin)
        {
            this.m_ip = ip;
            this.m_port = port;
            this.m_pin = pin;

            // Init port numbers
            getPorts();
            // Connect to websockets
            WS_Connect($"ws://{this.m_ip}:{this.m_wsPort}");
            // Processing 
            RunProcess();

            return true;
        }

        protected void RunProcess()
        {
            Task.Run(() => WS_ReceiveAsync());
        }

        private void getPorts()
        {
            string config_json_url = $"http://{m_ip}:{m_port}/public/default/config.json";

            // ToDo: have to processing exeptions
            var webRequest = (HttpWebRequest)HttpWebRequest.Create(config_json_url);
            webRequest.UserAgent = "videosdk";

            var response = webRequest.GetResponse();
            var content = response.GetResponseStream();

            using (var reader = new StreamReader(content))
            {
                string strContent = reader.ReadToEnd();

                if (this.m_debug)
                    Console.WriteLine("Complete config.json: {0}", strContent);

                // ToDo: have to processing exeptions
                var json = JObject.Parse(strContent);

                // ToDo: have to processing exeptions
                // Read ports
                this.m_wsPort = (int)json["config"]["websocket"]["port"];
                this.m_httpPort = (int)json["config"]["http"]["port"];
            }
        }

        private void WS_Connect(string url)
        {
            Uri uri = new Uri(url);
            try
            {
                this.m_webSocket.ConnectAsync(uri, m_cancellationTokenSource.Token).Wait();

                if (m_webSocket.State == WebSocketState.Open)
                {
                    if (this.m_debug)
                        Console.WriteLine("Socket connection successfully.");

                    this.WS_Auth().Wait();
                }
                else
                    throw new Exception("WebSocket not opened.");
            }
            catch (Exception e)
            {
                if (this.m_debug)
                    Console.WriteLine($"WS_Connect error: {e.Message}");
                throw e;
            }

        }

        private async Task WS_SendAsync(string data)
        {
            if (m_webSocket.State == WebSocketState.Open)
            {
                ArraySegment<byte> bytesToSend;

                bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));

                if (this.m_debug)
                    Console.WriteLine("Sending: {0}", data);

                await m_webSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, this.m_cancellationTokenSource.Token);
            }
            else
                throw new Exception($"WS_SendData error: WebSocket not opened.");
        }

        private async Task WS_Auth()
        {
            string data;

            if (m_pin == "")
                data = "{\"method\": \"auth\", \"type\": \"unsecured\"}";
            else
                data = "{\"method\": \"auth\", \"type\": \"secured\", \"credentials\": \"" + this.m_pin + "\"}";

            await WS_SendAsync(data);
        }

        private async Task WS_ReceiveAsync()
        {
            byte[] buffer = new byte[m_receiveChunkSize];
            while (m_webSocket.State == WebSocketState.Open)
            {
                var result = await m_webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await m_webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    string response = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // ToDo: have to processing exeptions
                    var json = JObject.Parse(response);

                    //  "method": "event"
                    if (String.Compare("event", (string)json["method"], true) == 0)
                    {
                        // Fire event
                        if (OnEvent != null)
                            OnEvent(this, response);
                    }
                    else
                    {
                        // Fire event
                        if (OnMethod != null)
                            OnMethod(this, response);
                    }

                    if (m_debug)
                        Console.WriteLine("Complete receive: {0}", response);
                }
            }
        }

        // =================================================================================
        // TrueConfVideoSDKInterface
        // =================================================================================
        public void call(string trueconf_id)
        {
            string data = "{\"method\": \"call\", \"peerId\": \"" + trueconf_id + "\"}";
            WS_SendAsync(data).Wait();
        }

        public event EventHandler<string> OnEvent;// { add { m_OnEvent += value; } remove { m_OnEvent -= value; } }
        public event EventHandler<string> OnMethod;// { add { m_OnEvent += value; } remove { m_OnEvent -= value; } }
        // =================================================================================
    }
}
