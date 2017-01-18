using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;
using RTLSProvider.Actor;
using RTLSProvider.Amqp;
using RTLSProvider.ItemSense;
using RTLSProvider.Rest;

namespace RTLSProvider
{
    partial class ConnectorService : ServiceBase
    {
        private readonly EventLog _eventLog;
        private ConnectorActors _actorSystem;
        private ItemSenseProxy _itemSense;
        public static short ProcessError = 10;
        public static int HttpServerLogId = 100;
        private HttpServer _server;
        private bool _running;

        public ConnectorService()
        {
            InitializeComponent();
            _eventLog = new EventLog("Application",".", "impinj_rtls_connector");
        }

        protected override void OnStart(string[] args)
        {
            Startup();

        }

        public void Startup()
        {
            var appSettings = ConfigurationManager.AppSettings;
            _eventLog.WriteEntry("Starting Connector:" + appSettings.Get("ConfigurationPort") , EventLogEntryType.Information,1,1);
            RtlsMessage.RtlsInvertY = appSettings.Get("RTLSInvertY").ToLower() == "true";
            _server = new HttpServer(appSettings, this, _eventLog);
            _server.Start();
            if(appSettings.Get("ItemSenseUrl").Length > 0)
                Run(appSettings);
            else
                _eventLog.WriteEntry("First Run, No ItemSense", EventLogEntryType.Information, 1, 1);

        }

        public void Run(NameValueCollection appSettings)
        {
            _eventLog.WriteEntry("Running against ItemSense:" + appSettings.Get("ItemSenseUrl"), EventLogEntryType.Information, 1, 1);
            _actorSystem = new ConnectorActors("ImpinjRTLS", appSettings, _eventLog, CreateOutputQueue(appSettings));
            _itemSense = new ItemSenseProxy(appSettings, new RabbitQueue());
            _itemSense.ConsumeQueue(new AmqpRegistrationParams(), _actorSystem.ProcessAmqp());
            _running = true;
        }

        private static IRabbitQueue CreateOutputQueue(NameValueCollection appSettings)
        {
            var mQueue= new RabbitQueue();
            var factory = mQueue.CreateFactory(new QueueFactoryParams()
            {
                UserName = appSettings.Get("TargetUser"),
                Password = appSettings.Get("TargetPassword"),
                MessageTtl = appSettings.Get("MessageTTL")
            });
            mQueue.Initialize(factory);
            mQueue.Declare(appSettings.Get("TargetQueue"));
            return mQueue;
        }

        protected override void OnShutdown()
        {
            Shutdown();
        }

        public void Shutdown()
        {
           _eventLog.WriteEntry("Stopping Connector",EventLogEntryType.Information,1,1);
            _itemSense?.ReleaseQueue();
            _actorSystem?.Terminate();
            _server?.Dispose();
            _running = false;
        }

        public string DiscardTags(DiscardRequest discardRequest)
        {
            return _actorSystem.DiscardTags(discardRequest);
        }

        public List<RtlsMessage> ReportItems(ReportRequest reportRequest)
        {
            return _actorSystem.ReportItems(reportRequest);
        }

        public bool Running()
        {
            return _running;
        }
    }
}
