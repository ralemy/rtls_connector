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
                    Sender.Tell(FilterTags(message.Arguments), Self);
                });
            Receive<DiscardRequest>(message =>
            {
                if (message == null || message.DiscardedTags == null) return;
                message.DiscardedTags.ForEach(tag =>
                {
                    if (_currentTags.ContainsKey(tag)) _currentTags.Remove(tag);
                });
                Sender.Tell("Tags Discarded", Self);
                broker.Tell(message,ActorRefs.NoSender);
            });
        }

        private string FindArgument(IEnumerable<string> args, string arg)
        {
            try
            {
                return args.First(key => key.ToLower() == arg);
            }
            catch (InvalidOperationException)
            {
            }
            catch (ArgumentNullException)
            {
            }
            return null;
        }

        private List<RtlsMessage> FilterTags(NameValueCollection args)
        {
            var result = _currentTags.ToList().ConvertAll<RtlsMessage>(p => p.Value);
            var key = FindArgument(args.AllKeys, "epcprefix");
            if (key != null)
                result.RemoveAll(msg => !msg.Epc.StartsWith(args[key]));
//            key = FindArgument(args.AllKeys, "fromtime");
            return result;
        }

        public static Props Props(IRabbitQueue outputQueue)
        {
            return Akka.Actor.Props.Create<TagReporter>(outputQueue);
        }
    }
}