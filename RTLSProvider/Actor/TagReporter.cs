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
            ActorSelection broker = Context.ActorSelection("/user/TagBroker");

            Receive<RtlsMessage>(message =>
            {
                _currentTags[message.Epc] = message;
                outputQueue.Publish(outputQueue.PublishQueueName(), JsonConvert.SerializeObject(message));
            });
            Receive<ReportRequest>(
                message =>
                {
                    Sender.Tell(
                        JsonConvert.SerializeObject(_currentTags.ToList().ConvertAll<RtlsMessage>(p => p.Value)), Self);
                });
            Receive<DiscardRequest>(message =>
            {
                message.DiscardedTags.ForEach(tag =>
                {
                    if (_currentTags.ContainsKey(tag)) _currentTags.Remove(tag);
                });
                Sender.Tell("Tags Discarded", Self);
                broker.Tell(message,ActorRefs.NoSender);
            });
        }

        public static Props Props(IRabbitQueue outputQueue)
        {
            return Akka.Actor.Props.Create<TagReporter>(outputQueue);
        }
    }
}