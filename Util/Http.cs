using System.Collections.Generic;
using System.Web;
using NLog;
using RestSharp;

namespace VideoClient.Util
{
    public class Http
    {
        public static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static string UrlEncode(string text)
        {
            return HttpUtility.UrlEncode(text);
        }
        
        public static string UrlDecode(string text)
        {
            return HttpUtility.UrlDecode(text);
        }

        public static string SendPost(string url, Dictionary<string, string> headers,
            Dictionary<string, string> parameters)
        {
            _logger.Info("-----------SendPost_Begin-----------");
            _logger.Info("url -> " + url);

            var client = new RestClient(UrlDecode(url));
            // client.Options.MaxTimeout
            // client.Timeout = -1;
            var request = new RestRequest("", Method.Post);

            foreach (KeyValuePair<string, string> h in headers)
            {
                request.AddHeader(h.Key, h.Value);
                _logger.Info(h.Key + " : " + h.Value);
            }

            _logger.Info(" ");

            foreach (KeyValuePair<string, string> p in parameters)
            {
                request.AddParameter(p.Key, "dataList".Equals(p.Key) ? UrlDecode(p.Value) : p.Value);
                _logger.Info(p.Key + " : " + p.Value);
            }

            RestResponse response = client.Execute(request);

            _logger.Info("StatusCode -> " + response?.StatusCode);
            _logger.Info("StatusDescription -> " + response?.StatusDescription);
            _logger.Info("ResponseContent -> " + response?.Content);
            _logger.Info("-----------SendPost_Over------------");

            return response.Content;
        }

        public static string SendPostXml(string url, Dictionary<string, string> headers, string xmlStr)
        {
            string u = UrlDecode(url);
            string x = UrlDecode(xmlStr);
            x = x.Replace("\\n", "");
            x = x.Replace("\\", "");

            _logger.Info("-----------SendPostXml_Begin-----------");
            _logger.Info("url -> " + u);
            _logger.Info("xml -> " + x);

            var client = new RestClient(u);
            // client.Timeout = -1;
            var request = new RestRequest("", Method.Post);

            request.AddHeader("Content-Type", "text/xml;charset=UTF-8");

            foreach (KeyValuePair<string, string> h in headers)
            {
                // if ("Content-Length".Equals(h.Key))
                // {
                //     request.AddHeader(h.Key, x.Length + "");
                //     log(h.Key + " : " + x.Length);
                // }
                // else 
                if ("Authorization".Equals(h.Key) || "SOAPAction".Equals(h.Key))
                {
                    request.AddHeader(h.Key, h.Value);
                    _logger.Info(h.Key + " : " + h.Value);
                }
            }

            request.AddParameter("text/xml;charset=UTF-8", x, ParameterType.RequestBody);

            RestResponse response = client.Execute(request);

            _logger.Info("StatusCode -> " + response?.StatusCode);
            _logger.Info("StatusDescription -> " + response?.StatusDescription);
            _logger.Info("ResponseContent -> " + response?.Content);
            _logger.Info("-----------SendPostXml_Over------------");

            return response?.Content;
        }

        public void testXml()
        {
            var client = new RestClient("http://dev.lssvc.cn/services/v1_1/VidyoPortalUserService/");
            // client.Timeout = -1;
            var request = new RestRequest("", Method.Post);
            request.AddHeader("Authorization", "Basic YWRtOjEyMzQ=");
            request.AddHeader("SOAPAction", "myAccount");
            request.AddHeader("Content-Type", "text/xml;charset=UTF-8");
            request.AddParameter(
                "text/xml;charset=UTF-8",
                "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:v1=\"http://portal.vidyo.com/user/v1_1\">\n   <soapenv:Header/>\n   <soapenv:Body>\n      <v1:MyAccountRequest/>\n   </soapenv:Body>\n</soapenv:Envelope>",
                ParameterType.RequestBody);
            RestResponse response = client.Execute(request);
            _logger.Info("result->" + response.Content);
        }
    }
}