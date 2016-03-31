using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LogConfigLayer;
using Semafone.Json;
using System.Collections.Concurrent;

namespace Semafone
{

    public class ClientRequest 
    {
        private static ConcurrentDictionary<string, Semafone> SemafoneClients = new ConcurrentDictionary<string, Semafone>();
        private const string ConnectTo = "connecttoapplication";
        private const string Entersecuremode = "enter-secure-mode";
        private const string Exitsecuremode = "exit-secure-mode";
        private const string Resetpan = "reset-pan";
        private const string Resetcvc = "reset-cvc";
        private const string ConnectionCount = "connection-count";
        private const string ConnectionDetail = "connection-detail";
        private const string Dtmf = "dtmf";

        private string _semafonePassword = Semafone.Password;

        public Semafone GetSemafone ( string webSocket)
        {
            return SemafoneClients[webSocket];
        }
        public void SetSemafone(string webSocket,Semafone semafone)
        {
            if (!SemafoneClients.ContainsKey(webSocket))
                SemafoneClients.TryAdd(webSocket, semafone);
        }

        public bool IsSemafoneExist(string webSocket)
        {
            return SemafoneClients.ContainsKey(webSocket) ? (SemafoneClients[webSocket] != null? true : false) : false;
                
        }
        public string ProcessClientMessage(string webSocket, String theMessage)
        {
            try
            {
                #region Ignore invalid json message
                if (!(theMessage.StartsWith("{") && theMessage.EndsWith("}")))
                {
                    TpsLogManager<Semafone>.Error("Error processClientMessage - Invalid json message " + theMessage + " received from " + webSocket);
                    return jsonErrorMessage(webSocket, "1", "Invalid json message", ErrorSeverity.Error);
                }
                #endregion
                try
                {
                    JObject request = new JObject();
                    request = JObject.Parse(theMessage);
                    if (request.First == null)
                    {
                        TpsLogManager<Semafone>.Error("Error processClientMessage - Invalid json message " + theMessage + " received from " + webSocket);
                        return jsonErrorMessage(webSocket, "1", "Invalid json message", ErrorSeverity.Error);
                    }

                    string command = ((Newtonsoft.Json.Linq.JProperty)(request.First)).Name;
                    #region Ignore invalid command
                    if (String.IsNullOrEmpty(command))
                    {
                        TpsLogManager<Semafone>.Error("Error processClientMessage - Invalid json message " + theMessage + " received from " + webSocket);
                        return jsonErrorMessage(webSocket, "1", "Invalid json message", ErrorSeverity.Error);
                    }
                    #endregion
                    return ProcessMessage(webSocket, theMessage, request, command);
                }
                catch (JsonException e)
                {
                    if (String.IsNullOrEmpty(theMessage))
                    {
                        TpsLogManager<Semafone>.Error("Error processClientMessage : Null or empty messsage is received from " + webSocket);
                        return jsonErrorMessage(webSocket, "1", "json error", ErrorSeverity.Error);
                    }
                    TpsLogManager<Semafone>.Error("Error processClientMessage for " + webSocket + ": " + e.Message + e.StackTrace);
                    return jsonErrorMessage(webSocket, "1", e.Message, ErrorSeverity.Error);
                }

            }
            catch (Exception e)
            {
                TpsLogManager<Semafone>.Error("Error processClientMessage : " + e.Message + e.StackTrace);
                return jsonErrorMessage(webSocket, "1", e.Message, ErrorSeverity.Error);
            }
        }

        private string ProcessMessage(string webSocket, String theMessage, JObject request, string command)
        {
            try
            {
                
                #region Parse Json message
                switch (command)
                {
                    case ConnectTo:
                        return ProcessConnectTo(webSocket, request);
                    case Entersecuremode:
                          return  ProcessPciEnterSecureMode(webSocket, request);
                    case Exitsecuremode:
                       return ProcessPciExitSecureMode(webSocket);
                    case Resetpan:
                       return ProcessPciResetPan(webSocket);
                    case Resetcvc:
                        return ProcessPciResetCvc(webSocket);
                    //case Dtmf:
                        //return ProcessDtmfTone(webSocket, request);
                        
                    //case ConnectionCount:
                    //    ProcessConnectionCount(webSocket);
                    //    break;
                    //case ConnectionDetail:
                    //    ProcessConnectionDetail(webSocket);
                    //    break;
                    default:
                        string errorMessage = "Invalid request message received from " + webSocket + ": " + theMessage;
                        TpsLogManager<Semafone>.Error(errorMessage);
                        return jsonErrorMessage(webSocket, "1", "ERROR: Invalid json message", ErrorSeverity.Error);
                }
                //return jsonErrorMessage(webSocket, "1", "ERROR: Invalid json message", ErrorSeverity.Error);
            }
            catch (Exception e)
            {
                TpsLogManager<Semafone>.Error(e.Message);
                return jsonErrorMessage(webSocket, "1", e.Message, ErrorSeverity.Error);
            }
        }

        private string ProcessConnectTo(string webSocket, JObject request)
        {
            return JsonConvert.SerializeObject(new { message = "Semafone connected succesfully" });
        }
        private string ProcessPciEnterSecureMode(string webSocket, JObject request)
        {
            Semafone semafone= null;
            string dpmTarget = "";
            string semafoneUrn = "";
            string enterSecureModeOption = "";
            foreach (var x in request)
            {
                string name = x.Key;
                JToken value = x.Value;
                enterSecureModeOption = value.Value<String>("mode");
                dpmTarget = value.Value<String>("dpmTarget");
                semafoneUrn = value.Value<String>("semafoneUrn");
            }

            try
            {
                if (IsSemafoneExist(webSocket))
                {
                    semafone = GetSemafone(webSocket);
                }
                else
                {

                    if (String.IsNullOrEmpty(semafoneUrn))
                    {
                        return jsonErrorMessage(webSocket, "2", "Request to PCI Enter Secure Mode - semafoneCR missing.", ErrorSeverity.Error);
                        
                    }
                    
                    if (String.IsNullOrEmpty(dpmTarget))
                    {
                        return jsonErrorMessage(webSocket, "3", "Request to PCI Enter Secure Mode - semafoneURL missing.", ErrorSeverity.Error);
                    }
                    #endregion

                    semafone = new Semafone(webSocket, dpmTarget, semafoneUrn, _semafonePassword);
                }


                if (semafone != null)
                {
                    if (semafone.IsInSecureMode)
                    {
                        return jsonErrorMessage(webSocket, "4", "Failed to enter secure mode. Semafone is already in secure mode.", ErrorSeverity.Error);
                    }
                    else
                    {
                        if (!semafone.Login())
                        {

                            if (!semafone.EnterSecureMode(enterSecureModeOption))
                            {
                                semafone.Logout();
                                SetSemafone(webSocket, null);
                                return jsonErrorMessage(webSocket, "5", "Failed to enter secure mode using dpmtarget.", ErrorSeverity.Error);
                            }
                        }
                        else
                        {
                            if (!semafone.EnterSecureMode(enterSecureModeOption))
                            {
                                semafone.Logout();
                                SetSemafone(webSocket, null);
                                return jsonErrorMessage(webSocket, "5", "Failed to enter secure mode using dpmtarget.", ErrorSeverity.Error);
                            }
                        }

                        if (!semafone.ListenForMaskedData())
                        {
                            TpsLogManager<Semafone>.Error("processPCIEnterSecureMode : Failed for websocket "+  webSocket +" to start listen for masked data.");
                            return jsonErrorMessage(webSocket, "6", "Failed to start listen for masked data.", ErrorSeverity.Error);
                        }

                        SetSemafone(webSocket, semafone);
                        SemafoneEnterSecureModeHandset semafoneHandset = new SemafoneEnterSecureModeHandset();
                        semafoneHandset.Semafone = new SemafoneEnterSecureMode() { Enteredsecuremode = true, GenesysId = "", SemafoneUrl = dpmTarget, SemafoneCr = semafoneUrn };
                        string message = JsonConvert.SerializeObject(semafoneHandset);

                        TpsLogManager<Semafone>.Info( "Entered secure mode semafone message has been sent to " + webSocket);
                        TpsLogManager<Semafone>.Info(message);
                        return message;
                    }
                }
                else
                {
                    return jsonErrorMessage(webSocket, "7", "Could not create Semafone instance on " + webSocket, ErrorSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                TpsLogManager<Semafone>.Error(ex.Message);
                return jsonErrorMessage(webSocket, "7", "Could not create Semafone instance on " + webSocket, ErrorSeverity.Error);
            }

        }

        internal string ProcessPciExitSecureMode(string webSocket)
        {
            if (webSocket == null)
            {
                TpsLogManager<Semafone>.Warn("processPCIExitSecureMode : Client has either been disconnected or doesn't exist");
                return "Client has either been disconnected or doesn't exist";
            }
            try
            {
                bool success = false;
                Semafone semafone = GetSemafone(webSocket);
                if (semafone != null)
                {

                    // exit secure mode and logout from Semafone to ensure no more events come through
                    success = semafone.StopListeningForEvents();
                    if (!success)
                        TpsLogManager<Semafone>.Error("processPCIExitSecureMode : Could not stop listening for Semafone events for " + webSocket + ".");
                    success = semafone.ExitSecureMode();
                    if (!success)
                        TpsLogManager<Semafone>.Error("processPCIExitSecureMode : Could not request to exit Semafone secure mode for " + webSocket + ".");
                    success = semafone.Logout();
                    if (!success)
                        TpsLogManager<Semafone>.Error("processPCIExitSecureMode : Could not logout to exit Semafone secure mode for " + webSocket + ".");

                    if (success)

                        return ReturnExitedSecureMode(webSocket);
                    else
                        return jsonErrorMessage(webSocket, "8", "Failed to exit secure mode for " + webSocket + ".", ErrorSeverity.Error);
                }
                else
                    return jsonErrorMessage(webSocket, "8", "Failed to exit secure mode for " + webSocket + ".", ErrorSeverity.Error);
            }
            catch (Exception ex)
            {
                TpsLogManager<Semafone>.Error(ex.Message);
                return jsonErrorMessage(webSocket, "8", "Error during exit secure mode for " + webSocket + ".", ErrorSeverity.Error);
            }
        }

        private string ProcessPciResetPan(string webSocket)
        {
            if (string.IsNullOrEmpty(webSocket))
            {
                TpsLogManager<Semafone>.Warn("processPCIResetPAN : Client has either been disconnected or doesn't exist");
                return "";
            }
            try
            {
                Semafone semafone = GetSemafone(webSocket);
                if (semafone != null)
                {
                    if (semafone.ResetPan())
                    {
                        SemafoneResetPanHandset semafoneHandset = new SemafoneResetPanHandset();
                        semafoneHandset.Semafone = new SemafoneResetPan() { Panreset = true };
                        string message = JsonConvert.SerializeObject(semafoneHandset);

                        TpsLogManager<Semafone>.Info("Reset PAN semafone message has been sent to webSocket " + webSocket);
                        TpsLogManager<Semafone>.Info(message);
                        return message;
                    }
                    else
                    {
                        TpsLogManager<Semafone>.Error("Failed to reset pan for " + webSocket);
                        return jsonErrorMessage(webSocket, "10", "Failed to reset pan for " + webSocket + ".", ErrorSeverity.Error);
                    }
                }
                else
                {
                    TpsLogManager<Semafone>.Error("Failed to reset pan for " + webSocket);
                    return jsonErrorMessage(webSocket, "10", "Failed to reset pan for " + webSocket + ".", ErrorSeverity.Error);
                }

            }
            catch (Exception ex)
            {
                TpsLogManager<Semafone>.Error("Failed to reset pan for " + webSocket + " due to Error: "+ ex.Message);
                return jsonErrorMessage(webSocket, "10", "Failed to reset pan for " + webSocket + ".", ErrorSeverity.Error);

            }
        }

        private string ProcessPciResetCvc(string webSocket)
        {
            if (webSocket == null)
            {
                TpsLogManager<Semafone>.Warn("processPCIResetCVC : Client has either been disconnected or doesn't exist");
                return "";
            }
            try
            {
                Semafone semafone = GetSemafone(webSocket);
                if (semafone != null)
                {
                    if (semafone.ResetCvc())
                    {
                        SemafoneResetCvcHandset semafoneHandset = new SemafoneResetCvcHandset();
                        semafoneHandset.Semafone = new SemafoneResetCvc() { Cvcreset = true };
                        string message = JsonConvert.SerializeObject(semafoneHandset);

                        TpsLogManager<Semafone>.Info("Reset CVC semafone message has been sent to webSocket " + webSocket);
                        TpsLogManager<Semafone>.Info(message);
                        return message;
                    }
                    else
                    {
                        TpsLogManager<Semafone>.Error("Failed to reset cvc for " + webSocket);
                        return jsonErrorMessage(webSocket, "10", "Failed to reset pan for " + webSocket + ".", ErrorSeverity.Error);
                    }
                }
                else
                {
                    TpsLogManager<Semafone>.Error("Failed to reset cvc for " + webSocket);
                    return jsonErrorMessage(webSocket, "10", "Failed to reset pan for " + webSocket + ".", ErrorSeverity.Error);
                }

            }
            catch (Exception ex)
            {
                TpsLogManager<Semafone>.Error("Failed to reset cvc for " + webSocket + " due to Error: " + ex.Message);
                return jsonErrorMessage(webSocket, "10", "Failed to reset pan for " + webSocket + ".", ErrorSeverity.Error);
            }
        }



        //private string ProcessDtmfTone(string webSocket, JObject request)
        //{
        //    if (webSocket == null)
        //    {
        //        TpsLogManager<Semafone>.Warn("processPCIResetCVC : Client has either been disconnected or doesn't exist");
        //        return "";
        //    }
        //    try
        //    {
        //        string tone = "";
        //        string line = "";
        //        foreach (var x in request)
        //        {
        //            foreach (var y in x.Value)
        //            {
        //                var attribute = (Newtonsoft.Json.Linq.JProperty)(y);
        //                switch (attribute.Name)
        //                {
        //                    case "key": tone = Convert.ToString(attribute.Value); break;
        //                    case "line": line = Convert.ToString(attribute.Value); break;
        //                    default:TpsLogManager<Semafone>.Error("ProcessDtmfTone : Invalid dtmf json message received from webSocket " + webSocket);
        //                         return jsonErrorMessage(webSocket, "1", "ERROR: Invalid dtmf json message", ErrorSeverity.Error); ;
        //                }
        //            }
        //        }

        //        if (!Regex.IsMatch(tone, @"^[\#\*\d]{1}$"))
        //        {
        //            TpsLogManager<Semafone>.Error("ProcessDtmfTone : Invalid dtmf character '" + tone + "' received from " + webSocket;
        //            return;
        //        }

        //        if (webSocket.SipServer != null)
        //            switch (line)
        //            {
        //                case "1": webSocket.SipServer.RequestSendDtmf(tone, webSocket.Line1ConnId); break;
        //                case "2": webSocket.SipServer.RequestSendDtmf(tone, webSocket.Line2ConnId); break;
        //                default: new ClientResponse().SendJsonErrorMessage(webSocket, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Failed to submit dtmf tone. Invalid line number.", ErrorSeverity.Error); return;
        //            }
        //        else
        //        {
        //            CLogger.WriteLog(ELogLevel.Error, "ProcessDtmfTone :Invalid request. No Sip server found.");
        //            new ClientResponse().SendJsonErrorMessage(webSocket, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Failed to submit dtmf tone " + ((webSocket.SipServer != null) ? " on extension [" + webSocket.SipServer.Extension + "]" : ""), ErrorSeverity.Error);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        CLogger.WriteLog(ELogLevel.Error, "ProcessDtmfTone: " + e.Message + Environment.NewLine + e.StackTrace);
        //        new ClientResponse().SendJsonErrorMessage(webSocket, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Failed to submit dtmf tone " + ((webSocket.SipServer != null) ? " on extension [" + webSocket.SipServer.Extension + "]" : ""), ErrorSeverity.Error);
        //    }
        //}

        //private void ProcessConnectionCount(string webSocket)
        //{
        //    if (webSocket == null)
        //    {
        //        CLogger.WriteLog(ELogLevel.Warn, "ProcessConnectionCount : Client has either been disconnected or doesn't exist");
        //        return;
        //    }
        //    webSocket.SendMessage(JsonConvert.SerializeObject(new { connections = WebSocketPipe.AgentConnectionsDictionary.Count.ToString() }));

        //    if (webSocket.AgentUserName == null)
        //    {
        //        WebSocketPipe.RemoveConnectionOnWebsocketDisconnect(webSocket);
        //    }
        //}




        //private void ProcessConnectionDetail(string webSocket)
        //{
        //    if (webSocket == null)
        //    {
        //        CLogger.WriteLog(ELogLevel.Warn, "ProcessConnectionDetails : Client has either been disconnected or doesn't exist");
        //        return;
        //    }


        //    List<object> agents = new List<object>();
        //    var numberOfWebsockets = 0;
        //    var numberOfSipConnections = 0;
        //    foreach (var conn in WebSocketPipe.AgentConnectionsDictionary.Keys)
        //    {
        //        if (conn.LastHandset != null)
        //        {
        //            agents.Add(new { ipaddress = conn.Socket.ClientAddressAndPort(), username = conn.AgentUserName, extension = conn.SipServer.Extension, hostname = conn.AgentHostName, status = conn.AgentState.ToString(), line1status = conn.LastHandset.Lines.Line1.LineStatus, line2status = conn.LastHandset.Lines.Line2.LineStatus, agentLoggedIn = conn.AgentLoginDateTime, lastResponseWasFor = conn.LastHandset.Status.Description, lastResponseSent = conn.LastHandset.Status.Datetime, lastRequest = conn.LastActionRequested.ToString() });
        //        }
        //        else
        //        {
        //            agents.Add(new { ipaddress = conn.Socket.ClientAddressAndPort(), username = conn.AgentUserName, extension = "Unknown", hostname = conn.AgentHostName, status = conn.AgentState.ToString(), line1status = "NotAvailable", line2status = "NotAvailable", agentLoggedIn = conn.AgentLoginDateTime, lastResponseWasFor = "Unknown", lastResponseSent = "Unknown", lastRequest = conn.LastActionRequested.ToString() });
        //        }
        //        if (conn.SipServer != null)
        //        {
        //            numberOfSipConnections = numberOfSipConnections + 1;
        //        }
        //        if (conn.Socket != null)
        //        {
        //            numberOfWebsockets = numberOfWebsockets + 1;
        //        }
        //    }
        //    webSocket.SendMessage(JsonConvert.SerializeObject(new { application = Softphone.Server.ConfigServer.ConfigServerInstance.GetCFGApplicationObject().Name, Agents = agents, SipConnections = numberOfSipConnections, WebSocketConnections = numberOfWebsockets }));

        //    if (webSocket.AgentUserName == null)
        //    {
        //        WebSocketPipe.RemoveConnectionOnWebsocketDisconnect(webSocket);
        //    }
        //}


        public string GetSemafonePassword()
        {
            return _semafonePassword;

        }

        internal string ReturnExitedSecureMode(string webSocket)
        {
            if (webSocket == null)
            {
                TpsLogManager<Semafone>.Warn("sendExitedSecureMode : Client has either been disconnected or doesn't exist for " + webSocket + ".");
                return jsonErrorMessage(webSocket, "9", "Client has either been disconnected or doesn't exist for " + webSocket + ".",ErrorSeverity.Error);
            }
            try
            {
                SemafoneExitSecureModeHandset semafoneHandset = new SemafoneExitSecureModeHandset();
                semafoneHandset.Semafone = new SemafoneExitSecureMode() { Exitsecuremode = true };
                string message = JsonConvert.SerializeObject(semafoneHandset);
                Semafone semaphoneInstance; 
                SemafoneClients.TryRemove(webSocket, out semaphoneInstance);
                TpsLogManager<Semafone>.Info("Exit secure mode semafone message has been sent for " +  webSocket);
                TpsLogManager<Semafone>.Info(message);
                return message;
            }
            catch (Exception e)
            {
                TpsLogManager<Semafone>.Error("Error sendExitedSecureMode : " + e.Message + e.StackTrace);
                return jsonErrorMessage(webSocket, "8", "Error during ReturnExitedSecureMode for " + webSocket + ".", ErrorSeverity.Error);
            }
        }

        private string jsonErrorMessage(string webSocket, string errorCode, string message, ErrorSeverity severity, string label = "", bool sendMessage = true)
        {
            try
            {
                #region Create Errorobject

                ErrorObject error = new ErrorObject
                {
                    Id = SemafoneConnection.GetNextResponseId(),
                    Datetime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"),
                    ErrorSeverity = severity.ToString(),
                    ErrorLabel = label,
                    ErrorCode = Convert.ToInt32(errorCode),
                    ErrorMessage = message
                };
                ErrorResponse errorResponseMessage = new ErrorResponse(error);
                #endregion
                string errorJson = JsonConvert.SerializeObject(errorResponseMessage);
                TpsLogManager<Semafone>.Error(message +" error message sent to " + webSocket);
                return errorJson;
            }
            catch (Exception e)
            {
                TpsLogManager<Semafone>.Error("Error jsonErrorMessage : " + e.Message + e.StackTrace);
                return e.Message;
            }
        }

    }
}

