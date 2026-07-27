using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Repositories.Sql.Migrations
{
    /// <inheritdoc />
    public partial class Metrikler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GunlukOzet",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Gun = table.Column<DateOnly>(type: "date", nullable: false),
                    Tip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Anahtar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Deger = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GunlukOzet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GunlukTuz",
                columns: table => new
                {
                    Gun = table.Column<DateOnly>(type: "date", nullable: false),
                    Tuz = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GunlukTuz", x => x.Gun);
                });

            migrationBuilder.CreateTable(
                name: "Olay",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZamanUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gun = table.Column<DateOnly>(type: "date", nullable: false),
                    ZiyaretciHash = table.Column<string>(type: "nchar(32)", fixedLength: true, maxLength: 32, nullable: false),
                    Tip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Deger = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SaniyeSure = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Olay", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ziyaret",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZamanUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gun = table.Column<DateOnly>(type: "date", nullable: false),
                    ZiyaretciHash = table.Column<string>(type: "nchar(32)", fixedLength: true, maxLength: 32, nullable: false),
                    Yol = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Dil = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    KaynakTipi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    KaynakHost = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Cihaz = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ziyaret", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GunlukOzet_Gun_Tip_Anahtar",
                table: "GunlukOzet",
                columns: new[] { "Gun", "Tip", "Anahtar" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Olay_Gun",
                table: "Olay",
                column: "Gun");

            migrationBuilder.CreateIndex(
                name: "IX_Olay_Gun_Tip",
                table: "Olay",
                columns: new[] { "Gun", "Tip" });

            migrationBuilder.CreateIndex(
                name: "IX_Ziyaret_Gun",
                table: "Ziyaret",
                column: "Gun");

            migrationBuilder.CreateIndex(
                name: "IX_Ziyaret_Gun_ZiyaretciHash",
                table: "Ziyaret",
                columns: new[] { "Gun", "ZiyaretciHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GunlukOzet");

            migrationBuilder.DropTable(
                name: "GunlukTuz");

            migrationBuilder.DropTable(
                name: "Olay");

            migrationBuilder.DropTable(
                name: "Ziyaret");
        }
    }
}
