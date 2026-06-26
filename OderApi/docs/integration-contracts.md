# Integration Contracts - Topic 01

This file records what Group 2 expects from the other services when the three
groups run together through the API Gateway.

## Gateway

Default gateway URL:

```text
http://localhost:5000
```

Order & Sales routes are handled by Group 2:

```text
/api/Sales/*
/api/Orders/*
/api/Customers/*
/api/Suppliers/*
/api/Payments/*
/api/Debts/*
/api/ProductStockCaches/*
/api/OutboxMessages/*
```

Product & Inventory routes are expected from Group 1:

```text
/api/products/{id}
/api/categories/*
```

User & Report routes are expected from Group 3:

```text
/api/auth/*
/api/reports/*
```

## Group 1 Product API Expected By Group 2

Group 2 can call this API during checkout when
`ProductIntegration__UseGatewayLookup=true`.

```http
GET /api/products/{id}
```

Minimum response fields:

```json
{
  "productId": 5,
  "productCode": "DT002",
  "productName": "Ban phim co AKKO",
  "categoryName": "Dien tu",
  "sellingPrice": 1290000,
  "quantityAvailable": 9,
  "stockStatus": "InStock"
}
```

Accepted aliases:

```text
id/ProductID
name/ProductName
price/Price
quality/stock/availableStock/Quantity
```

If Group 1 is not ready, Group 2 falls back to `ProductStockCaches`.

## Group 1 Event Consumed By Group 2

Queue name:

```text
stock.updated
```

Payload:

```json
{
  "eventName": "stock.updated",
  "productId": 5,
  "productCode": "DT002",
  "productName": "Ban phim co AKKO",
  "categoryName": "Dien tu",
  "sellingPrice": 1290000,
  "quantityAvailable": 9,
  "stockStatus": "InStock",
  "updatedAt": "2026-06-13T08:00:00Z"
}
```

## Group 2 Event Published For Group 3

Queue name:

```text
order.created
```

Payload:

```json
{
  "eventName": "order.created",
  "orderId": 1024,
  "orderCode": "ORD001024",
  "customerId": 7,
  "customerName": "Nguyen Van A",
  "totalAmount": 1500000,
  "discountAmount": 50000,
  "finalAmount": 1450000,
  "paidAmount": 500000,
  "debtAmount": 950000,
  "paymentMethod": "Cash",
  "paymentStatus": "Partial",
  "orderStatus": "Debt",
  "createdBy": "sales01",
  "createdAt": "2026-06-13T08:00:00Z",
  "items": [
    {
      "productId": 5,
      "productCode": "DT002",
      "productName": "Ban phim co AKKO",
      "quantity": 2,
      "unitPrice": 1290000,
      "subTotal": 2580000
    }
  ]
}
```

## Run Modes

Standalone Group 2:

```text
ProductIntegration__UseGatewayLookup=false
Frontend proxy: http://localhost:5002
```

Integrated with Group 1/3:

```text
ProductIntegration__UseGatewayLookup=true
Frontend proxy/API URL: http://localhost:5000
```
