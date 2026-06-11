using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_phong_kham_tu_nhan.Migrations
{
    /// <inheritdoc />
    public partial class AddLichLamViec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LichLamViecBacSis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BacSiId = table.Column<int>(type: "int", nullable: false),
                    Ngay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CaLam = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    SoBenhNhanToiDa = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaoLichBoi = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichLamViecBacSis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichLamViecBacSis_Doctors_BacSiId",
                        column: x => x.BacSiId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LichLamViecBacSis_BacSiId",
                table: "LichLamViecBacSis",
                column: "BacSiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichLamViecBacSis");
        }
    }
}
