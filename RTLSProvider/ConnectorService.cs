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
        public ConnectorService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Startup();

        }

        public void Startup()
        {
            var appSettings = ConfigurationManager.AppSettings;
        }

        protected override void OnStop()
        {
            Shutdown();
        }

        public void Shutdown()
        {
            

        }
    }
}
