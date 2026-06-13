using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentPort.PlatformApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase13Embedding768 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Embedding",
                table: "document_chunks",
                type: "vector(768)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "vector(64)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Embedding",
                table: "document_chunks",
                type: "vector(64)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "vector(768)");
        }
    }
}
