using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_phong_kham_tu_nhan.Migrations
{
    /// <inheritdoc />
    public partial class AddHoSoBacSi_AND_YeuCau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HoSoBacSis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BacSiId = table.Column<int>(type: "int", nullable: false),
                    HocVi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HocHam = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoCCHN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhamViHanhNghe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapCCHN = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayHetHanCCHN = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KinhNghiem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NamKinhNghiem = table.Column<int>(type: "int", nullable: false),
                    TruongDaoTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChuyenMonSau = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgonNgu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GiaiThuong = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapNhatLuc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoSoBacSis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoSoBacSis_Doctors_BacSiId",
                        column: x => x.BacSiId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauCapNhats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BacSiId = table.Column<int>(type: "int", nullable: false),
                    TenTruong = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GiaTriCu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GiaTriMoi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    LyDoTuChoi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaoLuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DuyetLuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DuyetBoi = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauCapNhats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YeuCauCapNhats_Doctors_BacSiId",
                        column: x => x.BacSiId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HoSoBacSis_BacSiId",
                table: "HoSoBacSis",
                column: "BacSiId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauCapNhats_BacSiId",
                table: "YeuCauCapNhats",
                column: "BacSiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoSoBacSis");

            migrationBuilder.DropTable(
                name: "YeuCauCapNhats");
        }
    }
}
