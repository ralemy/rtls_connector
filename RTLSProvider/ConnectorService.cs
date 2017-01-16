using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RTLSProvider
{
    partial class ConnectorService : ServiceBase
    {
        private readonly EventLog _eventLog;

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
            _eventLog.WriteEntry("Starting Connector:" + appSettings.Get("ItemSenseUrl") , EventLogEntryType.Information,1,1);

        }

        protected override void OnStop()
        {
            Shutdown();
        }

        public void Shutdown()
        {
           _eventLog.WriteEntry("Stopping Connector",EventLogEntryType.Information,1,1);            
        }
    }
}
