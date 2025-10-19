using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalNotesManager.Migrations
{
    /// <inheritdoc />
    public partial class AddContentRtfToNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentRtf",
                table: "Notes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentRtf",
                table: "Notes");
        }
    }
}
