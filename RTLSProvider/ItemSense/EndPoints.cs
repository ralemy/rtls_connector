using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSProvider.ItemSense
{
    internal static class EndPoints
    {
        public static readonly String MessageQueue = "/itemsense/data/v1/messageQueues/zoneTransition/configure";
        public static readonly String ZoneMap = "/itemsense/configuration/zoneMaps/show";
        public static readonly String GetItems = "/itemsense/data/v1/items/show";
    }
}
