using MassTransit;
using Shared.Messages;

namespace Payment.API.PaymentEvents // Namespace adını değiştir
{
    public class PaymentStartedEvent : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; }

        public PaymentStartedEvent(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public decimal TotalPrice { get; set; }
        public List<OrderItemMessage> OrderItems { get; set; }
    }
}
