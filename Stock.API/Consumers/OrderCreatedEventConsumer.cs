using MassTransit;
using MassTransit.Transports;
using Shared.OrderEvents;
using Shared.StockEvents;
using Stock.API.Services;
using MongoDB.Driver;
using Shared.Settings;

namespace Stock.API.Consumers
{
    public class OrderCreatedEventConsumer(MongoDbServices mongoDbService) : IConsumer<OrderCreatedEvent>
    {
        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            List<bool> stockResults = new();
            var stockCollection = mongoDbService.GetCollection<Stock.API.Models.Stock>();

            foreach (var orderItem in context.Message.OrderItems)
            {
                var filter = Builders<Stock.API.Models.Stock>.Filter.Where(s => s.ProductId == orderItem.ProductId && s.Count >= orderItem.Count);
                stockResults.Add(await (await stockCollection.FindAsync(filter)).AnyAsync());
            }

            var sendEndpoint = await context.GetSendEndpoint(new Uri($"queue:" +
                $"{RabbitMqSettings.StateMachineQueue}"));

            if (stockResults.TrueForAll(s => s.Equals(true)))
            {
                foreach (var OrderItem in context.Message.OrderItems)
                {
                    var filter = Builders<Stock.API.Models.Stock>.Filter.Eq(s => s.ProductId, OrderItem.ProductId);
                    var stock = await (await stockCollection.FindAsync(filter)).FirstOrDefaultAsync();

                    if (stock != null)
                    {
                        stock.Count -= OrderItem.Count;
                        await stockCollection.ReplaceOneAsync(x => x.ProductId == OrderItem.ProductId, stock);
                    }
                }

                StockReservedEvent stockReservedEvent = new(context.Message.CorrelationId)
                {
                    OrderItems = context.Message.OrderItems,
                };
                await sendEndpoint.Send(stockReservedEvent);
            }
            else
            {
                StockNotReservedEvent stockNotReservedEvent = new(context.Message.CorrelationId)
                {
                    Message = "Stok yetersiz..."
                };
                await sendEndpoint.Send(stockNotReservedEvent);
            }
        }
    }
}
