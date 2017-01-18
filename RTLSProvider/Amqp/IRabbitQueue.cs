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
        ConnectionFactory CreateFactory(QueueFactoryParams factoryParams);
        void Initialize(ConnectionFactory factory);
        void AddReceiver(EventHandler<BasicDeliverEventArgs> receiver);
        void Consume(String queue);
        void ReleaseQueue();
        void Publish(String queueName , String messsage);
        String PublishQueueName();
    }

    public class QueueFactoryParams //ToDo: review to move to configuration
    {
        public String HostName = "localhost";
        public Int32 Port = 5672;
        public String VirtualHost = "/";
        public String UserName;
        public String Password;
        public Boolean AutomaticRecoveryEnabled = true;
        public Boolean QueueNoAck = true;
        public Boolean QueueDurable = false;
        public Boolean QueueExclusive = false;
        public Boolean AutoDelete = false;
        public string MessageTtl = "60000";
    }
}
