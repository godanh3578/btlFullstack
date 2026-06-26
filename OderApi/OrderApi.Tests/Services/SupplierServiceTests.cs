using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.DTOs.Suppliers;
using OrderApi.Models;
using OrderApi.Services;

namespace OrderApi.Tests.Services
{
    public class SupplierServiceTests
    {
        private OrderDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new OrderDbContext(options);
        }

        [Fact]
        public async Task CreateAsync_ValidSupplier_ReturnsDto()
        {
            // Arrange
            var dbContext = GetDbContext("CreateAsync_ValidSupplier");
            var service = new SupplierService(dbContext);

            var dto = new CreateSupplierDto
            {
                SupplierCode = "SUP001",
                SupplierName = "Test Supplier",
                Phone = "123456789",
                TaxCode = "TX001"
            };

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("SUP001", result.SupplierCode);
            Assert.Equal(1, await dbContext.Suppliers.CountAsync());
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ThrowsException()
        {
            // Arrange
            var dbContext = GetDbContext("CreateAsync_DuplicateCode");
            dbContext.Suppliers.Add(new Supplier { SupplierCode = "SUP001", SupplierName = "Existing", Status = SupplierStatus.Active });
            await dbContext.SaveChangesAsync();
            
            var service = new SupplierService(dbContext);
            var dto = new CreateSupplierDto
            {
                SupplierCode = "SUP001",
                SupplierName = "New Supplier",
                Phone = "123456789",
                TaxCode = "TX001"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task GetAllAsync_WithSearchTerm_ReturnsFiltered()
        {
            // Arrange
            var dbContext = GetDbContext("GetAllAsync_WithSearchTerm");
            dbContext.Suppliers.AddRange(
                new Supplier { SupplierCode = "S1", SupplierName = "Alpha Co", Status = SupplierStatus.Active },
                new Supplier { SupplierCode = "S2", SupplierName = "Beta Ltd", Status = SupplierStatus.Active }
            );
            await dbContext.SaveChangesAsync();
            var service = new SupplierService(dbContext);

            // Act
            var result = await service.GetAllAsync("Alpha");

            // Assert
            Assert.Single(result);
            Assert.Equal("Alpha Co", result.First().SupplierName);
        }
    }
}
