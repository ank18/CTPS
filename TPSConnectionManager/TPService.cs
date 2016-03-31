using System;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using LogConfigLayer;
using System.Net;
using Microsoft.Owin;
using System.Net.Http;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using CommonContract;
using System.ComponentModel.Composition.Hosting;
using Microsoft.Owin.Hosting;
using System.IO;
using System.Linq;


namespace TPSConnectionManager
{
    public class TPService
    {
        private Thread _thread;
        private WebSocketPipe webSocketPipe;
        [ImportMany] // This is a signal to the MEF framework to load all matching exported assemblies.
        public IEnumerable<IConnect> ConnectToApplications { get; set; }
        CompositionContainer container;
        FileSystemWatcher fileSystemWatcher;
        private static TPService instance;
        private TPService() { }
        private Dictionary<String, String> connectionInfo = new Dictionary<String, String>(); 


        public static TPService Instance
        {
            get
            {
                if (instance == null)
                {
                    TpsLogManager<TPService>.ConfigLog(Properties.Settings.Default.OnlineLogAvailableFor, Properties.Settings.Default.OnlineLogMaximumCount);
                    string logMessage = string.Format(CultureInfo.CurrentCulture, "Starting worker of type '{0}'.", "TPService");
                    TpsLogManager<TPService>.Info(logMessage);
                    instance = new TPService();
                }
                TpsLogManager<TPService>.Debug("Instance to be returned");
                return instance;
            }
        }

        public void BuildCataLog()
        {
            try
            {
                var catalog = new AggregateCatalog();

                var folders = Directory.GetDirectories(Properties.Settings.Default.CatalogDirectoryPath, "*", SearchOption.AllDirectories);

                foreach (string folder in folders)
                {
                    var directoryCatalog = new DirectoryCatalog(folder);
                    catalog.Catalogs.Add(directoryCatalog);
                }
                catalog.Catalogs.Add(new DirectoryCatalog(Properties.Settings.Default.CatalogDirectoryPath));

                fileSystemWatcher = new FileSystemWatcher(Properties.Settings.Default.CatalogDirectoryPath);
                fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;

                fileSystemWatcher.Changed += OnDllAdded;
                fileSystemWatcher.Created += OnDllAdded;
                fileSystemWatcher.EnableRaisingEvents = true;
                container = new CompositionContainer(catalog);
                container.ComposeParts(instance);
            }
            catch (Exception ex)
            {
                string e = ex.Message;
            }
        }

        private void OnDllAdded(object sender, FileSystemEventArgs e)
        {
            var catalog = (AggregateCatalog)container.Catalog;
            var folders = Directory.GetDirectories(Properties.Settings.Default.CatalogDirectoryPath, "*", SearchOption.AllDirectories);
            foreach (string folder in folders)
            {
                if (catalog.Catalogs.Count > 0)
                    continue;

                var directoryCatalog = new DirectoryCatalog(folder);
                catalog.Catalogs.Add(directoryCatalog);
            }
            Refresh();
        }

        public void Refresh()
        {
            container.ComposeParts(instance);
        }

        public void Start()
        {
            try
            {
                instance.BuildCataLog();
                foreach (var connectToApplication in instance.ConnectToApplications)
                {
                    connectToApplication.SetupBeforeConfigServerConnected();
                    //If we have a username then we need to connect to config using that. Otherwise connect using the application name
                    TpsLogManager<TPService>.Debug("Service has entered into startup.  Instance running");
                    connectToApplication.InjectConfigServerInstance(
                        GenesysCoreServers.ConfigServer.GetConfigServerInstance(
                            connectToApplication.ConfigServerPrimaryUri,
                            connectToApplication.ConfigServerBackupUri,
                            connectToApplication.ClientConnectedToCMEApp));
                    connectionInfo = connectToApplication.SetupAfterConfigServerConnected();

                }
                
               // TpsLogManager<TPService>.ConfigLog(Properties.Settings.Default.OnlineLogAvailableFor, Properties.Settings.Default.OnlineLogMaximumCount);
                string logMessage = string.Format(CultureInfo.CurrentCulture, "Starting worker of type '{0}'.", GetType().FullName);
                TpsLogManager<TPService>.Info(logMessage);

                // Multiple thread instances cannot be created
                if (_thread == null || _thread.ThreadState == ThreadState.Stopped)
                {
                    _thread = new Thread(Startup) { Name = "Startup Thread", IsBackground = true };
                }

                // Start thread if it's not running yet
                if (_thread.ThreadState != ThreadState.Running)
                {
                    _thread.Start();
                }
               
              
               

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception ocurred: " + e.Message);
            }

        }

        public static string GetRemoteIPAddress(HttpRequestMessage request)
        {
            OwinContext owinContext = (OwinContext)request.Properties["MS_OwinContext"];
            return (owinContext != null) ? owinContext.Request.RemoteIpAddress : "";
        }

        private void Startup()
        {
            try
            {
                try
                {

                   webSocketPipe = new WebSocketPipe();
                }
                catch (Exception ex)
                {
                    TpsLogManager<TPService>.Error(ex.Message);
                    ex = null;
                }

                Boolean useHttps = false;
                string port = "8383", httpsPort = "";

                //foreach (var value in connectionInfo)
                //{
                //    switch (value.Key)
                //    {
                //        case "useHTTPS":
                //            useHttps = Convert.ToBoolean(value.Value);
                //            break;
                //        case "port":
                //            port = value.Value;
                //            break;
                //        case "httpsPort":
                //            httpsPort = value.Value;
                //            break;

                //    }
                //}

                var options = new StartOptions();
                TpsLogManager<TPService>.Debug("Setting base address");
                if (useHttps)
                    options.Urls.Add(@"https://+:" + httpsPort + "/");
                //options.Urls.Add("http://+:" + port + "/");
                options.Urls.Add("http://*:" + port + "/"); ;

                try
                {
                    // Start OWIN host 
                    using (WebApp.Start(options))
                    {

                        TpsLogManager<TPService>.Info("Web API listening at: " + options.Urls[0]);
                        Console.WriteLine("Web API listening at: " + options.Urls[0]);
                        try
                        {
                            Thread.Sleep(Timeout.Infinite);
                        }
                        catch (ThreadAbortException)
                        {
                            Thread.ResetAbort();
                        }
                        catch (ThreadInterruptedException ex)
                        {
                            ex = null;
                        }
                        finally
                        {
                            string logMessage = string.Format(CultureInfo.CurrentCulture, "Stopped worker of type '{0}'.", this.GetType().FullName);
                            Console.WriteLine(logMessage);
                            TpsLogManager<TPService>.Debug(logMessage);
                        }
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    TpsLogManager<TPService>.Error(ex.Message);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                TpsLogManager<TPService>.Error(e.Message, e);
            }
        }

        public void Stop()
        {
            string logMessage = string.Format(CultureInfo.CurrentCulture, "Stopping worker of type '{0}'.", GetType().FullName);
            TpsLogManager<TPService>.Info(logMessage);

            foreach (var app in instance.ConnectToApplications)
            {
                app.ShutDown();
                app.ConfigServer.Shutdown();
            }

            Task.WaitAll();
            instance = null;
            if (_thread != null)
                _thread.Interrupt();

        }


    }
}
