# Order & Sales Service — Nhóm 2

**Dịch vụ xử lý đơn bán hàng trong kiến trúc Microservices (Đề tài 01)**

## Vai trò Nhóm 2

| Chức năng | Mô tả |
|-----------|--------|
| OrderDB | Đơn hàng, khách hàng, NCC, thanh toán, công nợ |
| Sales | Checkout, tính tổng, chiết khấu |
| Tích hợp N1 | Consume `stock.updated` → `ProductStockCaches` |
| Tích hợp N3 | Publish `order.created` qua Outbox + RabbitMQ |

## Công nghệ

- **ASP.NET Core 8.0** Web API
- Entity Framework Core 8 + **SQL Server** (`OrderDB`)
- JWT Bearer (roles: `Admin`, `Sales`, `Warehouse`)
- RabbitMQ + Outbox pattern
- Swagger (Development)
- Frontend: **Vue 3 + Vite** (`frontend/`)

> ⚠️ Không dùng thư mục `storefront/` — đã deprecated.

## Yêu cầu môi trường

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **ASP.NET Core 8.0 Runtime** (bắt buộc để `dotnet run`)
- SQL Server 2019+ (hoặc Docker — xem bên dưới)
- RabbitMQ (tùy chọn; API vẫn chạy nếu RabbitMQ tắt)
- Node.js 18+ (frontend)

## Chạy nhanh (local)

### 1. Backend

```bash
cd OrderApi
dotnet run
```

- API: `http://localhost:5002`
- Swagger: `http://localhost:5002/swagger`

### 2. Frontend

```bash
cd frontend
npm install
npm run dev
```

- UI: `http://localhost:5173`
- Vite proxy `/api` → `http://localhost:5002` (xem `frontend/vite.config.js`)

### 3. Docker (SQL Server + RabbitMQ + API)

```bash
docker compose up --build
```

- API: `http://localhost:5002`
- RabbitMQ Management: `http://localhost:15672` (guest/guest)
- SQL Server: `localhost:1433` (sa / `YourStrong@Passw0rd`)

## API chính (PascalCase)

### Auth (demo)

```http
POST /api/auth/login
{ "username": "sales01", "role": "Sales" }
```

Roles: `Sales`, `Admin`, `Warehouse`

### Sales

| Method | Endpoint | Auth |
|--------|----------|------|
| POST | `/api/Sales/Checkout` | Anonymous (khách) / JWT (NV) |
| POST | `/api/Sales/calculate-total` | Sales, Admin |
| POST | `/api/Sales/apply-discount` | Sales, Admin |

### Orders

| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `/api/Orders?customerId=` | Anonymous (đơn của khách) |
| GET | `/api/Orders` | Sales, Admin, Warehouse |
| GET | `/api/Orders/lookup?orderCode=&phone=` | Anonymous |
| GET | `/api/Orders/{id}` | Anonymous |
| PUT | `/api/Orders/{id}/status` | Sales, Admin, Warehouse |

### Customers / Suppliers / Payments / Debts

- `GET/POST/PUT/DELETE /api/Customers` (đăng ký + demo-login public)
- `GET/POST/PUT/DELETE /api/Suppliers`
- `GET/POST /api/Payments`
- `GET/POST /api/Debts`

### Tích hợp

- `GET /api/ProductStockCaches` — cache từ `stock.updated`
- `GET /api/OutboxMessages` — hàng đợi `order.created`

## Sự kiện RabbitMQ

| Event | Hướng | Mô tả |
|-------|-------|--------|
| `stock.updated` | N1 → N2 | Cập nhật `ProductStockCaches` |
| `order.created` | N2 → N3 | Outbox → RabbitMQ sau checkout |

## Demo đề xuất

1. Khách: xem SP → giỏ hàng → đăng ký → checkout (chiết khấu / trả một phần → công nợ)
2. Sales: login footer → quản lý đơn, KH, thanh toán, công nợ
3. Warehouse: login role `Warehouse` → kho xuất hàng → xác nhận xuất
4. Integration: trang **Đồng bộ** — Outbox + ProductStockCaches

## Testing

```bash
dotnet test
```

## Cấu trúc repo

```
OrderApi/          # Backend ASP.NET Core 8
frontend/          # Frontend chính (Vue 3)
OrderApi.Tests/    # Unit tests
storefront/        # DEPRECATED — không dùng
docker-compose.yml # SQL + RabbitMQ + API
```

## Cấu hình

`OrderApi/appsettings.json`:

- `ConnectionStrings:DefaultConnection` — SQL Server
- `Jwt:*` — khóa JWT demo
- `RabbitMQ:Host` — hostname RabbitMQ

## Tác giả

Nhóm 2 — Order & Sales Service
