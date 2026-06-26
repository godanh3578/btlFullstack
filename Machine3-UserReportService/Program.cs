using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Machine4Frontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<OrderStore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Machine4Frontend");

var store = app.Services.GetRequiredService<UserStore>();
store.SeedAdmin(
    builder.Configuration["SeedAdmin:Email"] ?? "admin.user@khopro.local",
    builder.Configuration["SeedAdmin:Password"] ?? "AdminUser@123",
    builder.Configuration["SeedAdmin:Name"] ?? "Admin User",
    builder.Configuration["SeedAdmin:StoreName"] ?? "KhoPro");
store.SeedUser("warehouse.user@khopro.local", "Warehouse@123", "Warehouse User", "KhoPro", "Warehouse");
store.SeedUser("sales.user@khopro.local", "Sales@123", "Sales User", "KhoPro", "Sales");

app.MapGet("/", () => Results.Ok(new
{
    service = "KhoPro Machine 3 - User & Report Service",
    status = "running",
    api = "/api"
}));

app.MapPost("/api/auth/register", (RegisterRequest request, UserStore users) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Email và mật khẩu là bắt buộc." });
    }

    var user = users.CreateUser(request);
    return user is null
        ? Results.Conflict(new { message = "Email này đã được đăng ký." })
        : Results.Ok(new { user = UserResponse.From(user) });
});

app.MapPost("/api/auth/login", (LoginRequest request, UserStore users, JwtTokenService tokens) =>
{
    var user = users.ValidatePassword(request.Email, request.Password);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        accessToken = tokens.CreateToken(user),
        tokenType = "Bearer",
        user = UserResponse.From(user)
    });
});

app.MapPost("/api/auth/logout", (HttpRequest request, UserStore users, JwtTokenService tokens) =>
{
    var currentUser = GetCurrentUser(request, users, tokens);
    if (currentUser is null)
    {
        return Results.Unauthorized();
    }

    users.SetOnlineStatus(currentUser.Id, false);
    return Results.Ok(new { message = "Logged out." });
});

app.MapPost("/api/auth/offline", async (HttpRequest request, UserStore users, JwtTokenService tokens) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { message = "Dữ liệu phiên không hợp lệ." });
    }

    var form = await request.ReadFormAsync();
    var userId = tokens.ValidateToken(form["accessToken"].ToString());
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    users.SetOnlineStatus(userId, false);
    return Results.Ok(new { message = "Offline." });
});

app.MapPost("/api/auth/reset-password", (ResetPasswordRequest request, UserStore users) =>
{
    var updated = users.ResetPassword(request.Email, request.Password);
    return updated
        ? Results.Ok(new { message = "Đổi mật khẩu thành công." })
        : Results.NotFound(new { message = "Không tìm thấy email đã đăng ký." });
});

app.MapGet("/api/auth/check-active", (string email, UserStore users) =>
{
    var all = users.GetAll();
    var user = all.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
    if (user is null) return Results.Ok(new { isActive = true }); // chưa có tài khoản → không chặn
    return Results.Ok(new { isActive = user.IsActive });
});

app.MapGet("/api/users/me", (HttpRequest request, UserStore users, JwtTokenService tokens) =>
{
    var currentUser = GetCurrentUser(request, users, tokens);
    return currentUser is null
        ? Results.Unauthorized()
        : Results.Ok(new { user = UserResponse.From(currentUser) });
});

app.MapPut("/api/users/me", (HttpRequest request, UpdateProfileRequest profile, UserStore users, JwtTokenService tokens) =>
{
    var currentUser = GetCurrentUser(request, users, tokens);
    if (currentUser is null)
    {
        return Results.Unauthorized();
    }

    var updatedUser = users.UpdateProfile(currentUser.Id, profile);
    return Results.Ok(new { user = UserResponse.From(updatedUser!) });
});

app.MapGet("/api/users", (HttpRequest request, UserStore users, JwtTokenService tokens) =>
{
    var currentUser = GetCurrentUser(request, users, tokens);
    if (currentUser is null)
    {
        return Results.Unauthorized();
    }

    if (currentUser.Role != "admin-user")
    {
        return Results.Forbid();
    }

    return Results.Ok(users.GetAll().Select(UserResponse.From));
});

app.MapGet("/api/users/{id}", (string id, HttpRequest request, UserStore users, JwtTokenService tokens) =>
{
    var currentUser = GetCurrentUser(request, users, tokens);
    if (currentUser is null)
    {
        return Results.Unauthorized();
    }

    if (currentUser.Role != "admin-user")
    {
        return Results.Forbid();
    }

    var user = users.GetAnyById(id);
    return user is null
        ? Results.NotFound(new { message = "Khong tim thay tai khoan." })
        : Results.Ok(new { user = UserResponse.From(user) });
});

app.MapPut("/api/users/{id}", (string id, HttpRequest request, UpdateUserRequest updateRequest, UserStore users, JwtTokenService tokens) =>
{
    var currentUser = GetCurrentUser(request, users, tokens);
    if (currentUser is null)
    {
        return Results.Unauthorized();
    }

    if (currentUser.Role != "admin-user")
    {
        return Results.Forbid();
    }

    if (id == currentUser.Id && updateRequest.Role is not null && updateRequest.Role != currentUser.Role)
    {
        return Results.BadRequest(new { message = "Khong the doi vai tro cua chinh tai khoan dang dang nhap." });
    }

    if (id == currentUser.Id && updateRequest.IsActive == false)
    {
        return Results.BadRequest(new { message = "Khong the khoa chinh tai khoan dang dang nhap." });
    }

    var updatedUser = users.UpdateUser(id, updateRequest);
    return updatedUser is null
        ? Results.NotFound(new { message = "Khong tim thay tai khoan hoac vai tro khong hop le." })
        : Results.Ok(new { user = UserResponse.From(updatedUser) });
});

app.MapDelete("/api/users/{id}", (string id, HttpRequest request, UserStore users, JwtTokenService tokens) =>
{
    var currentUser = GetCurrentUser(request, users, tokens);
    if (currentUser is null)
    {
        return Results.Unauthorized();
    }

    if (currentUser.Role != "admin-user")
    {
        return Results.Forbid();
    }

    if (id == currentUser.Id)
    {
        return Results.BadRequest(new { message = "Khong the xoa chinh tai khoan dang dang nhap." });
    }

    return users.DeleteUser(id)
        ? Results.Ok(new { message = "Da xoa tai khoan." })
        : Results.NotFound(new { message = "Khong tim thay tai khoan." });
});

app.MapPut("/api/users/{id}/role", (string id, HttpRequest request, UpdateUserRoleRequest roleRequest, UserStore users, JwtTokenService tokens) =>
{
    var currentUser = GetCurrentUser(request, users, tokens);
    if (currentUser is null)
    {
        return Results.Unauthorized();
    }

    if (currentUser.Role != "admin-user")
    {
        return Results.Forbid();
    }

    if (id == currentUser.Id)
    {
        return Results.BadRequest(new { message = "Khong the doi vai tro cua chinh tai khoan dang dang nhap." });
    }

    var updatedUser = users.UpdateRole(id, roleRequest.Role);
    return updatedUser is null
        ? Results.NotFound(new { message = "Khong tim thay tai khoan." })
        : Results.Ok(new { user = UserResponse.From(updatedUser) });
});

app.MapGet("/api/reports/summary", (HttpRequest request, UserStore users, JwtTokenService tokens) =>
{
    var currentUser = GetCurrentUser(request, users, tokens);
    if (currentUser is null)
    {
        return Results.Unauthorized();
    }

    var allUsers = users.GetAll().ToList();
    return Results.Ok(new
    {
        totalUsers = allUsers.Count,
        activeUsers = allUsers.Count(user => user.IsOnline),
        adminUsers = allUsers.Count(user => user.Role == "admin-user"),
        normalUsers = allUsers.Count(user => user.Role == "user")
    });
});

app.MapGet("/api/Orders", (string? customerId, OrderStore orders) =>
    Results.Ok(orders.GetOrders(customerId)));

app.MapGet("/api/Orders/lookup", (string orderCode, string? phone, OrderStore orders) =>
{
    var order = orders.Lookup(orderCode, phone);
    return order is null
        ? Results.NotFound(new { message = "Không tìm thấy đơn hàng phù hợp." })
        : Results.Ok(order);
});

app.MapGet("/api/Orders/{id}", (string id, OrderStore orders) =>
{
    var order = orders.GetOrder(id);
    return order is null
        ? Results.NotFound(new { message = "Không tìm thấy đơn hàng." })
        : Results.Ok(order);
});

app.MapPost("/api/Orders", (CreateOrderRequest request, OrderStore orders) =>
{
    if (string.IsNullOrWhiteSpace(request.CustomerId)
        || string.IsNullOrWhiteSpace(request.CustomerName)
        || request.Items is null
        || request.Items.Count == 0)
    {
        return Results.BadRequest(new { message = "Thông tin khách hàng và sản phẩm là bắt buộc." });
    }

    return Results.Ok(orders.CreateOrder(request));
});

app.MapPut("/api/Orders/{id}/status", (string id, UpdateOrderStatusRequest request, OrderStore orders) =>
{
    var order = orders.UpdateStatus(id, request.Status);
    return order is null
        ? Results.NotFound(new { message = "Không tìm thấy đơn hàng." })
        : Results.Ok(order);
});

app.MapDelete("/api/Orders/{id}", (string id, OrderStore orders) =>
    orders.DeleteOrder(id)
        ? Results.Ok(new { message = "Đã xóa đơn hàng." })
        : Results.NotFound(new { message = "Không tìm thấy đơn hàng." }));

app.MapGet("/api/Customers", (OrderStore orders) => Results.Ok(orders.GetCustomers()));
app.MapGet("/api/Suppliers", () => Results.Ok(Array.Empty<object>()));
app.MapGet("/api/Payments", () => Results.Ok(Array.Empty<object>()));
app.MapGet("/api/Debts", (OrderStore orders) => Results.Ok(orders.GetDebts()));
app.MapGet("/api/OutboxMessages", (OrderStore orders) => Results.Ok(orders.GetEvents()));

app.Run();

static UserAccount? GetCurrentUser(HttpRequest request, UserStore users, JwtTokenService tokens)
{
    var authHeader = request.Headers.Authorization.ToString();
    if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    var userId = tokens.ValidateToken(authHeader["Bearer ".Length..].Trim());
    return userId is null ? null : users.TouchOnline(userId);
}

public sealed class UserStore
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public UserStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "users.json");
    }

    public void SeedAdmin(string email, string password, string name, string storeName)
    {
        SeedUser(email, password, name, storeName, "admin-user");
    }

    public void SeedUser(string email, string password, string name, string storeName, string role)
    {
        lock (_lock)
        {
            if (!IsValidRole(role))
            {
                return;
            }

            var users = ReadUsers();
            if (users.Any(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            users.Add(new UserAccount
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = NormalizeEmail(email),
                Name = name,
                StoreName = storeName,
                Province = "",
                Phone = "",
                Birthday = "",
                Gender = "",
                Role = role,
                PasswordHash = PasswordHasher.Hash(password),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });

            WriteUsers(users);
        }
    }

    public UserAccount? CreateUser(RegisterRequest request)
    {
        lock (_lock)
        {
            var email = NormalizeEmail(request.Email);
            var users = ReadUsers();
            if (users.Any(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var user = new UserAccount
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = email,
                Name = request.Name?.Trim() ?? "",
                StoreName = request.StoreName?.Trim() ?? "",
                Province = request.Province?.Trim() ?? "",
                Phone = request.Phone?.Trim() ?? "",
                Birthday = "",
                Gender = "",
                Role = "user",
                PasswordHash = PasswordHasher.Hash(request.Password),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            users.Add(user);
            WriteUsers(users);
            return user;
        }
    }

    public UserAccount? ValidatePassword(string email, string password)
    {
        lock (_lock)
        {
            var users = ReadUsers();
            var user = users.FirstOrDefault(item =>
                item.Email.Equals(NormalizeEmail(email), StringComparison.OrdinalIgnoreCase) && item.IsActive);

            if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
            {
                return null;
            }

            user.IsOnline = true;
            user.LastLoginAt = DateTimeOffset.UtcNow;
            user.LastSeenAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            WriteUsers(users);
            return user;
        }
    }

    public UserAccount? TouchOnline(string id)
    {
        lock (_lock)
        {
            var users = ReadUsers();
            var user = users.FirstOrDefault(item => item.Id == id && item.IsActive);
            if (user is null)
            {
                return null;
            }

            user.IsOnline = true;
            user.LastSeenAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            WriteUsers(users);
            return user;
        }
    }

    public bool SetOnlineStatus(string id, bool isOnline)
    {
        lock (_lock)
        {
            var users = ReadUsers();
            var user = users.FirstOrDefault(item => item.Id == id);
            if (user is null)
            {
                return false;
            }

            user.IsOnline = isOnline;
            if (isOnline)
            {
                user.LastSeenAt = DateTimeOffset.UtcNow;
            }
            user.UpdatedAt = DateTimeOffset.UtcNow;
            WriteUsers(users);
            return true;
        }
    }

    public bool ResetPassword(string email, string password)
    {
        lock (_lock)
        {
            var users = ReadUsers();
            var user = users.FirstOrDefault(item => item.Email.Equals(NormalizeEmail(email), StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                return false;
            }

            user.PasswordHash = PasswordHasher.Hash(password);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            WriteUsers(users);
            return true;
        }
    }

    public UserAccount? GetById(string id)
    {
        lock (_lock)
        {
            return ReadUsers().FirstOrDefault(user => user.Id == id && user.IsActive);
        }
    }

    public UserAccount? GetAnyById(string id)
    {
        lock (_lock)
        {
            return ReadUsers().FirstOrDefault(user => user.Id == id);
        }
    }

    public IReadOnlyList<UserAccount> GetAll()
    {
        lock (_lock)
        {
            var users = ReadUsers();
            MarkStaleUsersOffline(users);
            WriteUsers(users);
            return users;
        }
    }

    public UserAccount? UpdateProfile(string id, UpdateProfileRequest profile)
    {
        lock (_lock)
        {
            var users = ReadUsers();
            var user = users.FirstOrDefault(item => item.Id == id);
            if (user is null)
            {
                return null;
            }

            user.Name = profile.Name?.Trim() ?? user.Name;
            user.StoreName = profile.StoreName?.Trim() ?? user.StoreName;
            user.Province = profile.Province?.Trim() ?? user.Province;
            user.Phone = profile.Phone?.Trim() ?? user.Phone;
            user.Birthday = profile.Birthday?.Trim() ?? user.Birthday;
            user.Gender = profile.Gender?.Trim() ?? user.Gender;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            WriteUsers(users);
            return user;
        }
    }

    public UserAccount? UpdateUser(string id, UpdateUserRequest updateRequest)
    {
        lock (_lock)
        {
            var users = ReadUsers();
            var user = users.FirstOrDefault(item => item.Id == id);
            if (user is null)
            {
                return null;
            }

            if (updateRequest.Role is not null)
            {
                var normalizedRole = updateRequest.Role.Trim();
                if (!IsValidRole(normalizedRole))
                {
                    return null;
                }

                user.Role = normalizedRole;
            }

            user.Name = updateRequest.Name?.Trim() ?? user.Name;
            user.StoreName = updateRequest.StoreName?.Trim() ?? user.StoreName;
            user.Province = updateRequest.Province?.Trim() ?? user.Province;
            user.Phone = updateRequest.Phone?.Trim() ?? user.Phone;
            user.Birthday = updateRequest.Birthday?.Trim() ?? user.Birthday;
            user.Gender = updateRequest.Gender?.Trim() ?? user.Gender;
            user.IsActive = updateRequest.IsActive ?? user.IsActive;
            if (!user.IsActive)
            {
                user.IsOnline = false;
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            WriteUsers(users);
            return user;
        }
    }

    public UserAccount? UpdateRole(string id, string role)
    {
        lock (_lock)
        {
            var normalizedRole = role.Trim();
            if (!IsValidRole(normalizedRole))
            {
                return null;
            }

            var users = ReadUsers();
            var user = users.FirstOrDefault(item => item.Id == id);
            if (user is null)
            {
                return null;
            }

            user.Role = normalizedRole;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            WriteUsers(users);
            return user;
        }
    }

    public bool DeleteUser(string id)
    {
        lock (_lock)
        {
            var users = ReadUsers();
            var removed = users.RemoveAll(item => item.Id == id) > 0;
            if (!removed) return false;

            WriteUsers(users);
            return true;
        }
    }

    private List<UserAccount> ReadUsers()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<UserAccount>>(json, _jsonOptions) ?? [];
    }

    private void WriteUsers(List<UserAccount> users)
    {
        var json = JsonSerializer.Serialize(users, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private static void MarkStaleUsersOffline(List<UserAccount> users)
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-25);
        foreach (var user in users)
        {
            var lastSeen = user.LastSeenAt ?? user.LastLoginAt ?? user.UpdatedAt ?? user.CreatedAt;
            if (user.IsOnline && (!user.IsActive || lastSeen < cutoff))
            {
                user.IsOnline = false;
                user.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsValidRole(string role)
    {
        return role is "admin-user" or "user" or "Admin" or "Sales" or "Warehouse";
    }
}

public sealed class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(UserAccount user)
    {
        var now = DateTimeOffset.UtcNow;
        var expireMinutes = int.TryParse(_configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 480;
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };
        var payload = new Dictionary<string, object>
        {
            ["sub"] = user.Id,
            ["email"] = user.Email,
            ["role"] = user.Role,
            ["iss"] = _configuration["Jwt:Issuer"] ?? "KhoPro.UserReportService",
            ["aud"] = _configuration["Jwt:Audience"] ?? "KhoPro.Frontend",
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(expireMinutes).ToUnixTimeSeconds()
        };

        var encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var signature = Sign($"{encodedHeader}.{encodedPayload}");

        return $"{encodedHeader}.{encodedPayload}.{signature}";
    }

    public string? ValidateToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        var expectedSignature = Sign($"{parts[0]}.{parts[1]}");
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(parts[2])))
        {
            return null;
        }

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var payload = JsonDocument.Parse(payloadJson);
        var root = payload.RootElement;

        if (!root.TryGetProperty("exp", out var expElement) ||
            DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64()) <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return root.TryGetProperty("sub", out var subElement) ? subElement.GetString() : null;
    }

    private string Sign(string data)
    {
        var secret = _configuration["Jwt:Secret"] ?? "CHANGE_THIS_SECRET_ON_MACHINE_3_MINIMUM_32_CHARS";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        output = output.PadRight(output.Length + (4 - output.Length % 4) % 4, '=');
        return Convert.FromBase64String(output);
    }
}

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? Name,
    string? StoreName,
    string? Phone,
    string? Province);

public sealed record LoginRequest(string Email, string Password);

public sealed record ResetPasswordRequest(string Email, string Password);

public sealed record UpdateProfileRequest(
    string? Name,
    string? StoreName,
    string? Province,
    string? Phone,
    string? Birthday,
    string? Gender);

public sealed record UpdateUserRoleRequest(string Role);

public sealed record UpdateUserRequest(
    string? Name,
    string? StoreName,
    string? Province,
    string? Phone,
    string? Birthday,
    string? Gender,
    string? Role,
    bool? IsActive);

public sealed class OrderStore
{
    private readonly object _lock = new();
    private readonly string _ordersPath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public OrderStore(IHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);
        _ordersPath = Path.Combine(dataDirectory, "orders.json");
        if (!File.Exists(_ordersPath))
        {
            WriteOrders([]);
        }
    }

    public IReadOnlyList<SalesOrder> GetOrders(string? customerId)
    {
        lock (_lock)
        {
            var orders = ReadOrders();
            if (!string.IsNullOrWhiteSpace(customerId))
            {
                orders = orders
                    .Where(order => order.CustomerId.Equals(customerId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return orders.OrderByDescending(order => order.OrderDate).ToList();
        }
    }

    public SalesOrder? GetOrder(string id)
    {
        lock (_lock)
        {
            return ReadOrders().FirstOrDefault(order =>
                order.OrderId.Equals(id, StringComparison.OrdinalIgnoreCase)
                || order.OrderCode.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
    }

    public SalesOrder? Lookup(string orderCode, string? phone)
    {
        lock (_lock)
        {
            return ReadOrders().FirstOrDefault(order =>
                order.OrderCode.Equals(orderCode.Trim(), StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(phone)
                    || NormalizePhone(order.Phone) == NormalizePhone(phone)));
        }
    }

    public SalesOrder CreateOrder(CreateOrderRequest request)
    {
        lock (_lock)
        {
            var orders = ReadOrders();
            var subtotal = request.Items.Sum(item => item.UnitPrice * item.Quantity);
            var discount = Math.Clamp(request.DiscountAmount, 0, subtotal);
            var finalAmount = Math.Max(0, subtotal - discount);
            var paidAmount = Math.Clamp(request.PaidAmount, 0, finalAmount);
            var now = DateTimeOffset.UtcNow;

            var order = new SalesOrder
            {
                OrderId = Guid.NewGuid().ToString("N"),
                OrderCode = $"WEB{now.ToUnixTimeMilliseconds().ToString()[^9..]}",
                CustomerId = request.CustomerId.Trim(),
                CustomerName = request.CustomerName.Trim(),
                Email = request.Email?.Trim() ?? "",
                Phone = request.Phone?.Trim() ?? "",
                Address = request.Address?.Trim() ?? "",
                Items = request.Items.Select(item => new SalesOrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName?.Trim() ?? "Sản phẩm",
                    CategoryName = item.CategoryName?.Trim() ?? "",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = item.UnitPrice * item.Quantity
                }).ToList(),
                TotalAmount = subtotal,
                DiscountAmount = discount,
                FinalAmount = finalAmount,
                PaidAmount = paidAmount,
                DebtAmount = Math.Max(0, finalAmount - paidAmount),
                PaymentMethod = request.PaymentMethod?.Trim() ?? "Cash",
                PaymentStatus = paidAmount >= finalAmount ? "Paid" : paidAmount > 0 ? "Partial" : "Unpaid",
                OrderStatus = "Pending",
                Source = "Web 5173",
                OrderDate = now,
                UpdatedAt = now
            };

            orders.Add(order);
            WriteOrders(orders);
            return order;
        }
    }

    public SalesOrder? UpdateStatus(string id, string? status)
    {
        lock (_lock)
        {
            var orders = ReadOrders();
            var order = orders.FirstOrDefault(item =>
                item.OrderId.Equals(id, StringComparison.OrdinalIgnoreCase)
                || item.OrderCode.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (order is null) return null;

            order.OrderStatus = NormalizeStatus(status);
            order.UpdatedAt = DateTimeOffset.UtcNow;
            WriteOrders(orders);
            return order;
        }
    }

    public bool DeleteOrder(string id)
    {
        lock (_lock)
        {
            var orders = ReadOrders();
            var removed = orders.RemoveAll(order =>
                order.OrderId.Equals(id, StringComparison.OrdinalIgnoreCase)
                || order.OrderCode.Equals(id, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed) WriteOrders(orders);
            return removed;
        }
    }

    public IReadOnlyList<object> GetCustomers()
    {
        lock (_lock)
        {
            return ReadOrders()
                .GroupBy(order => order.CustomerId)
                .Select(group =>
                {
                    var latest = group.OrderByDescending(order => order.OrderDate).First();
                    return (object)new
                    {
                        customerId = latest.CustomerId,
                        name = latest.CustomerName,
                        fullName = latest.CustomerName,
                        latest.Email,
                        latest.Phone,
                        latest.Address,
                        orderCount = group.Count(),
                        totalSpent = group.Sum(order => order.FinalAmount)
                    };
                })
                .ToList();
        }
    }

    public IReadOnlyList<object> GetDebts()
    {
        lock (_lock)
        {
            return ReadOrders()
                .Where(order => order.DebtAmount > 0)
                .Select(order => (object)new
                {
                    debtId = order.OrderId,
                    orderId = order.OrderId,
                    order.OrderCode,
                    order.CustomerId,
                    order.CustomerName,
                    debtAmount = order.DebtAmount,
                    paidAmount = order.PaidAmount,
                    remainingAmount = order.DebtAmount,
                    debtStatus = "Unpaid"
                })
                .ToList();
        }
    }

    public IReadOnlyList<object> GetEvents()
    {
        lock (_lock)
        {
            return ReadOrders()
                .OrderByDescending(order => order.OrderDate)
                .Select(order => (object)new
                {
                    id = order.OrderId,
                    eventName = "order.created",
                    aggregateId = order.OrderId,
                    createdAt = order.OrderDate,
                    processed = true
                })
                .ToList();
        }
    }

    private List<SalesOrder> ReadOrders()
    {
        var json = File.ReadAllText(_ordersPath);
        return JsonSerializer.Deserialize<List<SalesOrder>>(json, _jsonOptions) ?? [];
    }

    private void WriteOrders(List<SalesOrder> orders)
    {
        File.WriteAllText(_ordersPath, JsonSerializer.Serialize(orders, _jsonOptions));
    }

    private static string NormalizePhone(string? phone) =>
        new((phone ?? "").Where(char.IsDigit).ToArray());

    private static string NormalizeStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "confirmed" => "Confirmed",
            "processing" => "Processing",
            "completed" => "Completed",
            "cancelled" => "Cancelled",
            _ => "Pending"
        };
    }
}

public sealed record CreateOrderRequest(
    string CustomerId,
    string CustomerName,
    string? Email,
    string? Phone,
    string? Address,
    decimal DiscountAmount,
    string? PaymentMethod,
    decimal PaidAmount,
    List<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(
    string ProductId,
    string? ProductName,
    string? CategoryName,
    decimal Quantity,
    decimal UnitPrice);

public sealed record UpdateOrderStatusRequest(string? Status);

public sealed class SalesOrder
{
    public string OrderId { get; set; } = "";
    public string OrderCode { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public List<SalesOrderItem> Items { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DebtAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string PaymentStatus { get; set; } = "Unpaid";
    public string OrderStatus { get; set; } = "Pending";
    public string Source { get; set; } = "Web 5173";
    public DateTimeOffset OrderDate { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class SalesOrderItem
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public sealed class UserAccount
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string Name { get; set; } = "";
    public string StoreName { get; set; } = "";
    public string Province { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Birthday { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Role { get; set; } = "user";
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOnline { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
}

public sealed record UserResponse(
    string Id,
    string Email,
    string Account,
    string Name,
    string StoreName,
    string Province,
    string Phone,
    string Birthday,
    string Gender,
    string Role,
    bool IsActive,
    bool IsOnline,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt)
{
    public static UserResponse From(UserAccount user) => new(
        user.Id,
        user.Email,
        user.Email,
        user.Name,
        user.StoreName,
        user.Province,
        user.Phone,
        user.Birthday,
        user.Gender,
        user.Role,
        user.IsActive,
        user.IsOnline,
        user.CreatedAt,
        user.LastLoginAt);
}
