using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddRequestDetailItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "ContractId", table: "DiscountRequestDetails", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<int>(name: "ServiceId", table: "DiscountRequestDetails", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "CategoryId", table: "DiscountRequestDetails", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ItemName", table: "DiscountRequestDetails", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ItemNameAr", table: "DiscountRequestDetails", type: "nvarchar(max)", nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ContractId", table: "DiscountRequestDetails");
            migrationBuilder.DropColumn(name: "ServiceId", table: "DiscountRequestDetails");
            migrationBuilder.DropColumn(name: "CategoryId", table: "DiscountRequestDetails");
            migrationBuilder.DropColumn(name: "ItemName", table: "DiscountRequestDetails");
            migrationBuilder.DropColumn(name: "ItemNameAr", table: "DiscountRequestDetails");
        }
    }
}
