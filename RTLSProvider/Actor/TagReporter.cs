using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Util.Internal;
using Newtonsoft.Json;
using RTLSProvider.Amqp;
using RTLSProvider.ItemSense;

namespace RTLSProvider.Actor
{
    class TagReporter : ReceiveActor
    {
        readonly Dictionary<string, RtlsMessage> _currentTags = new Dictionary<string, RtlsMessage>();

        public TagReporter(IRabbitQueue outputQueue)
        {
            Receive<RtlsMessage>(message =>
            {
                _currentTags.Add(message.Epc, message);
                outputQueue.Publish(outputQueue.PublishQueueName(), JsonConvert.SerializeObject(message));
            });
            Receive<String>(message =>
            {
                if (message.Equals("getItems"))
                    Sender.Tell(_currentTags.ToList().ConvertAll<RtlsMessage>(p => p.Value),Self);
            });
        }

        public static Props Props(IRabbitQueue outputQueue)
        {
            return Akka.Actor.Props.Create<TagReporter>(outputQueue);
        }
    }
}