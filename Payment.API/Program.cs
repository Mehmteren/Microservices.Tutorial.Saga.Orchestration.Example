using MassTransit;
using Payment.API.Consumers;
using Payment.API.PaymentEvents;
using Shared.Settings;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<PaymentStartedEventConsumer>();

    configurator.UsingRabbitMq((context, _confgure) =>
    {
        _confgure.Host(builder.Configuration["RabbitMq"]);

        _confgure.ReceiveEndpoint(RabbitMqSettings.Payment_StartedEventQueue, e =>
    e.ConfigureConsumer<PaymentStartedEventConsumer>(context));
    });
});

var app = builder.Build();

app.Run();
