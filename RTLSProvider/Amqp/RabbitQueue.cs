using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RTLSProvider.Amqp
{
    public class RabbitQueue
    {
        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;
        private QueueFactoryParams _factoryParams;

        public void Initialize(ConnectionFactory connectionFactory)
        {
            if (connectionFactory != null) _connection = connectionFactory.CreateConnection();
            if (_connection != null) _channel = _connection.CreateModel();
            if (_channel != null) _consumer = new EventingBasicConsumer(_channel);
        }

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

        public void Declare(string queue)
        {
            if (_factoryParams != null)
                _channel.QueueDeclare(queue: queue, durable: _factoryParams.QueueDurable,
                    exclusive: _factoryParams.QueueExclusive,
                    autoDelete: _factoryParams.AutoDelete, arguments: null);
        }

        public void Publish(string queue, string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: null, body: body);
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