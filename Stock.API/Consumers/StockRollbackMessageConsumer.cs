using MassTransit;
using Shared.Messages;
using Stock.API.Services;
using MongoDB.Driver;

namespace Stock.API.Consumers
{
    public class StockRollbackMessageConsumer(MongoDbServices mongoDbService) : IConsumer<StockRollBackMessage>
    {
        public async Task Consume(ConsumeContext<StockRollBackMessage> context)
        {
            var stockCollection = mongoDbService.GetCollection<Stock.API.Models.Stock>();

            foreach (var orderItem in context.Message.OrderItems)
            {
                var filter = Builders<Stock.API.Models.Stock>.Filter.Eq(x => x.ProductId, orderItem.ProductId);
                var stock = await (await stockCollection.FindAsync(filter)).FirstOrDefaultAsync();

                if (stock != null)  // Eğer ürün stokta varsa güncelle
                {
                    stock.Count += orderItem.Count;
                    await stockCollection.ReplaceOneAsync(x => x.ProductId == orderItem.ProductId, stock);
                }
            }
        }
    }
}
