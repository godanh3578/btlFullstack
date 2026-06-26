using System.Text.Json;
using MassTransit;
using OrderApi.Events;

namespace OrderApi.Services
{
    public sealed class MassTransitEventPublisher
    {
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly IConfiguration _config;

        public MassTransitEventPublisher(ISendEndpointProvider sendEndpointProvider, IConfiguration config)
        {
            _sendEndpointProvider = sendEndpointProvider;
            _config = config;
        }

        public async Task PublishAsync(string eventName, string payload, CancellationToken cancellationToken = default)
        {
            if (eventName == "order.created")
            {
                var message = JsonSerializer.Deserialize<IntegrationEventEnvelope<OrderCreatedEvent>>(payload)
                    ?? throw new InvalidOperationException("Invalid order.created payload.");

                await SendAsync(eventName, message, cancellationToken);
                return;
            }

            var fallback = new RawIntegrationEvent
            {
                EventName = eventName,
                Payload = JsonSerializer.Deserialize<JsonElement>(payload)
            };
            await SendAsync(eventName, fallback, cancellationToken);
        }

        private async Task SendAsync<T>(string eventName, T message, CancellationToken cancellationToken)
            where T : class
        {
            var exchange = _config["RabbitMQ:OrderExchange"] ?? "order.exchange";
            var routingKey = _config[$"RabbitMQ:RoutingKeys:{eventName}"] ?? eventName;
            var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"exchange:{exchange}"));

            await endpoint.Send(message, context =>
            {
                context.SetRoutingKey(routingKey);
            }, cancellationToken);
        }

        private sealed class RawIntegrationEvent
        {
            public string EventName { get; set; } = "";
            public JsonElement Payload { get; set; }
        }
    }
}
