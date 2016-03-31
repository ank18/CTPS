using System;
using System.Collections.Generic;
using System.Configuration;

namespace CommonContract
{
    public abstract class ConnectionBase :IConnect
    {
        public event WSSendMessageEventHandler WebSocketSendMessageEvent;
        public IConfigServer ConfigServer { get; set; }
        public string ApplicationIdentifier { get; set; }
        public string ConfigServerPrimaryUri { get; set; }
        public string ConfigServerBackupUri { get; set; }
        public string UserName{ get; set; }
        public Boolean Encrypted { get; set; }
        public string PassWord{ get; set; }
        public string ClientConnectedToCMEApp { get; set; }
        public Dictionary<string, string> ApplicationSettings { get; set; }
        
        public ConnectionBase(string path)
        {
            try
            {
                Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
                ApplicationSettings = new Dictionary<string, string>();
                foreach (var setting in cfg.AppSettings.Settings.AllKeys)
                    ApplicationSettings.Add(setting, cfg.AppSettings.Settings[setting].Value);
            }
            catch (Exception ex)
            {
                
            }
        }
        public void InjectConfigServerInstance(IConfigServer configServer)
        {
            if (configServer != null)
            {
                ConfigServer = configServer;
                //ConfigServer.ConfigUpdatedEvent += OnConfigServerChange;
            }
        }

        public virtual void SendWebSocketMessage(string webSocket, string message)
        {
            if (WebSocketSendMessageEvent != null)
                WebSocketSendMessageEvent(webSocket, message);
        }

        public virtual string ProcessOwinMessage(string appName)
        {
            return "WebServer is not configured for this request " + appName;
        }

        public virtual void ProcessWebSocketMessage(string appName, string jsonRequest, string socket)
        {
            if (appName == ApplicationIdentifier)
                SendWebSocketMessage(socket, "WebServer is not configured for request " + jsonRequest);
        }
        
        public abstract void OnConfigServerChange(string objectName,string objectType, object updatedObject);

        public virtual void SetupBeforeConfigServerConnected()
        {
            ;
        }

        public virtual Dictionary<string, string> SetupAfterConfigServerConnected()
        {
            return null;
        }

        public virtual void ShutDown()
        {
            ;
        }
    }
}
