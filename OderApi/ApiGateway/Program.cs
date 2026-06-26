using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://0.0.0.0:5000;http://0.0.0.0:7000");

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "CHANGE_THIS_SECRET_ON_MACHINE_3_MINIMUM_32_CHARS";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "KhoPro.UserReportService";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "KhoPro.Frontend";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "role"
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayCors", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (!ShouldRateLimit(context))
            return RateLimitPartition.GetNoLimiter("unlimited");

        var key = GetRateLimitKey(context);

        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHealthChecks();
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { status = "Healthy", service = "ApiGateway" });
        return;
    }

    await next();
});

app.UseCors("GatewayCors");
app.UseAuthentication();
app.UseRateLimiter();
app.MapHealthChecks("/health");
await app.UseOcelot();
app.Run();

static bool ShouldRateLimit(HttpContext context)
{
    if (!HttpMethods.IsPost(context.Request.Method))
        return false;

    var path = context.Request.Path.Value ?? "";
    return path.Equals("/api/orders", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/payments", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/sales/checkout", StringComparison.OrdinalIgnoreCase);
}

static string GetRateLimitKey(HttpContext context)
{
    var user = context.User;
    var claimValue = user.FindFirst("sub")?.Value
        ?? user.FindFirst("nameid")?.Value
        ?? user.FindFirst("name")?.Value
        ?? user.Identity?.Name;

    if (!string.IsNullOrWhiteSpace(claimValue))
        return $"user:{claimValue}";

    var authHeader = context.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrWhiteSpace(authHeader))
        return $"token:{authHeader.GetHashCode()}";

    return $"ip:{context.Connection.RemoteIpAddress}";
}
