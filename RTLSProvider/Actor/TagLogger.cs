using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace RTLSProvider.Actor
{
    class TagLogger : ReceiveActor
    {
        public TagLogger(EventLog logger)
        {
            Receive<WebException>(
                message => { logger.WriteEntry(message.Message, EventLogEntryType.Information, 2, 2); });
            ReceiveAny(message => { logger.WriteEntry(message.ToString(), EventLogEntryType.Information, 2, 2); });
        }

        public static Props Props(EventLog eventLog)
        {
            return Akka.Actor.Props.Create<TagLogger>(eventLog);
        }
    }
}