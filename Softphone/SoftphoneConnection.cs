using System;
using System.Collections.Generic;
using CommonContract;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Softphone
{
    [Export(typeof(IConnect))]
    public class SoftphoneConnection : ConnectionBase
    {
        private static int _responses = 0;
        public static SoftphoneConnection SoftphoneConnectionInstance;
        public string ApplicationName { get { return base.ApplicationIdentifier; } }
        SoftphoneConnection() : base(System.Reflection.Assembly.GetExecutingAssembly().Location)
        {
            if (ApplicationSettings.ContainsKey("ApplicationIdentifier")) ApplicationIdentifier = ApplicationSettings["ApplicationIdentifier"];
            if (ApplicationSettings.ContainsKey("GenesysConfigURIPri")) ConfigServerPrimaryUri = ApplicationSettings["GenesysConfigURIPri"];
            if (ApplicationSettings.ContainsKey("GenesysConfigURIBack")) ConfigServerBackupUri = ApplicationSettings["GenesysConfigURIBack"];
            if (ApplicationSettings.ContainsKey("SoftphoneCMEAppName")) ClientConnectedToCMEApp = ApplicationSettings["SoftphoneCMEAppName"];
            SoftphoneConnectionInstance = this;
        }

        public override void ProcessWebSocketMessage(string appName, string message, string socket)
        {
            if (appName.ToLower() == ApplicationIdentifier.ToLower())
                Task.Factory.StartNew(() => SendWebSocketMessage(socket, new ClientRequest().ProcessClientMessage(socket, message)));

        }

        public override void OnConfigServerChange(string objectName, string objectType, object updatedObject)
        {
            //string s = "hj";
            return;
        }

        public override Dictionary<String, String> SetupAfterConfigServerConnected()
        {

            //Will need to be changed to return connection values if needed for webapi
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
