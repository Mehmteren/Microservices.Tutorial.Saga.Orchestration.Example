using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.API.Consumers;
using Order.API.Context;
using Order.API.ViewModels;
using Shared.OrderEvents;
using Shared.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<OrderCompletedEventConsumer>();
    configurator.AddConsumer<OrderFailedEventConsumer>();

    configurator.UsingRabbitMq((context, _configure) =>
    {
        _configure.Host(builder.Configuration["RabbitMQ"]);

        _configure.ReceiveEndpoint(RabbitMqSettings.Order_OrderCompletedEventQueue ,e => e.ConfigureConsumer<OrderCompletedEventConsumer>(context));
        _configure.ReceiveEndpoint(RabbitMqSettings.Order_OrderFailedEventQueue, e => e.ConfigureConsumer<OrderFailedEventConsumer>(context));


    });
});

builder.Services.AddDbContext<OrderDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("MSSQLServer")));



var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/create-order", async (CreateOrderVM model, OrderDbContext context,
    ISendEndpointProvider sendEndpointProvider) =>
{
    Order.API.Models.Order order = new()
    {
        BuyerId = model.BuyerId,
        CreatedDate = DateTime.UtcNow,
        OrderStatus = Order.API.Enums.OrderStatus.Suspend,
        TotalPrice = model.OrderItem.Sum(oi => oi.Count * oi.Price),
        OrderItems = model.OrderItem.Select(oi => new
        Order.API.Models.OrderItem
        {
            Price = oi.Price,
            Count = oi.Count,
            ProductId = oi.ProductId,
        }).ToList(),
    };

    await context.Orders.AddAsync(order);
    await context.SaveChangesAsync();

    //burada siparis? olus?turuluyor bizde orderstartedevent i yayýnlamalýyýz.
    //orderstartedevent u?ru?nde bir ýnstace olus?turalým.bunu statemachine e go?ndermeliyiz.
    //tetikleyici event
    OrderStartedEvent orderStartedEvent = new()
    {
        BuyerId = model.BuyerId,
        OrderId = order.Id,
        TotalPrice = model.OrderItem.Sum(oi => oi.Count * oi.Price),
        OrderItems = model.OrderItem.Select(oi => new Shared.Messages.OrderItemMessage
        {
            Price = oi.Price,
            Count = oi.Count,
            ProductId = oi.ProductId,
        }).ToList(),
    };


    var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:" +
        $"{RabbitMqSettings.StateMachineQueue}"));
    await sendEndpoint.Send<OrderStartedEvent>(orderStartedEvent);
    //event tu?ru?nden bir ýnstance(orderStartedEvent) go?ndericeg?imizi bildirdik.


});

app.Run();