using Topshelf;

namespace TPSConnectionManager
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<TPService>(s =>
                {
                    s.ConstructUsing(name => TPService.Instance);
                    s.WhenStarted(svc => svc.Start());
                    s.WhenStopped(svc => svc.Stop());
                });

                x.EnableShutdown();
                x.RunAsLocalSystem();
                x.SetDescription("Main service for all Concierge Applications");
                x.SetDisplayName("TPSConnectionManager");
                x.SetServiceName("TPSConnectionManager");
            });
            
        }

    }



}
