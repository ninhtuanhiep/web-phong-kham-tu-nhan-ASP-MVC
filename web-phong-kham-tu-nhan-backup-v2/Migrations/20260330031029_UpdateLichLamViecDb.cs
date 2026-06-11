using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_phong_kham_tu_nhan.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLichLamViecDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhiChuGoc",
                table: "LichLamViecBacSis",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LyDoXinNghi",
                table: "LichLamViecBacSis",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GhiChuGoc",
                table: "LichLamViecBacSis");

            migrationBuilder.DropColumn(
                name: "LyDoXinNghi",
                table: "LichLamViecBacSis");
        }
    }
}
