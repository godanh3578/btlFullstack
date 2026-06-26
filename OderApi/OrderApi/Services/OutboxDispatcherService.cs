using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Models;

namespace OrderApi.Services
{
    public class OutboxDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxDispatcherService> _logger;

        public OutboxDispatcherService(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxDispatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox dispatcher error");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<MassTransitEventPublisher>();

            const int maxRetryCount = 3;
            var pending = await db.OutboxMessages
                .Where(m => m.Status == OutboxMessageStatus.Pending && m.RetryCount < maxRetryCount)
                .OrderBy(m => m.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);

            foreach (var message in pending)
            {
                try
                {
                    await publisher.PublishAsync(message.EventName, message.Payload, cancellationToken);

                    message.Status = OutboxMessageStatus.Processed;
                    message.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    if (message.RetryCount >= maxRetryCount)
                        message.Status = OutboxMessageStatus.Failed;

                    _logger.LogWarning(ex, "Failed to publish outbox message {Id}", message.OutboxMessageId);
                }
            }

            if (pending.Count > 0)
                await db.SaveChangesAsync(cancellationToken);
        }
    }
}
