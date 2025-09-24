using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Messages
{
    public class PaymentCompletedMessage : ValueChangedMessage<int>
    {
        public PaymentCompletedMessage(int quotaId) : base(quotaId) { }
    }
}
