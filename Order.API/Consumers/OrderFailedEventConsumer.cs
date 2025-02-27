using MassTransit;
using Order.API.Context;
using Shared.OrderEvents;

namespace Order.API.Consumers
{
    public class OrderFailedEventConsumer(OrderDbContext orderDbContext) : IConsumer<OrderFailEvent>
    {
        public async Task Consume(ConsumeContext<OrderFailEvent> context)
        {
            Order.API.Models.Order order = await orderDbContext.Orders.FindAsync(context.Message.OrderId);
            if (order != null) //orderıd ye karşılık sipariş varsa if e girer.
            {
                order.OrderStatus = Enums.OrderStatus.Fail;
                await orderDbContext.SaveChangesAsync();
            }

        }
    }
}