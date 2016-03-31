using System.Collections.Generic;

namespace CommonContract
{
    public delegate void WSSendMessageEventHandler(string socket,string message);
   
    public interface IConnect
    {
        event WSSendMessageEventHandler WebSocketSendMessageEvent;
        IConfigServer ConfigServer { get; set; }
        string ApplicationIdentifier { get; set; }
        string ConfigServerPrimaryUri { get; set; }
        string ConfigServerBackupUri { get; set; }
        string UserName { get; set; }
        string PassWord { get; set; }
        string ClientConnectedToCMEApp { get; set; }
        Dictionary<string, string> ApplicationSettings { get; set; }
        string ProcessOwinMessage(string appName);
        void ProcessWebSocketMessage(string appName, string message, string socket);
        void InjectConfigServerInstance(IConfigServer configServer);
        void SetupBeforeConfigServerConnected();
        Dictionary<string, string> SetupAfterConfigServerConnected();
        void ShutDown();
        
    }
}
