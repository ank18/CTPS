using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Fleck;
using LogConfigLayer;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TPSConnectionManager
{
    public class WebSocketPipe
    {
        #region private fields
        private const String DEFAULT_DATE_TIME_FORMAT = "MM/dd/yyyy HH:mm:ss.fff";
        private static int maximumConcurrentConnections = 500;
        private static int alarmBeforeRemainingConnectionCount = 5;
        private string hostname = string.Empty;
        #endregion

        /// <summary>
        /// Store connections of online users.  
        /// </summary>

        public static ConcurrentDictionary<string, IWebSocketConnection> webSocketConnections = new ConcurrentDictionary<string, IWebSocketConnection>();
        public static ConcurrentDictionary<string, string> applicationConnections = new ConcurrentDictionary<string, string>();

        public WebSocketServer server;
        public WebSocketPipe()
        {

            try
            {
                if (Properties.Settings.Default.WebSocketConnectionString == null)
                {
                    TpsLogManager<WebSocketPipe>.Error("Error on WebSocketPipe : websocketconnectionstring not configured in cme.");
                    return;
                }
                //var uri = new Uri(Properties.Settings.Default.WebSocketConnectionString.ToString());
                
                //var ips = Dns.GetHostAddresses(uri.DnsSafeHost);
                //IPAddress ipToUse = null;
                //foreach (var ip in ips)
                //{
                //    if (ip.AddressFamily == AddressFamily.InterNetwork)
                //    {
                //        ipToUse = ip;
                //        break;
                //    }
                //}
                //server = new Fleck.WebSocketServer(uri.Port, Properties.Settings.Default.WebSocketConnectionString.ToString());
                server = new Fleck.WebSocketServer(Properties.Settings.Default.WebSocketConnectionString.ToString());

                //if (Properties.Settings.Default.UseSecureWebSockets.ToLower().Equals("true"))
                //{
                //    var webSocketCertPath = Properties.Settings.Default.WebSocketCertificatePath;
                //    var webSocketCertPassword = Properties.Settings.Default.WebSocketCertificatePassword.Decrypt();
                //    if (!string.IsNullOrWhiteSpace(webSocketCertPath))
                //    {
                //        server.Certificate = string.IsNullOrEmpty(webSocketCertPassword)
                //            ? new X509Certificate2(webSocketCertPath)
                //            : new X509Certificate2(webSocketCertPath, webSocketCertPassword);
                //    }
                //}
                server.Start(socket =>
                {
                    socket.OnOpen = () => { OnConnect(socket); };
                    socket.OnClose = () => { OnDisconnect(socket); };
                    socket.OnMessage = message => { OnReceive(socket, message); };
                    socket.OnError = exception => { OnError(socket, exception); };
                });
                //maximumConcurrentConnections = CMEDatabase.CMEDatabaseInstance.getMaximumWebSocketConcurrentConnections();
            }
            catch (Exception ex)
            {
                TpsLogManager<WebSocketPipe>.Error("Error on WebSocketPipe : " + ex.Message + ex.StackTrace);
            }


        }

        public void OnError(IWebSocketConnection socket, Exception message)
        {
            try
            {
                TpsLogManager<WebSocketPipe>.Error("Error on ClientConnectionServer.OnError : " + message.InnerException != null ? message.InnerException.Message : message.Message);
                ////check frorced client disconnect and then remove connection?? yes no??
                //if (message.GetType() == typeof(System.IO.IOException)
                //    || (message.InnerException.Message == "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host")
                //    || message.InnerException.GetType() == typeof(System.Net.Sockets.SocketException)
                //    || (message.InnerException.Message == "An existing connection was forcibly closed by the remote host"))
                OnDisconnect(socket);
            }
            catch (Exception ex)
            {
                TpsLogManager<WebSocketPipe>.Error("Error on WebSocketPipe.OnError : " + ex.Message + ex.StackTrace);
            }
        }

        public void OnConnect(IWebSocketConnection socket)
        {
            try
            {
                webSocketConnections.TryAdd(socket.ClientAddressAndPort(),socket);
                Console.WriteLine("New websocket connection added for browser :" + socket.ClientAddressAndPort());
                TpsLogManager<WebSocketPipe>.Info("New websocket connection added for browser : " + socket.ClientAddressAndPort());

            }
            catch (Exception ex)
            {
                TpsLogManager<WebSocketPipe>.Error("Error on WebSocketPipe.OnConnect : " + ex.Message + ex.StackTrace);
            }
        }

        public static void logStats()
        {
            TpsLogManager<WebSocketPipe>.Info("Current Websocket connection statistics");
            TpsLogManager<WebSocketPipe>.Info("Websocket maximumConcurrentConnections = " + maximumConcurrentConnections);
            TpsLogManager<WebSocketPipe>.Info("Websocket connection count = " + webSocketConnections.Count());
        }

        /// <summary>
        /// Event fired when a data is received from the Alchemy Websockets server instance.
        /// Parses data as JSON and calls the appropriate message or sends an error message.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static void OnReceive(IWebSocketConnection socket, string message)
        {
            Console.WriteLine("Received Data From :" + socket.ClientAddressAndPort());
            try
            {
                try
                {
                    string appName = "";
                    if (applicationConnections.ContainsKey(socket.ClientAddressAndPort()))
                    {
                        appName = applicationConnections[socket.ClientAddressAndPort()];
                        foreach (var ConnectToApplication in TPService.Instance.ConnectToApplications)
                        {
                            ConnectToApplication.WebSocketSendMessageEvent -= OnWebSocketSendMessageEvent;
                            ConnectToApplication.WebSocketSendMessageEvent += OnWebSocketSendMessageEvent;
                            ConnectToApplication.ProcessWebSocketMessage(appName, message, socket.ClientAddressAndPort());
                        }
                    }
                    else
                    {
                        if (isConnectToCommand(socket, message, out appName))
                        {
                            TpsLogManager<WebSocketPipe>.Info("New connection added for " + appName + " on " + socket.ClientAddressAndPort());
                            Console.WriteLine("New connection added for " + appName + " on " + socket.ClientAddressAndPort());
                            foreach (var ConnectToApplication in TPService.Instance.ConnectToApplications)
                            {
                                ConnectToApplication.WebSocketSendMessageEvent -= OnWebSocketSendMessageEvent;
                                ConnectToApplication.WebSocketSendMessageEvent += OnWebSocketSendMessageEvent;
                                ConnectToApplication.ProcessWebSocketMessage(appName, message, socket.ClientAddressAndPort());
                            }
                        }
                        else
                        {
                            Console.WriteLine("Unsuccessful attempt by " + socket.ClientAddressAndPort() + " : " + message);
                            socket.Send(JsonConvert.SerializeObject(new { Error = " Invalid json message" }));
                        }
                    }
                }
                catch (Exception e)
                {
                    TpsLogManager<WebSocketPipe>.Error("Error OnReceive : " + e.Message + e.StackTrace);
                    socket.Send(JsonConvert.SerializeObject(new { Error = e.Message }));
                }
                
            }
            catch (Exception ex)
            {
                TpsLogManager<WebSocketPipe>.Error("Error on WebSocketPipe.OnReceive : " + ex.Message + ex.StackTrace, ex);
                socket.Send(JsonConvert.SerializeObject(new { Error = ex.Message }));
            }
        }

        static void OnWebSocketSendMessageEvent(string socket, string message)
        {
            var webSocket = webSocketConnections.FirstOrDefault(x => x.Key == socket).Value;
            if (webSocket != null)
            {
                webSocket.Send(message);
                TpsLogManager<WebSocketPipe>.Debug("Message sent to " + webSocket.ClientAddressAndPort() + " : " + message);
                Console.WriteLine("Message sent to " + webSocket.ClientAddressAndPort() + " : " + message);
            }
        }

        public void OnDisconnect(IWebSocketConnection socket)
        {
            try
            {
                DisconnectWebsocket(socket);

            }
            catch (Exception ex)
            {
                TpsLogManager<WebSocketPipe>.Error("Error on WebSocketPipe.OnDisconnect " + socket.ClientAddressAndPort() + " : " + ex.Message + ex.StackTrace, ex);
            }
        }

        private void DisconnectWebsocket(IWebSocketConnection socket)
        {
            string applicationName = "";
            if (applicationConnections.ContainsKey(socket.ClientAddressAndPort()))
            {
                applicationName = applicationConnections[socket.ClientAddressAndPort()];
                applicationConnections.TryRemove(socket.ClientAddressAndPort(), out applicationName);
                TpsLogManager<WebSocketPipe>.Info("Connection removed for " + applicationName + " on " + socket.ClientAddressAndPort());
                Console.WriteLine("connection removed for " + applicationName + " on " + socket.ClientAddressAndPort());
            }
            webSocketConnections.TryRemove(socket.ClientAddressAndPort(), out socket);
            Console.WriteLine("Websocket client disconnected : " + socket.ClientAddressAndPort());
            TpsLogManager<WebSocketPipe>.Info("Websocket client disconnected : " + socket.ClientAddressAndPort());
        }

        private static  bool isConnectToCommand(IWebSocketConnection socket,string message,out string applicationName)
        {
            applicationName = "";
            #region Ignore invalid json message
            if (!(message.StartsWith("{") && message.EndsWith("}")))
            {
                TpsLogManager<WebSocketPipe>.Error("Error OnReceive - Invalid json message " + message + " received from " + socket.ClientAddressAndPort());
                socket.Send(JsonConvert.SerializeObject(new { Error = " Invalid json message" }));
                return false;
            }
            #endregion
            try
            {
                JObject request = new JObject();
                request = JObject.Parse(message);
                if (request.First == null)
                {
                    TpsLogManager<WebSocketPipe>.Error("Error OnReceive - Invalid json message " + message + " received from " + socket.ClientAddressAndPort());
                    socket.Send(JsonConvert.SerializeObject(new { Error = " Invalid json message" }));
                    return false;
                }

                string command = ((Newtonsoft.Json.Linq.JProperty)(request.First)).Name;
                #region Ignore invalid command
                if (String.IsNullOrEmpty(command))
                {
                    TpsLogManager<WebSocketPipe>.Error("Error OnReceive - Invalid json message " + message + " received from " + socket.ClientAddressAndPort());
                    socket.Send(JsonConvert.SerializeObject(new { Error = " Invalid json message" }));
                    return false;

                }
                #endregion
                if (command== "connecttoapplication")
                {
                    foreach (var x in request)
                    {
                        string name = x.Key;
                        JToken value = x.Value;
                        applicationName = value.Value<String>("appname");
                    }
                    applicationConnections.TryAdd(socket.ClientAddressAndPort(), applicationName);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                if (String.IsNullOrEmpty(message))
                {
                    TpsLogManager<WebSocketPipe>.Error("Error OnReceive : Null or empty messsage is received from " + socket.ClientAddressAndPort());
                    socket.Send(JsonConvert.SerializeObject(new { Error = " Invalid json message" }));
                    return false;
                }
                TpsLogManager<WebSocketPipe>.Error("Error OnReceive for " + socket.ClientAddressAndPort() + ": " + e.Message + e.StackTrace);
                socket.Send(JsonConvert.SerializeObject(new { Error = e.Message }));
                return false;
            }
        }
        
    }
}
