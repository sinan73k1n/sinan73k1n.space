namespace Portfolio.Services.Admin;

/// <summary>Doğrulama sonucu: geçerliyse kullanılacak uzantı, değilse sebep.</summary>
public readonly record struct GorselSonuc(bool Gecerli, string Uzanti, string? Hata)
{
    public static GorselSonuc Olur(string uzanti) => new(true, uzanti, null);
    public static GorselSonuc Olmaz(string hata) => new(false, "", hata);
}

/// <summary>
/// Yüklenen dosyanın gerçekten görsel olduğunu doğrular.
/// <para>
/// ⛔ <b>Uzantıya ve Content-Type'a GÜVENİLMEZ</b> — ikisi de istemciden gelir ve
/// serbestçe uydurulabilir. Bu yüzden dosyanın ilk baytları (magic bytes) okunur;
/// gerçek tür oradan belirlenir ve uzantı ona göre YENİDEN atanır.
/// </para>
/// <para>
/// ⛔ <b>SVG bilerek REDDEDİLİR:</b> SVG bir XML belgesidir, içinde
/// <c>&lt;script&gt;</c> taşıyabilir. Aynı origin'den servis edilen bir SVG,
/// tarayıcıda doğrudan açıldığında JS çalıştırır → depolanmış XSS. Portfolyo
/// için gerekli de değil (oyun kapakları raster).
/// </para>
/// </summary>
public static class GorselDogrulayici
{
    /// <summary>Kapak görselleri için üst sınır. 16/10 kapak için fazlasıyla yeter.</summary>
    public const int MaxBayt = 3 * 1024 * 1024;

    public static GorselSonuc Dogrula(Stream akis, long uzunluk)
    {
        if (uzunluk <= 0) return GorselSonuc.Olmaz("Dosya boş.");
        if (uzunluk > MaxBayt) return GorselSonuc.Olmaz($"Dosya çok büyük ({uzunluk / 1024 / 1024.0:0.#} MB). Sınır 3 MB.");

        Span<byte> bas = stackalloc byte[12];
        var okunan = 0;
        while (okunan < bas.Length)
        {
            var n = akis.Read(bas[okunan..]);
            if (n <= 0) break;
            okunan += n;
        }
        if (akis.CanSeek) akis.Position = 0;
        if (okunan < 12) return GorselSonuc.Olmaz("Dosya tanınamadı (çok kısa).");

        // JPEG: FF D8 FF
        if (bas[0] == 0xFF && bas[1] == 0xD8 && bas[2] == 0xFF) return GorselSonuc.Olur(".jpg");

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bas[0] == 0x89 && bas[1] == 0x50 && bas[2] == 0x4E && bas[3] == 0x47 &&
            bas[4] == 0x0D && bas[5] == 0x0A && bas[6] == 0x1A && bas[7] == 0x0A) return GorselSonuc.Olur(".png");

        // WebP: "RIFF" .... "WEBP"
        if (bas[0] == (byte)'R' && bas[1] == (byte)'I' && bas[2] == (byte)'F' && bas[3] == (byte)'F' &&
            bas[8] == (byte)'W' && bas[9] == (byte)'E' && bas[10] == (byte)'B' && bas[11] == (byte)'P') return GorselSonuc.Olur(".webp");

        // GIF: "GIF87a" / "GIF89a"
        if (bas[0] == (byte)'G' && bas[1] == (byte)'I' && bas[2] == (byte)'F') return GorselSonuc.Olur(".gif");

        // AVIF/HEIF: .... "ftyp"
        if (bas[4] == (byte)'f' && bas[5] == (byte)'t' && bas[6] == (byte)'y' && bas[7] == (byte)'p')
        {
            var marka = System.Text.Encoding.ASCII.GetString(bas[8..12]);
            if (marka is "avif" or "avis") return GorselSonuc.Olur(".avif");
            return GorselSonuc.Olmaz("Bu video/HEIF biçimi desteklenmiyor. JPG, PNG, WebP, GIF veya AVIF yükle.");
        }

        // SVG (ve genel olarak metin) — bilinçli ret
        if (bas[0] == (byte)'<' || (bas[0] == 0xEF && bas[1] == 0xBB && bas[2] == 0xBF))
            return GorselSonuc.Olmaz("SVG/metin dosyası kabul edilmiyor (script taşıyabilir). JPG, PNG, WebP veya AVIF yükle.");

        return GorselSonuc.Olmaz("Tanınmayan dosya türü. JPG, PNG, WebP, GIF veya AVIF yükle.");
    }
}
