using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RTLSProvider
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "impinj",
                Password = "It3ms3ns3"
            };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "locate", durable: false, exclusive: false, autoDelete: false,
                        arguments: null);
                    var message = "Hello World";
                    var body = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish(exchange:"", routingKey:"locate", basicProperties:null, body:body);
                    Console.WriteLine(" Sent {0}", message);
                }
            }
            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }
    }
}
