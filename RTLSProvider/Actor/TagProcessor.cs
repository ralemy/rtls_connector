using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using Akka.Actor;
using RTLSProvider.ItemSense;

namespace RTLSProvider.Actor
{
    internal class TagProcessor : ReceiveActor
    {
        public static Props Props(string epc, int heartbeat)
        {
            return Akka.Actor.Props.Create<TagProcessor>(epc, 2000, heartbeat);
        }

        private RtlsMessage _location = new RtlsMessage();
        private ITargetReportable _candidate;
        private readonly ActorSelection _tagReporter;
        private readonly Timer _t, _heartbeat;
        private readonly string _epc;
        private string _state = "initial";
        private readonly int _amqpNoiseTimer;
        private readonly int _heartbeatTimer;
        private readonly Regex _regex;

        public TagProcessor(string epc, int amqpNoiseTimer, int heartbeatTimer)
        {
            _epc = epc;
            _amqpNoiseTimer = amqpNoiseTimer;
            _heartbeatTimer = heartbeatTimer;
            _t = new Timer(ReportDelay, null, Timeout.Infinite, Timeout.Infinite);
            _heartbeat = new Timer(HeartBeat, null, Timeout.Infinite, Timeout.Infinite);
            _tagReporter = Context.ActorSelection("/user/TagReporter");
            _regex = new Regex(@"(.+)_([.\d]+)_([.\d]+)$");
            Receive<AmqpMessage>(message =>
            {
                switch (_state)
                {
                    case "initial":
                        HandleInitial(message);
                        break;
                    case "Absent":
                        HandleAbsent(message);
                        break;
                    case "Present":
                        HandlePresent(message);
                        break;
                    case "Waiting":
                        HandleWaiting(message);
                        break;
                }
            });
            Receive<ImpinjItem>(item =>
            {
                if (item.Zone.Equals(_location.ItemSenseZone)) return;
                if (!_state.Equals("Waiting"))
                    Cache(item);
                else if (!item.Zone.Equals(_candidate.Zone))
                    Cache(item);
            });
        }

        private void HeartBeat(object state)
        {
            if (_state.Equals("Absent")) return;
            _location.TimeStamp = DateTime.Now.ToUniversalTime();
            _tagReporter.Tell(_location, ActorRefs.NoSender);
            StartHeartBeat();
        }

        private void HandleWaiting(ITargetReportable message)
        {
            if (message.Zone.Equals(_candidate.Zone))
                Report(message);
            else
                Cache(message);
        }

        private void HandlePresent(ITargetReportable message)
        {
            if (!message.Zone.Equals(_location.ItemSenseZone))
                Cache(message);
        }

        private void HandleAbsent(ITargetReportable message)
        {
            if (!message.Zone.Equals("ABSENT"))
                Report(message);
        }

        private void HandleInitial(ITargetReportable message)
        {
            if (message.Zone.Equals("ABSENT"))
                Report(message);
            else
                Cache(message);
        }

        private void Report(ITargetReportable message)
        {
            var newState = message.Zone.Equals("ABSENT") ? "Absent" : "Present";
            _location = new RtlsMessage(message);
            _state = newState;
            StopTimer();
            if (newState == "Absent")
                StopHeartBeat();
            else
            {
                StartHeartBeat();
                _tagReporter.Tell(_location, ActorRefs.NoSender);
            }
        }


        private void StartHeartBeat()
        {
            _heartbeat.Change(_heartbeatTimer, Timeout.Infinite);
        }

        private void StopHeartBeat()
        {
            _heartbeat.Change(Timeout.Infinite, Timeout.Infinite);
        }


        private void Cache(ITargetReportable message)
        {
            _candidate = message;
            _state = "Waiting";
            _t.Change(_amqpNoiseTimer, Timeout.Infinite);
        }

        private void ReportDelay(object state)
        {
            if (_candidate != null)
                if (!_candidate.Zone.Equals(_location.ItemSenseZone))
                    Report(_candidate);
            StopTimer();
        }

        private void StopTimer()
        {
            _candidate = null;
            _t.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}