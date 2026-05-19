using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestHub.Migrations
{
    /// <inheritdoc />
    public partial class AddBasisColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Basis",
                table: "AccessRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Basis",
                table: "AccessRequests");
        }
    }
}
