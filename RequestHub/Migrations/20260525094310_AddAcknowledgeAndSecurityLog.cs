using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestHub.Migrations
{
    /// <inheritdoc />
    public partial class AddAcknowledgeAndSecurityLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "RequestHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "RequestHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "AccessRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcknowledgedBy",
                table: "AccessRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAcknowledged",
                table: "AccessRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "RequestHistories");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "RequestHistories");

            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "AccessRequests");

            migrationBuilder.DropColumn(
                name: "AcknowledgedBy",
                table: "AccessRequests");

            migrationBuilder.DropColumn(
                name: "IsAcknowledged",
                table: "AccessRequests");
        }
    }
}
