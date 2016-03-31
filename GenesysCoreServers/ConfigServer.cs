using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using CommonContract;
using Genesyslab.Platform.ApplicationBlocks.Commons.Broker;
using Genesyslab.Platform.ApplicationBlocks.Commons.Protocols;
using Genesyslab.Platform.ApplicationBlocks.ConfigurationObjectModel;
using Genesyslab.Platform.ApplicationBlocks.ConfigurationObjectModel.CfgObjects;
using Genesyslab.Platform.ApplicationBlocks.ConfigurationObjectModel.Queries;
using Genesyslab.Platform.Commons.Collections;
using Genesyslab.Platform.Commons.Protocols;
using Genesyslab.Platform.Configuration.Protocols.Types;
using Genesyslab.Platform.Management.Protocols.MessageServer;
using Genesyslab.Platform.Voice.Protocols.TServer.Events;
using LogConfigLayer;

namespace GenesysCoreServers
{
    public sealed class ConfigServer : IConfigServer
    {
        private const string CConfServerIdentifier = "configServer";
        private readonly string configServerPrimaryUri;
        private readonly string configServerBackupUri;
        private readonly string applicationConnectedTo;
        private readonly string userName;
        private readonly string passWord;
        private ProtocolManagementService protocolManagementService;
        private EventBrokerService eventBrokerService;
        private IConfService confServiceContract;
        private CfgApplication cfgApplication;
        private String configServerId;
        private Boolean connected = false;
        private ConfServerConfiguration confServerConfiguration;
        private static List<ConfigServer> configServerInstances;
        private string PointerValue;
        private Dictionary<string, string> ApplicationSettings;
        readonly List<String> _registeredObjects = new List<string>();

        public event ConfigServerEventHandler ConfigUpdatedEvent;

        public object MessageServer { get; set; }
        private IMessageServer msgServer;

        private ConfigServer(string confServUriPri, string confServUriBack, string appName)
        {

            configServerPrimaryUri = confServUriPri;
            configServerBackupUri = confServUriBack;
            applicationConnectedTo = appName;
            int configServerAddpClientTimeout = 0, configServerAddpServerTimeout = 0;
            string configServerAddpTrace = "";

            /*To get configuration server specific config (addp settings)*/
            try
            {
                string path = Assembly.GetExecutingAssembly().Location;
                Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
                ApplicationSettings = new Dictionary<string, string>();
                foreach (var setting in cfg.AppSettings.Settings.AllKeys)
                    ApplicationSettings.Add(setting, cfg.AppSettings.Settings[setting].Value);

                configServerAddpClientTimeout = (ApplicationSettings.ContainsKey("ConfigServerAPPDClientTimeout")) ? Convert.ToInt32(ApplicationSettings["ConfigServerAPPDClientTimeout"]) : 30;
                configServerAddpServerTimeout = (ApplicationSettings.ContainsKey("ConfigServerADDPServerTimeout")) ? Convert.ToInt32(ApplicationSettings["ConfigServerADDPServerTimeout"]) : 30;
                configServerAddpTrace = (ApplicationSettings.ContainsKey("ConfigServerADDPTrace")) ? ApplicationSettings["ConfigServerADDPTrace"] : "both";

            }
            catch (Exception ex)
            {
                TpsLogManager<MessageServer>.Error("Cannot read config values from configuration file: " + ex.Message);
            }

            Connect(applicationConnectedTo, configServerAddpClientTimeout, configServerAddpServerTimeout, configServerAddpTrace);
            while (!connected)
                Thread.Sleep(1000);
            SetPointerValue();
            if (cfgApplication != null)
            {
                int port = 0;
                string host = "";
                foreach (CfgConnInfo server in cfgApplication.AppServers)
                {
                    if ((server.AppServer.Type != CfgAppType.CFGMessageServer) || (server.AppServer.State != CfgObjectState.CFGEnabled) || (server.AppServer.IsPrimary != CfgFlag.CFGTrue))
                        continue;
                    CfgApplication messageServerApp = server.AppServer;
                    port = Convert.ToInt32(messageServerApp.ServerInfo.Port);
                    host = messageServerApp.ServerInfo.Host.Name;
                    break;
                }
                int lcaPort = 4999;
                if (cfgApplication.ServerInfo != null)
                    if (cfgApplication.ServerInfo.Host != null)
                        if (cfgApplication.ServerInfo.Host.LCAPort != null)
                            lcaPort = Convert.ToInt32(cfgApplication.ServerInfo.Host.LCAPort);
                if ((port != 0) && (!string.IsNullOrEmpty(host)))
                    SetUpMessageServer(appName, cfgApplication.DBID, lcaPort, host, port);
            }
        }

        private void SetUpMessageServer(string appName, int dbid, int lcaPort, string host, int port)
        {
            MessageServer = new MessageServer(appName, dbid, lcaPort, host, port);
            msgServer = (MessageServer)MessageServer;
           // msgServer.SetStatus(GenesysCoreServers.MessageServer.ModeInitializing);
            msgServer.ConnectToMessageServer();
            int i = 1;
            while (!msgServer.IsConnectedToMessageServer() && i <5)
            {
                Thread.Sleep(1000);
                i++;
            }
            msgServer.SendMessageToMessageServer(97000, LogCategory.Alarm, LogLevel.Info, "TPS Started");
                TpsLogManager<ConfigServer>.Debug("Message server has been started");

        }

        private void SetPointerValue()
        {
            PointerValue = GetSpecificObjectValue(applicationConnectedTo, "CFGApplication", "Repoint Characters");
            if (PointerValue == "")
                TpsLogManager<ConfigServer>.Error("Pointer character could not be set");
        }

        private void OnProtocolOpened(object sender, ProtocolEventArgs eaEventArgs)
        {
            connected = true;
            Console.WriteLine("Connection open, reading " + applicationConnectedTo + " application object.");
            cfgApplication =
               new CfgApplicationQuery(confServiceContract)
               {
                   Name = applicationConnectedTo
               }.ExecuteSingleResult();
        }

        private void OnProtocolClosed(object sender, ProtocolEventArgs e)
        {
            Console.WriteLine("Connection has " + e.Protocol.State);
            if (msgServer != null)
            msgServer.SendMessageToMessageServer(97002, LogCategory.Alarm, LogLevel.Alarm, "Connection to Configuration Server has been lost!");

        }

        private void OnConfEvent(ConfEvent @event)
        {
            var type = @event.ObjectType;
            var dbid = @event.ObjectId;
            TpsLogManager<ConfigServer>.Debug("Registered object has been updated: " + dbid);
            var objectName = GetSpecificObjectValue("", type.ToString(), "Name", false, dbid);
            ICollection updatedObjectCollection = GetObjectFromConfig(type, dbid);
            OnCmeUpate(objectName, type.ToString(), updatedObjectCollection);

        }

        private ICollection GetObjectFromConfig(CfgObjectType type, int dbid)
        {
            ICollection returnedObjectCollection = null;
            try
            {
                CfgFilterBasedQuery query = new CfgFilterBasedQuery(type);
                query.Filter["dbid"] = Convert.ToInt32(dbid);

                dynamic obj = confServiceContract.RetrieveObject(query);
                returnedObjectCollection = GenesysCollection(obj);

            }
            catch (Exception e)
            {
                TpsLogManager<ConfigServer>.Error(e.Message);
            }
            return returnedObjectCollection;
        }

        private void OnCmeUpate(String objectName, string objectType, object updatedObject)
        {
            //To raise an event when there has been an update from one of our registered objects
            if (ConfigUpdatedEvent != null)
            {
                TpsLogManager<ConfigServer>.Debug("Update event fired");
                ConfigUpdatedEvent(objectName, objectType, updatedObject);

            }
            else
            {
                TpsLogManager<ConfigServer>.Error("ConfigUpdatedEvent is null");
            }
        }

        private void Connect(string appName, int addpClientTimeout, int addpServerTimeout, string addpTrace)
        {
            configServerId = CConfServerIdentifier + Guid.NewGuid();
            confServerConfiguration = new ConfServerConfiguration(configServerId)
            {
                Uri = new Uri(configServerPrimaryUri),
                ClientName = appName,
                UserName = "",
                UserPassword = "",
                WarmStandbyUri = new Uri(configServerBackupUri),
                WarmStandbyAttempts = 5,
                UseAddp = true,
                AddpClientTimeout = addpClientTimeout,
                AddpServerTimeout = addpServerTimeout,
                AddpTrace = addpTrace
            };


            protocolManagementService = new ProtocolManagementService();
            protocolManagementService.ProtocolOpened += OnProtocolOpened;
            protocolManagementService.ProtocolClosed += OnProtocolClosed;
            protocolManagementService.Register(confServerConfiguration);

            eventBrokerService = BrokerServiceFactory.CreateEventBroker(protocolManagementService.Receiver);
            confServiceContract = ConfServiceFactory.CreateConfService(protocolManagementService[configServerId], eventBrokerService);
            confServiceContract.Register(OnConfEvent);

            protocolManagementService.BeginOpen();
        }



        private ICollection GenesysCollection(dynamic dynamicObject)
        {

            Dictionary<string, Dictionary<string, List<Tuple<string, string, bool>>>> genesysProperties = new Dictionary<string, Dictionary<string, List<Tuple<string, string, bool>>>>();
            //dynamic dynamicObject = retrievedObject;
            Type typeOfDynamic = dynamicObject.GetType();
            if (typeOfDynamic.GetProperties().Any(p => p.Name.Equals("UserProperties")))
            {
                Dictionary<string, List<Tuple<string, string, bool>>> genesysSection = new Dictionary<string, List<Tuple<string, string, bool>>>();
                foreach (var option in dynamicObject.UserProperties)
                {
                    List<Tuple<string, string, bool>> items = new List<Tuple<string, string, bool>>();
                    foreach (DictionaryEntry eachValue in (KeyValueCollection)option.Value)
                        items.Add(new Tuple<string, string, bool>(eachValue.Key.ToString(), eachValue.Value.ToString(), (eachValue.Value.ToString().StartsWith(PointerValue) && eachValue.Value.ToString().Length > 1 ? true : false)));
                    genesysSection.Add(option.Key.ToString(), items);
                }
                if (genesysSection.Count > 0)
                    genesysProperties.Add("UserProperties", genesysSection);
            }

            if (typeOfDynamic.GetProperties().Any(p => p.Name.Equals("Options")))
            {
                var gen = (KeyValueCollection)dynamicObject.Options["General"];
                PointerValue = (gen != null) ? gen["Repoint Characters"].ToString() : ">";
                Dictionary<string, List<Tuple<string, string, bool>>> genesysSection = new Dictionary<string, List<Tuple<string, string, bool>>>();
                foreach (var option in dynamicObject.Options)
                {
                    List<Tuple<string, string, bool>> items = new List<Tuple<string, string, bool>>();
                    foreach (DictionaryEntry eachValue in (KeyValueCollection)option.Value)
                        items.Add(new Tuple<string, string, bool>(eachValue.Key.ToString(), eachValue.Value.ToString(), (eachValue.Value.ToString().StartsWith(PointerValue) && eachValue.Value.ToString().Length > 1 ? true : false)));
                    genesysSection.Add(option.Key.ToString(), items);
                }
                if (genesysSection.Count > 0)
                    genesysProperties.Add("Options", genesysSection);
            }

            if (typeOfDynamic.GetProperties().Any(p => p.Name.Equals("AppServers")))
            {
                Dictionary<string, List<Tuple<string, string, bool>>> genesysSection = new Dictionary<string, List<Tuple<string, string, bool>>>();

                foreach (CfgConnInfo nextServer in dynamicObject.AppServers)
                {
                    List<Tuple<string, string, bool>> items = new List<Tuple<string, string, bool>>();
                    if ((nextServer.AppServer.State == CfgObjectState.CFGEnabled) && (nextServer.AppServer.IsPrimary == CfgFlag.CFGTrue) && (nextServer.AppServer.IsServer == CfgFlag.CFGTrue))
                    {
                        CfgApplication server = nextServer.AppServer;

                        items.Add(new Tuple<string, string, bool>(server.ServerInfo.Host.Name, server.ServerInfo.Port, false));
                        if (server.ServerInfo.BackupServer != null)
                            items.Add(new Tuple<string, string, bool>(server.ServerInfo.BackupServer.ServerInfo.Host.Name, server.ServerInfo.BackupServer.ServerInfo.Port, false));
                        genesysSection.Add(server.Name, items);
                    }
                }

                if (genesysSection.Count > 0)
                    genesysProperties.Add("AppServers", genesysSection);
            }

            if (typeOfDynamic.GetProperties().Any(p => p.Name.Equals("Agents")))
            {
                List<string> items = new List<string>();
                foreach (var agent in dynamicObject.Agents)
                {
                    items.Add(agent.UserName);
                }

                if (items.Count > 0)
                    //If we are requesting agents for a VAG then we should just return the section and not the whole genesys properties 
                    return items;
            }

            return genesysProperties;
        }

        private ICollection BuildObjectDictionary(IEnumerable genesysObjects)
        {
            List<String> genesysReturnedObjectIdentifiers = new List<String>();

            foreach (var o in genesysObjects)
            {
                dynamic dObject = o;
                Type typeOfDynamic = dObject.GetType();
                genesysReturnedObjectIdentifiers.Add(typeOfDynamic.GetProperties().Any(p => p.Name.Equals("GroupInfo"))
                    ? dObject.GroupInfo.Name
                    : dObject.Name);
            }
            return genesysReturnedObjectIdentifiers;
        }

        public static ConfigServer GetConfigServerInstance(string configServerPrimaryUri, string configServerBackupUri, string applicationConnectedTo, bool subscribeToThisApplication = false)
        {
            if (string.IsNullOrEmpty(configServerPrimaryUri) || string.IsNullOrEmpty(configServerBackupUri) || string.IsNullOrEmpty(applicationConnectedTo))
                return null;
            if (configServerInstances == null) configServerInstances = new List<ConfigServer>();
            var configServerInstance = configServerInstances.FirstOrDefault(x => (x.configServerPrimaryUri == configServerPrimaryUri) && (x.configServerBackupUri == configServerBackupUri) && (x.applicationConnectedTo == applicationConnectedTo));

            if (configServerInstance == null)
            {
                configServerInstance = new ConfigServer(configServerPrimaryUri, configServerBackupUri, applicationConnectedTo);
                configServerInstances.Add(configServerInstance);
            }

            if (subscribeToThisApplication)
            {
                if (configServerInstance.cfgApplication != null)
                {
                    Console.WriteLine("Subscribing to changes to application object " + applicationConnectedTo);
                    TpsLogManager<ConfigServer>.Debug("Subscribing to changes to application object " + applicationConnectedTo);
                    configServerInstance.confServiceContract.Subscribe(configServerInstance.cfgApplication);
                }
            }
            return configServerInstance;
        }

        public String GetSpecificObjectValue(string genesysObjectName, string genesysObjectType, string propertyToRetrieve, bool subscribeForChanges = false, int dbid = 0)
        {
            try
            {
                CfgObjectType type = (CfgObjectType)Enum.Parse(typeof(CfgObjectType), genesysObjectType);
                CfgFilterBasedQuery query = new CfgFilterBasedQuery(type);

                if (dbid != 0) //Then we need to find an object from its dbid rather than its name
                {
                    query.Filter["dbid"] = dbid;
                }
                else
                {
                    query.Filter["name"] = genesysObjectName;
                }

                dynamic dynamicObject = confServiceContract.RetrieveObject(query);

                if (dynamicObject != null)
                {
                    if (subscribeForChanges)
                        if (!_registeredObjects.Contains(genesysObjectName))
                        {
                            confServiceContract.Subscribe(dynamicObject);
                            _registeredObjects.Add(genesysObjectType);
                            TpsLogManager<ConfigServer>.Debug("Registered for changes against: " + genesysObjectType);
                        }

                    Type typeOfDynamic = dynamicObject.GetType();

                    //To be used for Calling Lists or other objects to retrieve actual properties of the object
                    if (typeOfDynamic.GetProperties().Any(p => p.Name.Equals(propertyToRetrieve)))
                    {
                        var propertyInfo = dynamicObject.GetType().GetProperty(propertyToRetrieve);
                        var value = propertyInfo.GetValue(dynamicObject, null);
                        return value.ToString();
                    }

                    //To get a value from an objects options
                    if (typeOfDynamic.GetProperties().Any(p => p.Name.Equals("Options")))
                    {
                        foreach (var option in dynamicObject.Options)
                        {
                            KeyValueCollection splitValues = (KeyValueCollection)option.Value;

                            foreach (DictionaryEntry eachValue in splitValues)
                            {
                                if (eachValue.Key.ToString() == propertyToRetrieve)
                                    return eachValue.Value.ToString();
                            }
                        }
                    }


                    // To get a value from an objects User Properties
                    if (typeOfDynamic.GetProperties().Any(p => p.Name.Equals("UserProperties")))
                    {
                        foreach (var option in dynamicObject.UserProperties)
                        {
                            KeyValueCollection splitValues = (KeyValueCollection)option.Value;

                            foreach (DictionaryEntry eachValue in splitValues)
                            {
                                if (eachValue.Key.ToString() == propertyToRetrieve)
                                    return eachValue.Value.ToString();
                            }
                        }

                    }
                }
                else
                {
                    TpsLogManager<ConfigServer>.Error("Configuration item could not be retrieved: " + genesysObjectName);
                }
            }
            catch (Exception e)
            {
                TpsLogManager<ConfigServer>.Error(e.Message);
            }

            return "";
        }

        public ICollection GetObjectProperties(string objectName, string objectType, bool subscribeForChanges = false)
        {
            CfgObjectType type = (CfgObjectType)Enum.Parse(typeof(CfgObjectType), objectType);
            CfgFilterBasedQuery query = new CfgFilterBasedQuery(type);
            query.Filter["name"] = objectName;
            var obj = confServiceContract.RetrieveObject(query);
            if (obj != null)
                if (!_registeredObjects.Contains(objectName))
                {
                    confServiceContract.Subscribe(obj);
                    _registeredObjects.Add(objectName);
                    TpsLogManager<ConfigServer>.Debug("Registered for changes against: " + objectName);
                }


            return GenesysCollection(obj);
        }



        public ICollection GetAllObjectsForType(string objectType)
        {

            switch (objectType)
            {
                case "CFGApplication":
                    CfgApplicationQuery appQuery = new CfgApplicationQuery(confServiceContract);
                    var apps = confServiceContract.RetrieveMultipleObjects<CfgApplication>(appQuery);
                    return BuildObjectDictionary(apps);
                case "CFGAgentGroup":
                    CfgAgentGroupQuery agQuery = new CfgAgentGroupQuery(confServiceContract);
                    var agentGroups = confServiceContract.RetrieveMultipleObjects<CfgAgentGroup>(agQuery);
                    return BuildObjectDictionary(agentGroups);
            }

            return null;
        }

        public void Shutdown()
        {
            TpsLogManager<ConfigServer>.Debug("Config server Shutdown() BEGIN");
            try
            {
                FinalizePsdkApplicationBlocks();
                if (msgServer != null)
                    msgServer.ShutDown();

            }
            catch (Exception e)
            {
                TpsLogManager<ConfigServer>.Error("Shutdown is incomplete: " + e.Message);
            }

            TpsLogManager<ConfigServer>.Debug("Config server Shutdown() END");
        }

        private void FinalizePsdkApplicationBlocks()
        {

            if (confServiceContract != null)
            {
                ConfServiceFactory.ReleaseConfService(confServiceContract);
            }
            // Cleanup code
            eventBrokerService.Deactivate();

            eventBrokerService.Dispose();

            // Close Connection if opened (check status of protocol object)
            IProtocol protocol = protocolManagementService[configServerId];

            if (protocol.State == ChannelState.Opened) // Close only if the protocol state is opened
            {
                try
                {
                    protocol.Close();
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message + "\n" + ex.StackTrace + "\n");
                }
            }

            protocolManagementService.Unregister(configServerId);
        }
    }

}
