using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Magnar.AI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDefaultConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Provider_Type_IsDefault",
                table: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_Provider_Type_IsDefault_WorkspaceId",
                table: "Provider",
                columns: new[] { "Type", "IsDefault", "WorkspaceId" },
                unique: true,
                filter: "[IsDefault] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Provider_Type_IsDefault_WorkspaceId",
                table: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_Provider_Type_IsDefault",
                table: "Provider",
                columns: new[] { "Type", "IsDefault" },
                unique: true,
                filter: "[IsDefault] = 1");
        }
    }
}
