using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using Owin;

namespace TPSConnectionManager
{
   

    class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            
            EnableCorsAttribute CorsAttribute = new EnableCorsAttribute("*", "*", "GET,POST");
            config.EnableCors(CorsAttribute);

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/v1/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            RegisterApis(config);

            appBuilder.UseWebApi(config);

        }

        
        public static void RegisterApis(HttpConfiguration config)
        {
            // remove default Xml handler
            var matches = config.Formatters.Where(f => f.SupportedMediaTypes
                                             .Where(m => m.MediaType.ToString() == "application/xml" ||
                                                         m.MediaType.ToString() == "text/xml")
                                             .Count() > 0)
                                .ToList();
            foreach (var match in matches)
                config.Formatters.Remove(match);
        }
    }
}
