using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderApi.Data;
using RabbitMQ.Client;

namespace OrderApi.Services
{
    public sealed class OrderDbHealthCheck : IHealthCheck
    {
        private readonly OrderDbContext _dbContext;

        public OrderDbHealthCheck(OrderDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
                return HealthCheckResult.Healthy("OrderDB is reachable.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("OrderDB is not reachable.", ex);
            }
        }
    }

    public sealed class RabbitMqHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _config;

        public RabbitMqHealthCheck(IConfiguration config)
        {
            _config = config;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _config["RabbitMQ:Host"] ?? "localhost",
                    UserName = _config["RabbitMQ:Username"] ?? "guest",
                    Password = _config["RabbitMQ:Password"] ?? "guest"
                };

                await using var connection = await factory.CreateConnectionAsync(cancellationToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                return HealthCheckResult.Healthy("RabbitMQ is reachable.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ is not reachable.", ex);
            }
        }
    }
}
