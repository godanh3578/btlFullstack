using Microsoft.EntityFrameworkCore;
using OrderApi.Models;

namespace OrderApi.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(OrderDbContext db)
        {
            if (!await db.ProductStockCaches.AnyAsync())
            {
                db.ProductStockCaches.AddRange(
                    new ProductStockCache
                    {
                        ProductId = 1,
                        ProductCode = "GD001",
                        ProductName = "Nồi cơm điện Sunhouse 1.8L",
                        CategoryName = "Gia dụng",
                        SellingPrice = 650000,
                        QuantityAvailable = 12,
                        StockStatus = StockStatus.InStock
                    },
                    new ProductStockCache
                    {
                        ProductId = 2,
                        ProductCode = "GD002",
                        ProductName = "Máy xay sinh tố Philips",
                        CategoryName = "Gia dụng",
                        SellingPrice = 890000,
                        QuantityAvailable = 8,
                        StockStatus = StockStatus.InStock
                    },
                    new ProductStockCache
                    {
                        ProductId = 3,
                        ProductCode = "GD003",
                        ProductName = "Quạt điện Panasonic",
                        CategoryName = "Gia dụng",
                        SellingPrice = 780000,
                        QuantityAvailable = 0,
                        StockStatus = StockStatus.OutOfStock
                    },
                    new ProductStockCache
                    {
                        ProductId = 4,
                        ProductCode = "DT001",
                        ProductName = "Chuột Logitech M331",
                        CategoryName = "Điện tử",
                        SellingPrice = 320000,
                        QuantityAvailable = 30,
                        StockStatus = StockStatus.InStock
                    },
                    new ProductStockCache
                    {
                        ProductId = 5,
                        ProductCode = "DT002",
                        ProductName = "Bàn phím cơ AKKO",
                        CategoryName = "Điện tử",
                        SellingPrice = 1290000,
                        QuantityAvailable = 9,
                        StockStatus = StockStatus.InStock
                    },
                    new ProductStockCache
                    {
                        ProductId = 6,
                        ProductCode = "TP001",
                        ProductName = "Gạo ST25 túi 5kg",
                        CategoryName = "Thực phẩm",
                        SellingPrice = 185000,
                        QuantityAvailable = 25,
                        StockStatus = StockStatus.InStock
                    },
                    new ProductStockCache
                    {
                        ProductId = 7,
                        ProductCode = "TT001",
                        ProductName = "Áo thun cotton nam",
                        CategoryName = "Thời trang",
                        SellingPrice = 150000,
                        QuantityAvailable = 18,
                        StockStatus = StockStatus.InStock
                    },
                    new ProductStockCache
                    {
                        ProductId = 8,
                        ProductCode = "VP001",
                        ProductName = "Bút bi Thiên Long hộp 20 cây",
                        CategoryName = "Văn phòng phẩm",
                        SellingPrice = 65000,
                        QuantityAvailable = 50,
                        StockStatus = StockStatus.InStock
                    });

                await db.SaveChangesAsync();
            }
        }
    }
}
