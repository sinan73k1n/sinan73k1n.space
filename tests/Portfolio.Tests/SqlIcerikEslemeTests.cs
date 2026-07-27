using System.Text.Json;
using Portfolio.Entities;
using Portfolio.Entities.Content;
using Portfolio.Repositories.Sql;

namespace Portfolio.Tests;

/// <summary>
/// Faz 4'ün asıl riski SQL değil, EŞLEME: içerik JSON deposundan tablolara
/// taşınırken bir şey kaybolursa site sessizce eksik açılır. Bu testler
/// dönüşümü **veritabanı olmadan** kapıya alır (sunucudaki mssql'e ihtiyaç yok).
/// </summary>
public class SqlIcerikEslemeTests
{
    private static readonly JsonSerializerOptions Karsilastirma = new() { WriteIndented = false };

    /// <summary>Derin karşılaştırma için içerik ağacını tek metne indirger.</summary>
    private static string Imza(SiteContent i) => JsonSerializer.Serialize(i, Karsilastirma);

    [Fact]
    public void Gercek_seed_icerigi_satirlara_donup_geri_gelince_AYNI_kalir()
    {
        // Uydurma veri değil, sitenin GERÇEK tohumu: şemanın her köşesi temsil edilsin.
        var once = SeedIcerik.Olustur();

        // İçerik → SQL satırları → tekrar içerik
        var sonra = IcerikSatirlari.Ayristir(once).Birlestir();

        Assert.Equal(Imza(once), Imza(sonra));

        // Boş bir içeriğin "aynı" çıkması anlamsız olurdu — gerçekten veri var mı?
        Assert.NotEmpty(sonra.I18n);
        Assert.NotEmpty(sonra.Games);
        Assert.NotEmpty(sonra.Demos);
        Assert.NotEmpty(sonra.Covers);
    }

    [Fact]
    public void Siralama_Order_kolonundan_kurulur_DB_sirasina_guvenilmez()
    {
        var icerik = new SiteContent
        {
            Logs = { "birinci", "ikinci", "ucuncu" },
            Techs = { new Tech { Name = "A" }, new Tech { Name = "B" }, new Tech { Name = "C" } }
        };
        icerik.Roles["tr"] = new List<string> { "Geliştirici", "Tasarımcı" };

        var satirlar = IcerikSatirlari.Ayristir(icerik);

        // MSSQL ORDER BY'sız sıra GARANTİ ETMEZ; en kötü hâli taklit et.
        satirlar.Logs.Reverse();
        satirlar.Techs.Reverse();
        satirlar.HeroRoles.Reverse();

        var geri = satirlar.Birlestir();

        Assert.Equal(new[] { "birinci", "ikinci", "ucuncu" }, geri.Logs);
        Assert.Equal(new[] { "A", "B", "C" }, geri.Techs.Select(x => x.Name));
        Assert.Equal(new[] { "Geliştirici", "Tasarımcı" }, geri.Roles["tr"]);
    }

    [Fact]
    public void Paylasilan_ceviri_nesnesi_kopyalanir()
    {
        // EF'te bir "owned" örnek iki sahibe ait olamaz. JSON içe aktarma sonrası
        // aynı Localized örneği iki kayda düşerse kopyalamasak çalışma anında patlardı.
        var ortak = new Localized { Tr = "ortak" };
        var icerik = new SiteContent
        {
            Facts = { new Fact { Value = 1, Label = ortak }, new Fact { Value = 2, Label = ortak } }
        };

        var satirlar = IcerikSatirlari.Ayristir(icerik);

        Assert.NotSame(satirlar.Facts[0].Label, satirlar.Facts[1].Label);
        Assert.Equal("ortak", satirlar.Facts[1].Label.Tr);
    }

    [Fact]
    public void Etiket_listesi_ve_kapak_renk_cifti_bozulmadan_doner()
    {
        var icerik = new SiteContent
        {
            Demos = { new Demo { Path = "d1", Name = "Demo", Tags = { "canvas", "webgl" }, Html = "<b>x</b>" } }
        };
        icerik.Covers["violet"] = new List<string> { "#7c3aed", "#312e81" };

        var geri = IcerikSatirlari.Ayristir(icerik).Birlestir();

        Assert.Equal(new[] { "canvas", "webgl" }, geri.Demos[0].Tags);
        Assert.Equal("<b>x</b>", geri.Demos[0].Html);          // demo HTML'i sanitize EDİLMEZ
        Assert.Equal(new[] { "#7c3aed", "#312e81" }, geri.Covers["violet"]);
    }

    [Fact]
    public void Eksik_ceviri_TR_fallback_davranisini_kaybetmez()
    {
        var icerik = new SiteContent
        {
            Games = { new Game { Name = "Oyun", Desc = new Localized { Tr = "türkçe", En = "" } } }
        };

        var geri = IcerikSatirlari.Ayristir(icerik).Birlestir();

        Assert.Equal("türkçe", geri.Games[0].Desc.Get("en"));   // boş EN → TR
        Assert.Equal("türkçe", geri.Games[0].Desc.Get("ru"));
    }

    [Fact]
    public void Bos_icerik_coketmez()
    {
        var geri = IcerikSatirlari.Ayristir(new SiteContent()).Birlestir();

        Assert.Equal("", geri.Meta.Name);
        Assert.Empty(geri.Games);
        Assert.Empty(geri.I18n);
    }
}
