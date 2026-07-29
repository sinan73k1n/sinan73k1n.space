using Portfolio.Repositories;
using Portfolio.Services.Admin;

namespace Portfolio.Tests;

/// <summary>
/// Görsel yükleme doğrulaması. Kritik nokta: uzantı ve Content-Type İSTEMCİDEN gelir,
/// güvenilmez → tür dosyanın ilk baytlarından belirlenir.
/// </summary>
public class GorselDogrulayiciTests
{
    private static MemoryStream Akis(params byte[] bas)
    {
        var veri = new byte[64];                 // en az 12 bayt okunabilsin
        bas.CopyTo(veri, 0);
        return new MemoryStream(veri);
    }

    [Fact]
    public void JPEG_kabul_edilir()
    {
        var a = Akis(0xFF, 0xD8, 0xFF, 0xE0);
        var s = GorselDogrulayici.Dogrula(a, a.Length);
        Assert.True(s.Gecerli);
        Assert.Equal(".jpg", s.Uzanti);
    }

    [Fact]
    public void PNG_kabul_edilir()
    {
        var a = Akis(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A);
        var s = GorselDogrulayici.Dogrula(a, a.Length);
        Assert.True(s.Gecerli);
        Assert.Equal(".png", s.Uzanti);
    }

    [Fact]
    public void WebP_kabul_edilir()
    {
        var a = Akis((byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P');
        var s = GorselDogrulayici.Dogrula(a, a.Length);
        Assert.True(s.Gecerli);
        Assert.Equal(".webp", s.Uzanti);
    }

    [Fact]
    public void AVIF_kabul_edilir()
    {
        var a = Akis(0, 0, 0, 0x20, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'a', (byte)'v', (byte)'i', (byte)'f');
        var s = GorselDogrulayici.Dogrula(a, a.Length);
        Assert.True(s.Gecerli);
        Assert.Equal(".avif", s.Uzanti);
    }

    [Fact]
    public void SVG_REDDEDILIR_script_tasiyabilir()
    {
        // "<svg ..." → metin/XML; içinde <script> olabilir → depolanmış XSS riski
        var veri = System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");
        var a = new MemoryStream(veri);

        var s = GorselDogrulayici.Dogrula(a, a.Length);

        Assert.False(s.Gecerli);
        Assert.Contains("SVG", s.Hata);
    }

    [Fact]
    public void Gorsel_kilifina_girmis_calistirilabilir_dosya_reddedilir()
    {
        // "kapak.jpg" adıyla gönderilmiş bir ELF/script → baytlar ele verir
        var veri = System.Text.Encoding.UTF8.GetBytes("#!/bin/sh\nrm -rf /\n# uzun dolgu................");
        var a = new MemoryStream(veri);

        var s = GorselDogrulayici.Dogrula(a, a.Length);

        Assert.False(s.Gecerli);
    }

    [Fact]
    public void Cok_buyuk_dosya_reddedilir()
    {
        var a = Akis(0xFF, 0xD8, 0xFF);
        var s = GorselDogrulayici.Dogrula(a, GorselDogrulayici.MaxBayt + 1);
        Assert.False(s.Gecerli);
        Assert.Contains("büyük", s.Hata);
    }

    [Fact]
    public void Bos_dosya_reddedilir() => Assert.False(GorselDogrulayici.Dogrula(new MemoryStream(), 0).Gecerli);

    [Fact]
    public void Cok_kisa_dosya_reddedilir()
    {
        var a = new MemoryStream(new byte[] { 0xFF, 0xD8 });
        Assert.False(GorselDogrulayici.Dogrula(a, a.Length).Gecerli);
    }

    [Fact]
    public void Dogrulama_sonrasi_akis_basa_sarilir()
    {
        // Doğrulayıcı baytları okur; sonra kaydeden taraf baştan okuyabilmeli
        var a = Akis(0xFF, 0xD8, 0xFF);
        GorselDogrulayici.Dogrula(a, a.Length);
        Assert.Equal(0, a.Position);
    }
}

public class FileMediaStoreTests : IDisposable
{
    private readonly string _kok = Path.Combine(Path.GetTempPath(), "portfolio-medya-" + Guid.NewGuid().ToString("N"));

    private FileMediaStore Kur() => new(_kok);

    [Fact]
    public async Task Kaydet_dosyayi_yazar_ve_web_yolu_doner()
    {
        var depo = Kur();
        var yol = await depo.KaydetAsync(new MemoryStream(new byte[] { 1, 2, 3 }), ".png");

        Assert.StartsWith(FileMediaStore.WebOnek, yol);
        Assert.EndsWith(".png", yol);
        Assert.True(File.Exists(Path.Combine(_kok, "games", Path.GetFileName(yol))));
    }

    [Fact]
    public async Task Kaydet_dosya_adini_KENDI_uretir_kullanicidan_almaz()
    {
        var depo = Kur();
        var a = await depo.KaydetAsync(new MemoryStream(new byte[] { 1 }), ".png");
        var b = await depo.KaydetAsync(new MemoryStream(new byte[] { 1 }), ".png");

        Assert.NotEqual(a, b);                                   // çakışma yok
        Assert.DoesNotContain("..", a);                          // path traversal yok
        Assert.Matches(@"^/uploads/games/\d{8}-[0-9a-f]{32}\.png$", a);
    }

    [Fact]
    public async Task Sil_kendi_dosyasini_siler()
    {
        var depo = Kur();
        var yol = await depo.KaydetAsync(new MemoryStream(new byte[] { 1 }), ".png");
        var disk = Path.Combine(_kok, "games", Path.GetFileName(yol));

        await depo.SilAsync(yol);

        Assert.False(File.Exists(disk));
    }

    [Theory]
    [InlineData("../../appsettings.json")]
    [InlineData("/uploads/games/../../../etc/passwd")]
    [InlineData("https://baska-site.com/x.png")]
    [InlineData("/css/site.css")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Sil_depo_disindaki_yollara_DOKUNMAZ(string? yol)
    {
        // Kritik: elle yazılmış ya da eski bir yol, silme çağrısına dönüşüp
        // sistemdeki başka dosyayı silmemeli.
        var kanit = Path.Combine(_kok, "dokunulmaz.txt");
        Directory.CreateDirectory(_kok);
        await File.WriteAllTextAsync(kanit, "duruyor");

        await Kur().SilAsync(yol);

        Assert.True(File.Exists(kanit));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_kok)) Directory.Delete(_kok, true); } catch { }
    }
}
