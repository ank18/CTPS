using System;
using System.Collections;
using System.Threading;
using Softphone.Json;

using Genesyslab.Platform.ApplicationBlocks.Commons.Protocols;
using Genesyslab.Platform.ApplicationBlocks.ConfigurationObjectModel.CfgObjects;
using Genesyslab.Platform.Commons.Collections;
using Genesyslab.Platform.Commons.Protocols;
using Genesyslab.Platform.OpenMedia.Protocols.InteractionServer.Requests.InteractionManagement;
using Genesyslab.Platform.Voice.Protocols;
using Genesyslab.Platform.Voice.Protocols.TServer;
using Genesyslab.Platform.Voice.Protocols.TServer.Events;
using Genesyslab.Platform.Voice.Protocols.TServer.Requests.Agent;
using Genesyslab.Platform.Voice.Protocols.TServer.Requests.Dn;
using Genesyslab.Platform.Voice.Protocols.TServer.Requests.Dtmf;
using Genesyslab.Platform.Voice.Protocols.TServer.Requests.Party;
using Genesyslab.Platform.Voice.Protocols.TServer.Requests.Queries;
using Genesyslab.Platform.Voice.Protocols.TServer.Requests.Userdata;
using LogConfigLayer;

namespace Softphone
{
    public class SipServer
    {
        #region internal constants
        internal const String KeyNotReadyReasonCode = "ReasonCode";
        internal const String KeyLogoutReasonCode = "ReasonCode";
        internal static String DefaultLocation = null;
        internal const String SemafoneUuidKeyName = "SemafoneCallId";
        internal const String SemafoneUrnKeyName = "SEMA_URN";
        internal const String SemafoneDpmKeyName = "SEMA-DPM-Target";
        internal const String CtiConnidKeyName = "conn-1";
        internal const String RepAgtRelease = "REP_AGT_RELEASE";
        #endregion

        #region Private variables
        private const String TserverServerApp = "TServer_Server_App";
        private Boolean _isClientASoftphone = false;
        private CfgApplication _applicationConfiguration;
        private TServerProtocol _tServerProtocol;
        private String _agentId = "";
        private String _agentPassword = "";
        private String _extension = "";
        private String _extensionSwitch = "";
        private String _hostname = "";
        private String _agentQueue = "";
        private Thread _msgReceiver;
        private String _primaryUrl = "";
        private String _secondaryUrl = "";
        private Boolean _continueRunning = true;
        private Thread _watchDog;
        private String _srConnectionId = "";
        #endregion

        #region Internal Properties
        internal String AgentId
        {
            get { return _agentId; }
        }

        internal String AgentPassword
        {
            get { return _agentPassword; }
        }

        internal String Extension
        {
            get { return _extension; }
        }

        internal String ExtensionSwitch
        {
            get { return _extensionSwitch; }
        }

        internal String AgentQueue
        {
            get { return _agentQueue; }
        }
        #endregion

        public SipServer(string srConnectionId, String extension, String extensionSwitch, String agentId, String agentQueue, String agentPassword, string primaryUrl, string secondaryUrl, Boolean IsClientASoftphone)
        {

            this._srConnectionId = srConnectionId;
            this._applicationConfiguration = ConfigServer.ConfigServerInstance.GetCFGApplicationObject();
            this._extension = extension ?? this._extension;
            this._extensionSwitch = extensionSwitch ?? this._extensionSwitch;
            this._agentId = agentId ?? "";
            this._agentQueue = agentQueue ?? "";
            this._agentPassword = agentPassword ?? "";
            this._isClientASoftphone = IsClientASoftphone;
            this._primaryUrl = primaryUrl ?? this._primaryUrl;
           TpsLogManager<SipServer>.Info("Primary SIP server URL set to [" + this._primaryUrl + "]");
            this._secondaryUrl = secondaryUrl ?? this._secondaryUrl;
           TpsLogManager<SipServer>.Info("Secondary SIP server URL set to [" + this._secondaryUrl + "]");
            this._hostname = primaryUrl == null ? this._hostname : primaryUrl.Substring(6).Split(':')[0];
           TpsLogManager<SipServer>.Info("hostname set to [" + this._hostname + "]");
        }

        #region Private methods


        private void NotifyRestoreConnection(EventRestoreConnection theEvent)
        {
            try
            {
                RequestRegisterAddress();
               TpsLogManager<SipServer>.Info("Connection restored");
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error notifyRestoreConnection : " + e.Message + e.StackTrace); }
        }

        private void CheckConnection()
        {
            while (_continueRunning)
            {
                try
                {
                    if ((_tServerProtocol.State.GetHashCode() == ChannelState.Closed.GetHashCode()))
                        if (!Connect(_isClientASoftphone))
                        {
                            String url = this._primaryUrl;
                            this._primaryUrl = _secondaryUrl;
                            this._secondaryUrl = url;
                        }
                }
                catch (Exception e) { TpsLogManager<SipServer>.Error("Error checkConnection  tServerProtocol.State.GetHashCode : " + e.Message + e.StackTrace); }

                try
                {
                    Thread.Sleep(10 * 1000);
                }
                catch (Exception e) { TpsLogManager<SipServer>.Error("Error checkConnection  Thread.Sleep : " + e.Message + e.StackTrace); }
            }
        }

        private void PushAttachedData(String connId, KeyValueCollection newData)
        {
            try
            {
               TpsLogManager<SipServer>.Info("Attaching new user data for connId [" + connId + "]");
                _tServerProtocol.Send(RequestUpdateUserData.Create(_extension, new ConnectionId(connId), newData));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error pushAttachedData : " + e.Message + e.StackTrace); }
        }

        private KeyValueCollection UpdateAttachedDataTable(Hashtable newData, KeyValueCollection oldUserData)
        {
            try
            {
                KeyValueCollection keepNecessaryUserData = oldUserData; //keepNecessaryKeys(oldUserData);
                foreach (String key in newData.Keys)
                {
                    if (keepNecessaryUserData.ContainsKey(key))
                        keepNecessaryUserData.Remove(key);
                    keepNecessaryUserData.Add(key, newData[key]);
                   TpsLogManager<SipServer>.Info("Attached data [" + key + "] set to value [" + (newData[key] ?? "") + "]");
                }
                return keepNecessaryUserData;
            }
            catch (Exception e) { TpsLogManager<SipServer>.Warn("Error updateAttachedDataTable : " + e.Message + e.StackTrace); return null; }
        }


        private Boolean ConnectBackup()
        {
            Boolean result = false;
            String uri = (string.IsNullOrEmpty(_secondaryUrl) ? _primaryUrl : _secondaryUrl);

            try
            {
                TServerConfiguration tServerConfiguration = new TServerConfiguration(TserverServerApp);

               TpsLogManager<SipServer>.Info("Client name [" + Properties.Settings.Default.ApplicationName + "]");
                tServerConfiguration.ClientName = Properties.Settings.Default.ApplicationName;

               TpsLogManager<SipServer>.Info("Client password [" + _applicationConfiguration.Password + "]");
                tServerConfiguration.ClientPassword = _applicationConfiguration.Password;

               TpsLogManager<SipServer>.Info("Telephony server URI [" + uri + "]");
                tServerConfiguration.Uri = new Uri(uri);

               TpsLogManager<SipServer>.Info("Creating telephony endpoint...");
                Endpoint tserverEndpoint = new Endpoint(new Uri(uri));
               TpsLogManager<SipServer>.Info("Telephony endpoint created.");

               TpsLogManager<SipServer>.Info("Setting telephony endpoint parameters...");
                _tServerProtocol = new TServerProtocol(tserverEndpoint)
                {
                    ClientName = _applicationConfiguration.Name,
                    ClientPassword = _applicationConfiguration.Password
                };
               TpsLogManager<SipServer>.Info("Telephony endpoint parameters set.");

               TpsLogManager<SipServer>.Info("Start message broker receiver thread...");
                _msgReceiver = new Thread(new ThreadStart(this.ReceiveMessages));
                _msgReceiver.Start();
               TpsLogManager<SipServer>.Info("Message broker receiver thread started.");

               TpsLogManager<SipServer>.Info("Opening connection to telephony server...");
                _tServerProtocol.Open();
               TpsLogManager<SipServer>.Info("Telephony server connection established.");

                try
                {
                    if (_watchDog == null)
                    {
                       TpsLogManager<SipServer>.Info("Creating watchdog thread...");
                        _watchDog = new Thread(new ThreadStart(this.CheckConnection));
                       TpsLogManager<SipServer>.Info("Watchdog thread created.");
                    }

                   TpsLogManager<SipServer>.Info("Restarting watchdog thread...");
                    _watchDog.Start();
                   TpsLogManager<SipServer>.Info("Watchdog thread started.");
                }
                catch (Exception e) { TpsLogManager<SipServer>.Error("Error connectBackup : " + e.Message + e.StackTrace); }

                result = _tServerProtocol.State.GetHashCode() == ChannelState.Opened.GetHashCode() ? true : false;
            }
            catch (ProtocolException pe)
            {
                TpsLogManager<SipServer>.Error("Connection to telephony server failed. " + pe.StackTrace);
            }
            catch (NullReferenceException ne)
            {
                TpsLogManager<SipServer>.Error("Error connectBackup  NullReferenceException : " + ne.StackTrace);
            }
            finally
            {
               TpsLogManager<SipServer>.Info("Connection to telephony server was " + (result ? "successfully." : "unsuccessful."));
                result = result && IsSipServerPrimary();
            }

            return result;
        }

        private void ReceiveMessages()
        {
            try
            {
                lock (this)
                {
                    while (_continueRunning)
                    {
                        if (_tServerProtocol == null) break;
                        IMessage message = _tServerProtocol.Receive();
                        if (message != null)
                            ProcessReceivedMessages(message);
                    }
                }
            }
            catch (Exception ex)
            {
               TpsLogManager<SipServer>.Info("ReceiveMessage error: " + ex.Message);
                _continueRunning = true;
            }
        }

        private void KeepNecessaryKeys(AgentConnection agentConnection)
        {
            try
            {
                var keepUserDataKeysFromCme = (KeyValueCollection)_applicationConfiguration.Options["RemoveTransferData"];
                if ((keepUserDataKeysFromCme.Count < 1))
                {
                    TpsLogManager<SipServer>.Error("Could not find list of attached data keys to retain.");
                    return;
                }

                KeyValueCollection tempCollection = new KeyValueCollection();
                foreach (string key in agentConnection.EventUserData.AllKeys)
                    tempCollection.Add(key, agentConnection.EventUserData[key]);

                foreach (string key in tempCollection.AllKeys)
                    if (!keepUserDataKeysFromCme.ContainsKey(key))
                    {
                        agentConnection.EventUserData.Remove(key);
                        TpsLogManager<SipServer>.Debug( "Removed Key: " + key);
                    }

                #region For Anonymous Call
                if (agentConnection.IsAnonymousCall)
                    if (agentConnection.EventUserData.ContainsKey("htel"))
                        agentConnection.EventUserData["htel"] = "0Anonymous";
                    else
                        agentConnection.EventUserData.Add("htel", "0Anonymous");
                #endregion

            }
            catch (Exception e)
            {
                TpsLogManager<SipServer>.Error("Error keepNecessaryKeys : " + e.Message + e.StackTrace);
            }
        }

        private Boolean IsSipServerPrimary()
        {
            Boolean result = false;

            try
            {
               TpsLogManager<SipServer>.Info("Querying the server to determine working mode.");
                IMessage response = _tServerProtocol.Request(RequestQueryServer.Create());

                switch (response.Id)
                {
                    case EventError.MessageId:
                        try
                        {
                            EventError theEvent = (EventError)response;
                            //notifyError(theEvent);
                           TpsLogManager<SipServer>.Info("Error [" + theEvent.ErrorCode + "] " + theEvent.ErrorMessage);

                            switch (theEvent.ErrorCode)
                            {
                                case 56: // Associated with the invalid connID problem during quick transfers.
                                    break;
                                default:
                                    new ClientResponse().SendJsonErrorMessage(_srConnectionId, Convert.ToString(theEvent.ErrorCode), theEvent.ErrorMessage, ErrorSeverity.Error);
                                    break;
                            }
                        }
                        catch (Exception e) { TpsLogManager<SipServer>.Error("Error keepNecessaryKeys EventError.MessageId : " + e.Message + e.StackTrace); }
                        break;
                    case EventServerInfo.MessageId:
                        try
                        {
                            EventServerInfo theEvent = (EventServerInfo)response;
                           TpsLogManager<SipServer>.Info("Received server info to determined server mode.");

                            switch (theEvent.ServerRole)
                            {
                                case ServerRole.Primary:
                                    result = true;
                                   TpsLogManager<SipServer>.Info("Server is in [primary] mode.");
                                    break;
                                case ServerRole.Backup:
                                    result = false;
                                   TpsLogManager<SipServer>.Info("Server is in [backup] mode.");
                                    Disconnect();
                                    break;
                                default:
                                   TpsLogManager<SipServer>.Info("Server is in [unknown] mode.");
                                    break;
                            }
                        }
                        catch (Exception e) { TpsLogManager<SipServer>.Error("Error keepNecessaryKeys EventServerInfo.MessageId : " + e.Message + e.StackTrace); }
                        break;
                    default:
                       TpsLogManager<SipServer>.Info(response.Name);
                        ProcessReceivedMessages(response);
                        break;
                }
            }
            catch (Exception e)
            {
                TpsLogManager<SipServer>.Error("Error keepNecessaryKeys : " + e.Message + e.StackTrace);
            }

            return result;
        }

        #endregion

        internal Boolean Connect(Boolean isClientASoftphone)
        {
            Boolean result = false;
            this._isClientASoftphone = isClientASoftphone;
            try
            {

                var tServerConfiguration = new TServerConfiguration(TserverServerApp)
                {
                    ClientName = Properties.Settings.Default.ApplicationName,
                    ClientPassword = _applicationConfiguration.Password,
                    Uri = new Uri(_primaryUrl)
                };
                _continueRunning = true;
               TpsLogManager<SipServer>.Info("Created tServerConfiguration object");
               TpsLogManager<SipServer>.Info("Client name [" + Properties.Settings.Default.ApplicationName + "]");
               TpsLogManager<SipServer>.Info("Client password [" + _applicationConfiguration.Password + "]");
               TpsLogManager<SipServer>.Info("Telephony server URI [" + _primaryUrl + "]");
               TpsLogManager<SipServer>.Info("Creating telephony endpoint...");
                var tserverEndpoint = new Endpoint(new Uri(_primaryUrl));
               TpsLogManager<SipServer>.Info("Telephony endpoint created.");

               TpsLogManager<SipServer>.Info("Setting telephony endpoint parameters...");

                _tServerProtocol = new TServerProtocol(tserverEndpoint)
                {
                    ClientName = _applicationConfiguration.Name,
                    ClientPassword = _applicationConfiguration.Password
                };
               TpsLogManager<SipServer>.Info("Telephony endpoint parameters set.");

               TpsLogManager<SipServer>.Info("Start message broker receiver thread...");
                _msgReceiver = new Thread(new ThreadStart(this.ReceiveMessages));
                _msgReceiver.Start();
               TpsLogManager<SipServer>.Info("Message broker receiver thread started.");

               TpsLogManager<SipServer>.Info("Opening connection to telephony server...");
                _tServerProtocol.Open();
               TpsLogManager<SipServer>.Info("Telephony server connection established.");

                try
                {
                    if (_watchDog == null)
                    {
                       TpsLogManager<SipServer>.Info("Creating watchdog thread...");
                        _watchDog = new Thread(new ThreadStart(this.CheckConnection));
                       TpsLogManager<SipServer>.Info("Watchdog thread created.");
                    }

                   TpsLogManager<SipServer>.Info("Restarting watchdog thread...");
                    _watchDog.Start();
                   TpsLogManager<SipServer>.Info("Watchdog thread started.");
                }
                catch (Exception e)
                {
                    TpsLogManager<SipServer>.Error("Error connect  watchDog : " + e.Message + e.StackTrace);
                }

                result = _tServerProtocol.State.GetHashCode() == ChannelState.Opened.GetHashCode() ? true : false;
            }
            catch (ProtocolException pe)
            {
               TpsLogManager<SipServer>.Info("Connection to telephony server failed. " + pe.StackTrace);
            }
            catch (NullReferenceException ne)
            {
                TpsLogManager<SipServer>.Error("Error connect  NullReferenceException : " + ne.StackTrace);
            }
            finally
            {
               TpsLogManager<SipServer>.Info("Connection to telephony server was " + (result ? "successful." : "unsuccessful."));
                result = result && IsSipServerPrimary();
            }

            if (!result)
                result = ConnectBackup();

            return result;
        }

        internal void Disconnect()
        {
            try
            {
                if (_tServerProtocol != null)
                {
                   TpsLogManager<SipServer>.Info("Closing connection from telephony server...");
                    _tServerProtocol.Close();
                   TpsLogManager<SipServer>.Info("Connection closed from telephony server.");
                }
                else
                    return;
            }
            catch (ProtocolException pe)
            {
                TpsLogManager<SipServer>.Error("Error produced when closing connection to telephony server. " + pe.StackTrace);
            }
            catch (NullReferenceException ne)
            {
                TpsLogManager<SipServer>.Error("Error disconnect NullReferenceException : " + ne.StackTrace);
            }
            finally
            {
                _tServerProtocol = null;
                _continueRunning = false;
                _msgReceiver = null;
            }
        }

        #region SIP Requests methods
        internal void RequestRegisterAddress()
        {
            try
            {
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for register.");
               TpsLogManager<SipServer>.Info("Requesting registration of DN [" + _extension + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Dn.RequestRegisterAddress.Create(_extension, RegisterMode.ModeShare, ControlMode.RegisterDefault, AddressType.DN));
            }
            catch (NullReferenceException ne)
            {
                TpsLogManager<SipServer>.Error("Error requestRegisterAddress NullReferenceException: " + ne.Message + ne.StackTrace);
            }
            catch (Exception e)
            {
                TpsLogManager<SipServer>.Error("Error requestRegisterAddress : " + e.Message + e.StackTrace);
            }
        }

        internal void RequestUnregisterAddress()
        {
            try
            {
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for unregister.");
               TpsLogManager<SipServer>.Info("Requesting unregistration of DN [" + _extension + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Dn.RequestUnregisterAddress.Create(_extension, ControlMode.RegisterDefault));
            }
            catch (NullReferenceException ne)
            {
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestUnregisterAddress : " + e.Message + e.StackTrace); }
        }

        internal void RequestAgentLogin()
        {
            try
            {
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for agent login.");
                if (string.IsNullOrEmpty(_agentId)) throw new Exception("No agent Id provided for agent login.");
                int attempt = 0;
                while ((_tServerProtocol == null) && (attempt < 6))
                {
                    attempt++;
                    Thread.Sleep(1000);
                }
               TpsLogManager<SipServer>.Info("Requesting agent login for agent [" + _agentId + "] on DN [" + _extension + "]");
                if (_tServerProtocol == null)
                {
                    TpsLogManager<SipServer>.Error("Error RequestAgentLogin - tserver not responded within 5 seconds");
                    return;
                }
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Agent.RequestAgentLogin.Create(_extension, AgentWorkMode.ManualIn, _agentQueue, _agentId, _agentPassword, null, null));
            }
            catch (NullReferenceException ne)
            {
                TpsLogManager<SipServer>.Error("Error RequestAgentLogin  NullReferenceException : " + ne.Message + ne.StackTrace);
            }
            catch (Exception e)
            {
                TpsLogManager<SipServer>.Error("Error RequestAgentLogin : " + e.Message + e.StackTrace);
            }
        }

        internal void RequestAgentNotReady(String reason)
        {
            try
            {
                if (string.IsNullOrEmpty(reason)) throw new Exception("No reason provided for agent not ready.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for agent not ready.");
                if (string.IsNullOrEmpty(_agentId)) throw new Exception("No agent Id provided for agent not ready.");
               TpsLogManager<SipServer>.Info("Requesting agent not ready for agent [" + _agentId + "] on DN [" + _extension + "] for reason [" + reason + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Agent.RequestAgentNotReady.Create(_extension, AgentWorkMode.ManualIn, _agentQueue, new KeyValueCollection { { KeyNotReadyReasonCode, reason } }, null));
            }
            catch (NullReferenceException ne)
            {
                TpsLogManager<SipServer>.Error("Error requestAgentNotReady  NullReferenceException : " + ne.StackTrace);
            }
            catch (Exception e)
            {
                TpsLogManager<SipServer>.Error("Error requestAgentNotReady : " + e.Message + e.StackTrace);
            }
        }

        internal void RequestAgentReady()
        {
            try
            {
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for agent ready.");
                if (string.IsNullOrEmpty(_agentId)) throw new Exception("No agent Id provided for agent ready.");
               TpsLogManager<SipServer>.Info("Requesting agent ready for agent [" + _agentId + "] on DN [" + _extension + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Agent.RequestAgentReady.Create(_extension, AgentWorkMode.ManualIn, _agentQueue, null, null));
            }
            catch (NullReferenceException ne)
            {
                TpsLogManager<SipServer>.Error("Error requestAgentReady  NullReferenceException : " + ne.StackTrace);
            }
            catch (Exception e)
            {
                TpsLogManager<SipServer>.Error("Error requestAgentReady : " + e.Message + e.StackTrace);
            }
        }

        internal void RequestAgentLogout(String reason)
        {
            try
            {
                if (string.IsNullOrEmpty(reason)) throw new Exception("No reason provided for agent logout.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for agent logout.");
                if (string.IsNullOrEmpty(_agentId)) throw new Exception("No agent Id provided for agent logout.");
               TpsLogManager<SipServer>.Info("Requesting agent logout for agent [" + _agentId + "] on DN [" + _extension + "] for reason [" + reason + "]");
                if (_isClientASoftphone)
                    _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Agent.RequestAgentLogout.Create(_extension, _agentQueue, new KeyValueCollection { { KeyLogoutReasonCode, reason } }, null));

            }
            catch (NullReferenceException ne)
            {
                TpsLogManager<SipServer>.Error("Error requestAgentLogout  NullReferenceException : " + ne.StackTrace, ne);
            }
            catch (Exception e)
            {
                TpsLogManager<SipServer>.Error("Error requestAgentLogout : " + e.Message + e.StackTrace, e);
            }
        }

        internal void RequestAnswerCall(String connId)
        {
            try
            {
                if (string.IsNullOrEmpty(connId)) throw new Exception("No connid provided to answer call.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided to answer call.");
               TpsLogManager<SipServer>.Info("Requesting answer of connId [" + connId + "] on DN [" + _extension + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Party.RequestAnswerCall.Create(_extension, new ConnectionId(connId)));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestAnswerCall : " + e.Message + e.StackTrace); }
        }

        internal void RequestReleaseCall(String connId, KeyValueCollection oldUserData)
        {
            try
            {
                KeyValueCollection keepNecessaryUserData = new KeyValueCollection(); //keepNecessaryKeys(oldUserData);
                if (string.IsNullOrEmpty(connId)) throw new Exception("No connid provided to release call.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided to release call.");
               TpsLogManager<SipServer>.Info("Requesting release of connId [" + connId + "] on DN [" + _extension + "]");
                // This attach data (REP_AGT_RELEASE as Y) differentiate between Call hangup by an agent or customer call release. 
                keepNecessaryUserData.Add(RepAgtRelease, "Y");
                PushAttachedData(connId, keepNecessaryUserData);
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Party.RequestReleaseCall.Create(_extension, new ConnectionId(connId)));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestReleaseCall : " + e.Message + e.StackTrace); }
        }

        internal void RequestHoldCall(String connId)
        {
            try
            {
                if (string.IsNullOrEmpty(connId)) throw new Exception("No connid provided to hold call.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided to hold call.");
               TpsLogManager<SipServer>.Info("Requesting hold  connId [" + connId + "] on DN [" + _extension + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Party.RequestHoldCall.Create(_extension, new ConnectionId(connId)));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestHoldCall : " + e.Message + e.StackTrace); }
        }

        internal void RequestRetrieveCall(String connId)
        {
            try
            {
                if (string.IsNullOrEmpty(connId)) throw new Exception("No connid provided for retrieve call.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for retrieve call.");
               TpsLogManager<SipServer>.Info("Requesting retrieval of connId [" + connId + "] on DN [" + _extension + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Party.RequestRetrieveCall.Create(_extension, new ConnectionId(connId)));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestRetrieveCall : " + e.Message + e.StackTrace); }
        }

        internal void RequestSingleStepTransfer(AgentConnection agentConnection, string transferLabel, string cn, string ct)
        {
            try
            {
                String destination = ConfigServer.ConfigServerInstance.GetRouting();
                if (string.IsNullOrEmpty(agentConnection.Line1ConnId)) throw new Exception("No connid provided for initialising single step transfer.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for initialising  single step transfer.");
                if (string.IsNullOrEmpty(destination)) throw new Exception("No destination provided for initialising single step transfer.");
                KeepNecessaryKeys(agentConnection);
                UpdateEventUserdata(agentConnection, transferLabel, cn, ct, ref destination);
                _tServerProtocol.Send(RequestDeleteUserData.Create(_extension, new ConnectionId(agentConnection.Line1ConnId)));
                _tServerProtocol.Send(RequestAttachUserData.Create(_extension, new ConnectionId(agentConnection.Line1ConnId), agentConnection.EventUserData));
               TpsLogManager<SipServer>.Info("Requesting single step transfer of connId [" + agentConnection.Line1ConnId + "] on DN [" + _extension + "] to destination [" + destination + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Party.RequestSingleStepTransfer.Create(_extension, new ConnectionId(agentConnection.Line1ConnId), destination, DefaultLocation, agentConnection.EventUserData, null, null));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestSingleStepTransfer : " + e.Message + e.StackTrace); }
        }

        internal void RequestTwoStepTransfer(AgentConnection agentConnection, string transferLabel, string cn, string ct)
        {
            try
            {
                String destination = ConfigServer.ConfigServerInstance.GetRouting();
                if (string.IsNullOrEmpty(agentConnection.Line1ConnId)) throw new Exception("No connid provided for initialising 2-step transfer.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for initialising 2-step transfer.");
                if (string.IsNullOrEmpty(destination)) throw new Exception("No destination provided for initialising 2-step transfer.");
                KeepNecessaryKeys(agentConnection);
                UpdateEventUserdata(agentConnection, transferLabel, cn, ct, ref destination);
                _tServerProtocol.Send(RequestDeleteUserData.Create(_extension, new ConnectionId(agentConnection.Line1ConnId)));
                _tServerProtocol.Send(RequestAttachUserData.Create(_extension, new ConnectionId(agentConnection.Line1ConnId), agentConnection.EventUserData));
               TpsLogManager<SipServer>.Info("Requesting two-step transfer for connId [" + agentConnection.Line1ConnId + "] on DN [" + _extension + "] to destination [" + destination + "]");
                _tServerProtocol.Send(RequestInitiateTransfer.Create(_extension, new ConnectionId(agentConnection.Line1ConnId), destination, DefaultLocation, agentConnection.EventUserData, null, null));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestTwoStepTransfer : " + e.Message + e.StackTrace); }
        }

        internal void RequestConferenceCall(AgentConnection agentConnection, string transferLabel, string cn, string ct)
        {
            try
            {
                String destination = ConfigServer.ConfigServerInstance.GetRouting();
                if (string.IsNullOrEmpty(agentConnection.Line1ConnId)) throw new Exception("No connid provided for initialising conference.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for initialising conference.");
                if (string.IsNullOrEmpty(destination)) throw new Exception("No destination provided for initialising conference.");
                KeepNecessaryKeys(agentConnection);
                UpdateEventUserdata(agentConnection, transferLabel, cn, ct, ref destination);
                _tServerProtocol.Send(RequestDeleteUserData.Create(_extension, new ConnectionId(agentConnection.Line1ConnId)));
                _tServerProtocol.Send(RequestAttachUserData.Create(_extension, new ConnectionId(agentConnection.Line1ConnId), agentConnection.EventUserData));
               TpsLogManager<SipServer>.Info("Requesting conference for connId [" + agentConnection.Line1ConnId + "] on DN [" + _extension + "] to destination [" + destination + "]");
                _tServerProtocol.Send(RequestInitiateConference.Create(_extension, new ConnectionId(agentConnection.Line1ConnId), destination, DefaultLocation, agentConnection.EventUserData, null, null));

            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestConferenceCall : " + e.Message + e.StackTrace); }
        }

        private void UpdateEventUserdata(AgentConnection agentConnection, string transferLabel, string cn, string ct, ref String destination)
        {
            agentConnection.EventUserData.AddOrUpdate("cn", cn);

            if (transferLabel.Contains("TransferLabel"))
                agentConnection.EventUserData.AddOrUpdate("SFT_SERVICECALLTYPE", transferLabel);
            else
            {
                if (agentConnection.EventUserData.ContainsKey("SFT_SERVICECALLTYPE")) agentConnection.EventUserData["SFT_SERVICECALLTYPE"] = "";
                destination = transferLabel;
            }
            if (agentConnection.EventUserData.ContainsKey("SCREENPOP_TRANSFERCOUNT"))
            {
                int tranCount;
                if (int.TryParse(agentConnection.EventUserData["SCREENPOP_TRANSFERCOUNT"].ToString(), out tranCount))
                    agentConnection.EventUserData["SCREENPOP_TRANSFERCOUNT"] = Convert.ToString(tranCount + 1);
                else
                    agentConnection.EventUserData["SCREENPOP_TRANSFERCOUNT"] = "2";
            }
            else
                agentConnection.EventUserData.Add("SCREENPOP_TRANSFERCOUNT", "2");
            agentConnection.EventUserData.AddOrUpdate("ct", ct);
            if (agentConnection.LastHandset != null)
                if (agentConnection.LastHandset.Lines != null)
                    if (agentConnection.LastHandset.Lines.Line1 != null)
                        if (agentConnection.LastHandset.Lines.Line1.CallDirection.ToLower() == "outbound")
                        {
                            agentConnection.EventUserData.AddOrUpdate("htel", agentConnection.LastHandset.Lines.Line1.ConnectedTo);

                            #region Semafone outbound attached data
                            if (agentConnection.LastHandset != null)
                                if (agentConnection.LastHandset.Interaction.ContainsKey("genesysId")) /////If we don't have a SEMA_URN to add then don't add a DPM target either
                                {
                                    agentConnection.EventUserData.AddOrOnlyUpdateWhenValueIsNullOrEmpty("SEMA_URN",
                                        agentConnection.LastHandset.Interaction.GetAsString("genesysId"));
                                    // Get dpmtarget based on datacenter values 
                                    string dpmTarget = ConfigServer.ConfigServerInstance.IsSipDatacentreAndDpmDatacentreSame(agentConnection.ServerName) ? ConfigServer.ConfigServerInstance.PrimaryDpmTarget : ConfigServer.ConfigServerInstance.SecondaryDpmTarget;
                                    agentConnection.EventUserData.AddOrOnlyUpdateWhenValueIsNullOrEmpty("SEMA-DPM-Target", dpmTarget);
                                }

                            #endregion
                        }

        }

        internal void RequestCompleteConference(String activeConnId, String heldConnId)
        {
            try
            {
                if (string.IsNullOrEmpty(activeConnId)) throw new Exception("No connid provided for conference complete.");
                if (string.IsNullOrEmpty(heldConnId)) throw new Exception("No held connid provided for conference complete.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for conference complete.");
               TpsLogManager<SipServer>.Info("Requesting complete conference for connId [" + activeConnId + "] on DN [" + _extension + "] from connId [" + heldConnId + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Party.RequestCompleteConference.Create(_extension, new ConnectionId(activeConnId), new ConnectionId(heldConnId)));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestCompleteConference : " + e.Message + e.StackTrace); }
        }

        internal void RequestCompleteTransfer(String activeConnId, String heldConnId)
        {
            try
            {
                if (string.IsNullOrEmpty(activeConnId)) throw new Exception("No connid provided for transfer complete.");
                if (string.IsNullOrEmpty(heldConnId)) throw new Exception("No held connid provided for transfer complete.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for transfer complete.");
               TpsLogManager<SipServer>.Info("Requesting complete transfer for connId [" + activeConnId + "] on DN [" + _extension + "] from connId [" + heldConnId + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Party.RequestCompleteTransfer.Create(_extension, new ConnectionId(activeConnId), new ConnectionId(heldConnId)));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestCompleteTransfer : " + e.Message + e.StackTrace); }
        }

        internal void RequestAlternateCall(String activeConnId, String heldConnId)
        {
            try
            {
                if (string.IsNullOrEmpty(activeConnId)) throw new Exception("No active connid provided for alternate call.");
                if (string.IsNullOrEmpty(heldConnId)) throw new Exception("No held connid provided for alternate call.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for alternate call.");
               TpsLogManager<SipServer>.Info("Requesting alternate call for connId [" + activeConnId + "] on DN [" + _extension + "] to connId [" + heldConnId + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Party.RequestAlternateCall.Create(_extension, new ConnectionId(activeConnId), new ConnectionId(heldConnId)));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestAlternateCall : " + e.Message + e.StackTrace); }
        }


        internal void RequestMakeCall(string transferLabel, String destination, AgentConnection agentState)
        {
            try
            {
                KeyValueCollection keepNecessaryUserData = new KeyValueCollection();
                if (!String.IsNullOrEmpty(transferLabel))
                    keepNecessaryUserData.Add("SFT_SERVICECALLTYPE", transferLabel);
                if (string.IsNullOrEmpty(destination)) throw new Exception("No destination provided for making call.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for making call.");
                CLogger.WriteLog(ELogLevel.Info,
                    "Requesting make call on DN [" + _extension + "] to destination [" + destination + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Party.RequestMakeCall.Create(_extension, destination, MakeCallType.Regular, DefaultLocation, keepNecessaryUserData, null, null));

            }
            catch (Exception e)
            {
                TpsLogManager<SipServer>.Error("Error requestMakeCall : " + e.Message + e.StackTrace);
            }
        }

        internal void RequestMuteCall(String connId)
        {
            try
            {
                if (string.IsNullOrEmpty(connId)) throw new Exception("No connection provided for unmute call");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for unmute call");
               TpsLogManager<SipServer>.Info("Requesting mute of connId [" + (connId ?? "") + "] on DN [" + (_extension ?? "") + "]");
                _tServerProtocol.Send(RequestSetMuteOn.Create(_extension, new ConnectionId(connId)));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestMuteCall : " + e.Message + e.StackTrace); }
        }

        internal void RequestUnmuteCall(String connId)
        {
            try
            {
                if (string.IsNullOrEmpty(connId)) throw new Exception("No connection provided for unmute call");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for unmute call");
               TpsLogManager<SipServer>.Info("Requesting unmute of connId [" + (connId ?? "") + "] on DN [" + (_extension ?? "") + "]");
                _tServerProtocol.Send(RequestSetMuteOff.Create(_extension, new ConnectionId(connId)));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestUnmuteCall : " + e.Message + e.StackTrace); }
        }

        internal void RequestDistributeUserEvent(string connId, KeyValueCollection userDataDictionary)
        {
            try
            {
                if (string.IsNullOrEmpty(connId)) throw new Exception("No connection provided to save outbound dialler outcome.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided to save outbound dialler outcome.");
                var commonProperties = CommonProperties.Create();
                commonProperties.ThisDN = _extension;
                commonProperties.ConnID = new ConnectionId(connId); ;
                commonProperties.UserData = userDataDictionary;
               TpsLogManager<SipServer>.Info("Requesting to save outbound dialler outcome for connId [" + connId + "] on DN [" + _extension + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Special.RequestDistributeUserEvent.Create(_extension, commonProperties));
            }
            catch (Exception e)
            {
                TpsLogManager<SipServer>.Error("Error on RequestDistributeUserEvent : " + e.Message + e.StackTrace);
            }
        }


        internal void RequestSendDtmf(String tone, String connId)
        {

            try
            {
                if (string.IsNullOrEmpty(connId)) throw new Exception("No connection provided to send dtmf tone.");
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided to send dtmf tone.");
                if (string.IsNullOrEmpty(tone)) throw new Exception("No input key provided to send dtmf tone.");
               TpsLogManager<SipServer>.Info("Requesting DTMF for connId [" + connId + "] on DN [" + _extension + "] with tone [" + tone + "]");
                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Dtmf.RequestSendDtmf.Create(_extension, new ConnectionId(connId), tone, null, null));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestSendDTMF : " + e.Message + e.StackTrace); }
        }

        #region *** To do
        internal void RequestQueryCall(String connId)
        {
           TpsLogManager<SipServer>.Info("Requesting query for connId [" + (connId == null ? "" : connId) + "] on DN [" + (_extension == null ? "" : _extension) + "]");

            try
            {
                if ((_extension == null) || (_extension.CompareTo("") == 0))
                    TpsLogManager<SipServer>.Error("Error requestQueryCall No extension provided for query call.");
                //throw new Exception("No extension provided for query call.");

                if ((connId == null) || (connId.CompareTo("") == 0))
                    TpsLogManager<SipServer>.Error("Error requestQueryCall No connid provided for query call.");
                //throw new Exception("No connid provided for query call.");

                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Queries.RequestQueryCall.Create(_extension, new ConnectionId(connId), CallInfoType.StatusQuery, null));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestQueryCall : " + e.Message + e.StackTrace); }
        }

        internal void RequestQueryAddress()
        {
            try
            {
               TpsLogManager<SipServer>.Info("Requesting query address on DN [" + (_extension == null ? "" : _extension) + "]");
                if ((_extension == null) || (_extension.CompareTo("") == 0))
                    throw new Exception("No extension provided for query address.");

                _tServerProtocol.Send(Genesyslab.Platform.Voice.Protocols.TServer.Requests.Queries.RequestQueryAddress.Create(_extension, AddressType.DN, AddressInfoType.AddressStatus));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestQueryAddress : " + e.Message + e.StackTrace); }
        }
        #endregion



        internal void RequestUpdateAttachedData(Hashtable newData, String connId, KeyValueCollection oldUserData)
        {

            try
            {
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided to attach updated data.");
                if (string.IsNullOrEmpty(connId)) throw new Exception("No connid provided to attach updated data.");
               TpsLogManager<SipServer>.Info("Requesting update attached data for connId [" + connId + "] on DN [" + _extension + "]");
                _tServerProtocol.Send(RequestUpdateUserData.Create(_extension, new ConnectionId(connId), UpdateAttachedDataTable(newData, oldUserData)));
            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestUpdateAttachedData : " + e.Message + e.StackTrace); }
        }

        private void RequestDeleteAttachedData(String connId)
        {
            try
            {
                if (string.IsNullOrEmpty(_extension)) throw new Exception("No extension provided for deleting attached data.");
                if (string.IsNullOrEmpty(connId)) throw new Exception("No connid provided for deleting attached data.");
               TpsLogManager<SipServer>.Info("Requesting delete attached data for connId [" + connId + "] on DN [" + _extension + "]");
                _tServerProtocol.Send(RequestDeleteUserData.Create(_extension, new ConnectionId(connId)));

            }
            catch (Exception e) { TpsLogManager<SipServer>.Error("Error requestDeleteAttachedData : " + e.Message + e.StackTrace); }
        }
        #endregion

        internal void ProcessReceivedMessages(IMessage response)
        {
            switch (response.Id)
            {
                case EventLinkConnected.MessageId:
                    try
                    {
                        EventLinkConnected theEvent = (EventLinkConnected)response;
                        RequestRegisterAddress();
                    }
                    catch (Exception e) { TpsLogManager<SipServer>.Error("Error dispatch_function EventLinkConnected : " + e.Message + e.StackTrace); }
                    break;
                case EventRestoreConnection.MessageId:
                    try
                    {
                        EventRestoreConnection theEvent = (EventRestoreConnection)response;
                        NotifyRestoreConnection(theEvent);
                    }
                    catch (Exception e) { TpsLogManager<SipServer>.Error("Error dispatch_function EventRestoreConnection : " + e.Message + e.StackTrace); }
                    break;
                default:
                    try
                    {

                        //using (ClientResponse clientResponse = new ClientResponse())
                        //    clientResponse.SendNotification(new SoftphoneClientEvent(_srConnectionId, response));

                        SoftphoneClientEvent softphoneClient = new SoftphoneClientEvent(_srConnectionId, response);
                        new ClientResponse().SendNotification(softphoneClient);
                        //Task task = Task.Factory.StartNew(() => { new ClientResponse().SendNotification(new SoftphoneClientEvent(_srConnectionId, response)); });
                    }
                    catch (Exception e) { TpsLogManager<SipServer>.Error("Error dispatch_function EventAgentLogin : " + e.Message + e.StackTrace); }
                    break;
            }
        }

    }
}
