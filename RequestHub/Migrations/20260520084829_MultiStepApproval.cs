using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestHub.Migrations
{
    /// <inheritdoc />
    public partial class MultiStepApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalSteps_Users_ApproverId",
                table: "ApprovalSteps");

            migrationBuilder.AlterColumn<int>(
                name: "ApproverId",
                table: "ApprovalSteps",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "ApprovalSteps",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalSteps_Users_ApproverId",
                table: "ApprovalSteps",
                column: "ApproverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalSteps_Users_ApproverId",
                table: "ApprovalSteps");

            migrationBuilder.AlterColumn<int>(
                name: "ApproverId",
                table: "ApprovalSteps",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "ApprovalSteps",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalSteps_Users_ApproverId",
                table: "ApprovalSteps",
                column: "ApproverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
