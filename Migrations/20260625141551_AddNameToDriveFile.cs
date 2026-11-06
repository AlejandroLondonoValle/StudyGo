using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyGo.Migrations
{
    /// <inheritdoc />
    public partial class AddNameToDriveFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "DriveFiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "DriveFiles");
        }
    }
}
