using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderApi.Migrations
{
    /// <inheritdoc />
    public partial class SyncManualSchemaSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Suppliers]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'Suppliers', N'Note') IS NULL
                BEGIN
                    ALTER TABLE [Suppliers]
                    ADD [Note] nvarchar(500) NOT NULL
                        CONSTRAINT [DF_Suppliers_Note] DEFAULT N''
                END

                IF OBJECT_ID(N'[Suppliers]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'Suppliers', N'TaxCode') IS NULL
                BEGIN
                    ALTER TABLE [Suppliers]
                    ADD [TaxCode] nvarchar(20) NOT NULL
                        CONSTRAINT [DF_Suppliers_TaxCode] DEFAULT N''
                END

                IF OBJECT_ID(N'[Orders]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'Orders', N'DiscountType') IS NULL
                BEGIN
                    ALTER TABLE [Orders]
                    ADD [DiscountType] nvarchar(10) NOT NULL
                        CONSTRAINT [DF_Orders_DiscountType] DEFAULT N'Fixed'
                END

                IF OBJECT_ID(N'[Orders]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'Orders', N'DiscountValue') IS NULL
                BEGIN
                    ALTER TABLE [Orders]
                    ADD [DiscountValue] decimal(18,2) NOT NULL
                        CONSTRAINT [DF_Orders_DiscountValue] DEFAULT 0
                END

                IF OBJECT_ID(N'[Orders]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'Orders', N'StockRestoredAt') IS NULL
                BEGIN
                    ALTER TABLE [Orders]
                    ADD [StockRestoredAt] datetime2 NULL
                END

                IF OBJECT_ID(N'[Customers]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'Customers', N'AvatarUrl') IS NULL
                BEGIN
                    ALTER TABLE [Customers]
                    ADD [AvatarUrl] nvarchar(500) NULL
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Snapshot-only sync migration. Columns are owned by earlier manual
            // migrations/startup guards, so rollback should not drop them here.
        }
    }
}
