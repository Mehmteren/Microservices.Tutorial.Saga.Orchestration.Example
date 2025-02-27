using MassTransit;
using Payment.API.PaymentEvents;
using SagaStateMachine.Service.StateInstances;
using Shared.Messages;
using Shared.OrderEvents;
using Shared.PaymentStartedEvent;
using Shared.Settings;
using Shared.StockEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SagaStateMachine.Service.StateMachines
{
    public class OrderStateMachine : MassTransitStateMachine<OrderStateInstance>
    {
        //Eventleri temsil edelim.Buraya gelen eventler.
        public Event<OrderStartedEvent> OrderStartedEvent { get; set; }
        public Event<StockReservedEvent> StockReservedEvent { get; set; }
        public Event<StockNotReservedEvent> StockNotReservedEvent { get; set; }
        public Event<PaymentCompletedEvent> PaymentCompletedEvent { get; set; }
        public Event<PaymentFailedEvent> PaymentFailedEvent { get; set; }

        //başka eventlerde var ama onlar statemachine tarafından gönderilecek eventler.
        //diğer servisleri tetiklemek için oluşturulan eventler.
        //stateleri tanımlayalım...

        public State OrderCreated { get; set; }
        public State StockReserved { get; set; }
        public State StockNotReserved { get; set; }
        public State PaymentCompleted { get; set; }
        public State PaymentFailed { get; set; }


        public OrderStateMachine()
        {
            //state machinede yapılacak çalışmalardaki durum bilgilendismresi
            //burada tanımlanacak.currentstate
            InstanceState(instance => instance.CurrentState);

            Event(() => OrderStartedEvent,  //orderstartedevenrt gelirse tetikleyici old. anlar.
                orderStateInstance => orderStateInstance.CorrelateById<int>(database =>
                database.OrderId, @event => @event.Message.OrderId) //eşit mi?
                //varsa yeni bir corelationıd oluşturmayacağız.
                //yoksa oluşturucaz.
                .SelectId(e => Guid.NewGuid())); //yeni correlationıd.

            Event(() => StockReservedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event =>
                @event.Message.CorrelationId));

            Event(() => StockNotReservedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event =>
                @event.Message.CorrelationId));

            Event(() => PaymentCompletedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event =>
                @event.Message.CorrelationId));

            Event(() => PaymentFailedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event =>
                @event.Message.CorrelationId));


            //corelationıd yi db den alıp ordercreatedevent ile stockapı ye gönderelim.
            Initially(When(OrderStartedEvent)
                .Then(context =>
                {
                    context.Instance.OrderId = context.Data.OrderId;
                    context.Instance.BuyerId = context.Data.BuyerId;
                    context.Instance.TotalPrice = context.Data.TotalPrice;
                    context.Instance.CreateDate = DateTime.UtcNow;
                })
            .TransitionTo(OrderCreated)
                .Send(new Uri($"queue:{RabbitMqSettings.Stock_OrderCreatedEventQueue}"),
                 context => new OrderCreatedEvent(context.Instance.CorrelationId)
                 {
                     OrderItems = context.Data.OrderItems,
                 }));

            During(OrderCreated,
                When(StockReservedEvent) //gelen event bu ise 
                .TransitionTo(StockReserved)  //state i bu duruma çek.
                .Send(new Uri($"queue:{RabbitMqSettings.Payment_StartedEventQueue}"),
                context => new PaymentStartedEvent(context.Instance.CorrelationId)
                {
                    TotalPrice = context.Instance.TotalPrice,
                    OrderItems = context.Data.OrderItems
                }));
            When(StockNotReservedEvent)
            .TransitionTo(StockNotReserved)
            .Send(new Uri($"queue:{RabbitMqSettings.Order_OrderFailedEventQueue}"),
            context => new OrderFailEvent
            {
                OrderId = context.Instance.OrderId,
                Message = context.Data.Message
            });



            During(StockReserved,
                When(PaymentCompletedEvent) //gelen event bu ise 
                .TransitionTo(PaymentCompleted)  //state i bu duruma çek.
                .Send(new Uri($"queue:{RabbitMqSettings.Order_OrderCompletedEventQueue}"),
                context => new OrderCompletedEvent
                {
                    OrderId = context.Instance.OrderId,
                })
               .Finalize(),
                When(PaymentFailedEvent)
            .TransitionTo(PaymentFailed)
               .Send(new Uri($"queue:{RabbitMqSettings.Order_OrderFailedEventQueue}"),
               context => new OrderFailEvent
               {
                   OrderId = context.Instance.OrderId,
                   Message = context.Data.Message
               })
               .Send(new Uri($"queue:{RabbitMqSettings.Stock_RollbackMessageQueue}"),
               context => new StockRollBackMessage
               {
                   OrderItems = context.Data.OrderItems,
               }));
            SetCompletedWhenFinalized();
        }
    }
}