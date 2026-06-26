namespace OrderApi.Events
{
    public sealed class IntegrationEventEnvelope<T>
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string EventType { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public T? Data { get; set; }
    }
}
