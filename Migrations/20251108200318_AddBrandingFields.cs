using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberNetBooking.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoPath",
                table: "ShopInfos",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteName",
                table: "ShopInfos",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoPath",
                table: "ShopInfos");

            migrationBuilder.DropColumn(
                name: "SiteName",
                table: "ShopInfos");
        }
    }
}
