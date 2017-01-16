using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSProvider.Actor
{
    class ReportRequest
    {
        public Dictionary<string, string> Arguments = new Dictionary<string, string>();
    }

    class DiscardRequest
    {
        public List<string> DiscardedTags = new List<string>();
    }
}