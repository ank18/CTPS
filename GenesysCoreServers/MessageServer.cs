using Genesyslab.Platform.ApplicationBlocks.Commons.Broker;
using Genesyslab.Platform.ApplicationBlocks.Commons.Protocols;
using Genesyslab.Platform.Commons.Protocols;
using Genesyslab.Platform.Configuration.Protocols.Types;
using Genesyslab.Platform.Management.Protocols;
using Genesyslab.Platform.Management.Protocols.LocalControlAgent.Events;
using Genesyslab.Platform.Management.Protocols.LocalControlAgent.Requests;
using Genesyslab.Platform.Management.Protocols.LocalControlAgent.Responses;
using Genesyslab.Platform.Management.Protocols.MessageServer;
using Genesyslab.Platform.Management.Protocols.MessageServer.Requests;
using LogConfigLayer;
using System;
using System.Threading;

namespace GenesysCoreServers
{
    public class MessageServer : IMessageServer
    {
        public const int DefaultLcaTimeout = 30;
        public const String LcaProtocol = "Lca_App";

        public const int ModeUnknown = 0;
        public const int ModeStartPending = 1;
        public const int ModeStartTransition = 2;
        public const int ModeInitializing = 3;
        public const int ModeServiceUnavailable = 4;
        public const int ModeRunning = 5;
        public const int ModeStopPending = 6;
        public const int ModeStopping = 7;
        public const int ModeStopTransition = 8;
        public const int ModeStopped = 9;
        public const int ModeSuspended = 10;
        public const int ModeSuspending = 11;

        private readonly LcaConfiguration _lcaConfiguration = new LcaConfiguration(LcaProtocol);
        private MessageServerProtocol _messageServerProtocol;
        private LocalControlAgentProtocol _localControlAgentProtocol;
        private EventBrokerService _mEventBroker;

        
        private string applicationName = "";
        private int applicationDbId;
        private int lCAPort;
        private string messageServerHostName = "";
        private int messageServerPort;

        private int _lcaTimeout = DefaultLcaTimeout;
        
        private int _lastStatus = ModeUnknown;
        private Boolean _continueRunning = true;
        private bool _isLcaConnectedThreadRunning;
        private Thread checkLcaConnectionThread;

        public MessageServer(string appName, int dbid, int lcaPort, string host, int port)
        {
            applicationName = appName;
            TpsLogManager<MessageServer>.Info("Application Name set to [" + applicationName + "]");
            lCAPort = lcaPort;
            TpsLogManager<MessageServer>.Info("LCAPort set to [" + lCAPort + "]");

            checkLcaConnectionThread = new Thread(CheckLcaConnection) { Name = "Message server thread", IsBackground = true };
            checkLcaConnectionThread.Start();

            applicationDbId = dbid;
            TpsLogManager<MessageServer>.Info("Application DbId set to [" + applicationDbId + "]");
            messageServerHostName = host;
            TpsLogManager<MessageServer>.Info("Message server host name set to [" + messageServerHostName + "]");
            messageServerPort = port;
            TpsLogManager<MessageServer>.Info("Message server port set to[" + messageServerPort + "]");
            
            
            
            //ConnectToMessageServer();
        }

        public void SetStatus(int mode)
        {
            try
            {
                if (ConnectToLca())
                {
                    RequestUpdateStatus requestUpdateStatus = RequestUpdateStatus.Create();
                    requestUpdateStatus.ApplicationName = _lcaConfiguration.ClientName;
                    requestUpdateStatus.ExecutionMode = ApplicationExecutionMode.Primary;

                    switch (mode)
                    {
                        case ModeRunning:
                            requestUpdateStatus.ControlStatus = ApplicationStatus.Running.GetHashCode();
                            TpsLogManager<MessageServer>.Info("Application status set to [Running]");
                            break;
                        case ModeUnknown:
                            requestUpdateStatus.ControlStatus = ApplicationStatus.Unknown.GetHashCode();
                            TpsLogManager<MessageServer>.Info("Application status set to [Unknown]");
                            break;
                        case ModeInitializing:
                            requestUpdateStatus.ControlStatus = ApplicationStatus.Initializing.GetHashCode();
                            TpsLogManager<MessageServer>.Info("Application status set to [Initializing]");
                            break;
                        case ModeStopPending:
                            requestUpdateStatus.ControlStatus = ApplicationStatus.StopPending.GetHashCode();
                            TpsLogManager<MessageServer>.Info("Application status set to [Stop Pending]");
                            break;
                        case ModeServiceUnavailable:
                            requestUpdateStatus.ControlStatus = ApplicationStatus.ServiceUnavailable.GetHashCode();
                            TpsLogManager<MessageServer>.Info("Application status set to [Service Unavailable]");
                            break;
                        case ModeStartPending:
                            requestUpdateStatus.ControlStatus = ApplicationStatus.StartPending.GetHashCode();
                            TpsLogManager<MessageServer>.Info("Application status set to [Start Pending]");
                            break;
                        case ModeStartTransition:
                            requestUpdateStatus.ControlStatus = ApplicationStatus.StartTransition.GetHashCode();
                           TpsLogManager<MessageServer>.Info("Application status set to [Start Transition]");
                            break;
                        case ModeStopped:
                            requestUpdateStatus.ControlStatus = ApplicationStatus.Stopped.GetHashCode();
                            TpsLogManager<MessageServer>.Info("Application status set to [Stopped]");
                            break;
                        case ModeStopTransition:
                            requestUpdateStatus.ControlStatus = ApplicationStatus.StopTransition.GetHashCode();
                            TpsLogManager<MessageServer>.Info("Application status set to [Stop Transition]");
                            break;
                        case ModeSuspended:
                            requestUpdateStatus.ControlStatus = ApplicationStatus.Suspended.GetHashCode();
                            TpsLogManager<MessageServer>.Info("Application status set to [Suspended]");
                            break;
                        case ModeSuspending:
                            requestUpdateStatus.ControlStatus = ApplicationStatus.Suspending.GetHashCode();
                            TpsLogManager<MessageServer>.Info("Application status set to [Suspending]");
                            break;
                    }

                    _localControlAgentProtocol.Send(requestUpdateStatus);
                    TpsLogManager<MessageServer>.Info("Application status set.");
                    _lastStatus = mode;
                }
            }
            catch (Exception e) {
                 TpsLogManager<MessageServer>.Error(e.Message);
                 }
        }

        public void SendMessageToMessageServer(int entryId, LogCategory logCategory, LogLevel logLevel, String message)
        {
            try
            {
                if (!ConnectToMessageServer()) return;
                RequestLogMessage requestLogMessage = RequestLogMessage.Create();
                requestLogMessage.EntryId = entryId;
                requestLogMessage.EntryText = message ?? "";
                requestLogMessage.Time = DateTime.Now;
                requestLogMessage.Level = logLevel;
                requestLogMessage.EntryCategory = 0; // logCategory;
                _messageServerProtocol.ClientId = applicationDbId;
                _messageServerProtocol.Send(requestLogMessage);
                switch (logLevel)
                {
                    case LogLevel.Debug:

                        TpsLogManager<MessageServer>.Debug("Message [" + (message ?? "") + "] sent to message server.");
                        break;
                    case LogLevel.Alarm:

                        TpsLogManager<MessageServer>.Error("Message [" + (message ?? "") + "] sent to message server.");
                        break;
                    case LogLevel.Error:

                        TpsLogManager<MessageServer>.Error("Message [" + (message ?? "") + "] sent to message server.");
                        break;
                    case LogLevel.Info:

                        TpsLogManager<MessageServer>.Info("Message [" + (message ?? "") + "] sent to message server.");
                        break;
                    case LogLevel.Interaction:

                        TpsLogManager<MessageServer>.Info("Message [" + (message ?? "") + "] sent to message server.");
                        break;
                    case LogLevel.Unknown:
                    default:
                        TpsLogManager<MessageServer>.Info("Message [" + (message ?? "") + "] sent to message server.");
                        break;
                }
            }
            catch (Exception e)
            {
                TpsLogManager<MessageServer>.Error(e.Message);
            }
        }

        public Boolean IsConnectedToMessageServer()
        {
            Boolean result = false;

            try
            {
                if (_messageServerProtocol != null)
                {
                    if (_messageServerProtocol.State.GetHashCode() == ChannelState.Closed.GetHashCode())
                        result = false;
                    if (_messageServerProtocol.State.GetHashCode() == ChannelState.Closing.GetHashCode())
                        result = true;
                    if (_messageServerProtocol.State.GetHashCode() == ChannelState.Opened.GetHashCode())
                        result = true;
                    if (_messageServerProtocol.State.GetHashCode() == ChannelState.Opening.GetHashCode())
                        result = false;
                }
            }
            catch (Exception) { }

            return result;
        }

        private void OnEventChangeExecutionMode(IMessage message)
        {
            var @event = (EventChangeExecutionMode)message;

            _localControlAgentProtocol.ExecutionMode = @event.ExecutionMode;

            var response = ResponseExecutionModeChanged.Create();

            response.ExecutionMode = _localControlAgentProtocol.ExecutionMode;

            _localControlAgentProtocol.Send(response);

            if (_localControlAgentProtocol.ExecutionMode == ApplicationExecutionMode.Exiting)
            {
                TpsLogManager<MessageServer>.Warn("Application told to exit from solution control server!");
                Environment.Exit(0);
            }
        }

        public Boolean ConnectToMessageServer()
        {
            Boolean result = false;

            try
            {
                 TpsLogManager<MessageServer>.Info("Connecting to Message Server...");

                if (!IsConnectedToMessageServer())
                {
                    _messageServerProtocol = new MessageServerProtocol(new Endpoint(new Uri("tcp://" + (messageServerHostName) + ":" + messageServerPort)))
                        {
                            ClientType = (int) CfgAppType.CFGThirdPartyServer,
                            ClientName = applicationName,
                            ClientId = applicationDbId,
                            ClientHost = System.Net.Dns.GetHostName()
                        };
                     TpsLogManager<MessageServer>.Info("Message Server connection info\n" +
                    "Message Server Hostname: " + (messageServerHostName) + ")\n" +
                    "    Message Server Port: " + messageServerPort + "\n" +
                    "       Application Name: " + applicationName + "\n" +
                    "         Local Hostname: " + System.Net.Dns.GetHostName() )  ;
                    _messageServerProtocol.Open();
                    SendMessageToMessageServer(97000, LogCategory.Alarm, LogLevel.Info, applicationName + " Started");
                }
                else
                    result = true;
            }
            catch (Exception e)
            {
                TpsLogManager<MessageServer>.Error(e.Message);
                //ShutDown();
            }

            if (IsConnectedToMessageServer())
            {
                TpsLogManager<MessageServer>.Info("Connected to Message Server.");
            }
            else
            {
                TpsLogManager<MessageServer>.Error("Failed to connect to message server.");
            }
            return result;
        }

        public void DisconnectMessageServer()
        {
            try
            {
                //TpsLogManager<MessageServer>.Info("Disconnecting from Message Server...");
                _messageServerProtocol.Close();
            }
            catch (Exception e)
            {
               // TpsLogManager<MessageServer>.Error("Unable to disconnect from Message server: " + e.Message);
            }
            finally
            {
                _messageServerProtocol.Dispose();
                _messageServerProtocol = null;
               // TpsLogManager<MessageServer>.Info("Disconnected from Message Server.");
            }

        }

        public void ShutDown()
        {
            _continueRunning = false;
            while (_isLcaConnectedThreadRunning)
                Thread.Sleep(200);
            DisconnectLca();
            DisconnectMessageServer();
        }

        public Boolean SendAlarm(int code, String message)
        {
            Boolean result = false;
            RequestLogMessage alarm = RequestLogMessage.Create();

            try
            {

                TpsLogManager<MessageServer>.Info("Preparing to send alarm to message server.");

                if (IsConnectedToMessageServer() || ConnectToMessageServer())
                {
                    alarm.EntryId = code;
                    alarm.ClientHost = System.Net.Dns.GetHostName();
                    alarm.EntryText = message;
                    alarm.Level = LogLevel.Alarm;
                    alarm.EntryCategory = LogCategory.Alarm;
                    alarm.Time = DateTime.Now;
                    _messageServerProtocol.Send(alarm);
                    result = true;
                    TpsLogManager<MessageServer>.Info("Alarm [" + code + "], message [" + message + "] sent to message server.");
                }
            }
            catch (Exception e)
            {
                TpsLogManager<MessageServer>.Error("sendAlarm: " + e.Message);
            }

            return result;
        }

        private Boolean IsConnectedToLca()
        {
            Boolean result = false;

            try
            {
                if (_localControlAgentProtocol != null)
                {
                    if (_localControlAgentProtocol.State.GetHashCode() == ChannelState.Closed.GetHashCode())
                        result = false;
                    if (_localControlAgentProtocol.State.GetHashCode() == ChannelState.Closing.GetHashCode())
                        result = false;
                    if (_localControlAgentProtocol.State.GetHashCode() == ChannelState.Opened.GetHashCode())
                        result = true;
                    if (_localControlAgentProtocol.State.GetHashCode() == ChannelState.Opening.GetHashCode())
                        result = false;
                }
            }
            catch (Exception) { }

            return result;
        }

        public Boolean ConnectToLca()
        {
            Boolean result = false;

            try
            {
                if (!IsConnectedToLca())
                {
                    TpsLogManager<MessageServer>.Info("Connecting to Local Control Agent...");
                    _localControlAgentProtocol = new LocalControlAgentProtocol(lCAPort)
                    {
                        ClientName = applicationName,
                        ExecutionMode = ApplicationExecutionMode.Backup,
                        ControlStatus = (int)ApplicationStatus.Initializing,
                        Timeout = new TimeSpan(0, 0, _lcaTimeout)
                    };
                    //TpsLogManager<MessageServer>.Info("Initialising Local Control Agent parameters");
                    _mEventBroker = BrokerServiceFactory.CreateEventBroker(_localControlAgentProtocol);
                    _mEventBroker.Register(OnEventChangeExecutionMode, new MessageIdFilter(EventChangeExecutionMode.MessageId));
                    _localControlAgentProtocol.Open();
                    TpsLogManager<MessageServer>.Info("Connected to Local Control Agent.");
                }

                result = true;
            }
            catch (Exception e) { 
                TpsLogManager<MessageServer>.Error(e.Message); 
            }

            return result;
        }

        public void DisconnectLca()
        {
            try
            {
                 TpsLogManager<MessageServer>.Info("Disconnecting from Local Control Agent...");
                _localControlAgentProtocol.Close();
            }
            catch (Exception e)
            {
                TpsLogManager<MessageServer>.Error(e.Message);
            }
            finally
            {
                _localControlAgentProtocol.Dispose();
                _localControlAgentProtocol = null;
                TpsLogManager<MessageServer>.Info("Disconnected from Local Control Agent.");
            }
        }

        private void CheckLcaConnection()
        {
            Boolean wasConnected = false;

            while (_continueRunning)
            {
                try
                {
                    Thread.Sleep(5 * 1000);
                }
                catch (Exception e)
                {
                    TpsLogManager<MessageServer>.Error(e.Message);
                }
                finally
                {
                    if (!wasConnected && IsConnectedToLca())
                    {
                        TpsLogManager<MessageServer>.Info("Connection to Local Control Agent reconnected.");
                        SetStatus(_lastStatus);
                        wasConnected = true;
                        _isLcaConnectedThreadRunning = true;
                    }
                    else if (wasConnected && !IsConnectedToLca())
                    {
                        TpsLogManager<MessageServer>.Warn("Connection to Local Control Agent lost.");
                        wasConnected = false;
                        _isLcaConnectedThreadRunning = false;
                    }
                }
            }
            _isLcaConnectedThreadRunning = false;
        }

        public void FinalizePSDKApplicationBlocks()
        {

            // Cleanup code
            _mEventBroker.Deactivate();

            _mEventBroker.Dispose();


            try
            {
                _messageServerProtocol.Close();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "\n" + ex.StackTrace + "\n");
            }

            _messageServerProtocol.Dispose();
            _messageServerProtocol = null;
        }
    }
}
