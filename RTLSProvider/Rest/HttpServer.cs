using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Akka.Actor;
using Newtonsoft.Json;
using RTLSProvider.Actor;

namespace RTLSProvider.Rest
{
    public class DisposableImpl : IDisposable
    {
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool v)
        {
            if (_disposed) return;
            if (v) FreeManagedResources();
            _disposed = true;
        }

        protected virtual void FreeManagedResources()
        {
        }

        ~DisposableImpl()
        {
            Dispose(false);
        }
    }

    class HttpServer : DisposableImpl
    {
        private readonly int _port;
        private readonly ConnectorService _connectorService;
        private HttpListener _listener;
        private Thread _server;
        public NameValueCollection AppSettings;
        public string FlashMessage;
        public bool ConfigSaved;

        private readonly EventLog _logger;
        private ConnectorActors _system;
        private Inbox _inbox;

        public HttpServer(NameValueCollection appSettings, ConnectorService connectorService, EventLog eventLog)
        {
            ConfigSaved = false;
            FlashMessage = "";
            AppSettings = appSettings;
            _port = int.Parse(appSettings["ConfigurationPort"]);
            _connectorService = connectorService;
            _logger = eventLog;
        }

        public void Start()
        {
            if (_server != null)
            {
                _logger.WriteEntry("Http Server Already Started", EventLogEntryType.Error,
                    ConnectorService.HttpServerLogId, ConnectorService.ProcessError);
                throw new Exception("Already Started");
            }
            _server = new Thread(Listen);
            _server.Start();
        }


        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();
            try
            {
                while (true)
                    Process(_listener.GetContext());
            }
            catch (ThreadAbortException)
            {
                _logger.WriteEntry("Web Server Stopping", EventLogEntryType.Information,
                    ConnectorService.HttpServerLogId,
                    ConnectorService.ProcessError);
            }
            catch (Exception e)
            {
                _logger.WriteEntry(e.Message, EventLogEntryType.Error, ConnectorService.HttpServerLogId,
                    ConnectorService.ProcessError);
            }
        }


        protected virtual void Process(HttpListenerContext c)
        {
            var path = c.Request.Url.AbsolutePath;
            if (path.StartsWith("/rtls/"))
                ServeApi(c, path);
            else
                ServeFile(c, path);
        }

        private void ServeApi(HttpListenerContext c, string path)
        {
            if (path.StartsWith("/rtls/config"))
                ConfigCrud(c);
            else if (path.StartsWith("/rtls/items"))
                ServeFile(c, GetItems(c));
            else if (path.StartsWith("/rtls/discard"))
                ServeFile(c, DiscardItems(c));
            else ServeFile(c, path);
        }

        private string DiscardItems(HttpListenerContext c)
        {
            if (c.Request.HttpMethod != HttpMethod.Post.Method) return "Bad Method. Use Post";
            var payload = GetRequestPayload(c);
            try
            {
                _inbox.Send(_system.Reporter(), new DiscardRequest()
                {
                    DiscardedTags = JsonConvert.DeserializeObject<List<string>>(payload)
                });
                _inbox.Receive(TimeSpan.FromSeconds(10));
                return "Tags Discarded";
            }
            catch (TimeoutException)
            {
                return "Operation Timed out";
            }
        }

        private string GetItems(HttpListenerContext c)
        {
            try
            {
                _inbox.Send(_system.Reporter(), new ReportRequest());
                return (string) _inbox.Receive(TimeSpan.FromSeconds(10));
            }
            catch (TimeoutException)
            {
                return "Operation timed out";
            }
        }

        private void ConfigCrud(HttpListenerContext c)
        {
            if (c.Request.HttpMethod == HttpMethod.Get.Method)
                ServeFile(c, DumpConfiguration());
            else if (c.Request.HttpMethod == HttpMethod.Post.Method)
            {
                var payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(GetRequestPayload(c));
                ServeFile(c, UpdateConfig(payload));
            }
        }

        private string GetRequestPayload(HttpListenerContext c)
        {
            if (c.Request.HasEntityBody)
                using (var body = c.Request.InputStream)
                using (var reader = new StreamReader(body, c.Request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            return "";
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
                foreach (var key in AppSettings.AllKeys)
                {
                    AppSettings[key] = settings[key].Value;
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
            foreach (var key in AppSettings.AllKeys)
            {
                conf.Add(key, AppSettings.Get(key));
            }
            return JsonConvert.SerializeObject(conf);
        }

        private void ServeFile(HttpListenerContext c, string path)
        {
            SendResponse(c.Response, Encoding.UTF8.GetBytes(path));
        }


        public void SendResponse(HttpListenerResponse response, byte[] buffer)
        {
            response.AddHeader("Cache-Control", "no-cache");
            response.AddHeader("Pragma", "no-cache");
            var output = response.OutputStream;
            response.ContentLength64 = buffer.Length;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        public void Stop()
        {
            _listener?.Stop();
            _server?.Abort();
            _listener = null;
            _server = null;
        }

        protected override void Dispose(bool v)
        {
            Stop();
            base.Dispose(v);
        }

        public void SetActorSystem(ConnectorActors actorSystem)
        {
            _system = actorSystem;
            _inbox = _system.CreateInbox();
        }
    }
}