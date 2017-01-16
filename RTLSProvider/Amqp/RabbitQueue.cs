using System;
using System.Text;
using Akka.Actor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RTLSProvider.Amqp
{
    public class RabbitQueue : IRabbitQueue
    {
        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;
        private QueueFactoryParams _factoryParams;
        private string _publishQueueName;


        public ConnectionFactory CreateFactory(QueueFactoryParams factoryParams)
        {
            _factoryParams = factoryParams;
            return new ConnectionFactory()
            {
                HostName = factoryParams.HostName,
                Port = factoryParams.Port,
                VirtualHost = factoryParams.VirtualHost,
                UserName = factoryParams.UserName,
                Password = factoryParams.Password,
                AutomaticRecoveryEnabled = factoryParams.AutomaticRecoveryEnabled
            };
        }

        public void Initialize(ConnectionFactory connectionFactory)
        {
            if (connectionFactory != null) _connection = connectionFactory.CreateConnection();
            if (_connection != null) _channel = _connection.CreateModel();
            if (_channel != null) _consumer = new EventingBasicConsumer(_channel);
        }

        // Receive
        public void AddReceiver(EventHandler<BasicDeliverEventArgs> action)
        {
            _consumer.Received += action;
        }

        public void Consume(string queue)
        {
            _channel.BasicConsume(queue: queue,
                noAck: true,
                consumer: _consumer);
        }

        //Publish
        public void Declare(string queue)
        {
            if (_factoryParams != null)
            {
                _channel.QueueDeclare(queue: queue, durable: _factoryParams.QueueDurable,
                    exclusive: _factoryParams.QueueExclusive,
                    autoDelete: _factoryParams.AutoDelete, arguments: null);
                _publishQueueName = queue;
            }
        }

        public string PublishQueueName()
        {
            return _publishQueueName;
        }

        public void Publish(string queue, string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            var prop = _channel.CreateBasicProperties(); //ToDo: research the prop.contentType property
            prop.Expiration = _factoryParams.MessageTtl;
            _channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: prop, body: body);
        }

        public void ReleaseQueue()
        {
            if (_channel != null && _channel.IsOpen)
                _channel.Close();
            if (_connection != null && _connection.IsOpen)
                _connection.Close();
        }
    }
}