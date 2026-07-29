using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Repositories.Sql.Migrations
{
    /// <summary>
    /// Teknoloji notu düz metinden üç dilliye geçti: `Note` → `NoteTr/NoteEn/NoteRu`.
    ///
    /// <para>
    /// ⚠️ Üretilen taslak ÖNCE `Note`'u düşürüyordu → canlıdaki notlar silinirdi.
    /// Sıra elle düzeltildi: **yeni kolonlar eklenir → eski değer NoteTr'ye taşınır →
    /// eski kolon düşer.** EN/RU boş kalır, site boş dili TR'ye düşürür (Localized.Get),
    /// yani migration'dan hemen sonra görünüm değişmez; admin panelinden doldurulur.
    /// </para>
    /// </summary>
    public partial class TeknolojiNotuUcDilli : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NoteTr",
                table: "Tech",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NoteEn",
                table: "Tech",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NoteRu",
                table: "Tech",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Eldeki tek dilli not Türkçe kabul edilir (içerik TR yazılmıştı).
            migrationBuilder.Sql("UPDATE [Tech] SET [NoteTr] = ISNULL([Note], N'');");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Tech");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Tech",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Geri dönüşte Türkçe not korunur; EN/RU çevirileri KAYBOLUR (tek kolon var).
            migrationBuilder.Sql("UPDATE [Tech] SET [Note] = ISNULL([NoteTr], N'');");

            migrationBuilder.DropColumn(name: "NoteTr", table: "Tech");
            migrationBuilder.DropColumn(name: "NoteEn", table: "Tech");
            migrationBuilder.DropColumn(name: "NoteRu", table: "Tech");
        }
    }
}
