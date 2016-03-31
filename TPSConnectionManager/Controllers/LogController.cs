using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;



using LogConfigLayer;


namespace TPSConnectionManager.Controllers
{
    public class LogController : ApiController
    {
        public HttpResponseMessage Get()
        {
            return GetAllLogEntries();
        }

        public HttpResponseMessage GetById(string id)
        {
            return GetSpecificLogEntries(id);

        }

        [HttpPost]
        public HttpResponseMessage Post([FromBody] LogTypeClass logType)
        {
            if (logType != null)
            {
                LogsCollection.Clear(logType.logType);
                return GetSpecificLogEntries(logType.logType);
            }
            else
            {
                LogsCollection.Clear(null);
                return GetAllLogEntries();
            }
        }

        private HttpResponseMessage GetAllLogEntries()
        {
            if (Properties.Settings.Default.IsHtmlLog)
            {
                StringBuilder htmlString = new StringBuilder("");
                htmlString.Append("<html><body><form action='" +
                                  Url.Link("DefaultApi", new {httproute = "", controller = "Log"}) +
                                  "' method='post'><input type='submit' value='Delete'></form><table><tr style='border:solid 2px;'><th style='border:solid 1px; text-align:center;'>SN</th><th style='border:solid 1px; text-align:center;'>When</th><th style='border:solid 1px; text-align:center;'>Type</th><th style='border:solid 1px; text-align:center;'>Message</th><th style='border:solid 1px; text-align:center;'>Source</th><th style='border:solid 1px; text-align:center;'>Exception</th></tr>");
                foreach (var log in LogsCollection.Logs.OrderBy(x => x.Key))
                {
                    htmlString.Append("<tr style='border:solid 2px;'><td style='border:solid 1px;'>" +
                                      log.Key.ToString() + "</td><td style='border:solid 1px;'>" +
                                      ((LogEntity) log.Value).When + "</td><td style='border:solid 1px;'>" +
                                      ((LogEntity) log.Value).Type + "</td><td style='border:solid 1px;'>" +
                                      ((LogEntity) log.Value).Message + "</td><td style='border:solid 1px;'>" +
                                      ((LogEntity) log.Value).Source + "</td><td style='border:solid 1px;'>" +
                                      ((LogEntity) log.Value).Exception + "</td></tr>");
                }
                htmlString.Append("</table></body></html>");
                var response = new HttpResponseMessage();
                response.Content = new StringContent(htmlString.ToString());
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                return response;
            }
            else
                return Request.CreateResponse<Dictionary<long, LogEntity>>(HttpStatusCode.OK, LogsCollection.Logs);
        }

        private HttpResponseMessage GetSpecificLogEntries(string id)
        {
            if (Properties.Settings.Default.IsHtmlLog)
            {
                StringBuilder htmlString = new StringBuilder("");

                htmlString.Append("<html><body><form action='" +
                                  Url.Link("DefaultApi", new {httproute = "", controller = "Log"}) +
                                  "' method='post'><input type='hidden' name='logType' value='" + id +
                                  "' /><input type='submit' value='Delete'></form><table><tr style='border:solid 2px;'><th style='border:solid 1px; text-align:center;'>SN</th><th style='border:solid 1px; text-align:center;'>When</th><th style='border:solid 1px; text-align:center;'>Type</th><th style='border:solid 1px; text-align:center;'>Message</th><th style='border:solid 1px; text-align:center;'>Source</th><th style='border:solid 1px; text-align:center;'>Exception</th></tr>");
                foreach (var log in LogsCollection.Logs.OrderBy(x => x.Key))
                {
                    if (((LogEntity) log.Value).Type.ToLower() == id.ToLower())
                        htmlString.Append("<tr style='border:solid 2px;'><td style='border:solid 1px;'>" +
                                          log.Key.ToString() + "</td><td style='border:solid 1px;'>" +
                                          ((LogEntity) log.Value).When + "</td><td style='border:solid 1px;'>" +
                                          ((LogEntity) log.Value).Type + "</td><td style='border:solid 1px;'>" +
                                          ((LogEntity) log.Value).Message + "</td><td style='border:solid 1px;'>" +
                                          ((LogEntity) log.Value).Source + "</td><td style='border:solid 1px;'>" +
                                          ((LogEntity) log.Value).Exception + "</td></tr>");
                }
                htmlString.Append("</table></body></html>");
                var response = new HttpResponseMessage();
                response.Content = new StringContent(htmlString.ToString());
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                return response;
            }
            else
                return Request.CreateResponse<Dictionary<long, LogEntity>>(HttpStatusCode.OK, (LogsCollection.Logs.Where(x => ((LogEntity)x.Value).Type.ToLower() == id.ToLower())).ToDictionary(y => y.Key, y=> y.Value));
        }

    }

    public class LogTypeClass
    {
        public string logType { get; set; }
    }


}
