using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Akka.Actor;
using RTLSProvider.ItemSense;

//This class receives tag Reports from ItemSense and routes it for processing
namespace RTLSProvider.Actor
{
    public class TagBroker : ReceiveActor
    {
        readonly Dictionary<string, IActorRef> _tagProcessors = new Dictionary<string, IActorRef>();
        private readonly int _heartbeat;



        public TagBroker(NameValueCollection appSettings)
        {
            _heartbeat =  Int32.Parse(appSettings.Get("Heartbeat"));
            var processor = new Action<ITargetReportable>(m => GetEpcProcessor(m.Epc).Tell(m, ActorRefs.NoSender));

            Receive<AmqpMessage>(processor);
            Receive<List<ImpinjItem>>(message => message.ForEach(m => processor(m)));
        }





        private IActorRef GetEpcProcessor(string epc)
        {
            if (!_tagProcessors.ContainsKey(epc))
                _tagProcessors.Add(epc, Context.ActorOf(TagProcessor.Props(epc,_heartbeat), epc));
            return _tagProcessors[epc];
        }





        public static Props Props(NameValueCollection appSettings)
        {
            return Akka.Actor.Props.Create<TagBroker>(appSettings);
        }
    }
}
