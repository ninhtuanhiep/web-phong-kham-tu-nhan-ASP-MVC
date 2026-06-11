using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_phong_kham_tu_nhan.Migrations
{
    /// <inheritdoc />
    public partial class AddRescheduleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RescheduleAt",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RescheduleDate",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RescheduleNote",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RescheduleTimeSlot",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RescheduleAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RescheduleDate",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RescheduleNote",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RescheduleTimeSlot",
                table: "Appointments");
        }
    }
}
