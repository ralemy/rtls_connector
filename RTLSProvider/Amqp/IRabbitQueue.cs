using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RTLSProvider.Amqp
{
    public interface IRabbitQueue
    {
        ConnectionFactory CreateFactoy(QueueFactoryParams factoryParams);
        void Initialize(ConnectionFactory factory);
        void AddReceiver(EventHandler<BasicDeliverEventArgs> receiver);
        void Consume(String queue);
    }

    public class QueueFactoryParams
    {
        public String HostName;
        public Int32 Port;
        public String VirtualHost;
        public String UserName;
        public String Password;
        public Boolean AutomaticRecoveryEnabled;
    }
}
