using System.Text.Json;
using Portfolio.Entities;
using Portfolio.Entities.Content;
using Portfolio.Repositories;
using Portfolio.Repositories.Sql;
using Portfolio.Services;
using Portfolio.Services.Admin;

namespace Portfolio.Tests;

/// <summary>
/// Teknoloji notunun üç dilli olması (karar: 2026-07-29).
/// Kritik davranış: çip SAYISI üç dilde AYNI (tek liste), ama not dile göre değişir —
/// EN sayfada "oyun motoru" görünmemeli. Boş çeviri sessizce TR'ye düşer.
/// </summary>
public class TeknolojiNotuTests
{
    private static SiteContent Icerik() => new()
    {
        I18n = { ["tr"] = new Dictionary<string, string>() },
        Techs =
        {
            new Tech { Name = "Unity", Note = new Localized { Tr = "oyun motoru", En = "game engine", Ru = "игровой движок" } },
            new Tech { Name = "Git",   Note = new Localized { Tr = "sürüm" } }        // EN/RU bilerek boş
        }
    };

    [Fact]
    public async Task Not_dile_gore_degisir_bos_ceviri_TRye_duser()
    {
        var servis = new ContentService(new SahteDepo(Icerik()));

        var tr = await servis.GetAsync("tr");
        var en = await servis.GetAsync("en");
        var ru = await servis.GetAsync("ru");

        // Çip sayısı üç dilde aynı — liste tektir.
        Assert.Equal(2, tr.Techs.Count);
        Assert.Equal(tr.Techs.Count, en.Techs.Count);
        Assert.Equal(tr.Techs.Count, ru.Techs.Count);

        Assert.Equal("oyun motoru", tr.Techs[0].Note);
        Assert.Equal("game engine", en.Techs[0].Note);
        Assert.Equal("игровой движок", ru.Techs[0].Note);

        // Çevirisi olmayan not TR'ye düşer (boş çip gösterilmez).
        Assert.Equal("sürüm", en.Techs[1].Note);
    }

    [Fact]
    public async Task EN_kaydetmek_TR_notunu_BOZMAZ()
    {
        var depo = new SahteDepo(Icerik());
        var admin = new AdminContentService(depo, new SahteMedya());

        await admin.ListeKaydetAsync(ListeTipi.Teknolojiler, "en", new Dictionary<string, string?>
        {
            ["oge[0].name"] = "Unity",
            ["oge[0].note"] = "game engine",
            ["oge[1].name"] = "Git",
            ["oge[1].note"] = "version control"
        });

        var i = await depo.LoadAsync();
        Assert.Equal("oyun motoru", i.Techs[0].Note.Tr);      // TR yerinde
        Assert.Equal("game engine", i.Techs[0].Note.En);
        Assert.Equal("version control", i.Techs[1].Note.En);
        Assert.Equal("sürüm", i.Techs[1].Note.Tr);
    }

    [Fact]
    public async Task Bos_not_dil_rozetinde_sayilir_ve_TRden_doldurulabilir()
    {
        var depo = new SahteDepo(Icerik());
        var admin = new AdminContentService(depo, new SahteMedya());

        var oncesi = await admin.BosAlanSayilariAsync();
        Assert.True(oncesi["en"] >= 1);                       // Git'in EN notu boş

        var doldurulan = await admin.BoslariTrdenDoldurAsync("en");
        var i = await depo.LoadAsync();

        Assert.True(doldurulan >= 1);
        Assert.Equal("sürüm", i.Techs[1].Note.En);
        Assert.Equal("game engine", i.Techs[0].Note.En);      // dolu olan EZİLMEZ
    }

    [Fact]
    public void SQL_esleme_ceviriyi_kaybetmez()
    {
        var geri = IcerikSatirlari.Ayristir(Icerik()).Birlestir();

        Assert.Equal("oyun motoru", geri.Techs[0].Note.Tr);
        Assert.Equal("game engine", geri.Techs[0].Note.En);
        Assert.Equal("игровой движок", geri.Techs[0].Note.Ru);
    }

    [Fact]
    public void Eski_yedekteki_DUZ_METIN_not_TRye_okunur()
    {
        // 2026-07-29 öncesi alınmış admin yedeği: "note" düz metindi.
        const string eski = """
        { "i18n": { "tr": {} }, "techs": [ { "name": "Unity", "note": "oyun motoru" } ] }
        """;

        var i = JsonSerializer.Deserialize<SiteContent>(eski,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        Assert.Equal("oyun motoru", i.Techs[0].Note.Tr);
        Assert.Equal("", i.Techs[0].Note.En);
    }

    // ---- yardımcılar ----

    private sealed class SahteDepo : IContentStore
    {
        private SiteContent _i;
        public SahteDepo(SiteContent i) => _i = i;
        public Task<SiteContent> LoadAsync(CancellationToken ct = default) => Task.FromResult(_i);
        public Task SaveAsync(SiteContent icerik, CancellationToken ct = default) { _i = icerik; return Task.CompletedTask; }
    }

    private sealed class SahteMedya : IMediaStore
    {
        public Task<string> KaydetAsync(Stream akis, string uzanti, CancellationToken ct = default) => Task.FromResult("");
        public Task SilAsync(string? webYolu, CancellationToken ct = default) => Task.CompletedTask;
    }
}
