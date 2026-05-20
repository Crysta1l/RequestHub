using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestHub.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalStepsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApprovedId",
                table: "ApprovalSteps",
                newName: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApproverId",
                table: "ApprovalSteps",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_RequestId",
                table: "ApprovalSteps",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalSteps_AccessRequests_RequestId",
                table: "ApprovalSteps",
                column: "RequestId",
                principalTable: "AccessRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalSteps_Users_ApproverId",
                table: "ApprovalSteps",
                column: "ApproverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalSteps_AccessRequests_RequestId",
                table: "ApprovalSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalSteps_Users_ApproverId",
                table: "ApprovalSteps");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalSteps_ApproverId",
                table: "ApprovalSteps");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalSteps_RequestId",
                table: "ApprovalSteps");

            migrationBuilder.RenameColumn(
                name: "ApproverId",
                table: "ApprovalSteps",
                newName: "ApprovedId");
        }
    }
}
