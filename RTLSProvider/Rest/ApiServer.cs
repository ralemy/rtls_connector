using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json;
using RTLSProvider.Actor;

namespace RTLSProvider.Rest
{
    class ApiServer
    {
        private readonly NameValueCollection _appSettings;
        private ConnectorActors _system;


        public ApiServer(NameValueCollection appSettings, ConnectorActors system)
        {
            _appSettings = appSettings;
            _system = system;
        }

        public void SetActorSystem(ConnectorActors system)
        {
            _system = system;
        }

        public void ServeApi(HttpListenerContext c, string path)
        {
            if (path.StartsWith("/rtls/config"))
                ConfigCrud(c);
            else if (path.StartsWith("/rtls/items"))
                GetItems(c);
            else if (path.StartsWith("/rtls/discard"))
                DiscardItems(c);
            else if (path.StartsWith("/rtls/register"))
                RegisterToQueue(c);
            else
                HttpServer.SendError(c, 404, "Path Not found " + path);
        }

        private void RegisterToQueue(HttpListenerContext c)
        {
            HttpServer.SendResponse(c,@"{""serverUrl"":""http://localhost:5672/%2F"", ""queueId"":""locate""}");
        }

        private void DiscardItems(HttpListenerContext c)
        {
            if (_system == null)
                HttpServer.SendError(c, 500, "Configuration not correct");
            else if (c.Request.HttpMethod != HttpMethod.Post.Method)
                HttpServer.SendError(c, 400, "Bad Method. Use Post");
            else
                try
                {
                    var response = _system.DiscardTags(new DiscardRequest()
                    {
                        DiscardedTags = JsonConvert.DeserializeObject<List<string>>
                            (HttpServer.GetRequestPayload(c))
                    });
                    HttpServer.SendResponse(c, response);
                }
                catch (TimeoutException)
                {
                    HttpServer.SendError(c, 500, "Operation Timed out");
                }
        }


        private void GetItems(HttpListenerContext c)
        {
            if (_system == null) HttpServer.SendError(c, 500, "Configuration not correct");
            else
                try
                {
                    var items = _system.ReportItems(new ReportRequest()
                    {
                        Arguments = c.Request.QueryString
                    });
                    HttpServer.SendResponse(c, JsonConvert.SerializeObject(items));
                }
                catch (TimeoutException)
                {
                    HttpServer.SendError(c, 500, "Operation timed out");
                }
        }


        private void ConfigCrud(HttpListenerContext c)
        {
            if (c.Request.HttpMethod == HttpMethod.Get.Method)
                HttpServer.SendResponse(c, DumpConfiguration());
            else if (c.Request.HttpMethod == HttpMethod.Post.Method)
            {
                var payload = JsonConvert.DeserializeObject<Dictionary<string, string>>
                    (HttpServer.GetRequestPayload(c));
                HttpServer.SendResponse(c, UpdateConfig(payload));
            }
        }

        private string UpdateConfig(Dictionary<string, string> payload)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = config.AppSettings.Settings;
                foreach (var key in payload.Keys)
                    if (settings.AllKeys.Contains(key))
                        settings[key].Value = HttpUtility.UrlDecode(payload[key]);
                    else
                        settings.Add(key, payload[key]);
                foreach (var key in _appSettings.AllKeys)
                {
                    _appSettings[key] = settings[key].Value;
                }
                config.Save(ConfigurationSaveMode.Full);
                return @"{""result"":""success""}";
            }
            catch (Exception e)
            {
                return @"{""result"":""" + e.Message + @"""}";
            }
        }


        private string DumpConfiguration()
        {
            var conf = new Dictionary<string, string>();
            foreach (var key in _appSettings.AllKeys)
            {
                conf.Add(key, _appSettings.Get(key));
            }
            return JsonConvert.SerializeObject(conf);
        }
    }
}