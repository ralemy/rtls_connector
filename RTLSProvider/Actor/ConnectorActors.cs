using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using Akka.Actor;
using RTLSProvider.Amqp;
using RTLSProvider.ItemSense;

namespace RTLSProvider.Actor
{
    class ConnectorActors
    {
        private IActorRef _logger;
        private readonly IActorRef _reporter;
        private readonly IActorRef _broker;
        private readonly ActorSystem _system;
        private IRabbitQueue _mQueue;
        private Inbox _inbox;

        public ConnectorActors(string systemName, NameValueCollection appSettings, EventLog eventLog, IRabbitQueue mQueue)
        {
            _mQueue = mQueue;
            _system = ActorSystem.Create(systemName);
            _logger = _system.ActorOf(TagLogger.Props(eventLog), "Logger");
            _reporter = _system.ActorOf(TagReporter.Props(mQueue), "TagReporter");
            _broker = _system.ActorOf(TagBroker.Props(appSettings), "TagBroker");
            _inbox = CreateInbox();
        }

        public Action<AmqpMessage> ProcessAmqp()
        {
            return msg => _broker.Tell(msg, ActorRefs.NoSender);
        }

        public IActorRef Reporter()
        {
            return _reporter;
        }

        public void Terminate()
        {
            _system.Terminate();
            _mQueue.ReleaseQueue();
        }

        public Inbox CreateInbox()
        {
            return Inbox.Create(_system);
        }

        public List<RtlsMessage> ReportItems(ReportRequest r)
        {
            _inbox.Send(_reporter,r);
            return (List<RtlsMessage>) _inbox.Receive(TimeSpan.FromSeconds(10));
        }

        public string DiscardTags(DiscardRequest discardRequest)
        {
            _inbox.Send(_reporter,discardRequest);
            return (string) _inbox.Receive(TimeSpan.FromSeconds(10));
        }
    }
}
