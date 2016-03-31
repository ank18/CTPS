using System;
using System.Configuration;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Threading;
using Semafone.Client;
using Semafone.Client.Interface;
using Semafone.Client.Payload;
using LogConfigLayer;
using Semafone.Json;
using System.Collections.Generic;

namespace Semafone
{
    public class Semafone
    {
        
        private ISystemClient _systemClient = new SystemClientHttp();
        private ITelephonyClient _telephonyClient = new TelephonyClientHttp();
        private ISemafoneClientSession ClientSession { get; set; }
        private ISecureDataClient _secureDataClient;
        private ISecureDataSession _secureDataSession;
        private bool _isListeningForEvents = false;
        ICreditCardSecureDataClient _creditCard;
        private readonly string _webSocket;
        private readonly string _dpmTarget;
        private readonly string _semafoneUrn;
        private bool _inCvc = false;
        public int DpmTargetUsed;
        public static string Accountid = "5555";
        public static string Clientid = "2";
        public static string DPMTargetPingAttemptsBeforeDeclareFailure = "3";
        public static string Password = "Password";
        public static string Principle = "dev1";
        public static string Tenantid = "A";
        public static string WaitInMillisecondsBeforeNextDPMTargetPing = "2000";

        public bool IsInSecureMode { get; set; }
        
        public Semafone( string websocket, string dpmt, string surn, string password)
        {
            _dpmTarget = dpmt;
            _semafoneUrn = surn;
            _webSocket = websocket;
            Accountid = SemafoneConnection.SemafoneConnectionInstance.ConfigServer.GetSpecificObjectValue(SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp, "CFGApplication", "accountId", false);
            Clientid = SemafoneConnection.SemafoneConnectionInstance.ConfigServer.GetSpecificObjectValue(SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp, "CFGApplication", "clientId", false);
            Password = SemafoneConnection.SemafoneConnectionInstance.ConfigServer.GetSpecificObjectValue(SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp, "CFGApplication", "password", false);
            Principle = SemafoneConnection.SemafoneConnectionInstance.ConfigServer.GetSpecificObjectValue(SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp, "CFGApplication", "principle", false);
            Tenantid = SemafoneConnection.SemafoneConnectionInstance.ConfigServer.GetSpecificObjectValue(SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp, "CFGApplication", "tenantId", false);
            DPMTargetPingAttemptsBeforeDeclareFailure = SemafoneConnection.SemafoneConnectionInstance.ConfigServer.GetSpecificObjectValue(SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp, "CFGApplication", "DPMTargetPingAttemptsBeforeDeclareFailure", false);
            ConfigurationManager.AppSettings.Set("accountId", Accountid);
            ConfigurationManager.AppSettings.Set("clientId", Clientid);
            ConfigurationManager.AppSettings.Set("password", Password);
            ConfigurationManager.AppSettings.Set("principle", Principle);
            ConfigurationManager.AppSettings.Set("tenantId", Tenantid);
            ClientSession = new SemafoneClientSessionImpl();
            //if ((ConfigServer.ConfigServerInstance.PrimaryDpmTarget.Length > 5) && (dpmt.Contains(ConfigServer.ConfigServerInstance.PrimaryDpmTarget.Substring(ConfigServer.ConfigServerInstance.PrimaryDpmTarget.IndexOf(@"//") + 2, ConfigServer.ConfigServerInstance.PrimaryDpmTarget.IndexOf(".") - ConfigServer.ConfigServerInstance.PrimaryDpmTarget.IndexOf(@"//") - 2))))
            //    DpmTargetUsed = 1;
            //else
            //    if ((ConfigServer.ConfigServerInstance.SecondaryDpmTarget.Length > 5) && (dpmt.Contains(ConfigServer.ConfigServerInstance.SecondaryDpmTarget.Substring(ConfigServer.ConfigServerInstance.SecondaryDpmTarget.IndexOf(@"//") + 2, ConfigServer.ConfigServerInstance.SecondaryDpmTarget.IndexOf(".") - 1 - ConfigServer.ConfigServerInstance.SecondaryDpmTarget.IndexOf(@"//")))))
            //    DpmTargetUsed = 2;
            //else
            //    DpmTargetUsed = 0;
            IsInSecureMode = false;
        }

        public string GetUrn()
        {
            return _semafoneUrn;
        }

        public bool Login()
        {
            AuthenticationState authentication = AuthenticationState.Unknown;

            _systemClient = new SystemClientHttp(_dpmTarget);
            TpsLogManager<Semafone>.Debug("Created SystemClientHttp using" + _dpmTarget +" for " +  _webSocket);
            _telephonyClient = new TelephonyClientHttp(_dpmTarget);
            TpsLogManager<Semafone>.Debug("Created TelephonyClientHttp using" + _dpmTarget + " for " + _webSocket);
            _secureDataClient = new SecureDataClientHttp(_dpmTarget);
            TpsLogManager<Semafone>.Debug("Created SecureDataClientHttp using" + _dpmTarget + " for " + _webSocket);

            try
            {
                // Attempt to login to the DPM (Semafone), the SemafoneClientSession reference
                // passed in will be filled with our session information
                authentication = _systemClient.Login(ClientSession);
            }
            catch (SemafoneClientException e)
            {
                TpsLogManager<Semafone>.Error("Failed to login to Semafone" + " for " + _webSocket + ": " + e.Message + e.StackTrace);
                if (e.Error != null)
                {
                    TpsLogManager<Semafone>.Error("Failed to EnterSecureMode : " + e.Error.cause);
                }
            }

            try
            {
                if (authentication != AuthenticationState.Authenticated)
                {
                    // If we failed to login, we cannot proceed
                    TpsLogManager<Semafone>.Error("Not authenticated, cannot proceed");
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                TpsLogManager<Semafone>.Error("Failed to login to Semafone: Checking Authentication state :  " + e.Message + e.StackTrace);
            }
            return false;
        }

        public bool Logout()
        {
            bool success = false;

            try
            {
                success = _systemClient.Logout(ClientSession);
            }
            catch (SemafoneClientException e)
            {
                TpsLogManager<Semafone>.Error("Failed to logout of Semafone");
                if (e.Error != null)
                {
                    TpsLogManager<Semafone>.Error("Failed to Logout : " + e.Error.cause);
                }
                return success;
            }

            TpsLogManager<Semafone>.Debug("Logout of Semafone = " + success.ToString());
            return success;
        }

        public bool ListenForMaskedData()
        {
            TpsLogManager<Semafone>.Debug("Enter listening for events");

            if (_secureDataClient.IsCallEstablished(_secureDataSession))
            {
                ClientSession.Notification += OnEvent;
                if (ClientSession.Listener == null)
                {
                    // One Listener per Client Session -  Create Listener & connect to Queue & Update DPM Session with notification request
                    _isListeningForEvents = _secureDataClient.ListenForEvents(ClientSession, _secureDataSession);
                    if (!_isListeningForEvents)
                    {
                        TpsLogManager<Semafone>.Debug("Listening for events is false");
                    }
                }
                else
                {
                    // Update DPM Session with notification request
                    _isListeningForEvents = _secureDataClient.SubscribeToNotificationService(_secureDataSession, false, null);
                }
            }

            return true;
        }

        public void RemoveListnerForMaskedData()
        {
            this.ClientSession.Notification -= OnEvent;
        }

        public bool StopListeningForEvents()
        {
            try {
                if (_secureDataClient != null)
                {
                    _isListeningForEvents = _secureDataClient.StopListeningForEvents(ClientSession);
                    return _isListeningForEvents;
                }
                else
                    return false;
            }
            catch(Exception ex)
            {
                TpsLogManager<Semafone>.Error("Error in StopListeningForEvents: " + ex.Message );
                return false;
            }
        }

        public bool EnterSecureMode(string mode)
        {
            if (string.IsNullOrEmpty(Accountid))
            {
                TpsLogManager<Semafone>.Error("Failed to connect to Semafone: 'accountId' key is not defined in CME application object named " + SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp);
                Console.WriteLine("Failed to EnterSecureMode to Semafone: 'accountId' key is not defined in CME application object named " + SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp);
                return false;
            }
            if (string.IsNullOrEmpty(Clientid))
            {
                TpsLogManager<Semafone>.Error("Failed to connect to Semafone: 'clientId' key is not defined in CME application object named " + SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp);
                Console.WriteLine("Failed to EnterSecureMode to Semafone: 'clientId' key is not defined in CME application object named " + SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp);
                return false;
            }

            if (string.IsNullOrEmpty(Password))
            {
                TpsLogManager<Semafone>.Error("Failed to connect to Semafone: 'password' key is not defined in CME application object named " + SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp);
                Console.WriteLine("Failed to EnterSecureMode to Semafone: 'password' key is not defined in CME application object named " + SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp);
                return false;
            }

            if (string.IsNullOrEmpty(Principle))
            {
                TpsLogManager<Semafone>.Error("Failed to connect to Semafone: 'principle' key is not defined in CME application object named " + SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp);
                Console.WriteLine("Failed to EnterSecureMode to Semafone: 'principle' key is not defined in CME application object named " + SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp);
                return false;
            }

            if (string.IsNullOrEmpty(Principle))
            {
                TpsLogManager<Semafone>.Error("Failed to connect to Semafone: 'tenantId' key is not defined in CME application object named " + SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp);
                Console.WriteLine("Failed to EnterSecureMode to Semafone: 'tenantId' key is not defined in CME application object named " + SemafoneConnection.SemafoneConnectionInstance.ClientConnectedToCMEApp);
                return false;
            }
            TpsLogManager<Semafone>.Debug("EnterSecureMode - semafoneURN: " + _semafoneUrn + Environment.NewLine + "dpmTarget : " + _dpmTarget + Environment.NewLine + "tenantId : " + Semafone.Tenantid + Environment.NewLine + "clientId : " + Semafone.Clientid + Environment.NewLine + "accountId : " + Semafone.Accountid + Environment.NewLine + "principle : " + Semafone.Principle);

            _secureDataSession = new SecureDataSessionImpl(ClientSession) { Csr = { cr = _semafoneUrn } };
            TpsLogManager<Semafone>.Debug("SecureDataSession : " + _secureDataSession.ToString());

            try
            {
                _secureDataClient.EnterSecureMode(_secureDataSession);
            }
            catch (SemafoneClientException e)
            {
                TpsLogManager<Semafone>.Error("Failed to EnterSecureMode : " + e.Message + e.StackTrace);
                if (e.Error != null)
                {
                    TpsLogManager<Semafone>.Error("Failed to EnterSecureMode : " + e.Error.cause);
                }
            }

            var success = false;
            if (!_secureDataClient.IsInSecureMode(_secureDataSession))
            {
                TpsLogManager<Semafone>.Error("Failed to EnterSecureMode");
            }
            else
            {
                _creditCard = new CreditCardSecureDataImpl(_secureDataClient);
                switch (mode)
                {
                    case "panonly":
                        _creditCard.EnablePan(_secureDataSession);
                        _creditCard.DisableCvc(_secureDataSession);
                        break;
                    case "cvconly":
                        _creditCard.DisablePan(_secureDataSession);
                        _creditCard.EnableCvc(_secureDataSession);
                        break;
                    default:
                        break;
                }

                success = true;
                IsInSecureMode = true;
            }
            return success;
        }

        public bool ExitSecureMode()
        {
            bool exit = false;
            try
            {
                exit = _secureDataClient.ExitSecureMode(_secureDataSession);
            }
            catch (Exception ex)
            {
                TpsLogManager<Semafone>.Error("Failed to ExitSecureMode : " + ex.Message);
            }
            finally
            {
                IsInSecureMode = false;
            }
            return exit;
        }

        public bool ResetPan()
        {
            var success = false;
            try
            {
                if (_inCvc)
                {
                    _creditCard.Reset(_secureDataSession);
                    _inCvc = false;
                }
                else _creditCard.ResetPan(_secureDataSession);
                success = true;
            }
            catch (Exception ex)
            {
                TpsLogManager<Semafone>.Error ("Failed to ResetPAN : " + ex.Message + ex.StackTrace);
            }
            return success;
        }

        public bool ResetCvc()
        {
            var success = false;
            try
            {
                _creditCard.ResetCvc(_secureDataSession);
                success = true;
            }
            catch (Exception ex)
            {
                TpsLogManager<Semafone>.Error( "Failed to ResetCVC : " + ex.Message + ex.StackTrace);
            }
            return success;
        }


        /// <summary>
        /// When we subscribe to events, this method will be called
        /// </summary>
        /// <param name="notificationEvent"></param>
        private void OnEvent(INotificationCallbackEvent notificationEvent)
        {
            foreach (ElementSessionType element in notificationEvent.Payload.elements)
            {

                if (element.enabled && element.state == ElementSessionStateType.ACTIVE || element.state == ElementSessionStateType.COMPLETE)
                {
                    var el = new PciElement();
                    el.State = element.state.ToString();
                    el.ValidationState = element.validationState.ToString();
                    el.Enabled = (element.enabled ? "true" : "false");
                    el.Name = element.name.ToString();
                    el.Data = element.data;
                    el.Length = element.length.ToString();
                    el.Sizemin = element.size.min.ToString();
                    el.Sizemax = element.size.max.ToString();

                    if (notificationEvent.Payload.sessionData.Length >= 4)
                    {
                        PropertyEntryType propertyEntryType;

                        for (int i = 0; i <= 4; i++)
                        {
                            propertyEntryType = (PropertyEntryType)notificationEvent.Payload.sessionData.GetValue(i);
                            if (propertyEntryType.name.Equals("cardGroup.panLength"))
                            {
                                Console.WriteLine(propertyEntryType.name + " is " + propertyEntryType.Value);
                                TpsLogManager<Semafone>.Debug(propertyEntryType.name + " is " + propertyEntryType.Value);
                                el.PanLength = propertyEntryType.Value;
                            }
                            if (propertyEntryType.name.Equals("cardGroup.cvcLength"))
                            {
                                Console.WriteLine(propertyEntryType.name + " is " + propertyEntryType.Value);
                                TpsLogManager<Semafone>.Debug(propertyEntryType.name + " is " + propertyEntryType.Value);
                                el.CvcLength = propertyEntryType.Value;
                            }
                            if (propertyEntryType.name.Equals("cardGroup.issueNoReq"))
                            {
                                Console.WriteLine(propertyEntryType.name + " is " + propertyEntryType.Value);
                                TpsLogManager<Semafone>.Debug(propertyEntryType.name + " is " + propertyEntryType.Value);
                                el.IssueNoReq = propertyEntryType.Value;
                            }
                            if (propertyEntryType.name.Equals("cardGroup.validFromReq"))
                            {
                                Console.WriteLine(propertyEntryType.name + " is " + propertyEntryType.Value);
                                TpsLogManager<Semafone>.Debug(propertyEntryType.name + " is " + propertyEntryType.Value);
                                el.ValidFromReq = propertyEntryType.Value;
                            }
                            if (propertyEntryType.name.Equals("cardGroup.name"))
                            {
                                Console.WriteLine(propertyEntryType.name + " is " + propertyEntryType.Value);
                                TpsLogManager<Semafone>.Debug(propertyEntryType.name + " is " + propertyEntryType.Value);
                                el.CardType = propertyEntryType.Value;
                            }
                        }
                    }
                    if (el.Name == "CVC")
                    {
                        _inCvc = true;
                    }

                    SemafoneDtmf semafoneDtmf = new SemafoneDtmf() { Semafone = new SemafonePciElement() { Dtmf = el } };

                    SendSemafoneMaskedDtmf(_webSocket, semafoneDtmf);

                    Console.WriteLine(String.Format("State:[{0}] ValidationState:[{1}] Enabled:[{2}] {3}:[{4,-20}] Length:[{5}] Size:[{6}] ",
                        element.state,
                        element.validationState,
                        element.enabled ? "yes" : "no",
                        element.name,
                        element.data,
                        element.length,
                        element.size
                        ));
                }
            }

            // Check if we've automatically exited from secure mode
            if (!_secureDataClient.IsInSecureMode(_secureDataSession))
            {
                // send an exited secure mode response to the server
                IsInSecureMode = false;
                SemafoneExitSecureModeHandset semafoneHandset = new SemafoneExitSecureModeHandset();
                semafoneHandset.Semafone = new SemafoneExitSecureMode() { Exitsecuremode = true };
                string message = JsonConvert.SerializeObject(semafoneHandset);
                SemafoneConnection.SemafoneConnectionInstance.SendWebSocketMessage(_webSocket, message);
                TpsLogManager<Semafone>.Info("Exit secure mode semafone message has been sent for " + _webSocket);
                TpsLogManager<Semafone>.Info(message);
                // stop listening for anymore events
                StopListeningForEvents();
            }
        }

        internal static void SendSemafoneMaskedDtmf(string webSocket, SemafoneDtmf semafoneDtmf)
        {
            if (string.IsNullOrEmpty(webSocket))
            {
                TpsLogManager<Semafone>.Warn("sendSemafoneMaskedDTMF : Client has either been disconnected or doesn't exist");
            }
            try
            {
                string message = JsonConvert.SerializeObject(semafoneDtmf); ;
                SemafoneConnection.SemafoneConnectionInstance.SendWebSocketMessage(webSocket, message); 
                TpsLogManager<Semafone>.Info("SemafoneMaskedDTMF semafone message sent to "+webSocket);
                TpsLogManager<Semafone>.Info(message);
            }
            catch (Exception ex)
            {
                SemafoneConnection.SemafoneConnectionInstance.SendWebSocketMessage(webSocket, ex.Message);
                TpsLogManager<Semafone>.Error("sendSemafoneMaskedDTMF : " + ex.Message);
            }
        }


        //public static void CheckDpmTarget1()
        //{
        //    if (_dPmTarget1PingThread == null)
        //    {
        //        _dPmTarget1PingThread = new Thread(() => PingDpmTarget1());
        //        _dPmTarget1PingThread.IsBackground = true;
        //    }
        //    if (!_dPmTarget1PingThread.IsAlive)
        //    {
        //        _dPmTarget1PingThread = new Thread(() => PingDpmTarget1());
        //        _dPmTarget1PingThread.IsBackground = true;
        //        _dPmTarget1PingThread.Start();

        //    }
        //}
        //private static void PingDpmTarget1()
        //{
        //    bool runLoop = true;
        //    int waitInMillisecondsBeforeNextDpmTargetPing = Convert.ToInt32(Semafone.WaitInMillisecondsBeforeNextDPMTargetPing);
        //    int pingAttempts = Convert.ToInt32(Semafone.DPMTargetPingAttemptsBeforeDeclareFailure);
        //    int unsuccessfulPingCount = 0;
        //    WebClient client = new WebClient();
        //    if (pingAttempts > 0)
        //    {
        //        while (runLoop)
        //        {
        //            string args;
        //            try
        //            {
        //                args = client.DownloadString(new Uri(ConfigServer.ConfigServerInstance.PrimaryDpmTargetPingUrl));
        //            }
        //            catch (Exception ex)
        //            {
        //                ex = null;
        //                args = string.Empty;
        //            }
        //            if (!string.IsNullOrEmpty(args))
        //            {
        //                double result;
        //                TimeSpan t = new TimeSpan();
        //                if (double.TryParse(args, out result))
        //                {
        //                    t = TimeSpan.FromMilliseconds(Convert.ToDouble(args));
        //                    CLogger.WriteLog(ELogLevel.Debug, "DPMTarget 1 ping [" + ConfigServer.ConfigServerInstance.PrimaryDpmTargetPingUrl + "] response : " + (Convert.ToInt32(t.Days / 365.25)) + " Years " + (t.Days % 365) + " Days " + t.Hours + " Hours " + t.Minutes + " Minutes " + t.Seconds + " Seconds " + t.Milliseconds + " Milliseconds");
        //                    unsuccessfulPingCount = 0;
        //                }
        //                else
        //                {
        //                    CLogger.WriteLog(ELogLevel.Info, "DPMTarget 1 ping [" + ConfigServer.ConfigServerInstance.PrimaryDpmTargetPingUrl + "] failed in " + (unsuccessfulPingCount + 1) + " attempt.");
        //                    unsuccessfulPingCount++;
        //                }
        //            }
        //            else
        //            {
        //                CLogger.WriteLog(ELogLevel.Info, "DPMTarget 1 ping [" + ConfigServer.ConfigServerInstance.PrimaryDpmTargetPingUrl + "] failed in " + (unsuccessfulPingCount + 1) + " attempt.");
        //                unsuccessfulPingCount++;
        //            }
        //            if (unsuccessfulPingCount == pingAttempts)
        //            {
        //                CLogger.WriteLog(ELogLevel.Info, "DPMTarget 1 ping [" + ConfigServer.ConfigServerInstance.PrimaryDpmTargetPingUrl + "] failed. Exiting from secure mode for all agents using DPMTarget 1.");
        //                ConfigServer.ConfigServerInstance.SendManagementMessage(95008, LogCategory.Alarm, LogLevel.Alarm, "Semafone DPMTarget 1 failed.");
        //                //RequestExitSecureModeforAllAgent(1);
        //                RemoveSemafoneforAllAgent(1);
        //                runLoop = false;
        //            }
        //            if (runLoop)
        //            {
        //                if (!CheckIfAnyAgentIsUsingDpmTarget(1))
        //                    runLoop = false;
        //                else
        //                    Thread.Sleep(waitInMillisecondsBeforeNextDpmTargetPing);
        //            }
        //        }
        //    }
        //}

        //public static void CheckDpmTarget2()
        //{
        //    if (_dPmTarget2PingThread == null)
        //    {
        //        _dPmTarget2PingThread = new Thread(() => PingDpmTarget2()); ;
        //        _dPmTarget2PingThread.IsBackground = true;
        //    }
        //    if (!_dPmTarget2PingThread.IsAlive)
        //    {
        //        _dPmTarget2PingThread = new Thread(() => PingDpmTarget2());
        //        _dPmTarget2PingThread.IsBackground = true;
        //        _dPmTarget2PingThread.Start();
        //    }
        //}
        //private static void PingDpmTarget2()
        //{
        //    bool runLoop = true;
        //    int waitInMillisecondsBeforeNextDpmTargetPing = Convert.ToInt32(ConfigurationManager.AppSettings["WaitInMillisecondsBeforeNextDPMTargetPing"]);
        //    int pingAttempts = Convert.ToInt32(ConfigurationManager.AppSettings["DPMTargetPingAttemptsBeforeDeclareFailure"]);
        //    int unsuccessfulPingCount = 0;
        //    WebClient client = new WebClient();
        //    if (pingAttempts > 0)
        //    {
        //        while (runLoop)
        //        {
        //            string args;
        //            try
        //            {
        //                args = client.DownloadString(new Uri(ConfigServer.ConfigServerInstance.SecondaryDpmTargetPingUrl));
        //            }
        //            catch (Exception ex)
        //            {
        //                ex = null;
        //                args = string.Empty;
        //            }
        //            if (!string.IsNullOrEmpty(args))
        //            {
        //                double result;
        //                TimeSpan t = new TimeSpan();
        //                if (double.TryParse(args, out result))
        //                {
        //                    t = TimeSpan.FromMilliseconds(Convert.ToDouble(args));
        //                    CLogger.WriteLog(ELogLevel.Debug, "DPMTarget 2 ping [" + ConfigServer.ConfigServerInstance.SecondaryDpmTargetPingUrl + "] response : " + (Convert.ToInt32(t.Days / 365.25)) + " Years " + (t.Days % 365) + " Days " + t.Hours + " Hours " + t.Minutes + " Minutes " + t.Seconds + " Seconds " + t.Milliseconds + " Milliseconds");
        //                    unsuccessfulPingCount = 0;
        //                }
        //                else
        //                {
        //                    CLogger.WriteLog(ELogLevel.Info, "DPMTarget 2 ping [" + ConfigServer.ConfigServerInstance.SecondaryDpmTargetPingUrl + "] failed in " + (unsuccessfulPingCount + 1) + " attempt.");
        //                    unsuccessfulPingCount++;
        //                }
        //            }
        //            else
        //            {
        //                CLogger.WriteLog(ELogLevel.Info, "DPMTarget 2 ping [" + ConfigServer.ConfigServerInstance.SecondaryDpmTargetPingUrl + "] failed in " + (unsuccessfulPingCount + 1) + " attempt.");
        //                unsuccessfulPingCount++;
        //            }
        //            if (unsuccessfulPingCount == pingAttempts)
        //            {
        //                CLogger.WriteLog(ELogLevel.Info, "DPMTarget 2 ping [" + ConfigServer.ConfigServerInstance.SecondaryDpmTargetPingUrl + "] failed. Exiting from secure mode for all agents using DPMTarget 2.");
        //                ConfigServer.ConfigServerInstance.SendManagementMessage(95009, LogCategory.Alarm, LogLevel.Alarm, "Semafone DPMTarget 2 failed.");
        //                //RequestExitSecureModeforAllAgent(2);
        //                RemoveSemafoneforAllAgent(2);
        //                runLoop = false;
        //            }
        //            if (runLoop)
        //            {
        //                if (!CheckIfAnyAgentIsUsingDpmTarget(2))
        //                    runLoop = false;
        //                else
        //                    Thread.Sleep(waitInMillisecondsBeforeNextDpmTargetPing);
        //            }
        //        }
        //    }
        //}

        //private static bool CheckIfAnyAgentIsUsingDpmTarget(int dpmTarget)
        //{
        //    return WebSocketPipe.AgentConnectionsDictionary.Keys.Where(agentConnection => agentConnection.Semafone != null).Any(agentConnection => agentConnection.Semafone.DpmTargetUsed == dpmTarget);
        //}

        //private static void RemoveSemafoneforAllAgent(int dpmTarget)
        //{
        //    foreach (AgentConnection agentConnection in WebSocketPipe.AgentConnectionsDictionary.Keys)
        //        if (agentConnection.Semafone != null)
        //            if (agentConnection.Semafone.DpmTargetUsed == dpmTarget)
        //            {
        //                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneDpmtargetDown)), (dpmTarget == 2 ? "Secondary " : "Primary ") + "dpmtarget down.", ErrorSeverity.Error);
        //                agentConnection.Semafone.RemoveListnerForMaskedData();
        //                agentConnection.Semafone = null;
        //            }
        //}

        //private static void RequestExitSecureModeforAllAgent(int dpmTarget)
        //{
        //    foreach (AgentConnection agentConnection in WebSocketPipe.AgentConnectionsDictionary.Keys)
        //        if (agentConnection.Semafone != null)
        //            if (agentConnection.Semafone.DpmTargetUsed == dpmTarget)
        //            {
        //                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneDpmtargetDown)), (dpmTarget == 2 ? "Secondary " : "Primary ") + "dpmtarget down.", ErrorSeverity.Error);
        //                agentConnection.Semafone.ExitSecureMode();
        //            }

        //}
    }
}