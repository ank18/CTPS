using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace TPSConnectionManager.Controllers
{
    public class MainController : ApiController
    {

        public HttpResponseMessage GetByID(string appName)
        {
            try
            {
                
                foreach (var ConnectToApplication in TPService.Instance.ConnectToApplications)
                {
                    if (ConnectToApplication.ApplicationIdentifier == appName)
                    {
                        var response = new HttpResponseMessage();
                        response.Content = new StringContent(ConnectToApplication.ProcessOwinMessage(appName));
                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                        return response;
                    }
                }

                var outerResponse = new HttpResponseMessage();
                outerResponse.Content = new StringContent("You have NOT hit : " + appName);
                outerResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                return outerResponse;

            }
            catch (Exception ex)
            {

                var response = new HttpResponseMessage();
                response.Content = new StringContent("You have not hit chat");
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                return response;
            }

        }
    }
}
