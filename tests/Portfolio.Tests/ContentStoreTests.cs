using Portfolio.Entities;
using Portfolio.Entities.Content;
using Portfolio.Repositories;

namespace Portfolio.Tests;

/// <summary>
/// İçerik deposunun deploy güvenliği. Kritik: canlı içerik dosyası publish
/// klasörünün dışında durur; her deploy'da tohumla EZİLMEMELİDİR.
/// </summary>
public class JsonContentStoreTests : IDisposable
{
    private readonly string _klasor = Path.Combine(Path.GetTempPath(), "portfolio-icerik-" + Guid.NewGuid().ToString("N"));

    /// <summary>Testlerde koddaki gerçek seed yerine küçük, tanınabilir bir tohum.</summary>
    private static SiteContent Tohum() => new()
    {
        Meta = new SiteMeta { Name = "Tohum" },
        I18n = { ["tr"] = new Dictionary<string, string> { ["heroTag"] = "tohumdan" } }
    };

    [Fact]
    public async Task Dosya_yoksa_TOHUMDAN_uretilir_ve_diske_yazilir()
    {
        var canli = Path.Combine(_klasor, "veri", "content.json");
        var depo = new JsonContentStore(canli, Tohum);

        var i = await depo.LoadAsync();

        Assert.Equal("Tohum", i.Meta.Name);
        // Diske yazılmalı: yoksa kullanıcının sonraki düzenlemeleri tutunacağı
        // bir yer bulamaz ve her açılışta tohuma geri dönerdi.
        Assert.True(File.Exists(canli));
        Assert.Equal("Tohum", (await new JsonContentStore(canli).LoadAsync()).Meta.Name);
    }

    [Fact]
    public async Task Dosya_VARSA_tohum_UZERINE_YAZMAZ()
    {
        // Deploy senaryosu: sunucuda düzenlenmiş içerik var, yeni sürüm geliyor.
        var canli = Path.Combine(_klasor, "veri", "content.json");
        Directory.CreateDirectory(Path.GetDirectoryName(canli)!);
        File.WriteAllText(canli, """
        { "meta": { "name": "SUNUCUDA DÜZENLENMİŞ" }, "i18n": { "tr": { "heroTag": "canlı" } } }
        """);

        var i = await new JsonContentStore(canli, Tohum).LoadAsync();

        Assert.Equal("SUNUCUDA DÜZENLENMİŞ", i.Meta.Name);   // tohum ezmedi
    }

    [Fact]
    public async Task Kaydet_atomik_yazar_ve_yedek_birakir()
    {
        var canli = Path.Combine(_klasor, "veri", "content.json");
        var depo = new JsonContentStore(canli, Tohum);
        var i = await depo.LoadAsync();

        i.Meta.Name = "Değişti";
        await depo.SaveAsync(i);

        Assert.True(File.Exists(canli + ".bak"));                       // yedek alındı
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(canli)!, ".content.json.tmp")));  // geçici temizlendi
        Assert.Equal("Değişti", (await new JsonContentStore(canli).LoadAsync()).Meta.Name);
    }

    /// <summary>
    /// Tohum verilmezse koddaki gerçek <see cref="SeedIcerik"/> kullanılır.
    /// Eskiden burada "tohum DOSYASI da yoksa hata verir" testi vardı; tohum
    /// koda taşınınca o hata sınıfı ortadan kalktı — testin yerini bu aldı.
    /// </summary>
    [Fact]
    public async Task Tohum_verilmezse_koddaki_SeedIcerik_kullanilir()
    {
        var canli = Path.Combine(_klasor, "veri", "content.json");

        var i = await new JsonContentStore(canli).LoadAsync();

        Assert.Equal(SeedIcerik.Olustur().Meta.Name, i.Meta.Name);
        Assert.NotEmpty(i.I18n);
        Assert.NotEmpty(i.Games);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_klasor)) Directory.Delete(_klasor, true); } catch { }
    }
}
