using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace RTLSProvider.Rest
{
    public class DisposableImpl : IDisposable
    {
        private bool _disposed;

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
        private HttpListener _listener;
        private Thread _server;
        public NameValueCollection AppSettings;
        public string FlashMessage;
        public bool ConfigSaved;

        private readonly EventLog _logger;
        private readonly ApiServer _apiServer;

        public HttpServer(NameValueCollection appSettings, ConnectorService connectorService, EventLog eventLog)
        {
            ConfigSaved = false;
            FlashMessage = "";
            AppSettings = appSettings;
            _port = int.Parse(appSettings["ConfigurationPort"]);
            _logger = eventLog;
            _apiServer = new ApiServer(appSettings,connectorService);
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
                _apiServer.ServeApi(c,path);
            else
                SendError(c, 404, "Path Not found " + path);
        }



        public static string GetRequestPayload(HttpListenerContext c)
        {
            if (c.Request.HasEntityBody)
                using (var body = c.Request.InputStream)
                using (var reader = new StreamReader(body, c.Request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            return "";
        }

        public static void SendResponse(HttpListenerContext c, string path)
        {
            SendResponse(c.Response, Encoding.UTF8.GetBytes(path));
        }


        public static void SendResponse(HttpListenerResponse response, byte[] buffer)
        {
            response.AddHeader("Cache-Control", "no-cache");
            response.AddHeader("Pragma", "no-cache");
            var output = response.OutputStream;
            response.ContentLength64 = buffer.Length;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }


        protected override void FreeManagedResources()
        {
            _listener?.Stop();
            _server?.Abort();
            _listener = null;
            _server = null;
        }

        public static void SendError(HttpListenerContext c, int status, string message)
        {
            c.Response.StatusCode = status;
            SendResponse(c,message);
        }
    }
}