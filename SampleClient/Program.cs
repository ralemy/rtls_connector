using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SampleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "locate",
                Password = "demo2016"
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, msg) =>
                {
                    var body = msg.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine("Received {0}", message);
                };
                channel.BasicConsume(queue: "locate", noAck: true, consumer: consumer);
                Console.WriteLine("Press Enter to Exit");
                Console.ReadLine();
            }
        }
    }
}
