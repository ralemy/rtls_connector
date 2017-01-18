using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Xml;

namespace RTLSProvider
{
    [RunInstaller(true)]
    public partial class ServiceInstaller : System.ServiceProcess.ServiceInstaller
    {
        private readonly EventLog _logger = new EventLog("Application", ".", "impinj_rtls_connector");

        public ServiceInstaller()
        {
            Description = "Connector Service to send RAIN RFID data from Impinj platfrorm to an AMQP broker";
            DisplayName = "Impinj RTLS connector ";
            ServiceName = "impinj_rtls_connector";
            StartType = ServiceStartMode.Automatic;
            InitializeComponent();
        }

        public override void Uninstall(IDictionary savedState)
        {
            _logger.WriteEntry("Uninstalling RTLS Connector Services", EventLogEntryType.Information, 10, 2);
            base.Uninstall(savedState);
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            _logger.WriteEntry("Installing RTLS Connector Services", EventLogEntryType.Information, 10, 2);

            var propertyDictionary = CreatePropertyDictionary(getPropertyKeys(), Context.Parameters);

            UpdateProperties(propertyDictionary, Context.Parameters["assemblypath"]);
        }

        private static void UpdateProperties(StringDictionary propertyDictionary, string assemblyPath)
        {
            var doc = new XmlDocument();
            var appConfigPath = LoadConfigurattion(doc, assemblyPath);
            var appSettingsNode = FindAppSettingsNode(doc);
            if (appSettingsNode != null)
                foreach (var key in propertyDictionary.Keys)
                {
                    SetPropertyValue(appSettingsNode, key.ToString(), propertyDictionary[key.ToString()]);
                }
            doc.Save(appConfigPath);
        }

        private StringDictionary CreatePropertyDictionary(List<string> keys, StringDictionary dict)
        {
            var result = new StringDictionary();
            int port;
            keys.ForEach(key =>
            {
                switch (key)
                {
                    case "ConfigurationPort":
                        if (dict.ContainsKey(key))
                            if (int.TryParse(dict[key], out port))
                                if (port > 0 && port < 65535)
                                    result.Add(key, dict[key]);
                        break;
                    default:
                        result.Add(key, dict.ContainsKey(key) ? dict[key] : "");
                        break;
                }
            });
            return result;
        }

        private List<string> getPropertyKeys()
        {
            return new List<string> { "ConfigurationPort" };
        }

        private static void SetPropertyValue(XmlNode appSettingsNode, string key, string value)
        {
            var node =
                appSettingsNode.ChildNodes.Cast<XmlNode>()
                    .FirstOrDefault(n => n?.Attributes?["key"] != null && n.Attributes["key"].Value.ToLower() == key);
            if (node?.Attributes?["value"] != null) node.Attributes["value"].Value = value;
        }

        private static XmlNode FindChild(XmlNode n, string tagName)
        {
            return n.ChildNodes.Cast<XmlNode>().FirstOrDefault(nChildNode => nChildNode.Name.Equals(tagName));
        }

        private static XmlNode FindAppSettingsNode(XmlDocument doc)
        {
            return FindChild(doc.DocumentElement, "appSettings");
        }

        private static string LoadConfigurattion(XmlDocument doc, string assemblyPath)
        {
            var appConfigPath = assemblyPath + ".config";
            doc.Load(appConfigPath);
            return appConfigPath;
        }

        protected override void OnAfterInstall(IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
            using (var sc = new ServiceController(ServiceName))
            {
                sc.Start();
            }
        }
    }
    [RunInstaller(true)]
    public sealed class ServiceProccessInstaller : ServiceProcessInstaller
    {
        public ServiceProccessInstaller()
        {
            Account = ServiceAccount.LocalSystem;
        }

    }

}
