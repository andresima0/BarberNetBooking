using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberNetBooking.Migrations
{
    /// <inheritdoc />
    public partial class AddLunchTimesToWorkingHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "LunchEndTime",
                table: "WorkingHours",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "LunchStartTime",
                table: "WorkingHours",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LunchEndTime",
                table: "WorkingHours");

            migrationBuilder.DropColumn(
                name: "LunchStartTime",
                table: "WorkingHours");
        }
    }
}