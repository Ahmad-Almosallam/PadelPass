using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PadelPass.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingIsActiveToClubUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ClubUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ClubUsers");
        }
    }
}
