# Machine1 Product & Inventory Service

Minimal API backend for KhoPro product and inventory management.

## Run locally

```cmd
dotnet run --urls http://localhost:5001
```

## Main endpoints

- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products`
- `PUT /api/products/{id}`
- `DELETE /api/products/{id}`
- `POST /api/products/{id}/stock/adjust`
- `GET /api/inventory/summary`
- `GET /api/inventory/movements`
- `POST /api/inventory/movements`
- `GET /api/suppliers`
- `POST /api/suppliers`
- `PUT /api/suppliers/{id}`
- `DELETE /api/suppliers/{id}`

All business endpoints accept the Machine3 JWT bearer token.
