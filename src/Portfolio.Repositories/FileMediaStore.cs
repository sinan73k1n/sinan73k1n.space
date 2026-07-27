namespace Portfolio.Repositories;

/// <summary>
/// Yüklenen medya dosyalarının deposu (oyun kapakları).
/// Faz 4'te DB gelse bile görseller DOSYA olarak kalır — binary'yi DB'ye koymak
/// bu ölçekte gereksiz (yedekleme ve servis maliyeti artar).
/// </summary>
public interface IMediaStore
{
    /// <summary>Akışı kaydeder; siteden erişilecek **web yolunu** döner (örn. `/uploads/games/ab12….webp`).</summary>
    Task<string> KaydetAsync(Stream akis, string uzanti, CancellationToken ct = default);

    /// <summary>Daha önce kaydedilmiş bir dosyayı siler. Depo dışındaki yollar YOK SAYILIR.</summary>
    Task SilAsync(string? webYolu, CancellationToken ct = default);
}

public sealed class FileMediaStore : IMediaStore
{
    /// <summary>Web'den erişilen önek — bu önekle başlamayan yollar bize ait değildir.</summary>
    public const string WebOnek = "/uploads/games/";

    private readonly string _kokKlasor;   // wwwroot

    public FileMediaStore(string wwwrootYolu) => _kokKlasor = wwwrootYolu;

    private string DiskKlasoru => Path.Combine(_kokKlasor, "uploads", "games");

    public async Task<string> KaydetAsync(Stream akis, string uzanti, CancellationToken ct = default)
    {
        Directory.CreateDirectory(DiskKlasoru);

        // ⛔ Dosya adı KULLANICIDAN ALINMAZ — tamamen biz üretiriz.
        // Path traversal (../), Windows ayrılmış adları, gizli uzantı (x.php.jpg),
        // unicode hileleri ve çakışma riskinin tamamı böyle kapanır.
        var ad = $"{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}{uzanti}";
        var diskYolu = Path.Combine(DiskKlasoru, ad);

        await using (var hedef = File.Create(diskYolu))
            await akis.CopyToAsync(hedef, ct);

        return WebOnek + ad;
    }

    public Task SilAsync(string? webYolu, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(webYolu)) return Task.CompletedTask;

        // Yalnız BİZİM ürettiğimiz yollar silinebilir. Dışarıdan gelen bir yol
        // ("../../appsettings.json", "https://..." ya da elle yazılmış bir URL)
        // asla dosya silmeye dönüşmemeli.
        if (!webYolu.StartsWith(WebOnek, StringComparison.Ordinal)) return Task.CompletedTask;

        var ad = Path.GetFileName(webYolu);
        if (string.IsNullOrWhiteSpace(ad)) return Task.CompletedTask;

        var diskYolu = Path.Combine(DiskKlasoru, ad);

        // Ek güvenlik: çözümlenen yol gerçekten depo klasörünün İÇİNDE mi?
        var tam = Path.GetFullPath(diskYolu);
        var kok = Path.GetFullPath(DiskKlasoru) + Path.DirectorySeparatorChar;
        if (!tam.StartsWith(kok, StringComparison.Ordinal)) return Task.CompletedTask;

        try { if (File.Exists(tam)) File.Delete(tam); } catch { /* silinemedi: kritik değil */ }
        return Task.CompletedTask;
    }
}
