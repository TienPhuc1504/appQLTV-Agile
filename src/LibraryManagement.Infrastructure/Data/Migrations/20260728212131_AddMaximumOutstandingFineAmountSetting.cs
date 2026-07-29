using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaximumOutstandingFineAmountSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Description", "Key", "UpdatedAt", "UpdatedByEmployeeId", "Value" },
                values: new object[] { 9, "Tiền phạt chưa thanh toán tối đa vẫn được phép mượn", "MaximumOutstandingFineAmount", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "0" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 9);
        }
    }
}
