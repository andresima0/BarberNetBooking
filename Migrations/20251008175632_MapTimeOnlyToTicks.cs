using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberNetBooking.Migrations
{
    /// <inheritdoc />
    public partial class MapTimeOnlyToTicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "StartTime",
                table: "Appointments",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "EndTime",
                table: "Appointments",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "EndTime",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");
        }
    }
}
