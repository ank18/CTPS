using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Softphone.Model
{
    public class AgentConnection 
    {

        public delegate void SendMessageHandler(string message);
        public event SendMessageHandler SendMessageEvent;
        public string Socket { get; set; }
        public SipServer SipServer { get; set; }
        public ClientRequest ClientRequest { get; set; }
        public AgentActionRequested LastActionRequested { get; set; }
        public Handset LastHandset { get; set; }
        public AgentStates AgentState { get; set; }
        public string NotReadyReason { get; set; }
        //public Semafone.Semafone Semafone { get; set; }
        public string ServerName { get; set; }
        public string Line1ConnId { get; set; }
        public string Line2ConnId { get; set; }
        public string AgentUserName { get; set; }
        public string AgentHostName { get; set; }
        public AgentActionRequested AgentQueuedRequest { get; set; }
        public string AgentQueuedReason { get; set; }
        public string OutboundDiallerConnId { get; set; }
        public Genesyslab.Platform.Commons.Collections.KeyValueCollection OutboundDiallerUserData { get; set; }
        public bool IsSoftphone { get; set; }
        public bool IsLine1OnHold { get; set; }
        public bool IsLine2OnHold { get; set; }
        public bool IsLine1OnMute { get; set; }
        public bool IsLine2OnMute { get; set; }
        public bool IsLine1OnHoldByAgent { get; set; }
        public string Line2CallReason { get; set; }
        public bool IsAgentDisconnected { get; set; }
        public bool IsConnectionRestored { get; set; }
       // public bool IsLogoutRequired { get; set; }
        public bool IsAssuredConnect { get; set; }
        public bool IsAnonymousCall { get; set; }
       // public Genesyslab.Platform.Commons.Collections.KeyValueCollection EventUserData { get; set; }
        public string ConciergeSkill { get; set; }
        public string AgentLoginDateTime { get; set; }
        public string SubscribeToConciergeType { get; set; }
        public bool SubscribeToConciergeStatistics { get; set; }
        public bool IgnoreNotReadyAfterAssuredConnectCall { get; set; }
        public string CampaignCode { get; set; }
        public string CampaignDescription { get; set; }
        public Timer EngagingCallTimeout { get; set; }
        public bool IsEngagingTimeoutEnabled { get; set; }
        public bool HasTimerBeenSet { get; set; }
        public bool PreviousAssuredConnectCallNotMerged { get; set; }
        public bool EstablishedOnCall { get; set; }

        public void SendMessage(string message)
        {
            if (SendMessageEvent != null)
                SendMessageEvent( message);
        }

        public void DisconnectFromSip()
        {
            if (SipServer != null)
            {
                SipServer.Disconnect();
            }
        }

        //public AgentConnection(IWebSocketConnection socket)
        //{
        //    try
        //    {
        //        Socket = socket;
        //        IsAgentDisconnected = false;
        //        IsConnectionRestored = false;
        //        IsAssuredConnect = false;
                
        //        CampaignCode = "";
        //        CampaignDescription = "";
        //        ClientRequest = new ClientRequest();
        //        this.SendMessageEvent += new SendMessageHandler(SendWebSocketMessage);
        //    }
        //    catch (Exception e)
        //    {
        //        CLogger.WriteLog(Bskyb.CT.Softphone.Logging.ELogLevel.Error, "Error on Web socket connection: " + e.Message + " : " + e.ToString()); 
        //    }
        //}

        //internal void SendWebSocketMessage(String message)
        //{
        //    Socket.Send(message);
        //}
       
        //internal string GetResourceId()
        //{
        //     return this.Socket.ClientAddressAndPort();
        //}

        //public static AgentConnection GetAgentConnection(string resourceId)
        //{
        //    return WebSocketPipe.AgentConnectionsDictionary.Keys.FirstOrDefault(x => x.Socket.IsSameSocket(resourceId));
        //}


        //public static AgentConnection GetAgentConnectionforUserName(string userName)
        //{
        //   return WebSocketPipe.AgentConnectionsDictionary.Keys.FirstOrDefault(x => (x.AgentUserName != null) && (x.AgentUserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase)));
            
        //}

        //public static void RemoveConnectionForAgentUserNameCloseConnection(string userName)
        //{
        //    AgentConnection agentConnection = WebSocketPipe.AgentConnectionsDictionary.Keys.FirstOrDefault(x => (x.AgentUserName != null) && (x.AgentUserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase)));
        //    if (agentConnection != null)
        //    {
        //        WebSocketPipe.RemoveConnectionOnWebsocketDisconnect(agentConnection);
        //    }
        //}

        //public static void RemoveConnectionForAgentUserNameLogoutAndCloseConnection(string userName)
        //{
        //    AgentConnection agentConnection = WebSocketPipe.AgentConnectionsDictionary.Keys.FirstOrDefault(x => (x.AgentUserName != null) && (x.AgentUserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase)));
        //    if (agentConnection != null)
        //    {
        //        //WebSocketPipe.RemoveConnectionOnLogout(agentConnection);
        //        agentConnection.SipServer.RequestAgentLogout("0");
        //    }
        //}

        //internal AgentConnection Copy(string agentUserName)
        //{
        //    try
        //    {
        //        AgentConnection currentAgentConnection = AgentConnection.GetAgentConnectionforUserName(agentUserName);
        //        if (currentAgentConnection == null) return null;
        //        currentAgentConnection.LastActionRequested = this.LastActionRequested;
        //        currentAgentConnection.AgentQueuedRequest = this.AgentQueuedRequest;
        //        currentAgentConnection.LastHandset = this.LastHandset;
        //        currentAgentConnection.AgentState = this.AgentState;
        //        currentAgentConnection.NotReadyReason = this.NotReadyReason;
        //        currentAgentConnection.ServerName = this.ServerName;
        //        currentAgentConnection.Line1ConnId = this.Line1ConnId;
        //        currentAgentConnection.Line2ConnId = this.Line2ConnId;
        //        currentAgentConnection.AgentUserName = this.AgentUserName;
        //        currentAgentConnection.AgentHostName = this.AgentHostName;
        //        currentAgentConnection.IsSoftphone = this.IsSoftphone;
        //        currentAgentConnection.IsLine1OnHold = this.IsLine1OnHold;
        //        currentAgentConnection.IsLine2OnHold = this.IsLine2OnHold;
        //        currentAgentConnection.IsLine1OnMute = this.IsLine1OnMute;
        //        currentAgentConnection.IsLine2OnMute = this.IsLine2OnMute;
        //        currentAgentConnection.IsLine1OnHoldByAgent = this.IsLine1OnHoldByAgent;
        //        currentAgentConnection.Line2CallReason = this.Line2CallReason;
        //        currentAgentConnection.IsAgentDisconnected = this.IsAgentDisconnected;
        //        currentAgentConnection.IsConnectionRestored = this.IsConnectionRestored;
        //        //currentAgentConnection.IsLogoutRequired = this.IsLogoutRequired;
        //        currentAgentConnection.OutboundDiallerConnId = this.OutboundDiallerConnId;
        //        currentAgentConnection.EventUserData = this.EventUserData;
        //        currentAgentConnection.OutboundDiallerUserData = this.OutboundDiallerUserData;
        //        currentAgentConnection.IsAssuredConnect = this.IsAssuredConnect;
        //        currentAgentConnection.IgnoreNotReadyAfterAssuredConnectCall = this.IgnoreNotReadyAfterAssuredConnectCall;
        //        currentAgentConnection.IsAnonymousCall = this.IsAnonymousCall;
        //        currentAgentConnection.ConciergeSkill = this.ConciergeSkill;
        //        currentAgentConnection.SubscribeToConciergeType = this.SubscribeToConciergeType;
        //        currentAgentConnection.SubscribeToConciergeStatistics = this.SubscribeToConciergeStatistics;
        //        currentAgentConnection.AgentLoginDateTime = this.AgentLoginDateTime;
        //        currentAgentConnection.CampaignCode = this.CampaignCode;
        //        currentAgentConnection.CampaignDescription = this.CampaignDescription;
        //        currentAgentConnection.EstablishedOnCall = this.EstablishedOnCall;
                
        //        return currentAgentConnection;
        //    }
        //    catch (Exception e)
        //    {
        //        throw;
        //    }

        //}
    }
}
