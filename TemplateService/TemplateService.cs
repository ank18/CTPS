using System;
using System.Collections.Generic;
using CommonContract;
using GenesysCoreServers;

namespace TemplateService
{
    public class TemplateService
    {
        private readonly MessageServer _messageServer;
        private readonly IConfigServer _configServer;
        private Dictionary<string, string> ApplicationSettings;
        private string appName;
        public TemplateService(IConfigServer configServer, MessageServer messageClient)
        {
            _configServer = configServer;
            _messageServer = messageClient;
            _configServer.ConfigUpdatedEvent += MyHandler;
            
        }

        private void MyHandler(string objectname, string objecttype, object updatedobject)
        {
            throw new NotImplementedException();
        }
    }
}
