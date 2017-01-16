using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSProvider.Actor
{
    class ReportRequest
    {
        public static readonly string GetItems = "getItems";
        public String Command = "";
        public Dictionary<string,string> Arguments = new Dictionary<string, string>();
    }
}
