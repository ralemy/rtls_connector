using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RTLSProvider.ItemSense
{
    //Zone Map
    public class ZonePoint
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("z")] public float Z;
    }

    public class ZoneObject
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("floor")] public string Floor;
        [JsonProperty("points")] public List<ZonePoint> Points;
    }

    public class ZoneMap
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("facility")] public string Facility;
        [JsonProperty("zones")] public List<ZoneObject> Zones;
    }


    //AMQP
    public class AmqpRegistrationParams
    {
        [JsonProperty("epc", NullValueHandling = NullValueHandling.Ignore)]
        public string Epc { get; set; }

        [JsonProperty("fromZone", NullValueHandling = NullValueHandling.Ignore)]
        public string FromZone { get; set; }

        [JsonProperty("toZone", NullValueHandling = NullValueHandling.Ignore)]
        public string ToZone { get; set; }

        [JsonProperty("zoneTransitionsOnly", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Boolean ZoneTransitionsOnly { get; set; }
    }

    public class AmqpServerInfo
    {
        [JsonProperty("serverUrl")]
        public string ServerUrl { get; set; }

        [JsonProperty("queue")]
        public string Queue { get; set; }
    }

    public interface ITargetReportable
    {
        string Epc { get; set; }
        string Zone { get; set; }
        DateTime TimeStamp { get; set; }
        string X { get; set; }
        string Y { get; set; }
    }

    public class AmqpMessage : ITargetReportable
    {
        [JsonProperty("epc")]
        public string Epc { get; set; }

        [JsonProperty("toZone")]
        public string Zone { get; set; }

        [JsonProperty("observationTime")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("toX")]
        public string X { get; set; }

        [JsonProperty("toY")]
        public string Y { get; set; }
    }

    public class RtlsMessage
    {
        [JsonProperty("epc")]
        public string Epc { get; set; }

        [JsonProperty("area")]
        public string Area { get; set; }

        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonIgnore] public string ItemSenseZone = "";
        [JsonIgnore] private readonly Regex _regex = new Regex(@"(.+)_([-.\d]+)_([-.\d]+)$");
        [JsonIgnore] public static bool RtlsInvertY = true;

        public RtlsMessage()
        {
        }

        public RtlsMessage(ITargetReportable message)
        {
            Epc = message.Epc;
            TimeStamp = message.TimeStamp;
            ItemSenseZone = message.Zone;
            if (message.X != null)
                ConvertFromLocationMessage(message);
            else
                ConvertFromInventoryMessage(message);
            if (RtlsInvertY)
                Y *= -1;
        }

        private void ConvertFromLocationMessage(ITargetReportable message)
        {
            Area = message.Zone;
            X = float.Parse(message.X);
            Y = float.Parse(message.Y);
        }

        private void ConvertFromInventoryMessage(ITargetReportable message)
        {
            var m = _regex.Match(message.Zone);
            if (m.Success)
            {
                Area = m.Groups[1].ToString();
                X = float.Parse(m.Groups[2].ToString());
                Y = float.Parse(m.Groups[3].ToString());
            }
            else
            {
                Area = message.Zone;
                X = 0;
                Y = 0;
            }
        }
    }

    //GET Items
    public class ImpinjItem : ITargetReportable
    {
        [JsonProperty("epc")]
        public string Epc { get; set; }

        [JsonProperty("zone")]
        public string Zone { get; set; }

        [JsonProperty("lastModifiedTime")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("xLocation")]
        public string X { get; set; }

        [JsonProperty("yLocation")]
        public string Y { get; set; }
    }

    public class ItemsObject
    {
        [JsonProperty("items")] public List<ImpinjItem> Items;
        [JsonProperty("nextPageMarker")] public string NextPageMarker;
    }
}