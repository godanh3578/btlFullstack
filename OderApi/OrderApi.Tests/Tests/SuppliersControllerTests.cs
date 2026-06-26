#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApi.Controllers;
using OrderApi.Data;
using OrderApi.DTOs.Suppliers;
using OrderApi.Services;
using Xunit;

namespace OrderApi.Tests.Tests
{
    public class SuppliersControllerTests
    {
        [Fact]
        public async Task Create_Update_Search_Supplier_IncludesTaxCodeAndNote()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase("SuppliersTestDb_" + Guid.NewGuid())
                .Options;

            using var context = new OrderDbContext(options);
            var controller = new SuppliersController(new SupplierService(context));

            var createResult = await controller.Create(new CreateSupplierDto
            {
                SupplierCode = "NCC001",
                SupplierName = "Acme Supply",
                ContactPerson = "Anna",
                Phone = "0900111222",
                TaxCode = "TAX-001",
                Note = "Preferred supplier"
            });

            var created = Assert.IsType<SupplierDto>((createResult as OkObjectResult)!.Value);
            Assert.Equal("TAX-001", created.TaxCode);
            Assert.Equal("Preferred supplier", created.Note);

            var searchResult = await controller.GetAll("TAX-001");
            var searchList = Assert.IsAssignableFrom<IEnumerable<SupplierDto>>((searchResult as OkObjectResult)!.Value);
            Assert.Single(searchList);

            var updateResult = await controller.Update(created.SupplierId, new UpdateSupplierDto
            {
                SupplierName = "Acme Supply Updated",
                ContactPerson = "Anna",
                Phone = "0900111222",
                TaxCode = "TAX-002",
                Note = "Updated note",
                Status = "Active"
            });

            var updated = Assert.IsType<SupplierDto>((updateResult as OkObjectResult)!.Value);
            Assert.Equal("TAX-002", updated.TaxCode);
            Assert.Equal("Updated note", updated.Note);
        }
    }
}
