using System;
using System.Collections.Generic;
using CommonContract;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Collections;
using LogConfigLayer;

namespace Semafone
{
    [Export(typeof(IConnect))]
    public class SemafoneConnection : ConnectionBase 
    {
        private static int _responses=0;
        public ICollection cfgApplicationDictionary;
        public static SemafoneConnection SemafoneConnectionInstance;
        public string ApplicationName { get { return base.ApplicationIdentifier; } }  
        SemafoneConnection(): base(System.Reflection.Assembly.GetExecutingAssembly().Location)
        {
            if (ApplicationSettings.ContainsKey("ApplicationIdentifier")) ApplicationIdentifier = ApplicationSettings["ApplicationIdentifier"];
            if (ApplicationSettings.ContainsKey("GenesysConfigURIPri")) ConfigServerPrimaryUri = ApplicationSettings["GenesysConfigURIPri"];
            if (ApplicationSettings.ContainsKey("GenesysConfigURIBack")) ConfigServerBackupUri = ApplicationSettings["GenesysConfigURIBack"];
            if (ApplicationSettings.ContainsKey("SemafoneCMEAppName")) ClientConnectedToCMEApp = ApplicationSettings["SemafoneCMEAppName"];
            SemafoneConnectionInstance = this;
        }

        public override void ProcessWebSocketMessage(string appName, string message, string socket)
        {
            if (appName.ToLower() == ApplicationIdentifier.ToLower())
            {
                TpsLogManager<SemafoneConnection>.Info("Message received from " + socket + " :" + message);
                Task.Factory.StartNew(() => {
                    string response = new ClientRequest().ProcessClientMessage(socket, message);
                    SendWebSocketMessage(socket, response);
                    TpsLogManager<SemafoneConnection>.Info("Message sent to " + socket + " :" + response);
                });
            }

        }

        public override void OnConfigServerChange(string objectName, string objectType, object updatedObject)
        {
            //string s = "hj";
            return;
        }

        public override Dictionary<String, String> SetupAfterConfigServerConnected()
        {
            
           //cfgApplicationDictionary =  SemafoneConnection.SemafoneConnectionInstance.ConfigServer.GetObjectProperties(ClientConnectedToCMEApp, "CFGApplication",true);
            //Will need to be changed to return connection values if needed for webapi
            //      PopulateApplicationSettings();
            return null;
        }

      
        public static int GetNextResponseId()
        {
            if (_responses > int.MaxValue - 1)
                _responses = 0;
           return ++_responses;
        }
    }
}
