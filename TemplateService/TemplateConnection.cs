using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using CommonContract;

namespace TemplateService
{
    [Export(typeof(IConnect))]
    public class TemplateConnection : ConnectionBase
    {
        public static bool SetupDone = false;
        private static TemplateConnection instance;
        public TemplateConnection(): base(Assembly.GetExecutingAssembly().Location)
        {
            ApplicationIdentifier = "TemplateService";
            if (ApplicationSettings.ContainsKey("GenesysConfigURIPri")) ConfigServerPrimaryUri = ApplicationSettings["GenesysConfigURIPri"];
            if (ApplicationSettings.ContainsKey("GenesysConfigURIBack")) ConfigServerBackupUri = ApplicationSettings["GenesysConfigURIBack"];
            if (ApplicationSettings.ContainsKey("AppName")) ApplicationConnectedTo = ApplicationSettings["AppName"];
            SetupDone = false;
            instance = this;
        }

        public static IConfigServer GetConfigServerInstance()
        {
            return instance.ConfigServer;
        }

        public override string ProcessOwinMessage(string appName)
        {
            return "TPS happy 'This is working in Owin " + appName + "'.";
        }


        public override void OnConfigServerChange(string objectName, string objectType, object updatedObject)
        {
            ;
        }

        public override Dictionary<string, string> SetupAfterConfigServerConnected()
        {
            TemplateController.SetUpControllerConnections();

            //Will need to be changed to return connection values if needed for webapi
            return null;

        }
    }
}
