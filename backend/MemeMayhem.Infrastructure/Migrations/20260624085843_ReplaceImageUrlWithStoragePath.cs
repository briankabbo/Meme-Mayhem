using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemeMayhem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceImageUrlWithStoragePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "MemeCards",
                newName: "StoragePath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StoragePath",
                table: "MemeCards",
                newName: "ImageUrl");
        }
    }
}
