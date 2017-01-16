using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using RTLSProvider.Actor;
using RTLSProvider.Amqp;
using RTLSProvider.ItemSense;

namespace RTLSProvider
{
    partial class ConnectorService : ServiceBase
    {
        private readonly EventLog _eventLog;
        private ConnectorActors _actorSystem;
        private ItemSenseProxy _itemSense;

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
            Run(appSettings);

        }

        public void Run(NameValueCollection appSettings)
        {
            _actorSystem = new ConnectorActors("ImpinjRTLS", appSettings, _eventLog, CreateOutputQueue(appSettings));
            _itemSense = new ItemSenseProxy(appSettings, new RabbitQueue());
            _itemSense.ConsumeQueue(new AmqpRegistrationParams(), _actorSystem.ProcessAmqp());

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

        protected override void OnStop()
        {
            Shutdown();
        }

        public void Shutdown()
        {
           _eventLog.WriteEntry("Stopping Connector",EventLogEntryType.Information,1,1);
            _itemSense?.ReleaseQueue();
            _actorSystem?.Terminate();

        }
    }
}
