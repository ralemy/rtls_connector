using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSProvider.Actor
{
    class ReportRequest
    {
        public NameValueCollection Arguments = new NameValueCollection();
    }

    class DiscardRequest
    {
        public List<string> DiscardedTags = new List<string>();
    }
}