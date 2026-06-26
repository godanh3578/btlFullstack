namespace OrderApi.Services
{
    public interface IOutboxService
    {
        Task EnqueueAsync<T>(string eventName, T payload, CancellationToken cancellationToken = default);
        Task EnqueueOrderCreatedAsync(int orderId, CancellationToken cancellationToken = default);
    }
}
