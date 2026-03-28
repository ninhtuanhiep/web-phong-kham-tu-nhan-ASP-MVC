using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_phong_kham_tu_nhan.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbBacSi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Doctors",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TimeSlot",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Doctors");

            migrationBuilder.AlterColumn<string>(
                name: "TimeSlot",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
