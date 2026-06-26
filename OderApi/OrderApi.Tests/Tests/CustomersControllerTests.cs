using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderApi.Controllers;
using OrderApi.Data;
using OrderApi.DTOs.Customers;
using OrderApi.Models;
using OrderApi.Services;
using Xunit;

namespace OrderApi.Tests.Tests
{
    public class CustomersControllerTests
    {
        [Fact]
        public async Task Create_Get_Update_Delete_Customer()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase("CustomersTestDb_" + Guid.NewGuid())
                .Options;

            using var context = new OrderDbContext(options);
            var customerService = new CustomerService(context, NullLogger<CustomerService>.Instance);
            var controller = new CustomersController(customerService, context);

            var dto = new CreateCustomerDto
            {
                CustomerCode = "KH001",
                FullName = "Alice",
                Phone = "0123456789",
                Address = "Street 1"
            };

            var createResult = await controller.Create(dto);
            var created = Assert.IsType<CustomerDto>((createResult as OkObjectResult)!.Value);
            Assert.Equal("Alice", created.FullName);
            Assert.Equal("Thường", created.MembershipTier);

            var getAll = await controller.GetAll(null);
            var list = Assert.IsType<List<CustomerDto>>((getAll as OkObjectResult)!.Value);
            Assert.Single(list);

            var getById = await controller.GetById(created.CustomerId);
            var fetched = Assert.IsType<CustomerDto>((getById as OkObjectResult)!.Value);
            Assert.Equal(created.CustomerId, fetched.CustomerId);

            var updateDto = new UpdateCustomerDto
            {
                FullName = "Alice Updated",
                Phone = "0123456789",
                Address = "Street 1",
                Status = "Active",
                MembershipTier = "Vàng"
            };
            var updateResult = await controller.Update(created.CustomerId, updateDto);
            var updated = Assert.IsType<CustomerDto>((updateResult as OkObjectResult)!.Value);
            Assert.Equal("Alice Updated", updated.FullName);
            Assert.Equal("Vàng", updated.MembershipTier);

            var deleteResult = await controller.Delete(created.CustomerId);
            Assert.IsType<OkResult>(deleteResult);

            var afterDelete = await controller.GetById(created.CustomerId);
            Assert.IsType<NotFoundResult>(afterDelete);
        }

        [Fact]
        public async Task DeleteCustomer_WithExistingOrder_ReturnsBadRequest()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase("CustomersDeleteGuardDb_" + Guid.NewGuid())
                .Options;

            using var context = new OrderDbContext(options);
            var customerService = new CustomerService(context, NullLogger<CustomerService>.Instance);
            var controller = new CustomersController(customerService, context);

            var customer = new Customers
            {
                CustomerCode = "KH002",
                FullName = "Bob",
                Phone = "0987654321"
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            context.Orders.Add(new Order
            {
                OrderCode = "ORD000001",
                CustomerId = customer.CustomerId,
                CreatedByUserId = 1
            });
            await context.SaveChangesAsync();

            var deleteResult = await controller.Delete(customer.CustomerId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(deleteResult);
            Assert.Contains("don hang", badRequest.Value!.ToString());
        }
    }
}
