using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileServer.Migrations
{
    /// <inheritdoc />
    public partial class Production : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agency",
                schema: "public",
                table: "filerecords",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "agency",
                schema: "public",
                table: "filerecords");
        }
    }
}
