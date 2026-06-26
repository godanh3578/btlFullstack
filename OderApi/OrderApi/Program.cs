using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OrderApi.Data;
using OrderApi.Models;
using OrderApi.Services;
using Polly;
using Polly.Extensions.Http;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var localDbPath = Path.Combine(builder.Environment.ContentRootPath, ".localdb");
Directory.CreateDirectory(localDbPath);
var dataProtectionPath = Path.Combine(localDbPath, "keys");
Directory.CreateDirectory(dataProtectionPath);
AppDomain.CurrentDomain.SetData("DataDirectory", localDbPath);

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?.Replace("|DataDirectory|", localDbPath);

var useSqlServer = false;
if (!string.IsNullOrWhiteSpace(defaultConnection))
{
    try
    {
        using var probe = new SqlConnection(defaultConnection);
        probe.Open();
        useSqlServer = true;
    }
    catch
    {
        useSqlServer = false;
    }
}

builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(options => { });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("OrderApi");

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    if (useSqlServer && !string.IsNullOrWhiteSpace(defaultConnection))
        options.UseSqlServer(defaultConnection);
    else
        options.UseInMemoryDatabase("OrderApiWorkspaceDb");
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? "OrderApiSuperSecretKey123!@#ChangeMe2026";
var jwtIss = builder.Configuration["Jwt:Issuer"] ?? "OrderApi";
var jwtAud = builder.Configuration["Jwt:Audience"] ?? "OrderApiUsers";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIss,
            ValidAudience = jwtAud,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is ClaimsIdentity identity
                    && context.Principal.IsInRole("admin-user")
                    && !context.Principal.IsInRole("Admin"))
                {
                    identity.AddClaim(new Claim(identity.RoleClaimType, "Admin"));
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWalletTopUpService, WalletTopUpService>();
builder.Services.AddScoped<IDebtService, DebtService>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
builder.Services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();

builder.Services.AddHttpClient<IProductCatalogClient, ProductCatalogClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var gatewayBaseUrl = config["ProductIntegration:GatewayBaseUrl"];
    if (!string.IsNullOrWhiteSpace(gatewayBaseUrl))
        client.BaseAddress = new Uri(gatewayBaseUrl);

    client.Timeout = TimeSpan.FromSeconds(config.GetValue("ProductIntegration:TimeoutSeconds", 3));
})
.AddPolicyHandler((serviceProvider, _) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var retryCount = config.GetValue("ProductIntegration:RetryCount", 3);
    var baseDelayMs = config.GetValue("ProductIntegration:RetryBaseDelayMs", 200);

    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(response => response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount,
            attempt => TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1)));
});

builder.Services.AddHttpClient("UserReport", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = config["UserReportIntegration:BaseUrl"] ?? "http://localhost:8083";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockUpdatedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = context.GetRequiredService<IConfiguration>();
        var host = rabbitConfig["RabbitMQ:Host"] ?? "localhost";
        var username = rabbitConfig["RabbitMQ:Username"] ?? "guest";
        var password = rabbitConfig["RabbitMQ:Password"] ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        cfg.ReceiveEndpoint("stock.updated", endpoint =>
        {
            endpoint.ConfigureConsumer<StockUpdatedConsumer>(context);
        });
    });
});

builder.Services.AddScoped<MassTransitEventPublisher>();
builder.Services.AddHostedService<OutboxDispatcherService>();
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks()
    .AddCheck<OrderDbHealthCheck>("order-db", failureStatus: HealthStatus.Unhealthy)
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", failureStatus: HealthStatus.Degraded);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            if (builder.Environment.IsDevelopment())
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            else
                policy.SetIsOriginAllowed(_ => false).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

    if (useSqlServer)
    {
        try
        {
            db.Database.Migrate();
        }
        catch (Exception migrationEx)
        {
            Console.WriteLine($"Migration warning: {migrationEx.Message}");
        }

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[ProductStockCaches]', N'U') IS NOT NULL
               AND COL_LENGTH(N'ProductStockCaches', N'CategoryName') IS NULL
            BEGIN
                ALTER TABLE [ProductStockCaches]
                ADD [CategoryName] nvarchar(100) NOT NULL
                    CONSTRAINT [DF_ProductStockCaches_CategoryName] DEFAULT N''
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[Customers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'Customers', N'AvatarUrl') IS NULL
            BEGIN
                ALTER TABLE [Customers]
                ADD [AvatarUrl] nvarchar(500) NULL
            END

            IF OBJECT_ID(N'[Customers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'Customers', N'MembershipTier') IS NULL
            BEGIN
                ALTER TABLE [Customers]
                ADD [MembershipTier] nvarchar(30) NOT NULL
                    CONSTRAINT [DF_Customers_MembershipTier] DEFAULT N'Thường'
            END

            IF OBJECT_ID(N'[Customers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'Customers', N'WalletBalance') IS NULL
            BEGIN
                ALTER TABLE [Customers]
                ADD [WalletBalance] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_Customers_WalletBalance] DEFAULT 0
            END

            IF OBJECT_ID(N'[WalletTopUpRequests]', N'U') IS NULL
            BEGIN
                CREATE TABLE [WalletTopUpRequests] (
                    [WalletTopUpRequestId] int NOT NULL IDENTITY,
                    [RequestCode] nvarchar(50) NOT NULL,
                    [CustomerId] int NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [PaymentMethod] nvarchar(50) NOT NULL CONSTRAINT [DF_WalletTopUpRequests_PaymentMethod] DEFAULT N'BankTransfer',
                    [Status] int NOT NULL,
                    [Note] nvarchar(500) NOT NULL,
                    [RequestedAt] datetime2 NOT NULL,
                    [ReviewedAt] datetime2 NULL,
                    [ReviewedBy] nvarchar(100) NOT NULL,
                    CONSTRAINT [PK_WalletTopUpRequests] PRIMARY KEY ([WalletTopUpRequestId]),
                    CONSTRAINT [FK_WalletTopUpRequests_Customers_CustomerId]
                        FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION
                );

                CREATE INDEX [IX_WalletTopUpRequests_CustomerId]
                    ON [WalletTopUpRequests] ([CustomerId]);

                CREATE UNIQUE INDEX [IX_WalletTopUpRequests_RequestCode]
                    ON [WalletTopUpRequests] ([RequestCode]);
            END

            IF OBJECT_ID(N'[WalletTopUpRequests]', N'U') IS NOT NULL
               AND COL_LENGTH(N'WalletTopUpRequests', N'PaymentMethod') IS NULL
            BEGIN
                ALTER TABLE [WalletTopUpRequests]
                ADD [PaymentMethod] nvarchar(50) NOT NULL
                    CONSTRAINT [DF_WalletTopUpRequests_PaymentMethod] DEFAULT N'BankTransfer'
            END

            IF OBJECT_ID(N'[Suppliers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'Suppliers', N'TaxCode') IS NULL
            BEGIN
                ALTER TABLE [Suppliers]
                ADD [TaxCode] nvarchar(20) NOT NULL
                    CONSTRAINT [DF_Suppliers_TaxCode] DEFAULT N''
            END

            IF OBJECT_ID(N'[Suppliers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'Suppliers', N'Note') IS NULL
            BEGIN
                ALTER TABLE [Suppliers]
                ADD [Note] nvarchar(500) NOT NULL
                    CONSTRAINT [DF_Suppliers_Note] DEFAULT N''
            END

            IF OBJECT_ID(N'[OutboxMessages]', N'U') IS NOT NULL
               AND COL_LENGTH(N'OutboxMessages', N'ProcessedAt') IS NULL
            BEGIN
                ALTER TABLE [OutboxMessages]
                ADD [ProcessedAt] datetime2 NULL
            END
            """);
    }
    else
    {
        Console.WriteLine("OrderApi is using in-memory fallback storage.");
    }

    await DbSeeder.SeedAsync(db);
    await RestoreCancelledOrderStockAsync(db);
}
catch (Exception ex)
{
    Console.WriteLine($"Migration failed: {ex.Message}");
    Console.WriteLine("Database might not be available. API will run in demo mode.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();

static async Task RestoreCancelledOrderStockAsync(OrderDbContext db)
{
    var cancelledOrders = await db.Orders
        .IgnoreQueryFilters()
        .Include(o => o.Items)
        .Where(o => o.OrderStatus == OrderStatus.Cancelled && o.StockRestoredAt == null)
        .ToListAsync();

    if (cancelledOrders.Count == 0)
        return;

    foreach (var order in cancelledOrders)
    {
        foreach (var item in order.Items)
        {
            var stock = await db.ProductStockCaches
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);

            if (stock == null)
            {
                stock = new ProductStockCache
                {
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    SellingPrice = item.UnitPrice
                };
                db.ProductStockCaches.Add(stock);
            }

            stock.QuantityAvailable += item.Quantity;
            stock.StockStatus = stock.QuantityAvailable <= 0
                ? StockStatus.OutOfStock
                : stock.QuantityAvailable <= 5
                    ? StockStatus.LowStock
                    : StockStatus.InStock;
            stock.IsDeleted = false;
            stock.LastUpdatedAt = DateTime.UtcNow;
        }

        order.StockRestoredAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();
    Console.WriteLine($"Restored stock for {cancelledOrders.Count} cancelled order(s).");
}
