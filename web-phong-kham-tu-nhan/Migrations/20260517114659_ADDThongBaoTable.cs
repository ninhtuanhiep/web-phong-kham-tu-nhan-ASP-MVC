using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_phong_kham_tu_nhan.Migrations
{
    /// <inheritdoc />
    public partial class ADDThongBaoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Specialties_ChuyenKhoaId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_ChuyenKhoaId",
                table: "Patients");

            migrationBuilder.AlterColumn<int>(
                name: "ChuyenKhoaId",
                table: "Patients",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

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

            migrationBuilder.CreateTable(
                name: "BenhNhanChuyenKhoa",
                columns: table => new
                {
                    BenhNhansId = table.Column<int>(type: "int", nullable: false),
                    ChuyenKhoasId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenhNhanChuyenKhoa", x => new { x.BenhNhansId, x.ChuyenKhoasId });
                    table.ForeignKey(
                        name: "FK_BenhNhanChuyenKhoa_Patients_BenhNhansId",
                        column: x => x.BenhNhansId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BenhNhanChuyenKhoa_Specialties_ChuyenKhoasId",
                        column: x => x.ChuyenKhoasId,
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThongBaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NguoiNhanId = table.Column<int>(type: "int", nullable: false),
                    LoaiThongBao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LichHenId = table.Column<int>(type: "int", nullable: true),
                    LichLamViecId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBaos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongBaos_Appointments_LichHenId",
                        column: x => x.LichHenId,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ThongBaos_Users_NguoiNhanId",
                        column: x => x.NguoiNhanId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenhNhanChuyenKhoa_ChuyenKhoasId",
                table: "BenhNhanChuyenKhoa",
                column: "ChuyenKhoasId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaos_LichHenId",
                table: "ThongBaos",
                column: "LichHenId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaos_NguoiNhanId",
                table: "ThongBaos",
                column: "NguoiNhanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenhNhanChuyenKhoa");

            migrationBuilder.DropTable(
                name: "ThongBaos");

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

            migrationBuilder.AlterColumn<int>(
                name: "ChuyenKhoaId",
                table: "Patients",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ChuyenKhoaId",
                table: "Patients",
                column: "ChuyenKhoaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Specialties_ChuyenKhoaId",
                table: "Patients",
                column: "ChuyenKhoaId",
                principalTable: "Specialties",
                principalColumn: "Id");
        }
    }
}
