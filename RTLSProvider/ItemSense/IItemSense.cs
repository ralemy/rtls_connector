using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSProvider.ItemSense
{
    public interface IItemSense
    {
        void ConsumeQueue(AmqpRegistrationParams queueParams, Action<AmqpMessage> reporter);
        void ReleaseQueue();
    }
}
