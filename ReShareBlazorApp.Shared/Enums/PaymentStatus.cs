using System;
using System.Collections.Generic;
using System.Text;

namespace ReShareBlazorApp.Shared.Enums
{
    public enum PaymentStatus
    {
        Pending,
        Paid,
        Failed,
        Refunded,
        Cancelled,
        AwaitingShipment,
        Shipped,
        Delivered,
        NotDelivered
    }
}
