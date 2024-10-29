using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommentsApp.Common.Migrations
{
    /// <inheritdoc />
    public partial class update1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "Comments",
                newName: "TextFileUrl");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Comments");

            migrationBuilder.RenameColumn(
                name: "TextFileUrl",
                table: "Comments",
                newName: "FilePath");
        }
    }
}
