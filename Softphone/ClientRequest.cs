using Genesyslab.Platform.Commons.Collections;
using LogConfigLayer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Softphone.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Softphone
{
    public class ClientRequest //:IClientRequest
    {
        private static ConcurrentDictionary<string, AgentConnection> SoftphoneConnections = new ConcurrentDictionary<string, AgentConnection>();
        private const string Connect = "connect";
        private const string Disconnect = "disconnect-code-";
        private const string Notready = "not-ready-code-";
        private const string Ready = "ready";
        private const string Startconference = "conference";
        private const string Disconnectcodes = "disconnect-codes";
        private const string Notreadycodes = "not-ready-codes";
        private const string Dialline1 = "dial-line-1";
        private const string Dialline2 = "dial-line-2";
        private const string Hangupline1 = "hang-up-line-1";
        private const string Hangupline2 = "hang-up-line-2";
        private const string Transferoptions = "transfer-options";
        private const string Holdline1 = "hold-line-1";
        private const string Holdline2 = "hold-line-2";
        private const string Retrieveline1 = "retrieve-line-1";
        private const string Retrieveline2 = "retrieve-line-2";
        private const string Coldtransfer = "cold";
        private const string Warmtransfer = "warm";
        private const string Completetransfer = "completeWarm";
        private const string Completconference = "completeConference";
        private const string Idv = "idv";
        private const string Togglelines = "toggle-lines";
        private const string Muteline1 = "mute-line-1";
        private const string Muteline2 = "mute-line-2";
        private const string Unmuteline1 = "unmute-line-1";
        private const string Unmuteline2 = "unmute-line-2";
        private const string Entersecuremode = "enter-secure-mode";
        private const string Exitsecuremode = "exit-secure-mode";
        private const string Resetpan = "reset-pan";
        private const string Resetcvc = "reset-cvc";
        private const string ConnectionCount = "connection-count";
        private const string ConnectionSummary = "connection-summary";
        private const string ConnectionDetail = "connection-detail";
        private const string Subscribeconcierge = "subscribe-concierge";
        private const string Unsubscribeconcierge = "unsubscribe-concierge";
        private const string Callbackrequest = "callback";
        private const string Cancelcallback = "cancel-callback-request";
        private const string Outbounddialleroutcome = "odOutcome";
        private const string Dtmf = "dtmf";


        private bool IsValidRequest(AgentConnection agentConnection, string command)
        {
            try
            {
                if (command != null)
                {

                    if (command == "connect")
                    {
                        if (agentConnection.SipServer == null)
                        {
                            //if (WebSocketPipe.AgentConnectionsDictionary.Keys.Where(o => o.Socket.IsSameSocket(agentConnection.Socket)).Count() > 1)
                            //{
                            //    string trash;
                            //    WebSocketPipe.AgentConnectionsDictionary.TryRemove(WebSocketPipe.AgentConnectionsDictionary.Keys.Where(o => o.Socket.IsSameSocket(agentConnection.Socket)).Last(), out trash);
                            //    return false;
                            //}
                            return true;
                        }
                        else
                            return false;
                    }
                    else
                    {
                        if (command.Contains(Notreadycodes) || command.Contains(Disconnectcodes) || command.Contains(Transferoptions) || (command.Contains("disconnectcodes")) || (command.Contains("notreadycodes")) || (command.Contains("transferoptions")) || (command.Contains(Subscribeconcierge)) || (command.Contains(Unsubscribeconcierge)) || (command.Contains(Callbackrequest)) || (command.Contains(Cancelcallback)) || command.Contains(ConnectionCount) || command.Contains(ConnectionSummary) || command.Contains(ConnectionDetail))
                            return true;
                        if (agentConnection.LastHandset != null)
                        {

                            switch (agentConnection.LastHandset.Lines.Line1.LineStatus)
                            {
                                case "NotAvailable":
                                    switch (agentConnection.LastHandset.Lines.Line2.LineStatus)
                                    {
                                        case "NotAvailable": return IsValidCommandForL1NotAvaialableL2NotAvaialable(command);
                                        default: return true;
                                    }
                                case "NoCall":
                                    switch (agentConnection.LastHandset.Lines.Line2.LineStatus)
                                    {
                                        case "NotAvailable": return IsValidCommandForL1NoCallL2NotAvaialable(command, agentConnection);
                                        default: return true;
                                    }
                                case "OnCall":
                                    switch (agentConnection.LastHandset.Lines.Line2.LineStatus)
                                    {
                                        case "NoCall": return IsValidCommandForL1OnCallL2NoCall(command);
                                        case "OnHold": return IsValidCommandForL1OnCallL2OnHold(command);
                                        default: return true;
                                    }
                                case "OnHold":
                                    switch (agentConnection.LastHandset.Lines.Line2.LineStatus)
                                    {
                                        case "NoCall": return IsValidCommandForL1OnHoldL2NoCall(command);
                                        case "Outbound": return IsValidCommandForL1OnHoldL2Outbound(command);
                                        case "Conference": return IsValidCommandForL1OnHoldL2Conference(command);
                                        case "Transfer": return IsValidCommandForL1OnHoldL2Transfer(command);
                                        case "OnHold": return IsValidCommandForL1OnHoldL2OnHold(command);
                                        default: return true;
                                    }
                                default: return true;
                            }
                        }
                        return true;
                    }
                }
                else
                    return false;
            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "Error IsValidRequest : " + e.Message + " : " + e.ToString());
                return false;
            }
        }

        private bool IsValidCommandForL1OnHoldL2OnHold(string command)
        {
            #region Check valid command
            if (command.Contains(Disconnect))
            {
                return false;
            }
            else if (command.Contains(Notready))
            {
                return false;
            }
            else if (command.Contains(Startconference))
            {
                return false;
            }
            else if (command.Contains(Coldtransfer))
            {
                return false;
            }
            else if (command.Contains(Warmtransfer))
            {
                return false;
            }
            else if (command.Contains(Completetransfer))
            {
                return false;

            }
            else
            {
                switch (command)
                {
                    case Connect: return false;
                    case Ready: return false;
                    case Disconnectcodes: return true;
                    case Notreadycodes: return true;
                    case Dialline1: return false;
                    case Dialline2: return false;
                    case Hangupline1: return false;
                    case Hangupline2: return false;
                    case Transferoptions: return true;
                    case Holdline1: return false;
                    case Holdline2: return false;
                    case Muteline1: return true;
                    case Muteline2: return true;
                    case Unmuteline1: return true;
                    case Unmuteline2: return true;
                    case Retrieveline1: return true;
                    case Retrieveline2: return true;
                    case Completconference: return false;
                    case Idv: return true;
                    case Togglelines: return false;
                    case Entersecuremode: return true;
                    case Exitsecuremode: return true;
                    case Resetpan: return true;
                    case Resetcvc: return true;
                    case Outbounddialleroutcome: return false;
                    case Dtmf: return true;
                    default: return false;
                }
            }
            #endregion
        }

        private bool IsValidCommandForL1OnCallL2OnHold(string command)
        {
            #region Check valid command
            if (command.Contains(Disconnect))
            {
                return false;
            }
            else if (command.Contains(Notready))
            {
                return false;
            }
            else if (command.Contains(Startconference))
            {
                return false;
            }
            else if (command.Contains(Coldtransfer))
            {
                return false;
            }
            else if (command.Contains(Warmtransfer))
            {
                return false;
            }
            else if (command.Contains(Completetransfer))
            {
                return false;

            }
            else
            {
                switch (command)
                {
                    case Connect: return false;
                    case Ready: return false;
                    case Disconnectcodes: return true;
                    case Notreadycodes: return true;
                    case Dialline1: return false;
                    case Dialline2: return false;
                    case Hangupline1: return true;
                    case Hangupline2: return false;
                    case Transferoptions: return true;
                    case Holdline1: return true;
                    case Holdline2: return false;
                    case Muteline1: return true;
                    case Muteline2: return true;
                    case Unmuteline1: return true;
                    case Unmuteline2: return true;
                    case Retrieveline1: return false;
                    case Retrieveline2: return false;
                    case Completconference: return false;
                    case Idv: return true;
                    case Togglelines: return true;
                    case Entersecuremode: return true;
                    case Exitsecuremode: return true;
                    case Resetpan: return true;
                    case Resetcvc: return true;
                    case Outbounddialleroutcome: return false;
                    case Dtmf: return true;
                    default: return false;
                }
            }
            #endregion
        }

        private bool IsValidCommandForL1OnHoldL2Transfer(string command)
        {
            #region Check valid command
            if (command.Contains(Disconnect))
            {
                return false;
            }
            else if (command.Contains(Notready))
            {
                return false;
            }
            else if (command.Contains(Startconference))
            {
                return false;
            }
            else if (command.Contains(Coldtransfer))
            {
                return false;
            }
            else if (command.Contains(Warmtransfer))
            {
                return false;
            }
            else if (command.Contains(Completetransfer))
            {
                return true;

            }
            else
            {
                switch (command)
                {
                    case Connect: return false;
                    case Ready: return false;
                    case Disconnectcodes: return true;
                    case Notreadycodes: return true;
                    case Dialline1: return false;
                    case Dialline2: return false;
                    case Hangupline1: return false;
                    case Hangupline2: return true;
                    case Transferoptions: return true;
                    case Holdline1: return false;
                    case Holdline2: return true;
                    case Muteline1: return true;
                    case Muteline2: return true;
                    case Unmuteline1: return true;
                    case Unmuteline2: return true;
                    case Retrieveline1: return false;
                    case Retrieveline2: return false;
                    case Completconference: return false;
                    case Idv: return true;
                    case Togglelines: return true;
                    case Entersecuremode: return true;
                    case Exitsecuremode: return true;
                    case Resetpan: return true;
                    case Resetcvc: return true;
                    case Outbounddialleroutcome: return false;
                    case Dtmf: return true;
                    default: return false;
                }
            }
            #endregion
        }

        private bool IsValidCommandForL1OnHoldL2Conference(string command)
        {
            #region Check valid command
            if (command.Contains(Disconnect))
            {
                return false;
            }
            else if (command.Contains(Notready))
            {
                return false;
            }
            else if (command.Contains(Startconference))
            {
                return false;
            }
            else if (command.Contains(Coldtransfer))
            {
                return false;
            }
            else if (command.Contains(Warmtransfer))
            {
                return false;
            }
            else if (command.Contains(Completetransfer))
            {
                return false;

            }
            else
            {
                switch (command)
                {
                    case Connect: return false;
                    case Ready: return false;
                    case Disconnectcodes: return true;
                    case Notreadycodes: return true;
                    case Dialline1: return false;
                    case Dialline2: return false;
                    case Hangupline1: return false;
                    case Hangupline2: return true;
                    case Transferoptions: return true;
                    case Holdline1: return false;
                    case Holdline2: return true;
                    case Muteline1: return true;
                    case Muteline2: return true;
                    case Unmuteline1: return true;
                    case Unmuteline2: return true;
                    case Retrieveline1: return false;
                    case Retrieveline2: return false;
                    case Completconference: return true;
                    case Idv: return true;
                    case Togglelines: return true;
                    case Entersecuremode: return true;
                    case Exitsecuremode: return true;
                    case Resetpan: return true;
                    case Resetcvc: return true;
                    case Outbounddialleroutcome: return false;
                    case Dtmf: return true;
                    default: return false;
                }
            }
            #endregion
        }

        private bool IsValidCommandForL1OnHoldL2Outbound(string command)
        {
            #region Check valid command
            if (command.Contains(Disconnect))
            {
                return false;
            }
            else if (command.Contains(Notready))
            {
                return false;
            }
            else if (command.Contains(Startconference))
            {
                return false;
            }
            else if (command.Contains(Coldtransfer))
            {
                return false;
            }
            else if (command.Contains(Warmtransfer))
            {
                return false;
            }
            else if (command.Contains(Completetransfer))
            {
                return false;

            }
            else
            {
                switch (command)
                {
                    case Connect: return false;
                    case Ready: return false;
                    case Disconnectcodes: return true;
                    case Notreadycodes: return true;
                    case Dialline1: return false;
                    case Dialline2: return false;
                    case Hangupline1: return false;
                    case Hangupline2: return true;
                    case Transferoptions: return true;
                    case Holdline1: return false;
                    case Holdline2: return true;
                    case Muteline1: return true;
                    case Muteline2: return true;
                    case Unmuteline1: return true;
                    case Unmuteline2: return true;
                    case Retrieveline1: return false;
                    case Retrieveline2: return false;
                    case Completconference: return false;
                    case Idv: return true;
                    case Togglelines: return true;
                    case Entersecuremode: return true;
                    case Exitsecuremode: return true;
                    case Resetpan: return true;
                    case Resetcvc: return true;
                    case Outbounddialleroutcome: return false;
                    case Dtmf: return true;
                    default: return false;
                }
            }
            #endregion
        }

        private bool IsValidCommandForL1OnHoldL2NoCall(string command)
        {
            #region Check valid command
            if (command.Contains(Disconnect))
            {
                return false;
            }
            else if (command.Contains(Notready))
            {
                return false;
            }
            else if (command.Contains(Startconference))
            {
                return false;
            }
            else if (command.Contains(Coldtransfer))
            {
                return false;
            }
            else if (command.Contains(Warmtransfer))
            {
                return false;
            }
            else if (command.Contains(Completetransfer))
            {
                return false;
            }
            else
            {
                switch (command)
                {
                    case Connect: return false;
                    case Ready: return false;
                    case Disconnectcodes: return true;
                    case Notreadycodes: return true;
                    case Dialline1: return false;
                    case Dialline2: return true;
                    case Hangupline1: return false;
                    case Hangupline2: return false;
                    case Transferoptions: return true;
                    case Holdline1: return false;
                    case Holdline2: return false;
                    case Muteline1: return true;
                    case Muteline2: return false;
                    case Unmuteline1: return true;
                    case Unmuteline2: return false;
                    case Retrieveline1: return true;
                    case Retrieveline2: return false;
                    case Completconference: return false;
                    case Idv: return true;
                    case Togglelines: return false;
                    case Entersecuremode: return true;
                    case Exitsecuremode: return true;
                    case Resetpan: return true;
                    case Resetcvc: return true;
                    case Outbounddialleroutcome: return false;
                    case Dtmf: return true;
                    default: return false;
                }
            }
            #endregion
        }

        private bool IsValidCommandForL1OnCallL2NoCall(string command)
        {
            #region Check valid command
            if (command.Contains(Disconnect))
            {
                return false;
            }
            else if (command.Contains(Notready))
            {
                return false;
            }
            else if (command.Contains(Startconference))
            {
                return true;
            }
            else if (command.Contains(Coldtransfer))
            {
                return true;
            }
            else if (command.Contains(Warmtransfer))
            {
                return true;
            }
            else if (command.Contains(Completetransfer))
            {
                return false;
            }
            else
            {
                switch (command)
                {
                    case Connect: return false;
                    case Ready: return false;
                    case Disconnectcodes: return true;
                    case Notreadycodes: return true;
                    case Dialline1: return false;
                    case Dialline2: return true;
                    case Hangupline1: return true;
                    case Hangupline2: return false;
                    case Transferoptions: return true;
                    case Holdline1: return true;
                    case Holdline2: return false;
                    case Muteline1: return true;
                    case Muteline2: return false;
                    case Unmuteline1: return true;
                    case Unmuteline2: return false;
                    case Retrieveline1: return false;
                    case Retrieveline2: return false;
                    case Completconference: return false;
                    case Idv: return true;
                    case Togglelines: return false;
                    case Entersecuremode: return true;
                    case Exitsecuremode: return true;
                    case Resetpan: return true;
                    case Resetcvc: return true;
                    case Outbounddialleroutcome: return false;
                    case Dtmf: return true;
                    default: return false;
                }
            }
            #endregion
        }

        private bool IsValidCommandForL1NoCallL2NotAvaialable(string command, AgentConnection conn)
        {
            #region Check valid command
            if (command.Contains(Disconnect))
            {
                return true;
            }
            else if (command.Contains(Notready))
            {
                return true;
            }
            else if (command.Contains(Startconference))
            {
                return false;
            }
            else if (command.Contains(Coldtransfer))
            {
                return false;
            }
            else if (command.Contains(Warmtransfer))
            {
                return false;
            }
            else if (command.Contains(Completetransfer))
            {
                return false;
            }
            else
            {
                switch (command)
                {
                    case Connect: return false;
                    case Ready: return true;
                    case Disconnectcodes: return true;
                    case Notreadycodes: return true;
                    case Dialline1: return conn.AgentState == AgentStates.NotreadyOutbound ? true : false;
                    case Dialline2: return false;
                    case Hangupline1: return false;
                    case Hangupline2: return false;
                    case Transferoptions: return true;
                    case Holdline1: return false;
                    case Holdline2: return false;
                    case Muteline1: return false;
                    case Muteline2: return false;
                    case Unmuteline1: return false;
                    case Unmuteline2: return false;
                    case Retrieveline1: return false;
                    case Retrieveline2: return false;
                    case Completconference: return false;
                    case Idv: return false;
                    case Togglelines: return false;
                    case Entersecuremode: return false;
                    case Exitsecuremode: return false;
                    case Resetpan: return false;
                    case Resetcvc: return false;
                    case Outbounddialleroutcome: return false;
                    case Dtmf: return false;
                    default: return false;
                }
            }
            #endregion
        }

        private bool IsValidCommandForL1NotAvaialableL2NotAvaialable(string command)
        {
            #region Check valid command
            if (command.Contains(Disconnect))
            {
                return true;
            }
            else if (command.Contains(Notready))
            {
                return true;
            }
            else if (command.Contains(Startconference))
            {
                return false;
            }
            else if (command.Contains(Coldtransfer))
            {
                return false;
            }
            else if (command.Contains(Warmtransfer))
            {
                return false;
            }
            else if (command.Contains(Completetransfer))
            {
                return false;
            }
            else
            {
                switch (command)
                {
                    case Connect: return false;
                    case Ready: return true;
                    case Disconnectcodes: return true;
                    case Notreadycodes: return true;
                    case Dialline1: return false;
                    case Dialline2: return false;
                    case Hangupline1: return false;
                    case Hangupline2: return false;
                    case Transferoptions: return true;
                    case Holdline1: return false;
                    case Holdline2: return false;
                    case Muteline1: return false;
                    case Muteline2: return false;
                    case Unmuteline1: return false;
                    case Unmuteline2: return false;
                    case Retrieveline1: return false;
                    case Retrieveline2: return false;
                    case Completconference: return false;
                    case Idv: return false;
                    case Togglelines: return false;
                    case Entersecuremode: return false;
                    case Exitsecuremode: return false;
                    case Resetpan: return false;
                    case Resetcvc: return false;
                    case Outbounddialleroutcome: return true;
                    case Dtmf: return false;
                    default: return false;
                }
            }
            #endregion
        }

        public void ProcessClientMessage(string socket , String message)
        {
            try
            {

                #region Ignore invalid json message
                if (!(message.StartsWith("{") && message.EndsWith("}")))
                {
                    TpsLogManager<ClientRequest>.Error( "Error processClientMessage - Invalid json message " + message + " received from " + socket);
                    return;
                }
                #endregion
                try
                {
                    JObject request = new JObject();
                    request = JObject.Parse(message);
                    if (request.First == null)
                    {
                        TpsLogManager<ClientRequest>.Error( "Error processClientMessage - Invalid json message " + message + " received from " + socket);
                        return;
                    }

                    string command = ((Newtonsoft.Json.Linq.JProperty)(request.First)).Name;
                    #region Ignore invalid command
                    if (String.IsNullOrEmpty(command))
                    {
                        TpsLogManager<ClientRequest>.Debug("Error processClientMessage : Invalid command string received from " + socket);
                        return;
                    }
                    #endregion
                    ProcessMessage(socket, message, request, command);
                }
                catch (JsonException e)
                {
                    if (String.IsNullOrEmpty(message))
                    {
                        TpsLogManager<ClientRequest>.Debug(, "Error processClientMessage : Invalid command string received from " + socket);
                        return;
                    }
                    TpsLogManager<ClientRequest>.Error( "Error processClientMessage InvalidJson received from " + socket + ": " + e.Message + e.StackTrace);
                    return;
                }

            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "Error processClientMessage : " + e.Message + e.StackTrace);
                return;
            }
        }

        private void ProcessMessage(string socket, String theMessage, JObject request, string command)
        {
            try
            {
                AgentConnection agentConnection = null;
                if (SoftphoneConnections.ContainsKey(socket))
                {
                    agentConnection = SoftphoneConnections[socket];
                    if (agentConnection.SipServer != null)
                        TpsLogManager<ClientRequest>.Info("Processing message received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + "): " + theMessage);
                }
                else
                {
                    agentConnection = new AgentConnection();
                    TpsLogManager<ClientRequest>.Info("Connect message received from " + socket +" : " + theMessage);
                }

                #region Parse Json message
                if (IsValidRequest(agentConnection, command))
                {
                    if (command.Contains(Disconnect))
                    {
                        ProcessAgentLogout(agentConnection, request);
                    }
                    else if (command.Contains(Notready))
                    {
                        ProcessAgentNotReady(agentConnection, request);
                    }
                    else if (command.Contains(Startconference))
                    {
                        ProcessAgentConferencing(agentConnection, request);
                    }
                    else if (command.Contains(Coldtransfer))
                    {
                        ProcessSingleStepTransfer(agentConnection, request);
                    }
                    else if (command.Contains(Warmtransfer))
                    {
                        ProcessTwoStepTransfer(agentConnection, request);
                    }
                    else if (command.Contains(Completetransfer))
                    {
                        ProcessCompleteTransfer(agentConnection, request);
                    }
                    else
                    {
                        switch (command)
                        {
                            case Connect:
                                ProcessConnection(agentConnection, theMessage, request);
                                break;
                            case Outbounddialleroutcome:
                                ProcessOutboundDiallerOutcome(agentConnection, request);
                                break;
                            case Ready:
                                ProcessAgentReady(agentConnection);
                                break;
                            case Disconnectcodes:
                                ProcessDisconnectCodes(agentConnection);
                                break;
                            case Notreadycodes:
                                ProcessNotReadyCodes(agentConnection);
                                break;
                            case Dialline1:
                                ProcessMakeCall(agentConnection, request, 1);
                                break;
                            case Dialline2:
                                ProcessMakeCall(agentConnection, request, 2);
                                break;
                            case Holdline1:
                                ProcessAgentHoldLine1(agentConnection, request);
                                break;
                            case Holdline2:
                                ProcessAgentHoldLine2(agentConnection, request);
                                break;
                            case Retrieveline1:
                                ProcessAgentRetrieveLine1(agentConnection, request);
                                break;
                            case Retrieveline2:
                                ProcessAgentRetrieveLine2(agentConnection, request);
                                break;
                            case Hangupline1:
                                ProcessAgentHangupLine1(agentConnection, theMessage);
                                break;
                            case Hangupline2:
                                ProcessAgentHangupLine2(agentConnection, theMessage);
                                break;
                            case Transferoptions:
                                ProcessTransferOptions(agentConnection);
                                break;
                            case Completconference:
                                ProcessCompleteConferencing(agentConnection, request);
                                break;
                            case Idv:
                                ProcessIdv(agentConnection, request);
                                break;
                            case Togglelines:
                                ProcessToggleLines(agentConnection, request);
                                break;
                            case Muteline1:
                                ProcessMuteLine(agentConnection, request, 1);
                                break;
                            case Muteline2:
                                ProcessMuteLine(agentConnection, request, 2);
                                break;
                            case Unmuteline1:
                                ProcessUnMuteLine(agentConnection, request, 1);
                                break;
                            case Unmuteline2:
                                ProcessUnMuteLine(agentConnection, request, 2);
                                break;
                            case Entersecuremode:
                                ProcessPciEnterSecureMode(agentConnection, request);
                                break;
                            case Exitsecuremode:
                                ProcessPciExitSecureMode(agentConnection, theMessage);
                                break;
                            case Resetpan:
                                ProcessPciResetPan(agentConnection, theMessage);
                                break;
                            case Resetcvc:
                                ProcessPciResetCvc(agentConnection, theMessage);
                                break;
                            case Subscribeconcierge:
                                ProcessConcirgeSubscription(agentConnection, request, true);
                                break;
                            case Unsubscribeconcierge:
                                ProcessConcirgeSubscription(agentConnection, request, false);
                                break;
                            case Callbackrequest:
                                ProcessCallbackRequest(agentConnection, request);
                                break;
                            case Cancelcallback:
                                ProcessCancelCallback(agentConnection, request);
                                break;
                            case Dtmf:
                                ProcessDtmfTone(agentConnection, request);
                                break;
                            case ConnectionCount:
                                ProcessConnectionCount(agentConnection);
                                break;
                            case ConnectionSummary:
                                ProcessConnectionSummary(agentConnection);
                                break;
                            case ConnectionDetail:
                                ProcessConnectionDetail(agentConnection);
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    string errorMessage = "Invalid request message received from " + (agentConnection.SipServer == null ? agentConnection.Socket : agentConnection.AgentUserName) + "(" + agentConnection.AgentHostName + "): " + theMessage;
                    new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), errorMessage, ErrorSeverity.Error);
                    TpsLogManager<ClientRequest>.Error( errorMessage);
                }
                #endregion
            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "Error processMessage : " + e.Message + " : " + e.ToString());
            }
        }


        private void ProcessConnection(AgentConnection agentConnection, String theMessage, JObject request)
        {
            try
            {
                TpsLogManager<ClientRequest>.Info( "Processing message received from " + agentConnection.Socket + ": " + theMessage);
                #region Read json message and extract hostname, agentID
                TpsLogManager<ClientRequest>.Debug(, "Enter Region : Read json message and extract hostname, username and issoftphone");
                String hostname = "";
                String domainUsername = "";
                Boolean isSoftPhone = false;
                foreach (var x in request)
                {
                    string name = x.Key;
                    JToken value = x.Value;

                    if (name.Equals("connect"))
                    {
                        try
                        {
                            hostname = value.Value<String>("hostname");
                            if (!string.IsNullOrEmpty(hostname)) hostname = hostname.ToUpper();
                        }
                        catch (Exception e)
                        {
                            new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Unable to retrieve hostname.", ErrorSeverity.Error);
                            TpsLogManager<ClientRequest>.Error( "Unable to retrieve hostname: " + e.Message + e.StackTrace);
                            AgentConnection.RemoveConnectionForAgentUserNameCloseConnection(agentConnection.AgentUserName);
                            return;
                        }
                        //TpsLogManager<ClientRequest>.Debug(, "In Region : Read json message and extract hostname: "+ hostname);

                        try
                        {
                            domainUsername = value.Value<String>("username");
                        }
                        catch (Exception e)
                        {
                            new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Unable to retrieve username.", ErrorSeverity.Error);
                            TpsLogManager<ClientRequest>.Error( "Unable to retrieve username: " + e.Message + e.StackTrace);
                            AgentConnection.RemoveConnectionForAgentUserNameCloseConnection(agentConnection.AgentUserName);
                            return;
                        }
                        //TpsLogManager<ClientRequest>.Debug(, "In Region : Read json message and extract username :" + domainUsername);
                        try
                        {
                            isSoftPhone = value.Value<Boolean>("issoftphone");
                        }
                        catch (Exception e)
                        {
                            TpsLogManager<ClientRequest>.Error( "Unable to retrieve isSoftPhone flag: " + e.Message + e.StackTrace);
                        }
                        //TpsLogManager<ClientRequest>.Debug(, "In Region : Read json message and extract issoftphone :" + isSoftPhone);
                    }
                    else
                    {
                        TpsLogManager<ClientRequest>.Error( "Could not identify request code");
                    }
                }
                TpsLogManager<ClientRequest>.Debug(, "Exit Region : Read json message and extracted hostname, username and issoftphone");
                #endregion

                TpsLogManager<ClientRequest>.Debug(, "Get Configuration Message");
                Dictionary<string, Dictionary<string, string>> configurationMessage = ConfigServer.ConfigServerInstance.GetConfigurationMessage(hostname, domainUsername);

                #region Get Extension and Switch
                Dictionary<string, string> extensionDict = configurationMessage.ContainsKey("extension") ? (Dictionary<string, string>)configurationMessage["extension"] : null;
                if (extensionDict == null)
                {
                    new ClientResponse().SendJsonErrorMessage(agentConnection, ResponseCode.ResponseNoMatchingExtension);
                    AgentConnection.RemoveConnectionForAgentUserNameCloseConnection(agentConnection.AgentUserName);
                    return;
                }

                String extension = extensionDict.ContainsKey("number") ? extensionDict.FirstOrDefault(x => x.Key == "number").Value : null; ;
                String extensionSwitch = extensionDict.ContainsKey("switch") ? extensionDict.FirstOrDefault(x => x.Key == "switch").Value : null; ;
                if (string.IsNullOrEmpty(extension))
                {
                    new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Request to login agent missing extension.", ErrorSeverity.Error);
                    return;
                }
                if (string.IsNullOrEmpty(extensionSwitch))
                {
                    new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Request to login agent missing extension switch.", ErrorSeverity.Error);
                    return;
                }
                #endregion

                #region Restore connection
                //List<agentConnection> agentConnectionList = ClientConnectionServer.connections.Keys.Where(o => o.AgentUserName == domainUsername).ToList();
                AgentConnection oldAgentConnection = null;
                foreach (var connection in WebSocketPipe.AgentConnectionsDictionary.Keys)
                {
                    if (connection.SipServer != null)
                    {
                        if (connection.SipServer.Extension == extension && connection.IsSoftphone == isSoftPhone && connection.SipServer.ExtensionSwitch == extensionSwitch)
                        {
                            oldAgentConnection = connection;
                        }
                    }
                }


                if (oldAgentConnection != null)
                {
                    TpsLogManager<ClientRequest>.Debug(, "Entered into regain call control");
                    // Regain call control
                    if (oldAgentConnection.Socket.IsSameSocket(agentConnection.Socket))
                    {
                        oldAgentConnection.IsAgentDisconnected = false;
                    }
                    else
                    {
                        //AgentConnection currentAgentConnection = AgentConnection.GetAgentConnection(resourceId);

                        if (oldAgentConnection.IsSoftphone == isSoftPhone)
                        {
                            if (agentConnection.IsAgentDisconnected)
                            {
                                agentConnection = oldAgentConnection.Copy(agentConnection.AgentUserName);
                                agentConnection.IsAgentDisconnected = false;
                                //agentConnection.IsLogoutRequired = true;
                                TpsLogManager<ClientRequest>.Debug(, "IsLogoutRequired = true from ClientRequest.ProcessConnection");
                                // agentConnection.EstablishedOnCall = true;
                                // TpsLogManager<ClientRequest>.Debug(, "AgentConnection.Established flag has been set to true");
                                if (oldAgentConnection.SipServer != null)
                                    oldAgentConnection.SipServer.Disconnect();
                                string trash;
                                WebSocketPipe.AgentConnectionsDictionary.TryRemove(oldAgentConnection, out trash);
                                WebSocketPipe.LogStats();
                            }
                            else
                            {
                                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), domainUsername + " already connected to " + oldAgentConnection.AgentHostName + " (" + oldAgentConnection.GetResourceId() + ")" + " browser.", ErrorSeverity.Error);
                                string trash;
                                WebSocketPipe.AgentConnectionsDictionary.TryRemove(agentConnection, out trash);
                                agentConnection.Socket.Close();
                                WebSocketPipe.LogStats();
                                return;
                            }
                        }

                    }

                }

                #endregion

                TpsLogManager<ClientRequest>.Debug(, "processAgentLogin for Agent " + domainUsername + "(" + hostname + ") BEGIN");
                ProcessAgentLogin(agentConnection, configurationMessage, domainUsername, hostname, isSoftPhone);
                TpsLogManager<ClientRequest>.Debug(, "processAgentLogin for Agent " + domainUsername + "(" + hostname + ") END");
            }
            catch (Exception ex)
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), ex.StackTrace, ErrorSeverity.Error);
                AgentConnection.RemoveConnectionForAgentUserNameLogoutAndCloseConnection(agentConnection.AgentUserName);
            }

        }

        private void ProcessAgentLogin(AgentConnection agentConnection, Dictionary<string, Dictionary<string, string>> configurationDict, string agentUserName, string agentHostName, Boolean isSoftPhone)
        {
            if (configurationDict == null)
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Missing critical data for agent (" + agentConnection.Socket + ") login. Login will not be attempted to SIP server.", ErrorSeverity.Error);
                return;
            }
            if (configurationDict.Count == 0)
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Missing critical data for agent (" + agentConnection.Socket + ") login. Login will not be attempted to SIP server.", ErrorSeverity.Error);
                return;
            }

            Dictionary<string, string> loginDict = configurationDict.ContainsKey("logins") ? (Dictionary<string, string>)configurationDict["logins"] : null;
            Dictionary<string, string> extensionDict = configurationDict.ContainsKey("extension") ? (Dictionary<string, string>)configurationDict["extension"] : null;
            Dictionary<string, string> sipServer1Dict = configurationDict.ContainsKey("sipserver1") ? (Dictionary<string, string>)configurationDict["sipserver1"] : null;
            Dictionary<string, string> sipServer2Dict = configurationDict.ContainsKey("sipserver2") ? (Dictionary<string, string>)configurationDict["sipserver2"] : null;

            if (loginDict == null)
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, ResponseCode.ResponseNoMatchingPersonOrAgent);
                return;
            }
            if (extensionDict == null)
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, ResponseCode.ResponseNoMatchingExtension);
                return;
            }
            if ((sipServer1Dict == null) || (sipServer2Dict == null))
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, ResponseCode.ResponseNoMatchingTserver);
                return;
            }

            #region Get AgentID
            String agentId = loginDict.ContainsKey("acdid") ? loginDict.FirstOrDefault(x => x.Key == "acdid").Value : null;
            if (string.IsNullOrEmpty(agentId))
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Request to login agent missing agent Id.", ErrorSeverity.Error);
                return;
            }
            #endregion

            #region Get Extension
            String extension = extensionDict.ContainsKey("number") ? extensionDict.FirstOrDefault(x => x.Key == "number").Value : null; ;
            if (string.IsNullOrEmpty(extension))
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Request to login agent missing extension.", ErrorSeverity.Error);
                return;
            }
            #endregion
            #region Get Extension switch
            String extensionSwitch = extensionDict.ContainsKey("switch") ? extensionDict.FirstOrDefault(x => x.Key == "switch").Value : null; ;
            if (string.IsNullOrEmpty(extensionSwitch))
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Request to login agent missing extension switch.", ErrorSeverity.Error);
                return;
            }
            #endregion


            #region Get tServerName
            String tServerName = extensionDict.ContainsKey("tServerName") ? extensionDict.FirstOrDefault(x => x.Key == "tServerName").Value : null; ;
            if (string.IsNullOrEmpty(tServerName))
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Request to login agent missing tServer.", ErrorSeverity.Error);
                return;
            }
            #endregion

            #region Get primary URL
            string sipserver1Hostname = sipServer1Dict.ContainsKey("hostname") ? sipServer1Dict.FirstOrDefault(x => x.Key == "hostname").Value : null; ;
            string sipserver1Port = sipServer1Dict.ContainsKey("port") ? sipServer1Dict.FirstOrDefault(x => x.Key == "port").Value : null; ;
            if (string.IsNullOrEmpty(sipserver1Hostname) || (string.IsNullOrEmpty(sipserver1Port)))
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Request to login agent missing primary SIP server definition.", ErrorSeverity.Error);
                return;
            }
            string primaryUrl = "tcp://" + sipserver1Hostname + ":" + sipserver1Port;
            #endregion

            #region Get Secondary URL
            string sipserver2Hostname = sipServer2Dict.ContainsKey("hostname") ? sipServer2Dict.FirstOrDefault(x => x.Key == "hostname").Value : null; ;
            string sipserver2Port = sipServer2Dict.ContainsKey("port") ? sipServer2Dict.FirstOrDefault(x => x.Key == "port").Value : null; ;
            if (string.IsNullOrEmpty(sipserver2Hostname) || (string.IsNullOrEmpty(sipserver2Port)))
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Request to login agent missing secondary SIP server definition.", ErrorSeverity.Error);
                return;
            }
            string secondaryUrl = "tcp://" + sipserver2Hostname + ":" + sipserver2Port;
            #endregion

            try
            {
                SipServer sipServer = new SipServer(agentUserName, extension, extensionSwitch, agentId, "", "", primaryUrl, secondaryUrl, isSoftPhone);

                //var agentConnection = AgentConnection.GetAgentConnection(resourceId);
                if (agentConnection != null)
                {
                    agentConnection.IsSoftphone = isSoftPhone;
                    agentConnection.SipServer = sipServer;
                    agentConnection.ServerName = tServerName;
                    agentConnection.LastActionRequested = AgentActionRequested.Connect;
                    agentConnection.AgentUserName = agentUserName;
                    agentConnection.AgentHostName = agentHostName;
                }
                else
                {
                    TpsLogManager<ClientRequest>.Error( "ProcessAgentLogin : Missing agentconnection for " + agentConnection.Socket + ".");
                }

                if (!sipServer.Connect(isSoftPhone))
                {
                    new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Failed to connect to SIP server.", ErrorSeverity.Error);
                    return;
                }


            }
            catch (Exception ex)
            {
                TpsLogManager<ClientRequest>.Error( "Missing critical data for agent " + agentConnection.Socket + " login. Login will not be attempted to SIP server." + Environment.NewLine + ex.Message);

                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), ex.Message, ErrorSeverity.Error);
            }
        }

        private void ProcessAgentLogout(AgentConnection agentConnection, JObject request)
        {
            if (agentConnection != null)
            {
                if (agentConnection.IsSoftphone)
                {
                    if (agentConnection.SipServer != null)
                    {
                        string reason = ((Newtonsoft.Json.Linq.JProperty)(request.First)).Name.Split('-')[2];
                        agentConnection.SipServer.RequestAgentLogout(reason);
                        agentConnection.LastActionRequested = AgentActionRequested.Disconnect;
                        // agentConnection.IsLogoutRequired = false;
                    }
                    else
                        TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
                }

                else /*******This is the issue when logging out when already logged out.  Just need to figure out how to drop the websocket connection*/
                {
                    agentConnection.SipServer.RequestUnregisterAddress();
                }
            }
        }

        private void ProcessAgentReady(AgentConnection agentConnection)
        {
            if (agentConnection.SipServer != null)
            {
                TpsLogManager<ClientRequest>.Info( "Process Agent Ready check OutboundDiallerConnId : " + agentConnection.OutboundDiallerConnId);
                if (String.IsNullOrEmpty(agentConnection.OutboundDiallerConnId))
                {
                    agentConnection.SipServer.RequestAgentReady();
                    agentConnection.LastActionRequested = AgentActionRequested.Ready;
                }
                else
                {
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because previous call has not been dispositioned.");
                    agentConnection.SendMessage("Ignoring ready request received as previous call has not been dispositioned");
                }

            }
            else
                TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
        }

        private void ProcessDisconnectCodes(AgentConnection agentConnection)
        {
            String message = "";
            try
            {
                if (agentConnection == null)
                {
                    TpsLogManager<ClientRequest>.Warn( "sendDisconnectCodes : Client has either been disconnected or doesn't exist");
                    return;
                }
                object thislock = new object();
                lock (thislock)
                {
                    message = JsonFactory.JsonFactoryInstance.DisconnectCodesMessage();
                }
                agentConnection.SendMessage(message);
                TpsLogManager<ClientRequest>.Info( "Disconnect codes sent to agent " + agentConnection.AgentUserName + " : " + message);
            }
            catch (Exception e) { Console.WriteLine(e.StackTrace); }
        }

        private void ProcessNotReadyCodes(AgentConnection agentConnection)
        {
            String message = "";
            try
            {
                if (agentConnection == null)
                {
                    TpsLogManager<ClientRequest>.Warn( "sendNotReadyCodes : Client has either been disconnected or doesn't exist");
                    return;
                }
                object thislock = new object();
                lock (thislock)
                {
                    message = JsonFactory.JsonFactoryInstance.NotReadyCodesMessage();
                }
                agentConnection.SendMessage(message);
                TpsLogManager<ClientRequest>.Info( "Not ready codes sent to agent " + agentConnection.AgentUserName + " : " + message);
            }
            catch (Exception e) { Console.WriteLine(e.StackTrace); }
        }

        private void ProcessTransferOptions(AgentConnection agentConnection)
        {
            String message = "";
            try
            {
                if (agentConnection == null)
                {
                    TpsLogManager<ClientRequest>.Warn( "sendTransferOptions : Client has either been disconnected or doesn't exist");
                    return;
                }
                message = JsonFactory.JsonFactoryInstance.TransferOptionsMessage();
                agentConnection.SendMessage(ZipUnzip.Zip(message));
                TpsLogManager<ClientRequest>.Info( "Transfer options sent to agent " + agentConnection.AgentUserName + " : " + message);
            }
            catch (Exception e) { Console.WriteLine(e.StackTrace); }
        }

        private void ProcessAgentNotReady(AgentConnection agentConnection, JObject request)
        {
            String reason = "";

            try
            {
                reason = ((Newtonsoft.Json.Linq.JProperty)(request.First)).Name.Split('-')[3];
            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Warn( "Request to make agent not ready missing reason.");
            }
            finally
            {
                if (agentConnection.SipServer != null)
                {

                    if (String.IsNullOrEmpty(agentConnection.OutboundDiallerConnId))
                    {
                        agentConnection.SipServer.RequestAgentNotReady(reason);
                        agentConnection.LastActionRequested = ConfigServer.ConfigServerInstance.NotReadyOutboundDialReasons.Contains(reason) ? AgentActionRequested.NotreadyOutbound : AgentActionRequested.NotreadyNormal;
                    }
                    else
                    {
                        TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because previous call has not been dispositioned.");
                        agentConnection.SendMessage("Ignoring not ready request received as previous call has not been dispositioned");
                    }
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
            }
        }

        private void ProcessAgentHangupLine1(AgentConnection agentConnection, string theMessage)
        {

            if (agentConnection.EstablishedOnCall)
            //Only try and disconnect the call if we got established.  If not do nothing.
            {
                if (!String.IsNullOrEmpty(agentConnection.Line1ConnId))
                {
                    TpsLogManager<ClientRequest>.Warn( "Call hangup on line 1.");
                    if (agentConnection.SipServer != null)
                    {
                        agentConnection.SipServer.RequestReleaseCall(agentConnection.Line1ConnId,
                            agentConnection.EventUserData);
                        agentConnection.LastActionRequested = AgentActionRequested.Hangupline1;
                    }
                    else
                        TpsLogManager<ClientRequest>.Warn(
                            "Ignoring request because SIP server connection not established yet.");
                }
                else
                {
                    TpsLogManager<ClientRequest>.Warn( "Request to hang up call on line 1 dosent have a persisted connId.");
                }
            }
            else
            {
                TpsLogManager<ClientRequest>.Warn( "Request to hang up call on line 1 for call that wasn't established.");
            }

        }

        private void ProcessAgentHangupLine2(AgentConnection agentConnection, string theMessage)
        {
            if (!String.IsNullOrEmpty(agentConnection.Line2ConnId))
            {
                TpsLogManager<ClientRequest>.Warn( "Call hangup on line 2.");
                if (agentConnection.SipServer != null)
                {
                    agentConnection.SipServer.RequestReleaseCall(agentConnection.Line2ConnId, agentConnection.EventUserData);
                    agentConnection.LastActionRequested = AgentActionRequested.Hangupline2;
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
            }
            else
            {
                TpsLogManager<ClientRequest>.Warn( "Request to hang up call on line 2 dosent have a persisted connId.");
            }

        }

        private void ProcessAgentHoldLine1(AgentConnection agentConnection, JObject request)
        {
            if (!String.IsNullOrEmpty(agentConnection.Line1ConnId))
            {
                TpsLogManager<ClientRequest>.Warn( "Request to hold call on line 1.");
                if (agentConnection.SipServer != null)
                {
                    agentConnection.SipServer.RequestHoldCall(agentConnection.Line1ConnId);
                    agentConnection.IsLine1OnHoldByAgent = true;
                    agentConnection.LastActionRequested = AgentActionRequested.Holdline1;
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
            }
            else
            {
                TpsLogManager<ClientRequest>.Warn( "Request to hold call on line 1 dosent have a persisted connId.");
            }
        }

        private void ProcessAgentHoldLine2(AgentConnection agentConnection, JObject request)
        {
            if (!String.IsNullOrEmpty(agentConnection.Line2ConnId))
            {
                TpsLogManager<ClientRequest>.Warn( "Request to hold call on line 2.");
                if (agentConnection.SipServer != null)
                {
                    agentConnection.SipServer.RequestHoldCall(agentConnection.Line2ConnId);
                    agentConnection.LastActionRequested = AgentActionRequested.Holdline2;
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
            }
            else
            {
                TpsLogManager<ClientRequest>.Warn( "Request to hold call on line 2 dosent have a persisted connId.");
            }
        }

        private void ProcessAgentRetrieveLine1(AgentConnection agentConnection, JObject request)
        {
            if (!String.IsNullOrEmpty(agentConnection.Line1ConnId))
            {
                TpsLogManager<ClientRequest>.Warn( "Request to Retrieve call on line 1.");
                if (agentConnection.SipServer != null)
                {
                    if (agentConnection.IsLine1OnHold == true)
                    {
                        agentConnection.IsLine1OnHoldByAgent = false;
                        agentConnection.SipServer.RequestRetrieveCall(agentConnection.Line1ConnId);
                        agentConnection.LastActionRequested = AgentActionRequested.Retrieveline1;
                    }
                    else
                        TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
                }
            }
            else
            {
                TpsLogManager<ClientRequest>.Warn( "Request to retrieve call on line 1 dosent have a persisted connId.");
            }
        }

        private void ProcessAgentRetrieveLine2(AgentConnection agentConnection, JObject request)
        {
            if (!String.IsNullOrEmpty(agentConnection.Line2ConnId))
            {
                TpsLogManager<ClientRequest>.Warn( "Request to Retrieve call on line 2.");
                if (agentConnection.SipServer != null)
                {
                    if (agentConnection.IsLine2OnHold == true)
                    {
                        agentConnection.SipServer.RequestRetrieveCall(agentConnection.Line2ConnId);
                        agentConnection.LastActionRequested = AgentActionRequested.Retrieveline2;
                    }
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
            }
            else
            {
                TpsLogManager<ClientRequest>.Warn( "Request to retrieve call on line 2 dosent have a persisted connId.");
            }

        }

        private void ProcessMuteLine(AgentConnection agentConnection, JObject request, int lineNumber)
        {
            if (lineNumber == 1)
            {
                TpsLogManager<ClientRequest>.Warn( "Request to mute call on line 1.");
                if (!String.IsNullOrEmpty(agentConnection.Line1ConnId))
                {

                    if (agentConnection.SipServer != null)
                    {
                        if (!agentConnection.IsLine1OnMute)
                        {
                            agentConnection.SipServer.RequestMuteCall(agentConnection.Line1ConnId);
                            agentConnection.LastActionRequested = AgentActionRequested.Muteline1;
                        }
                        else
                            TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because call is already on mute.");
                    }
                    else
                        TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
                }
                else
                {
                    TpsLogManager<ClientRequest>.Warn( "Request to mute call on line 1 dosent have a persisted connId.");
                }

            }
            if (lineNumber == 2)
            {
                TpsLogManager<ClientRequest>.Warn( "Request to mute call on line 2.");
                if (!String.IsNullOrEmpty(agentConnection.Line2ConnId))
                {
                    if (agentConnection.SipServer != null)
                    {
                        if (!agentConnection.IsLine2OnMute)
                        {
                            agentConnection.SipServer.RequestMuteCall(agentConnection.Line2ConnId);
                            agentConnection.LastActionRequested = AgentActionRequested.Muteline2;
                        }
                        else
                            TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because call is already on mute.");
                    }
                    else
                        TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
                }
                else
                {
                    TpsLogManager<ClientRequest>.Warn( "Request to mute call on line 2 dosent have a persisted connId.");
                }
            }
        }

        private void ProcessUnMuteLine(AgentConnection agentConnection, JObject request, int lineNumber)
        {
            if (lineNumber == 1)
            {
                TpsLogManager<ClientRequest>.Warn( "Request to unmute call on line 1.");
                if (!String.IsNullOrEmpty(agentConnection.Line1ConnId))
                {

                    if (agentConnection.SipServer != null)
                    {
                        if (agentConnection.IsLine1OnMute)
                        {
                            agentConnection.SipServer.RequestMuteCall(agentConnection.Line1ConnId);
                            agentConnection.LastActionRequested = AgentActionRequested.Unmuteline1;
                        }
                        else
                            TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because call is not on mute.");
                    }
                    else
                        TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
                }
                else
                {
                    TpsLogManager<ClientRequest>.Warn( "Request to unmute call on line 1 dosent have a persisted connId.");
                }

            }
            if (lineNumber == 2)
            {
                TpsLogManager<ClientRequest>.Warn( "Request to unmute call on line 2.");
                if (!String.IsNullOrEmpty(agentConnection.Line2ConnId))
                {
                    if (agentConnection.SipServer != null)
                    {
                        if (agentConnection.IsLine2OnMute)
                        {
                            agentConnection.SipServer.RequestMuteCall(agentConnection.Line2ConnId);
                            agentConnection.LastActionRequested = AgentActionRequested.Unmuteline2;
                        }
                        else
                            TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because call is not on mute.");
                    }
                    else
                        TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
                }
                else
                {
                    TpsLogManager<ClientRequest>.Warn( "Request to unmute call on line 2 dosent have a persisted connId.");
                }
            }
        }
        private void ProcessAgentConferencing(AgentConnection agentConnection, JObject request)
        {
            try
            {
                string transferEndpoint = "";
                string endpointType = "";
                string caseNumber = "";
                string contactor = "";
                foreach (var x in request)
                {
                    string name = x.Key;
                    JToken value = x.Value;
                    transferEndpoint = value.Value<String>("transferEndpoint");
                    endpointType = value.Value<String>("endpointType");
                    caseNumber = value.Value<String>("caseNumber");
                    contactor = value.Value<String>("contactor");
                }
                if (String.IsNullOrEmpty(contactor))
                    contactor = "DC";
                if (String.IsNullOrEmpty(caseNumber))
                {
                    caseNumber = "";
                    TpsLogManager<ClientRequest>.Warn( "Case number is not supplied.");
                }

                if (endpointType != null)
                {
                    if (endpointType.ToLower() == "transferlabel")
                    {
                        if (agentConnection.SipServer != null)
                        {
                            if (!string.IsNullOrEmpty(ConfigServer.ConfigServerInstance.GetRouting()))
                            {
                                //ConnectionId line2Conn = new ConnectionId(agentConnection.Line_1_ConnId.ToString());
                                //agentConnection.SipServer.requestConferenceCall(agentConnection.Line_1_ConnId, line2Conn, CMEDatabase.CMEDatabaseInstance.getRouting(), agentConnection.Line_1_UserData, transferEndpoint, caseNumber);
                                agentConnection.SipServer.RequestConferenceCall(agentConnection, transferEndpoint, caseNumber, contactor);
                                agentConnection.LastActionRequested = AgentActionRequested.Initiateconference;
                            }
                            else
                                TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because routing is null or empty.");
                        }
                        else
                            TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
                    }
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request because endpointType is not recognized.");
            }

            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "ON processAgentConferencing: " + e.Message + e.StackTrace);
            }

        }

        private void ProcessSingleStepTransfer(AgentConnection agentConnection, JObject request)
        {

            try
            {
                string transferEndpoint = "";
                string endpointType = "";
                string caseNumber = "";
                string contactor = "";
                foreach (var x in request)
                {
                    string name = x.Key;
                    JToken value = x.Value;
                    transferEndpoint = value.Value<String>("transferEndpoint");
                    endpointType = value.Value<String>("endpointType");
                    caseNumber = value.Value<String>("caseNumber");
                    contactor = value.Value<String>("contactor");
                }
                if (String.IsNullOrEmpty(contactor))
                    contactor = "DC";

                if (String.IsNullOrEmpty(caseNumber))
                {
                    caseNumber = "";
                    TpsLogManager<ClientRequest>.Warn( "Case number is not supplied.");

                }

                if (endpointType != null)
                {
                    if (endpointType.ToLower() == "transferlabel")
                    {
                        if (agentConnection.SipServer != null)
                        {
                            if (!string.IsNullOrEmpty(ConfigServer.ConfigServerInstance.GetRouting()))
                            {
                                agentConnection.SipServer.RequestSingleStepTransfer(agentConnection, transferEndpoint, caseNumber, contactor);
                                agentConnection.LastActionRequested = AgentActionRequested.Coldtransfer;
                            }
                            else
                                TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because routing is null or empty.");
                        }
                        else
                            TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
                    }
                }

                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request because endpointType is not recognized.");
            }

            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "ON processSingleStepTransfer: " + e.Message + e.StackTrace);
            }
        }

        private void ProcessTwoStepTransfer(AgentConnection agentConnection, JObject request)
        {
            try
            {
                string transferEndpoint = "";
                string endpointType = "";
                string caseNumber = "";
                string contactor = "";
                foreach (var x in request)
                {
                    string name = x.Key;
                    JToken value = x.Value;
                    transferEndpoint = value.Value<String>("transferEndpoint");
                    endpointType = value.Value<String>("endpointType");
                    caseNumber = value.Value<String>("caseNumber");
                    contactor = value.Value<String>("contactor");
                }
                if (String.IsNullOrEmpty(contactor))
                    contactor = "DC";
                if (String.IsNullOrEmpty(caseNumber))
                {
                    caseNumber = "";
                    TpsLogManager<ClientRequest>.Warn( "Case number is not supplied.");
                }

                if (endpointType != null)
                {
                    if (endpointType.ToLower() == "transferlabel")
                    {
                        if (agentConnection.SipServer != null)
                        {
                            if (!string.IsNullOrEmpty(ConfigServer.ConfigServerInstance.GetRouting()))
                            {
                                agentConnection.SipServer.RequestTwoStepTransfer(agentConnection, transferEndpoint, caseNumber, contactor);
                                agentConnection.LastActionRequested = AgentActionRequested.Warmtransfer;
                            }
                            else
                                TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because routing is null or empty.");
                        }
                        else
                            TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
                    }
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request because endpointType is not recognized.");
            }

            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "ON processTwoStepTransfer: " + e.Message + e.StackTrace);
            }
        }

        private void ProcessCompleteTransfer(AgentConnection agentConnection, JObject request)
        {
            try
            {
                if (agentConnection.SipServer != null)
                {
                    agentConnection.SipServer.RequestCompleteTransfer(agentConnection.Line1ConnId, agentConnection.Line2ConnId);
                    agentConnection.LastActionRequested = AgentActionRequested.Completewarmtransfer;
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");

            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "processCompleteTransfer: " + e.Message + e.StackTrace);
            }

        }

        private static void ProcessCompleteConferencing(AgentConnection agentConnection, JObject request)
        {
            try
            {
                if (agentConnection.SipServer != null)
                {
                    agentConnection.SipServer.RequestCompleteConference(agentConnection.Line2ConnId, agentConnection.Line1ConnId);
                    agentConnection.LastActionRequested = AgentActionRequested.Completconference;
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");

            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "processCompleteConferencing: " + e.Message + e.StackTrace);
            }

        }

        private void ProcessIdv(AgentConnection agentConnection, JObject request)
        {
            try
            {
                Dictionary<string, string> idvDictionary = new Dictionary<string, string>();
                #region Populate dictionary for all id&v attributes supplied
                foreach (var x in request)
                {
                    string name = x.Key;
                    JToken value = x.Value;
                    foreach (var y in x.Value)
                    {
                        var attribute = (Newtonsoft.Json.Linq.JProperty)(y);
                        idvDictionary.Add(Convert.ToString(attribute.Name), Convert.ToString(attribute.Value));
                    }
                }
                #endregion
                if (agentConnection.SipServer != null)
                {
                    if (string.IsNullOrEmpty(agentConnection.Line1ConnId))
                        TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because no call established on line 1.");
                    else
                    {
                        System.Collections.Hashtable newData = new System.Collections.Hashtable();
                        #region Update hashtable collection based on id&v dictionary to update genesys KV
                        foreach (string key in ConfigServer.ConfigServerInstance.GetCFGApplicationObject().Options.GetAsKeyValueCollection("interaction_data_mapping").AllKeys)
                        {
                            string val = ConfigServer.ConfigServerInstance.GetCFGApplicationObject().Options.GetAsKeyValueCollection("interaction_data_mapping").GetAsString(key);
                            if (idvDictionary.ContainsKey(val))
                                newData.Add(key, Convert.ToString(idvDictionary[val]));

                        }
                        #endregion

                        agentConnection.SipServer.RequestUpdateAttachedData(newData, agentConnection.Line1ConnId, agentConnection.EventUserData);
                        // agentConnection.LastActionRequested = AgentActionRequested.Idv;
                    }
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "processIDV: " + e.Message + e.StackTrace);
            }
        }

        private void ProcessToggleLines(AgentConnection agentConnection, JObject request)
        {
            try
            {
                if (agentConnection.SipServer != null)
                {
                    if ((agentConnection.IsLine1OnHold) && (!agentConnection.IsLine2OnHold))
                        agentConnection.SipServer.RequestAlternateCall(agentConnection.Line2ConnId, agentConnection.Line1ConnId);
                    else
                        if ((!agentConnection.IsLine1OnHold) && (agentConnection.IsLine2OnHold))
                        agentConnection.SipServer.RequestAlternateCall(agentConnection.Line1ConnId, agentConnection.Line2ConnId);
                    agentConnection.LastActionRequested = AgentActionRequested.Togglelines;
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "processToggleLines: " + e.Message + e.StackTrace);
            }
        }

        private void ProcessMakeCall(AgentConnection agentConnection, JObject request, int lineNumber)
        {
            try
            {
                string transferEndpoint = "";
                foreach (var x in request)
                {
                    string name = x.Key;
                    JToken value = x.Value;
                    transferEndpoint = value.Value<String>("transferEndpoint");
                }

                if (!String.IsNullOrEmpty(transferEndpoint))
                {
                    if (agentConnection.SipServer != null)
                    {
                        if (!string.IsNullOrEmpty(ConfigServer.ConfigServerInstance.GetRouting()))
                        {
                            if (String.IsNullOrEmpty(agentConnection.Line1ConnId))
                            {
                                if (agentConnection.AgentState == AgentStates.NotreadyOutbound)
                                {

                                    if (transferEndpoint.Contains("TransferLabel"))
                                    {
                                        agentConnection.SipServer.RequestMakeCall(transferEndpoint, ConfigServer.ConfigServerInstance.GetRouting(), agentConnection);
                                        agentConnection.LastActionRequested = AgentActionRequested.Dialline1;
                                    }
                                    else
                                    {
                                        agentConnection.SipServer.RequestMakeCall("", transferEndpoint, agentConnection);
                                        agentConnection.LastActionRequested = AgentActionRequested.Dialline1;
                                    }
                                }
                            }
                            else
                            {
                                if ((lineNumber == 2) && (!agentConnection.IsLine1OnHold))
                                    agentConnection.SipServer.RequestHoldCall(agentConnection.Line1ConnId);
                                int attemptCount = 0;
                                while (!agentConnection.IsLine1OnHold)
                                {
                                    Thread.Sleep(1000);
                                    attemptCount++;
                                    if (attemptCount > 9)
                                        break;
                                }
                                if (!agentConnection.IsLine1OnHold)
                                {
                                    TpsLogManager<ClientRequest>.Error( "processMakeCall: Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because connection 1 hold request failed.");
                                }
                                else
                                {
                                    if (transferEndpoint.Contains("TransferLabel"))
                                    {
                                        agentConnection.SipServer.RequestMakeCall(transferEndpoint, ConfigServer.ConfigServerInstance.GetRouting(), agentConnection);
                                        agentConnection.LastActionRequested = AgentActionRequested.Dialline2;
                                    }
                                    else
                                    {
                                        agentConnection.SipServer.RequestMakeCall("", transferEndpoint, agentConnection);
                                        agentConnection.LastActionRequested = AgentActionRequested.Dialline2;
                                    }
                                }
                            }
                        }
                        else
                            TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because routing is null or empty.");
                    }
                    else
                        TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");
                }

                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request because endpointType is not recognized.");
            }

            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "ON processSingleStepTransfer: " + e.Message + e.StackTrace);
            }
        }


        private void ProcessPciEnterSecureMode(AgentConnection agentConnection, JObject request)
        {
            String dpmTarget = "";
            String semafoneUrn = "";
            Semafone.Semafone semafone = null;

            string enterSecureModeOption = "";
            foreach (var x in request)
            {
                string name = x.Key;
                JToken value = x.Value;
                enterSecureModeOption = value.Value<String>("mode");
            }

            try
            {
                bool useDpmTarget = ConfigServer.ConfigServerInstance.IsSipDatacentreAndDpmDatacentreSame(agentConnection.ServerName);
                if (agentConnection.Semafone != null)
                {
                    semafone = agentConnection.Semafone;
                }
                else
                {
                    #region Get SemafoneURN
                    if (agentConnection.LastHandset != null)
                    {
                        semafoneUrn = agentConnection.LastHandset.Interaction.ContainsKey("semafoneCR") ? agentConnection.LastHandset.Interaction.GetAsString("semafoneCR") : "";//request.request.semaurn; //doc.SelectNodes(REQUEST_NODE_NAME).agentConnection(0).SelectNodes(AGENT_ID_NODE_NAME).agentConnection(0).FirstChild.Value;
                        if (string.IsNullOrEmpty(semafoneUrn))
                            semafoneUrn = agentConnection.LastHandset.Interaction.ContainsKey("genesysId") ? agentConnection.LastHandset.Interaction.GetAsString("genesysId") : "";
                    }

                    if (String.IsNullOrEmpty(semafoneUrn))
                    {
                        new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Request to PCI Enter Secure Mode - semafoneCR missing.", ErrorSeverity.Error);
                        return;
                    }
                    #endregion

                    #region Get DPMTarget
                    if (agentConnection.LastHandset != null)
                        dpmTarget = agentConnection.LastHandset.Interaction.ContainsKey("semafoneURL") ? agentConnection.LastHandset.Interaction.GetAsString("semafoneURL") : "";
                    if (string.IsNullOrEmpty(dpmTarget))
                        // Get dpmtarget based on datacenter values 
                        dpmTarget = useDpmTarget ? ConfigServer.ConfigServerInstance.PrimaryDpmTarget : ConfigServer.ConfigServerInstance.SecondaryDpmTarget;

                    if (String.IsNullOrEmpty(dpmTarget))
                    {
                        new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Request to PCI Enter Secure Mode - semafoneURL missing.", ErrorSeverity.Error);
                        return;
                    }
                    #endregion

                    semafone = new Semafone.Semafone(agentConnection, dpmTarget, semafoneUrn, GetSemafonePassword());
                }


                if (semafone != null)
                {
                    if (semafone.IsInSecureMode)
                    {
                        new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneEnterSecureModeFailed)), "Failed to enter secure mode. Semafone is already in secure mode.", ErrorSeverity.Error);
                        return;
                    }
                    else
                    {
                        if (!semafone.Login())
                        {
                            //Already tried connection with primary now try with secondary
                            dpmTarget = useDpmTarget ? ConfigServer.ConfigServerInstance.SecondaryDpmTarget : ConfigServer.ConfigServerInstance.PrimaryDpmTarget;
                            semafone = new Semafone.Semafone(agentConnection, dpmTarget, semafoneUrn, GetSemafonePassword());
                            if (semafone != null)
                            {
                                if (!semafone.Login())
                                {
                                    //Already tried connection with secondary now try with primary again
                                    dpmTarget = useDpmTarget ? ConfigServer.ConfigServerInstance.PrimaryDpmTarget : ConfigServer.ConfigServerInstance.SecondaryDpmTarget;
                                    semafone = new Semafone.Semafone(agentConnection, dpmTarget, semafoneUrn, GetSemafonePassword());
                                    if (semafone != null)
                                    {
                                        if (!semafone.Login())
                                        {
                                            new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneLoginFailed)), (" Both dmptarget failed :" + dpmTarget + " " + semafoneUrn + " Failed to connect to semafone server."), ErrorSeverity.Error);
                                            agentConnection.Semafone = null;
                                            return;
                                        }
                                        else
                                        {
                                            if (!semafone.EnterSecureMode(enterSecureModeOption))
                                            {
                                                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneEnterSecureModeFailed)), "Failed to enter secure mode using primary dpmtarget.", ErrorSeverity.Error);
                                                semafone.Logout();
                                                agentConnection.Semafone = null;
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (!semafone.EnterSecureMode(enterSecureModeOption))
                                    {
                                        new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneEnterSecureModeFailed)), "Failed to enter secure mode using secondary dpmtarget.", ErrorSeverity.Error);
                                        semafone.Logout();
                                        agentConnection.Semafone = null;
                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!semafone.EnterSecureMode(enterSecureModeOption))
                            {
                                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneEnterSecureModeFailed)), "Failed to enter secure mode using primary dpmtarget.", ErrorSeverity.Error);
                                semafone.Logout();
                                agentConnection.Semafone = null;
                                return;
                            }
                        }

                        if (!semafone.ListenForMaskedData())
                        {
                            TpsLogManager<ClientRequest>.Error( "processPCIEnterSecureMode : Failed" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + " to start listen for masked data.");
                            new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneListenForDtmfFailed)), "Failed to start listen for masked data.", ErrorSeverity.Error);
                            return;
                        }

                        agentConnection.Semafone = semafone;
                        if (agentConnection.Semafone.DpmTargetUsed == 1) Semafone.Semafone.CheckDpmTarget1();
                        if (agentConnection.Semafone.DpmTargetUsed == 2) Semafone.Semafone.CheckDpmTarget2();
                        SemafoneEnterSecureModeHandset semafoneHandset = new SemafoneEnterSecureModeHandset();
                        semafoneHandset.Semafone = new SemafoneEnterSecureMode() { Enteredsecuremode = true, GenesysId = agentConnection.LastHandset.Interaction.ContainsKey("genesysId") ? agentConnection.LastHandset.Interaction.GetAsString("genesysId") : "", SemafoneUrl = dpmTarget, SemafoneCr = semafoneUrn };
                        string message = JsonConvert.SerializeObject(semafoneHandset);
                        agentConnection.SendMessage(message);
                        TpsLogManager<ClientRequest>.Info( " Entered secure mode semafone message has been sent to extension [" + agentConnection.SipServer.Extension + "] @ " + agentConnection.GetResourceId());
                        TpsLogManager<ClientRequest>.Info( message);
                    }
                }
                else
                {
                    new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneEnterSecureModeFailed)), "Could not create Semafone instance" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : ""), ErrorSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Could not create Semafone instance" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + ex.Message, ErrorSeverity.Error);
            }

        }

        private void ProcessPciExitSecureMode(AgentConnection agentConnection, String theMessage)
        {
            if (agentConnection == null)
            {
                TpsLogManager<ClientRequest>.Warn( "processPCIExitSecureMode : Client has either been disconnected or doesn't exist");
                return;
            }
            try
            {
                bool success = false;
                if (agentConnection.Semafone != null)
                {
                    // exit secure mode and logout from Semafone to ensure no more events come through
                    success = agentConnection.Semafone.StopListeningForEvents();
                    if (!success)
                        TpsLogManager<ClientRequest>.Error( "processPCIExitSecureMode : Could not stop listening for Semafone events" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + ".");
                    success = agentConnection.Semafone.ExitSecureMode();
                    if (!success)
                        TpsLogManager<ClientRequest>.Error( "processPCIExitSecureMode : Could not request to exit Semafone secure mode" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + ".");
                    success = agentConnection.Semafone.Logout();
                    if (!success)
                        TpsLogManager<ClientRequest>.Error( "processPCIExitSecureMode : Could not logout to exit Semafone secure mode" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + ".");

                    if (success)
                        SendExitedSecureMode(agentConnection);
                    else
                        new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneExitSecureModeFailed)), "Failed to exit secure mode" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + ".", ErrorSeverity.Error);
                }
                else
                    new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneExitSecureModeFailed)), "Failed to exit secure mode" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + ".", ErrorSeverity.Error);
            }
            catch (Exception ex)
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneExitSecureModeFailed)), "Error during exit secure mode" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + "." + ex.Message, ErrorSeverity.Error);
            }
        }

        private void ProcessPciResetPan(AgentConnection agentConnection, String theMessage)
        {
            if (agentConnection == null)
            {
                TpsLogManager<ClientRequest>.Warn( "processPCIResetPAN : Client has either been disconnected or doesn't exist");
                return;
            }
            try
            {
                if (agentConnection.Semafone != null)
                {
                    if (agentConnection.Semafone.ResetPan())
                    {
                        SemafoneResetPanHandset semafoneHandset = new SemafoneResetPanHandset();
                        semafoneHandset.Semafone = new SemafoneResetPan() { Panreset = true };
                        string message = JsonConvert.SerializeObject(semafoneHandset);
                        agentConnection.SendMessage(message);
                        TpsLogManager<ClientRequest>.Info( " Reset PAN semafone message has been sent to extension [" + agentConnection.SipServer.Extension + "] @ " + agentConnection.GetResourceId());
                        TpsLogManager<ClientRequest>.Info( message);
                    }
                    else
                    {
                        new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneResetPanFailed)), "Failed to reset pan" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + ".", ErrorSeverity.Error);
                    }
                }
                else
                    new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneResetPanFailed)), "Failed to reset pan" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + ".", ErrorSeverity.Error);

            }
            catch (Exception ex)
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneResetPanFailed)), "Failed to reset pan." + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + "." + ex.Message, ErrorSeverity.Error);

            }
        }

        private void ProcessPciResetCvc(AgentConnection agentConnection, String theMessage)
        {
            if (agentConnection == null)
            {
                TpsLogManager<ClientRequest>.Warn( "processPCIResetCVC : Client has either been disconnected or doesn't exist");
                return;
            }
            try
            {
                if (agentConnection.Semafone != null)
                {
                    if (agentConnection.Semafone.ResetCvc())
                    {
                        SemafoneResetCvcHandset semafoneHandset = new SemafoneResetCvcHandset();
                        semafoneHandset.Semafone = new SemafoneResetCvc() { Cvcreset = true };
                        string message = JsonConvert.SerializeObject(semafoneHandset);
                        agentConnection.SendMessage(message);
                        TpsLogManager<ClientRequest>.Info( " Reset CVC semafone message has been sent to extension [" + agentConnection.SipServer.Extension + "] @ " + agentConnection.GetResourceId());
                        TpsLogManager<ClientRequest>.Info( message);
                    }
                    else
                    {
                        new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneResetCvcFailed)), "Failed to reset cvc" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : ""), ErrorSeverity.Error);
                    }
                }
                else
                    new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneResetCvcFailed)), "Failed to reset cvc" + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : ""), ErrorSeverity.Error);

            }
            catch (Exception ex)
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseSemafoneResetCvcFailed)), "Failed to reset cvc." + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "") + ex.Message, ErrorSeverity.Error);
            }
        }


        private void ProcessConcirgeSubscription(AgentConnection agentConnection, JObject request, bool subscribe)
        {
            if (agentConnection == null)
            {
                TpsLogManager<ClientRequest>.Warn( "processConcirgeSubscription : Client has either been disconnected or doesn't exist");
                return;
            }
            try
            {
                if (subscribe)
                {
                    string conciergeName = "";
                    foreach (var x in request)
                    {
                        string name = x.Key;
                        JToken value = x.Value;
                        conciergeName = value.Value<String>("conciergeName");
                    }
                    agentConnection.SubscribeToConciergeType = conciergeName;
                    agentConnection.SubscribeToConciergeStatistics = true;
                    TpsLogManager<ClientRequest>.Info( agentConnection.AgentUserName + " is subscribed for concierge " + conciergeName + ".");
                }
                else
                {
                    agentConnection.SubscribeToConciergeStatistics = false;
                    agentConnection.SubscribeToConciergeType = "";
                    TpsLogManager<ClientRequest>.Info( agentConnection.AgentUserName + " is unsubscribed for concierge.");
                }
            }
            catch (Exception ex)
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Failed to subscribed for concierge " + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : ""), ErrorSeverity.Error);
            }
        }

        private async void ProcessCallbackRequest(AgentConnection agentConnection, JObject request)
        {
            if (agentConnection == null)
            {
                TpsLogManager<ClientRequest>.Warn( "processCallbackRequest : Client has either been disconnected or doesn't exist");
                return;
            }
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                    HttpResponseMessage response = await client.PostAsJsonAsync<JObject>(ConciergeConnection.ConciergeBaseURL, request);
                    string message = response.Content.ReadAsStringAsync().Result;
                    CLogger.WriteLog(response.IsSuccessStatusCode ? ELogLevel.Info : ELogLevel.Error, " " + "Response message for Callback request sent to agent " + agentConnection.AgentUserName + (agentConnection.SipServer != null ? " on extension " + agentConnection.SipServer.Extension : "") + " : " + message);
                    agentConnection.SendMessage(message);
                }
            }
            catch (Exception ex)
            {
                TpsLogManager<ClientRequest>.Error( "Error on ProcessCallbackRequest : " + ex.Message + Environment.NewLine + ex.StackTrace);
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Failed to book callback " + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : ""), ErrorSeverity.Error);
            }
        }

        private async void ProcessCancelCallback(AgentConnection agentConnection, JObject request)
        {
            if (agentConnection == null)
            {
                TpsLogManager<ClientRequest>.Warn( "processCancelCallback : Client has either been disconnected or doesn't exist");
                return;
            }
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                    HttpResponseMessage response = await client.PutAsJsonAsync<JObject>(ConciergeConnection.ConciergeBaseURL, request);
                    string message = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                        TpsLogManager<ClientRequest>.Info("Response message for Cancel callback request sent to agent " + agentConnection.AgentUserName + (agentConnection.SipServer != null ? " on extension " + agentConnection.SipServer.Extension : "") + " : " + message);
                    else
                        TpsLogManager<ClientRequest>.Error("Response message for Cancel callback request sent to agent " + agentConnection.AgentUserName + (agentConnection.SipServer != null ? " on extension " + agentConnection.SipServer.Extension : "") + " : " + message);
                    agentConnection.SendMessage(message);
                }
            }
            catch (Exception ex)
            {
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Failed to cancel callback " + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : ""), ErrorSeverity.Error);
            }
        }


        internal void ProcessOutboundDiallerEventEstablished(AgentConnection agentConnection)
        {
            try
            {

                if (agentConnection.SipServer != null)
                {
                    agentConnection.EstablishedOnCall = true;

                    if (agentConnection.OutboundDiallerUserData == null) agentConnection.OutboundDiallerUserData = new KeyValueCollection();
                    if (agentConnection.OutboundDiallerUserData.Count > 0)
                    {
                        #region Check if this is assured connect call and and assured connect flag is not set
                        //if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_ASSURED_HANDLE"))
                        if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_ASSURED_HANDLE") && (!agentConnection.IsAssuredConnect))
                        {
                            agentConnection.IsAssuredConnect = true;
                            if (agentConnection.OutboundDiallerUserData.ContainsKey("ENGAGING_CALL_TIMEOUT"))
                            {
                                double engagingTime;
                                if (double.TryParse(agentConnection.OutboundDiallerUserData["ENGAGING_CALL_TIMEOUT"].ToString(), out engagingTime))
                                {
                                    if (!agentConnection.HasTimerBeenSet)
                                    {
                                        agentConnection.EngagingCallTimeout = new System.Timers.Timer(engagingTime * 1000);
                                        agentConnection.EngagingCallTimeout.Elapsed +=
                                            (sender, e) => EngagingCallTimeout_Elapsed(sender, e, agentConnection);
                                        agentConnection.EngagingCallTimeout.Enabled = true;
                                        agentConnection.IsEngagingTimeoutEnabled = true;
                                        agentConnection.HasTimerBeenSet = true;
                                    }
                                }

                            }
                            agentConnection.IgnoreNotReadyAfterAssuredConnectCall = (!ConfigServer.ConfigServerInstance.NotReadyAfterAssuredConnectCallFixed);
                            return;
                        }
                        #endregion
                        var requiredDataValue = new KeyValueCollection();
                        if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_CAMPAIGN_NAME"))
                            requiredDataValue.Add("GSW_CAMPAIGN_NAME", agentConnection.OutboundDiallerUserData["GSW_CAMPAIGN_NAME"]);
                        if (agentConnection.OutboundDiallerUserData.ContainsKey("CUSTOM_IM_RECORD_STATUS"))
                            requiredDataValue.Add("CUSTOM_IM_RECORD_STATUS", "3");

                        if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_RECORD_HANDLE"))
                            requiredDataValue.Add("GSW_RECORD_HANDLE", agentConnection.OutboundDiallerUserData["GSW_RECORD_HANDLE"]);

                        if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_APPLICATION_ID"))
                            requiredDataValue.Add("GSW_APPLICATION_ID", agentConnection.OutboundDiallerUserData["GSW_APPLICATION_ID"]);
                        requiredDataValue.Add("GSW_AGENT_REQ_TYPE", "UpdateCallCompletionStats");

                        agentConnection.SipServer.RequestDistributeUserEvent(agentConnection.OutboundDiallerConnId, requiredDataValue);
                    }
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");

            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "ProcessOutboundDiallerOutcome: " + e.Message + Environment.NewLine + e.StackTrace);
            }
        }

        private void EngagingCallTimeout_Elapsed(object sender, ElapsedEventArgs e, AgentConnection agentConnection)
        {
            if (agentConnection.IsEngagingTimeoutEnabled)//Only disconnect the call if the timer is still active
                agentConnection.SipServer.RequestReleaseCall(agentConnection.Line1ConnId, agentConnection.EventUserData);
        }


        private void ProcessOutboundDiallerOutcome(AgentConnection agentConnection, JObject request)
        {
            try
            {

                //TpsLogManager<ClientRequest>.Info( "Cleared down OutboundDiallerConnId");
                //Moved the conn id to a local variable so we can accept agent events (ready/not ready) as the call has been dispositioned from an agent point of view and we shouldn't suppress these events
                //once the dispositioning has taken place at the agent.
                string outboundDiallerConnId = agentConnection.OutboundDiallerConnId;
                agentConnection.OutboundDiallerConnId = "";
                TpsLogManager<ClientRequest>.Info( "Cleared down OutboundDiallerConnId");

                Dictionary<string, string> outcomeDictionary = new Dictionary<string, string>();

                #region Populate dictionary for all outcome attributes supplied

                foreach (var x in request)
                {
                    string name = x.Key;
                    JToken value = x.Value;
                    foreach (var y in x.Value)
                    {
                        var attribute = (Newtonsoft.Json.Linq.JProperty)(y);
                        outcomeDictionary.Add(Convert.ToString(attribute.Name), Convert.ToString(attribute.Value));
                    }
                }
                #endregion

                if (string.IsNullOrEmpty(outboundDiallerConnId))
                {
                    TpsLogManager<ClientRequest>.Warn(
                        "Ignoring request received from " + agentConnection.AgentUserName + "(" +
                        agentConnection.AgentHostName + ") because outbound dialler connection Id not available.");
                    return;
                }
                if (agentConnection.SipServer != null)
                {
                    if (agentConnection.OutboundDiallerUserData == null) agentConnection.OutboundDiallerUserData = new KeyValueCollection();
                    if (agentConnection.OutboundDiallerUserData.Count > 0)
                    {
                        foreach (var data in outcomeDictionary)
                        {

                            if (!string.IsNullOrEmpty(data.Value))
                            {
                                switch (data.Key)
                                {
                                    case "agentOutcomeOne":
                                        if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_AGENT_OUTCOME_1"))
                                            agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_1"] = data.Value;
                                        else
                                            agentConnection.OutboundDiallerUserData.Add("GSW_AGENT_OUTCOME_1", data.Value);
                                        break;
                                    case "agentOutcomeTwo":
                                        if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_AGENT_OUTCOME_2"))
                                            agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_2"] = data.Value;
                                        else
                                            agentConnection.OutboundDiallerUserData.Add("GSW_AGENT_OUTCOME_2", data.Value);
                                        break;
                                    case "callbackDatetime":
                                        if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_DATE_TIME"))
                                            agentConnection.OutboundDiallerUserData["GSW_DATE_TIME"] = data.Value;
                                        else
                                            agentConnection.OutboundDiallerUserData.Add("GSW_DATE_TIME", data.Value);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        //Record callback outcome
                        if (agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_2"].ToString() == "CBKSCH")
                        {
                            DateTime dateEntered;
                            if (DateTime.TryParseExact(agentConnection.OutboundDiallerUserData["GSW_DATE_TIME"].ToString(), @"MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateEntered))
                            {
                                if (agentConnection.OutboundDiallerUserData.ContainsKey("CUSTOM_IM_DAILY_FROM"))
                                    agentConnection.OutboundDiallerUserData["CUSTOM_IM_DAILY_FROM"] = Convert.ToString((dateEntered.Hour * 3600) + (dateEntered.Minute * 60));
                                else
                                    agentConnection.OutboundDiallerUserData.Add("CUSTOM_IM_DAILY_FROM", Convert.ToString((dateEntered.Hour * 3600) + (dateEntered.Minute * 60)));
                                if (agentConnection.OutboundDiallerUserData.ContainsKey("CUSTOM_IM_DAILY_TILL"))
                                    agentConnection.OutboundDiallerUserData["CUSTOM_IM_DAILY_TILL"] = Convert.ToString((dateEntered.Hour * 3600) + (dateEntered.Minute * 60) + 3600);
                                else
                                    agentConnection.OutboundDiallerUserData.Add("CUSTOM_IM_DAILY_TILL", Convert.ToString((dateEntered.Hour * 3600) + (dateEntered.Minute * 60) + 3600));
                            }
                            else
                            {
                                TpsLogManager<ClientRequest>.Error( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + @") because callback datetime not supplied in correct format (MM/dd/yyyy HH:mm).");
                                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), @"Invalid callback datetime. Acceptable format is  MM/dd/yyyy HH:mm", ErrorSeverity.Error);
                                return;
                            }

                            if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_CALLBACK_TYPE"))
                                agentConnection.OutboundDiallerUserData["GSW_CALLBACK_TYPE"] = "Campaign";
                            else
                                agentConnection.OutboundDiallerUserData.Add("GSW_CALLBACK_TYPE", "Campaign");

                            //if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_CALL_RESULT"))
                            //    agentConnection.OutboundDiallerUserData["GSW_CALL_RESULT"] = "53";
                            //else
                            //    agentConnection.OutboundDiallerUserData.Add("GSW_CALL_RESULT", "53");
                        }
                        //else
                        //{
                        if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_CALL_RESULT"))
                            //Use GSW_CALL_RESULT = 9 for Answer Machine (GSW_AGENT_OUTCOME_1=MCT) 
                            agentConnection.OutboundDiallerUserData["GSW_CALL_RESULT"] = (Convert.ToString(agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_1"]) == "MCT" ? "9" : "33");
                        else
                            agentConnection.OutboundDiallerUserData.Add("GSW_CALL_RESULT", (Convert.ToString(agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_1"]) == "MCT" ? "9" : "33"));
                        // }


                        if (agentConnection.OutboundDiallerUserData.ContainsKey("CUSTOM_IM_RECORD_STATUS"))
                            agentConnection.OutboundDiallerUserData["CUSTOM_IM_RECORD_STATUS"] = "3";
                        else
                            agentConnection.OutboundDiallerUserData.Add("CUSTOM_IM_RECORD_STATUS", "3");

                        //set record type if call annswered by answering machine
                        if ((agentConnection.IsAssuredConnect) && (Convert.ToString(agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_1"]) == "MCT"))
                        {
                            TpsLogManager<ClientRequest>.Debug(, "CUSTOM_IM_RECORD_TYPE set to 3");
                            if (agentConnection.OutboundDiallerUserData.ContainsKey("CUSTOM_IM_RECORD_TYPE"))
                                agentConnection.OutboundDiallerUserData["CUSTOM_IM_RECORD_TYPE"] = "3";
                            else
                                agentConnection.OutboundDiallerUserData.Add("CUSTOM_IM_RECORD_TYPE", "3");
                        }

                        //***Check if there is call back scheduled use 'RecordReschedule'
                        if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_AGENT_REQ_TYPE"))
                            agentConnection.OutboundDiallerUserData["GSW_AGENT_REQ_TYPE"] = agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_2"].ToString() == "CBKSCH" ? "RecordReschedule" : "RecordProcessed";
                        else
                            agentConnection.OutboundDiallerUserData.Add("GSW_AGENT_REQ_TYPE", agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_2"].ToString() == "CBKSCH" ? "RecordReschedule" : "RecordProcessed");


                        if (agentConnection.OutboundDiallerUserData.ContainsKey("Business Result"))
                            agentConnection.OutboundDiallerUserData["Business Result"] = agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_1"];
                        else
                            agentConnection.OutboundDiallerUserData.Add("Business Result", agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_1"]);

                        var removeKeys = new List<string>();
                        // if (agentConnection.IsAssuredConnect)
                        //{
                        if (Convert.ToString(agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_1"]) == "MCT")
                        {
                            if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_TREATMENT"))
                                agentConnection.OutboundDiallerUserData["GSW_TREATMENT"] = "RecordTreatCampaign";
                            else
                                agentConnection.OutboundDiallerUserData.Add("GSW_TREATMENT", "RecordTreatCampaign");
                        }

                        foreach (var key in agentConnection.OutboundDiallerUserData.Keys)
                            if (key.ToString().StartsWith("CUSTOM_") && (!((key.ToString() == "CUSTOM_IM_DAILY_TILL") || (key.ToString() == "CUSTOM_IM_DAILY_FROM") || (key.ToString() == "CUSTOM_IM_DIAL_SCHED_TIME"))))
                                removeKeys.Add(key.ToString());

                        foreach (var key in removeKeys)
                            agentConnection.OutboundDiallerUserData.Remove(key);

                        agentConnection.SipServer.RequestDistributeUserEvent(outboundDiallerConnId, agentConnection.OutboundDiallerUserData);

                        if (agentConnection.OutboundDiallerUserData["GSW_AGENT_OUTCOME_2"].ToString() == "CBKSCH")
                        {
                            if (agentConnection.OutboundDiallerUserData.ContainsKey("GSW_AGENT_REQ_TYPE"))
                                agentConnection.OutboundDiallerUserData["GSW_AGENT_REQ_TYPE"] = "RecordProcessed";
                            else
                                agentConnection.OutboundDiallerUserData.Add("GSW_AGENT_REQ_TYPE", "RecordProcessed");
                            agentConnection.SipServer.RequestDistributeUserEvent(outboundDiallerConnId, agentConnection.OutboundDiallerUserData);
                        }
                        //agentConnection.OutboundDiallerConnId = "";
                        agentConnection.IsAssuredConnect = false;
                    }
                }
                else
                    TpsLogManager<ClientRequest>.Warn( "Ignoring request received from " + agentConnection.AgentUserName + "(" + agentConnection.AgentHostName + ") because SIP server connection not established yet.");

            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "ProcessOutboundDiallerOutcome: " + e.Message + e.StackTrace);
            }
        }


        private void ProcessDtmfTone(AgentConnection agentConnection, JObject request)
        {
            if (agentConnection == null)
            {
                TpsLogManager<ClientRequest>.Warn( "ProcessDtmfTone : Client has either been disconnected or doesn't exist");
                return;
            }
            try
            {
                string tone = "";
                string line = "";
                foreach (var x in request)
                {
                    foreach (var y in x.Value)
                    {
                        var attribute = (Newtonsoft.Json.Linq.JProperty)(y);
                        switch (attribute.Name)
                        {
                            case "key": tone = Convert.ToString(attribute.Value); break;
                            case "line": line = Convert.ToString(attribute.Value); break;
                            default: TpsLogManager<ClientRequest>.Error( "ProcessDtmfTone : Invalid dtmf json message received from " + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : "")); return;
                        }
                    }
                }

                if (!Regex.IsMatch(tone, @"^[\#\*\d]{1}$"))
                {
                    TpsLogManager<ClientRequest>.Error( "ProcessDtmfTone : Invalid dtmf character '" + tone + "' received from " + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : ""));
                    return;
                }

                if (!((line == "1") || (line == "2")))
                {
                    TpsLogManager<ClientRequest>.Error( "ProcessDtmfTone : Invalid line info received from " + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : ""));
                    new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Failed to submit dtmf tone. Invalid line number.", ErrorSeverity.Error);
                    return;
                }
                if (agentConnection.SipServer != null)
                    switch (line)
                    {
                        case "1": agentConnection.SipServer.RequestSendDtmf(tone, agentConnection.Line1ConnId); break;
                        case "2": agentConnection.SipServer.RequestSendDtmf(tone, agentConnection.Line2ConnId); break;
                        default: new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Failed to submit dtmf tone. Invalid line number.", ErrorSeverity.Error); return;
                    }
                else
                {
                    TpsLogManager<ClientRequest>.Error( "ProcessDtmfTone :Invalid request. No Sip server found.");
                    new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Failed to submit dtmf tone " + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : ""), ErrorSeverity.Error);
                }
            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "ProcessDtmfTone: " + e.Message + Environment.NewLine + e.StackTrace);
                new ClientResponse().SendJsonErrorMessage(agentConnection, Convert.ToString(Convert.ToInt32(ResponseCode.ResponseError)), "Failed to submit dtmf tone " + ((agentConnection.SipServer != null) ? " on extension [" + agentConnection.SipServer.Extension + "]" : ""), ErrorSeverity.Error);
            }
        }

        private void ProcessConnectionCount(AgentConnection agentConnection)
        {
            if (agentConnection == null)
            {
                TpsLogManager<ClientRequest>.Warn( "ProcessConnectionCount : Client has either been disconnected or doesn't exist");
                return;
            }
            agentConnection.SendMessage(JsonConvert.SerializeObject(new { connections = WebSocketPipe.AgentConnectionsDictionary.Count.ToString() }));

            if (agentConnection.AgentUserName == null)
            {
                WebSocketPipe.RemoveConnectionOnWebsocketDisconnect(agentConnection);
            }
        }


        private void ProcessConnectionSummary(AgentConnection agentConnection)
        {
            if (agentConnection == null)
            {
                TpsLogManager<ClientRequest>.Warn( "ProcessConnectionSummary : Client has either been disconnected or doesn't exist");
                return;
            }

            List<object> agents = new List<object>();
            foreach (var conn in WebSocketPipe.AgentConnectionsDictionary.Keys)
                if (conn.LastHandset != null)
                    agents.Add(new { username = conn.AgentUserName, hostname = conn.AgentHostName, extension = conn.SipServer.Extension, status = conn.AgentState.ToString(), line1status = conn.LastHandset.Lines.Line1.LineStatus, line2status = conn.LastHandset.Lines.Line2.LineStatus });
                else
                    agents.Add(new { username = conn.AgentUserName, hostname = conn.AgentHostName, extension = "Unknown", status = conn.AgentState.ToString(), line1status = "NotAvailable", line2status = "NotAvailable" });

            agentConnection.SendMessage(JsonConvert.SerializeObject(agents));

            if (agentConnection.AgentUserName == null)
            {
                WebSocketPipe.RemoveConnectionOnWebsocketDisconnect(agentConnection);
            }
        }

        private void ProcessConnectionDetail(AgentConnection agentConnection)
        {
            if (agentConnection == null)
            {
                TpsLogManager<ClientRequest>.Warn( "ProcessConnectionDetails : Client has either been disconnected or doesn't exist");
                return;
            }


            List<object> agents = new List<object>();
            var numberOfWebsockets = 0;
            var numberOfSipConnections = 0;
            foreach (var conn in WebSocketPipe.AgentConnectionsDictionary.Keys)
            {
                if (conn.LastHandset != null)
                {
                    agents.Add(new { ipaddress = conn.Socket, username = conn.AgentUserName, extension = conn.SipServer.Extension, hostname = conn.AgentHostName, status = conn.AgentState.ToString(), line1status = conn.LastHandset.Lines.Line1.LineStatus, line2status = conn.LastHandset.Lines.Line2.LineStatus, agentLoggedIn = conn.AgentLoginDateTime, lastResponseWasFor = conn.LastHandset.Status.Description, lastResponseSent = conn.LastHandset.Status.Datetime, lastRequest = conn.LastActionRequested.ToString() });
                }
                else
                {
                    agents.Add(new { ipaddress = conn.Socket, username = conn.AgentUserName, extension = "Unknown", hostname = conn.AgentHostName, status = conn.AgentState.ToString(), line1status = "NotAvailable", line2status = "NotAvailable", agentLoggedIn = conn.AgentLoginDateTime, lastResponseWasFor = "Unknown", lastResponseSent = "Unknown", lastRequest = conn.LastActionRequested.ToString() });
                }
                if (conn.SipServer != null)
                {
                    numberOfSipConnections = numberOfSipConnections + 1;
                }
                if (conn.Socket != null)
                {
                    numberOfWebsockets = numberOfWebsockets + 1;
                }
            }
            agentConnection.SendMessage(JsonConvert.SerializeObject(new { application = Softphone.Server.ConfigServer.ConfigServerInstance.GetCFGApplicationObject().Name, Agents = agents, SipConnections = numberOfSipConnections, WebSocketConnections = numberOfWebsockets }));

            if (agentConnection.AgentUserName == null)
            {
                WebSocketPipe.RemoveConnectionOnWebsocketDisconnect(agentConnection);
            }
        }


        public string GetSemafonePassword()
        {
            return _semafonePassword;

        }

        internal void SendExitedSecureMode(AgentConnection agentConnection)
        {
            if (agentConnection == null)
            {
                TpsLogManager<ClientRequest>.Warn("sendExitedSecureMode : Client has either been disconnected or doesn't exist");
                return;
            }
            try
            {
                agentConnection.Semafone = null;
                SemafoneExitSecureModeHandset semafoneHandset = new SemafoneExitSecureModeHandset();
                semafoneHandset.Semafone = new SemafoneExitSecureMode() { Exitsecuremode = true };
                string message = JsonConvert.SerializeObject(semafoneHandset);
                agentConnection.SendMessage(message);
                TpsLogManager<ClientRequest>.Info(
                    " Exit secure mode semafone message has been sent to extension [" +
                    agentConnection.SipServer.Extension + "] @ " + agentConnection.GetResourceId());
                TpsLogManager<ClientRequest>.Info( message);
            }
            catch (Exception e)
            {
                TpsLogManager<ClientRequest>.Error( "Error sendExitedSecureMode : " + e.Message + e.StackTrace);
            }
        }
    }
}
