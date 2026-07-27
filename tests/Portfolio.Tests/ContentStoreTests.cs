using Portfolio.Repositories;

namespace Portfolio.Tests;

/// <summary>
/// İçerik deposunun deploy güvenliği. Kritik: canlı içerik dosyası publish
/// klasörünün dışında durur; her deploy'da repo tohumuyla EZİLMEMELİDİR.
/// </summary>
public class JsonContentStoreTests : IDisposable
{
    private readonly string _klasor = Path.Combine(Path.GetTempPath(), "portfolio-icerik-" + Guid.NewGuid().ToString("N"));

    private string Tohum()
    {
        Directory.CreateDirectory(_klasor);
        var yol = Path.Combine(_klasor, "tohum.json");
        File.WriteAllText(yol, """
        { "meta": { "name": "Tohum" }, "i18n": { "tr": { "heroTag": "tohumdan" } } }
        """);
        return yol;
    }

    [Fact]
    public async Task Canli_dosya_yoksa_tohumdan_uretilir()
    {
        var canli = Path.Combine(_klasor, "veri", "content.json");
        var depo = new JsonContentStore(canli, Tohum());

        var i = await depo.LoadAsync();

        Assert.True(File.Exists(canli));
        Assert.Equal("Tohum", i.Meta.Name);
    }

    [Fact]
    public async Task Canli_dosya_VARSA_tohum_UZERINE_YAZMAZ()
    {
        // Deploy senaryosu: sunucuda düzenlenmiş içerik var, yeni sürüm geliyor.
        var tohum = Tohum();
        var canli = Path.Combine(_klasor, "veri", "content.json");
        Directory.CreateDirectory(Path.GetDirectoryName(canli)!);
        File.WriteAllText(canli, """
        { "meta": { "name": "SUNUCUDA DÜZENLENMİŞ" }, "i18n": { "tr": { "heroTag": "canlı" } } }
        """);

        var i = await new JsonContentStore(canli, tohum).LoadAsync();

        Assert.Equal("SUNUCUDA DÜZENLENMİŞ", i.Meta.Name);   // tohum ezmedi
    }

    [Fact]
    public async Task Kaydet_atomik_yazar_ve_yedek_birakir()
    {
        var canli = Path.Combine(_klasor, "veri", "content.json");
        var depo = new JsonContentStore(canli, Tohum());
        var i = await depo.LoadAsync();

        i.Meta.Name = "Değişti";
        await depo.SaveAsync(i);

        Assert.True(File.Exists(canli + ".bak"));                       // yedek alındı
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(canli)!, ".content.json.tmp")));  // geçici temizlendi
        Assert.Equal("Değişti", (await new JsonContentStore(canli).LoadAsync()).Meta.Name);
    }

    [Fact]
    public async Task Tohum_da_yoksa_anlasilir_hata_verir()
    {
        var depo = new JsonContentStore(Path.Combine(_klasor, "yok.json"), Path.Combine(_klasor, "tohum-da-yok.json"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => depo.LoadAsync());
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_klasor)) Directory.Delete(_klasor, true); } catch { }
    }
}
