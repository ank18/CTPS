using System;
using System.Web.Http;
using CommonContract;
using GenesysCoreServers;
using LogConfigLayer;


namespace TemplateService
{
    public class TemplateController : ApiController
    {
        public static IConfigServer ConfigServer { get; set; }
        public static MessageServer MessageServer { get; set; }
        public static TemplateService TemplateService { get; set; }

        public IHttpActionResult Get()
        {
            try
            {
                return Ok("This is a webapi project template");
            }
            catch (Exception ex)
            {
                TpsLogManager<TemplateController>.Error(ex.Message, ex);
                return Json(new { error = "This is a webapi project template and something has went wrong" });
            }

        }

        public static void SetUpControllerConnections()
        {

            ConfigServer = TemplateConnection.GetConfigServerInstance();
            MessageServer = (MessageServer)ConfigServer.MessageServer;
            TemplateService = new TemplateService(ConfigServer, MessageServer);

        }

    }
}
