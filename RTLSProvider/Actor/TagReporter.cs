using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Newtonsoft.Json;
using RTLSProvider.Amqp;
using RTLSProvider.ItemSense;

namespace RTLSProvider.Actor
{
    class TagReporter : ReceiveActor
    {
        public TagReporter(IRabbitQueue outputQueue)
        {
            Receive<RtlsMessage>(message =>
            {
                outputQueue.Publish(outputQueue.PublishQueueName(), JsonConvert.SerializeObject(message));
            });
        }

        public static Props Props(IRabbitQueue outputQueue)
        {
            return Akka.Actor.Props.Create<TagReporter>(outputQueue);
        }

    }
}
