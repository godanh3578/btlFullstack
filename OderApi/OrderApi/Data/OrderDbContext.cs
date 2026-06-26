using Microsoft.EntityFrameworkCore;
using OrderApi.Models;

namespace OrderApi.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customers> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Debt> Debts { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<WalletTopUpRequest> WalletTopUpRequests { get; set; }
        public DbSet<ProductStockCache> ProductStockCaches { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Return> Returns { get; set; }
        public DbSet<ReturnDetail> ReturnDetails { get; set; }
        public DbSet<SalesInvoice> SalesInvoices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Customers
            modelBuilder.Entity<Customers>()
                .HasKey(c => c.CustomerId);

            modelBuilder.Entity<Customers>()
                .HasIndex(c => c.CustomerCode)
                .IsUnique();

            modelBuilder.Entity<Customers>()
                .HasQueryFilter(c => !c.IsDeleted);

            // Suppliers
            modelBuilder.Entity<Supplier>()
                .HasKey(s => s.SupplierId);

            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.SupplierCode)
                .IsUnique();

            // Orders
            modelBuilder.Entity<Order>()
                .HasKey(o => o.OrderId);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnName("SubTotal");

            modelBuilder.Entity<Order>()
                .Property(o => o.FinalAmount)
                .HasColumnName("TotalAmount");

            modelBuilder.Entity<Order>()
                .Property(o => o.DebtAmount)
                .HasComputedColumnSql("[TotalAmount] - [PaidAmount]", stored: true);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderCode)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasQueryFilter(o => !o.IsDeleted);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderDetails
            modelBuilder.Entity<OrderDetail>()
                .HasKey(od => od.OrderDetailId);

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.OrderDetailId)
                .HasColumnName("OrderItemId");

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.SubTotal)
                .HasColumnName("LineTotal")
                .HasComputedColumnSql("[UnitPrice] * [Quantity] - [DiscountAmount]", stored: true);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderDetail>()
                .ToTable("OrderItems");

            modelBuilder.Entity<OrderDetail>()
                .HasQueryFilter(od => od.Order != null && !od.Order.IsDeleted);

            // Payments
            modelBuilder.Entity<Payment>()
                .HasKey(p => p.PaymentId);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentCode)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasQueryFilter(p => p.Order != null && !p.Order.IsDeleted);

            // Wallet top-up requests
            modelBuilder.Entity<WalletTopUpRequest>()
                .HasKey(r => r.WalletTopUpRequestId);

            modelBuilder.Entity<WalletTopUpRequest>()
                .HasIndex(r => r.RequestCode)
                .IsUnique();

            modelBuilder.Entity<WalletTopUpRequest>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WalletTopUpRequest>()
                .HasQueryFilter(r => r.Customer != null && !r.Customer.IsDeleted);

            // Debts
            modelBuilder.Entity<Debt>()
                .HasKey(d => d.DebtId);

            modelBuilder.Entity<Debt>()
                .ToTable("CustomerDebts");

            modelBuilder.Entity<Debt>()
                .Property(d => d.RemainingAmount)
                .HasComputedColumnSql("[DebtAmount] - [PaidAmount]", stored: true);

            modelBuilder.Entity<Debt>()
                .HasOne(d => d.Customer)
                .WithMany(c => c.Debts)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Debt>()
                .HasOne(d => d.Order)
                .WithOne(o => o.Debt)
                .HasForeignKey<Debt>(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Debt>()
                .HasQueryFilter(d =>
                    d.Customer != null && !d.Customer.IsDeleted &&
                    d.Order != null && !d.Order.IsDeleted);

            // ProductStockCache
            modelBuilder.Entity<ProductStockCache>()
                .HasKey(p => p.ProductStockCacheId);

            modelBuilder.Entity<ProductStockCache>()
                .HasIndex(p => p.ProductId)
                .IsUnique();

            modelBuilder.Entity<ProductStockCache>()
                .HasQueryFilter(p => !p.IsDeleted);

            // OutboxMessages
            modelBuilder.Entity<OutboxMessage>()
                .HasKey(o => o.OutboxMessageId);

            // AuditLogs
            modelBuilder.Entity<AuditLog>()
                .HasKey(a => a.AuditLogId);

            // Returns
            modelBuilder.Entity<Return>()
                .HasKey(r => r.ReturnId);
            modelBuilder.Entity<Return>()
                .HasIndex(r => r.ReturnCode)
                .IsUnique();
            modelBuilder.Entity<Return>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Return>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Return>()
                .HasQueryFilter(r =>
                    r.Customer != null && !r.Customer.IsDeleted &&
                    r.Order != null && !r.Order.IsDeleted);

            // ReturnDetails
            modelBuilder.Entity<ReturnDetail>()
                .HasKey(rd => rd.ReturnDetailId);
            modelBuilder.Entity<ReturnDetail>()
                .HasOne(rd => rd.Return)
                .WithMany(r => r.Items)
                .HasForeignKey(rd => rd.ReturnId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ReturnDetail>()
                .HasQueryFilter(rd => rd.Return != null);

            // SalesInvoices
            modelBuilder.Entity<SalesInvoice>()
                .HasKey(si => si.InvoiceId);
            modelBuilder.Entity<SalesInvoice>()
                .HasIndex(si => si.InvoiceCode)
                .IsUnique();
            modelBuilder.Entity<SalesInvoice>()
                .HasOne(si => si.Order)
                .WithMany()
                .HasForeignKey(si => si.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<SalesInvoice>()
                .HasOne(si => si.Customer)
                .WithMany()
                .HasForeignKey(si => si.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SalesInvoice>()
                .HasQueryFilter(si =>
                    si.Order != null && !si.Order.IsDeleted &&
                    si.Customer != null && !si.Customer.IsDeleted);

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(entityType => entityType.GetProperties())
                .Where(property => property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }
    }
}
