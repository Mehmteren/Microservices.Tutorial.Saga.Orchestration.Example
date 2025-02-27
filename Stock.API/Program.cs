using MassTransit;
using MongoDB.Driver;
using Shared.Settings;
using Stock.API.Consumers;
using Stock.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<OrderCreatedEventConsumer>();
    configurator.AddConsumer<StockRollbackMessageConsumer>();

    configurator.UsingRabbitMq((context, _configure) =>
    {
        _configure.Host(builder.Configuration["RabbitMQ"]);

        _configure.ReceiveEndpoint(RabbitMqSettings.Stock_OrderCreatedEventQueue, e =>
    e.ConfigureConsumer<OrderCreatedEventConsumer>(context));

        _configure.ReceiveEndpoint(RabbitMqSettings.Stock_RollbackMessageQueue, e =>
            e.ConfigureConsumer<StockRollbackMessageConsumer>(context));

    });
});

builder.Services.AddSingleton<MongoDbServices>();

var app = builder.Build();

using var scope = builder.Services.BuildServiceProvider().CreateScope();
var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDbServices>();
//herhangi bir veri yoksa dummy data ekleyelim ...
if (!await (await mongoDbService.GetCollection<Stock.API.Models.Stock>()
    .FindAsync<Stock.API.Models.Stock>(Builders<Stock.API.Models.Stock>.Filter.Empty))
    .AnyAsync())
{
    mongoDbService.GetCollection<Stock.API.Models.Stock>().InsertOne(new()
    {
        ProductId = 1,
        Count = 200,
    });
    mongoDbService.GetCollection<Stock.API.Models.Stock>().InsertOne(new()
    {
        ProductId = 2,
        Count = 300,
    });
    mongoDbService.GetCollection<Stock.API.Models.Stock>().InsertOne(new()
    {
        ProductId = 3,
        Count = 50,
    });
    mongoDbService.GetCollection<Stock.API.Models.Stock>().InsertOne(new()
    {
        ProductId = 4,
        Count = 10,
    });
    mongoDbService.GetCollection<Stock.API.Models.Stock>().InsertOne(new()
    {
        ProductId = 5,
        Count = 60,
    });
}


app.Run();