using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderApi.Migrations
{
    /// <inheritdoc />
    public partial class AlignGroup2RequiredSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Debts_Customers_CustomerId",
                table: "Debts");

            migrationBuilder.DropForeignKey(
                name: "FK_Debts_Orders_OrderId",
                table: "Debts");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Orders_OrderId",
                table: "OrderDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderDetails",
                table: "OrderDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Debts",
                table: "Debts");

            migrationBuilder.RenameTable(
                name: "OrderDetails",
                newName: "OrderItems");

            migrationBuilder.RenameTable(
                name: "Debts",
                newName: "CustomerDebts");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "Orders",
                newName: "SubTotal");

            migrationBuilder.RenameColumn(
                name: "FinalAmount",
                table: "Orders",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "SubTotal",
                table: "OrderItems",
                newName: "LineTotal");

            migrationBuilder.RenameColumn(
                name: "OrderDetailId",
                table: "OrderItems",
                newName: "OrderItemId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderDetails_OrderId",
                table: "OrderItems",
                newName: "IX_OrderItems_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_Debts_OrderId",
                table: "CustomerDebts",
                newName: "IX_CustomerDebts_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_Debts_CustomerId",
                table: "CustomerDebts",
                newName: "IX_CustomerDebts_CustomerId");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Orders",
                schema: null);

            migrationBuilder.DropColumn(
                name: "DebtAmount",
                table: "Orders");

            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                computedColumnSql: "[TotalAmount] - [PaidAmount]",
                stored: true);

            migrationBuilder.DropColumn(
                name: "LineTotal",
                table: "OrderItems");

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                computedColumnSql: "[UnitPrice] * [Quantity] - [DiscountAmount]",
                stored: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAmount",
                table: "CustomerDebts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                computedColumnSql: "[DebtAmount] - [PaidAmount]",
                stored: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems",
                column: "OrderItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CustomerDebts",
                table: "CustomerDebts",
                column: "DebtId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDebts_Customers_CustomerId",
                table: "CustomerDebts",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDebts_Orders_OrderId",
                table: "CustomerDebts",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDebts_Customers_CustomerId",
                table: "CustomerDebts");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDebts_Orders_OrderId",
                table: "CustomerDebts");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CustomerDebts",
                table: "CustomerDebts");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "CustomerDebts");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DebtAmount",
                table: "Orders");

            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.RenameTable(
                name: "OrderItems",
                newName: "OrderDetails");

            migrationBuilder.RenameTable(
                name: "CustomerDebts",
                newName: "Debts");

            migrationBuilder.RenameColumn(
                name: "SubTotal",
                table: "Orders",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "Orders",
                newName: "FinalAmount");

            migrationBuilder.RenameColumn(
                name: "LineTotal",
                table: "OrderDetails",
                newName: "SubTotal");

            migrationBuilder.RenameColumn(
                name: "OrderItemId",
                table: "OrderDetails",
                newName: "OrderDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderDetails",
                newName: "IX_OrderDetails_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerDebts_OrderId",
                table: "Debts",
                newName: "IX_Debts_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerDebts_CustomerId",
                table: "Debts",
                newName: "IX_Debts_CustomerId");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "OrderDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderDetails",
                table: "OrderDetails",
                column: "OrderDetailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Debts",
                table: "Debts",
                column: "DebtId");

            migrationBuilder.AddForeignKey(
                name: "FK_Debts_Customers_CustomerId",
                table: "Debts",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Debts_Orders_OrderId",
                table: "Debts",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Orders_OrderId",
                table: "OrderDetails",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
