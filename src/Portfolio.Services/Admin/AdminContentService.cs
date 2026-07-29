using System.Text.Json;
using Portfolio.Entities;
using Portfolio.Entities.Content;
using Portfolio.Repositories;

namespace Portfolio.Services.Admin;

/// <summary>
/// Admin tarafının içerik işlemleri: okuma, kaydetme, liste düzenleme (ekle/sil/sırala),
/// boş alan sayımı, "boşları TR'den doldur", JSON içe/dışa aktarma.
/// ⛔ Controller Entity görmez → dışarıya <see cref="BolumGorunumu"/> gibi sunum
/// nesneleri döner; Entity yalnız bu servisin içinde dolaşır.
/// </summary>
public interface IAdminContentService
{
    Task<BolumGorunumu> BolumGetirAsync(string slug, string lang, CancellationToken ct = default);

    /// <summary>Bir bölümdeki dile bağlı metinleri kaydeder.</summary>
    Task KopyaKaydetAsync(string lang, IDictionary<string, string?> degerler, CancellationToken ct = default);

    /// <summary>Liste öğesi ekler; yeni öğenin indeksini döner.</summary>
    Task<int> OgeEkleAsync(ListeTipi liste, CancellationToken ct = default);
    Task OgeSilAsync(ListeTipi liste, int index, CancellationToken ct = default);
    /// <summary>Öğeyi bir yukarı (-1) / aşağı (+1) taşır.</summary>
    Task OgeTasiAsync(ListeTipi liste, int index, int yon, CancellationToken ct = default);

    /// <summary>Liste öğelerinin alanlarını topluca kaydeder (form gönderimi).</summary>
    Task ListeKaydetAsync(ListeTipi liste, string lang, IDictionary<string, string?> form, CancellationToken ct = default);

    /// <summary>Dil başına boş/eksik kopya alanı sayısı — sekmelerdeki `EN · 3` rozeti.</summary>
    Task<IReadOnlyDictionary<string, int>> BosAlanSayilariAsync(CancellationToken ct = default);

    /// <summary>Hedef dildeki boş alanları TR metniyle doldurur; doldurulan alan sayısını döner.</summary>
    Task<int> BoslariTrdenDoldurAsync(string lang, CancellationToken ct = default);

    /// <summary>Tüm içeriği JSON olarak dışa aktarır (yedek).</summary>
    Task<string> DisaAktarAsync(CancellationToken ct = default);

    /// <summary>JSON'dan içeri aktarır. Geçersizse hiçbir şey yazılmaz.</summary>
    Task IceAktarAsync(string json, CancellationToken ct = default);

    /// <summary>Demo önizleme: kaydetmeden çalıştırmak için HTML (boşsa stub).</summary>
    Task<string> DemoHtmlAsync(int index, CancellationToken ct = default);

    /// <summary>
    /// Oyun kapağı yükler. Doğrulama başarısızsa <see cref="GorselSonuc.Hata"/> döner ve
    /// hiçbir şey yazılmaz. Başarılıysa eski görsel (bize aitse) silinir.
    /// </summary>
    Task<GorselSonuc> OyunGorseliYukleAsync(int index, Stream akis, long uzunluk, CancellationToken ct = default);

    /// <summary>Oyun kapağını kaldırır (dosyayı da siler) → kapak rengi preset'ine döner.</summary>
    Task OyunGorseliKaldirAsync(int index, CancellationToken ct = default);
}

/// <summary>Bir admin bölümünün düzenleme ekranına gereken her şey.</summary>
public sealed class BolumGorunumu
{
    public AdminSections.Bolum Bolum { get; init; } = null!;
    public string Lang { get; init; } = Portfolio.Entities.Lang.Fallback;
    /// <summary>Kopya anahtarı → o dildeki DEĞER (boşsa boş string; TR'ye DÜŞMEZ — admin gerçeği görmeli).</summary>
    public IReadOnlyDictionary<string, string> Degerler { get; init; } = new Dictionary<string, string>();
    /// <summary>Liste öğeleri (bölümün tipine göre doldurulur).</summary>
    public IReadOnlyList<ListeOgesi> Ogeler { get; init; } = Array.Empty<ListeOgesi>();
    public IReadOnlyDictionary<string, int> BosAlanlar { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<string> KapakSecenekleri { get; init; } = Array.Empty<string>();
    public SiteKunyesi Kunye { get; init; } = new();
}

/// <summary>Dile bağlı olmayan site künyesi (Genel bölümü).</summary>
public sealed class SiteKunyesi
{
    public string Name { get; set; } = "";
    public string Handle { get; set; } = "";
    public string Mail { get; set; } = "";
    public string Github { get; set; } = "";
    public string GithubUser { get; set; } = "";
    public string Play { get; set; } = "";
}

/// <summary>Listedeki tek bir kayıt — alanlar `ad → değer` olarak taşınır (jenerik şablon için).</summary>
public sealed class ListeOgesi
{
    public int Index { get; init; }
    public string Baslik { get; init; } = "";
    public Dictionary<string, string> Alanlar { get; init; } = new();
    /// <summary>Dile bağlı alanlar (o dildeki değer; boşsa boş).</summary>
    public Dictionary<string, string> CeviriAlanlari { get; init; } = new();
}

public sealed class AdminContentService : IAdminContentService
{
    private readonly IContentStore _depo;
    private readonly IMediaStore _medya;

    public AdminContentService(IContentStore depo, IMediaStore medya)
    {
        _depo = depo; _medya = medya;
    }

    // ---------------------------------------------------------------- okuma

    public async Task<BolumGorunumu> BolumGetirAsync(string slug, string lang, CancellationToken ct = default)
    {
        var bolum = AdminSections.Bul(slug) ?? AdminSections.Hepsi[0];
        lang = Lang.Normalize(lang);
        var i = await _depo.LoadAsync(ct);

        var degerler = new Dictionary<string, string>(StringComparer.Ordinal);
        var sozluk = i.I18n.TryGetValue(lang, out var s) ? s : new Dictionary<string, string>();
        foreach (var k in bolum.Kopyalar)
            degerler[k.Anahtar] = sozluk.TryGetValue(k.Anahtar, out var v) ? v : "";

        return new BolumGorunumu
        {
            Bolum = bolum,
            Lang = lang,
            Degerler = degerler,
            Ogeler = OgeleriGetir(i, bolum.Liste, lang),
            BosAlanlar = BosAlanlariHesapla(i),
            KapakSecenekleri = i.Covers.Keys.OrderBy(x => x).ToList(),
            Kunye = new SiteKunyesi
            {
                Name = i.Meta.Name, Handle = i.Meta.Handle, Mail = i.Meta.Mail,
                Github = i.Meta.Github, GithubUser = i.Meta.GithubUser, Play = i.Meta.Play
            }
        };
    }

    private static List<ListeOgesi> OgeleriGetir(SiteContent i, ListeTipi liste, string lang) => liste switch
    {
        ListeTipi.Loglar => i.Logs.Select((l, ix) => new ListeOgesi
        {
            Index = ix, Baslik = $"satır {ix + 1}",
            Alanlar = { ["text"] = l }
        }).ToList(),

        ListeTipi.Roller => (i.Roles.TryGetValue(lang, out var r) ? r : new List<string>())
            .Select((x, ix) => new ListeOgesi
            {
                Index = ix, Baslik = $"rol {ix + 1}",
                CeviriAlanlari = { ["text"] = x }
            }).ToList(),

        ListeTipi.Sayaclar => i.Facts.Select((f, ix) => new ListeOgesi
        {
            Index = ix, Baslik = $"sayaç {ix + 1}",
            Alanlar = { ["value"] = f.Value.ToString() },
            CeviriAlanlari = { ["label"] = DilDegeri(f.Label, lang) }
        }).ToList(),

        ListeTipi.Teknolojiler => i.Techs.Select((t, ix) => new ListeOgesi
        {
            Index = ix, Baslik = string.IsNullOrWhiteSpace(t.Name) ? $"teknoloji {ix + 1}" : t.Name,
            Alanlar = { ["name"] = t.Name },
            CeviriAlanlari = { ["note"] = DilDegeri(t.Note, lang) }
        }).ToList(),

        ListeTipi.Oyunlar => i.Games.Select((g, ix) => new ListeOgesi
        {
            Index = ix, Baslik = string.IsNullOrWhiteSpace(g.Name) ? $"oyun {ix + 1}" : g.Name,
            Alanlar = { ["name"] = g.Name, ["url"] = g.Url, ["image"] = g.Image, ["cover"] = g.Cover },
            CeviriAlanlari = { ["desc"] = DilDegeri(g.Desc, lang) }
        }).ToList(),

        ListeTipi.Demolar => i.Demos.Select((d, ix) => new ListeOgesi
        {
            Index = ix, Baslik = string.IsNullOrWhiteSpace(d.Name) ? $"demo {ix + 1}" : d.Name,
            Alanlar = { ["path"] = d.Path, ["name"] = d.Name, ["tags"] = string.Join(", ", d.Tags), ["html"] = d.Html },
            CeviriAlanlari = { ["desc"] = DilDegeri(d.Desc, lang) }
        }).ToList(),

        ListeTipi.Depolar => i.Repos.Select((r, ix) => new ListeOgesi
        {
            Index = ix, Baslik = string.IsNullOrWhiteSpace(r.Name) ? $"depo {ix + 1}" : r.Name,
            Alanlar = { ["name"] = r.Name, ["lang"] = r.Lang, ["updated"] = r.Updated, ["url"] = r.Url },
            CeviriAlanlari = { ["desc"] = DilDegeri(r.Desc, lang) }
        }).ToList(),

        _ => new List<ListeOgesi>()
    };

    /// <summary>Admin'de fallback YAPILMAZ: hangi alan gerçekten boş, görünmeli.</summary>
    private static string DilDegeri(Localized l, string lang) => lang switch
    {
        Lang.En => l.En,
        Lang.Ru => l.Ru,
        _ => l.Tr
    };

    private static void DilDegeriYaz(Localized l, string lang, string? deger)
    {
        deger ??= "";
        switch (lang)
        {
            case Lang.En: l.En = deger; break;
            case Lang.Ru: l.Ru = deger; break;
            default: l.Tr = deger; break;
        }
    }

    // ---------------------------------------------------------------- kopya kaydetme

    public async Task KopyaKaydetAsync(string lang, IDictionary<string, string?> degerler, CancellationToken ct = default)
    {
        lang = Lang.Normalize(lang);
        var i = await _depo.LoadAsync(ct);

        if (!i.I18n.TryGetValue(lang, out var sozluk))
            i.I18n[lang] = sozluk = new Dictionary<string, string>();

        // Yalnız TANIMLI anahtarlar yazılır — formdan gelen serbest anahtar sözlüğü kirletmesin.
        var izinli = AdminSections.TumKopyaAnahtarlari.ToHashSet(StringComparer.Ordinal);
        foreach (var (k, v) in degerler)
            if (izinli.Contains(k))
                sozluk[k] = v ?? "";

        await _depo.SaveAsync(i, ct);
    }

    // ---------------------------------------------------------------- liste işlemleri

    public async Task<int> OgeEkleAsync(ListeTipi liste, CancellationToken ct = default)
    {
        var i = await _depo.LoadAsync(ct);
        var index = liste switch
        {
            ListeTipi.Loglar => Ekle(i.Logs, "$ yeni satır"),
            ListeTipi.Roller => RolEkle(i),
            ListeTipi.Sayaclar => Ekle(i.Facts, new Fact { Value = 0, Label = new Localized() }),
            ListeTipi.Teknolojiler => Ekle(i.Techs, new Tech()),
            ListeTipi.Oyunlar => Ekle(i.Games, new Game { Cover = i.Covers.Keys.FirstOrDefault() ?? "violet", Desc = new Localized() }),
            ListeTipi.Demolar => Ekle(i.Demos, new Demo { Desc = new Localized(), Tags = new List<string>() }),
            ListeTipi.Depolar => Ekle(i.Repos, new RepoItem { Desc = new Localized() }),
            _ => -1
        };
        if (index >= 0) await _depo.SaveAsync(i, ct);
        return index;

        static int Ekle<T>(List<T> l, T oge) { l.Add(oge); return l.Count - 1; }
        static int RolEkle(SiteContent i)
        {
            foreach (var d in Lang.All)
            {
                if (!i.Roles.TryGetValue(d, out var l)) i.Roles[d] = l = new List<string>();
                l.Add("");
            }
            return i.Roles[Lang.Fallback].Count - 1;
        }
    }

    public async Task OgeSilAsync(ListeTipi liste, int index, CancellationToken ct = default)
    {
        var i = await _depo.LoadAsync(ct);
        var silindi = liste switch
        {
            ListeTipi.Loglar => Sil(i.Logs, index),
            ListeTipi.Roller => RolSil(i, index),
            ListeTipi.Sayaclar => Sil(i.Facts, index),
            ListeTipi.Teknolojiler => Sil(i.Techs, index),
            ListeTipi.Oyunlar => Sil(i.Games, index),
            ListeTipi.Demolar => Sil(i.Demos, index),
            ListeTipi.Depolar => Sil(i.Repos, index),
            _ => false
        };
        if (silindi) await _depo.SaveAsync(i, ct);

        static bool Sil<T>(List<T> l, int ix)
        {
            if (ix < 0 || ix >= l.Count) return false;
            l.RemoveAt(ix); return true;
        }
        static bool RolSil(SiteContent i, int ix)
        {
            var oldu = false;
            foreach (var d in Lang.All)
                if (i.Roles.TryGetValue(d, out var l) && ix >= 0 && ix < l.Count) { l.RemoveAt(ix); oldu = true; }
            return oldu;
        }
    }

    public async Task OgeTasiAsync(ListeTipi liste, int index, int yon, CancellationToken ct = default)
    {
        var i = await _depo.LoadAsync(ct);
        var tasindi = liste switch
        {
            ListeTipi.Loglar => Tasi(i.Logs, index, yon),
            ListeTipi.Roller => Lang.All.Aggregate(false, (o, d) => (i.Roles.TryGetValue(d, out var l) && Tasi(l, index, yon)) || o),
            ListeTipi.Sayaclar => Tasi(i.Facts, index, yon),
            ListeTipi.Teknolojiler => Tasi(i.Techs, index, yon),
            ListeTipi.Oyunlar => Tasi(i.Games, index, yon),
            ListeTipi.Demolar => Tasi(i.Demos, index, yon),
            ListeTipi.Depolar => Tasi(i.Repos, index, yon),
            _ => false
        };
        if (tasindi) await _depo.SaveAsync(i, ct);

        static bool Tasi<T>(List<T> l, int ix, int yon)
        {
            var hedef = ix + yon;
            if (ix < 0 || ix >= l.Count || hedef < 0 || hedef >= l.Count) return false;
            (l[ix], l[hedef]) = (l[hedef], l[ix]);
            return true;
        }
    }

    // ---------------------------------------------------------------- liste kaydetme

    public async Task ListeKaydetAsync(ListeTipi liste, string lang, IDictionary<string, string?> form, CancellationToken ct = default)
    {
        lang = Lang.Normalize(lang);
        var i = await _depo.LoadAsync(ct);

        // Form alan adı deseni: "oge[<index>].<alan>"
        string? Al(int ix, string alan) => form.TryGetValue($"oge[{ix}].{alan}", out var v) ? v : null;

        switch (liste)
        {
            case ListeTipi.Loglar:
                for (var ix = 0; ix < i.Logs.Count; ix++) i.Logs[ix] = Al(ix, "text") ?? i.Logs[ix];
                break;

            case ListeTipi.Roller:
                if (!i.Roles.TryGetValue(lang, out var roller)) i.Roles[lang] = roller = new List<string>();
                for (var ix = 0; ix < roller.Count; ix++) roller[ix] = Al(ix, "text") ?? roller[ix];
                break;

            case ListeTipi.Sayaclar:
                for (var ix = 0; ix < i.Facts.Count; ix++)
                {
                    if (int.TryParse(Al(ix, "value"), out var d)) i.Facts[ix].Value = d;
                    DilDegeriYaz(i.Facts[ix].Label, lang, Al(ix, "label"));
                }
                break;

            case ListeTipi.Teknolojiler:
                for (var ix = 0; ix < i.Techs.Count; ix++)
                {
                    i.Techs[ix].Name = Al(ix, "name") ?? i.Techs[ix].Name;
                    DilDegeriYaz(i.Techs[ix].Note, lang, Al(ix, "note"));
                }
                break;

            case ListeTipi.Oyunlar:
                for (var ix = 0; ix < i.Games.Count; ix++)
                {
                    var g = i.Games[ix];
                    g.Name = Al(ix, "name") ?? g.Name;
                    g.Url = Al(ix, "url") ?? g.Url;
                    // NOT: g.Image formdan YAZILMAZ — yalnız yükleme/kaldırma uçlarıyla değişir.
                    // Böylece elle yazılmış bir yol (../ ya da harici URL) sisteme sızmaz.
                    var kapak = Al(ix, "cover");
                    if (!string.IsNullOrWhiteSpace(kapak) && i.Covers.ContainsKey(kapak)) g.Cover = kapak;  // allowlist
                    DilDegeriYaz(g.Desc, lang, Al(ix, "desc"));
                }
                break;

            case ListeTipi.Demolar:
                for (var ix = 0; ix < i.Demos.Count; ix++)
                {
                    var d = i.Demos[ix];
                    d.Path = Al(ix, "path") ?? d.Path;
                    d.Name = Al(ix, "name") ?? d.Name;
                    var etiket = Al(ix, "tags");
                    if (etiket is not null)
                        d.Tags = etiket.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    // ⚠️ html KULLANICI İÇERİĞİ: sanitize EDİLMEZ, olduğu gibi saklanır;
                    // güvenlik iframe sandbox'ında sağlanır (bkz. vault workflow-rules §3).
                    d.Html = Al(ix, "html") ?? d.Html;
                    DilDegeriYaz(d.Desc, lang, Al(ix, "desc"));
                }
                break;

            case ListeTipi.Depolar:
                for (var ix = 0; ix < i.Repos.Count; ix++)
                {
                    var r = i.Repos[ix];
                    r.Name = Al(ix, "name") ?? r.Name;
                    r.Lang = Al(ix, "lang") ?? r.Lang;
                    r.Updated = Al(ix, "updated") ?? r.Updated;
                    r.Url = Al(ix, "url") ?? r.Url;
                    DilDegeriYaz(r.Desc, lang, Al(ix, "desc"));
                }
                break;

            case ListeTipi.Meta:
                i.Meta.Name = form.TryGetValue("meta.name", out var mn) ? mn ?? "" : i.Meta.Name;
                i.Meta.Handle = form.TryGetValue("meta.handle", out var mh) ? mh ?? "" : i.Meta.Handle;
                i.Meta.Mail = form.TryGetValue("meta.mail", out var mm) ? mm ?? "" : i.Meta.Mail;
                i.Meta.Github = form.TryGetValue("meta.github", out var mg) ? mg ?? "" : i.Meta.Github;
                i.Meta.GithubUser = form.TryGetValue("meta.githubUser", out var mu) ? mu ?? "" : i.Meta.GithubUser;
                i.Meta.Play = form.TryGetValue("meta.play", out var mp) ? mp ?? "" : i.Meta.Play;
                for (var ix = 0; ix < i.Logs.Count; ix++) i.Logs[ix] = Al(ix, "text") ?? i.Logs[ix];
                break;
        }

        await _depo.SaveAsync(i, ct);
    }

    // ---------------------------------------------------------------- çeviri yardımcıları

    public async Task<IReadOnlyDictionary<string, int>> BosAlanSayilariAsync(CancellationToken ct = default)
        => BosAlanlariHesapla(await _depo.LoadAsync(ct));

    private static Dictionary<string, int> BosAlanlariHesapla(SiteContent i)
    {
        var sonuc = new Dictionary<string, int>();
        var anahtarlar = AdminSections.TumKopyaAnahtarlari.ToList();

        foreach (var d in Lang.All)
        {
            var sayi = 0;
            var sozluk = i.I18n.TryGetValue(d, out var s) ? s : new Dictionary<string, string>();

            foreach (var k in anahtarlar)
                if (!sozluk.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v)) sayi++;

            // Roller
            var roller = i.Roles.TryGetValue(d, out var r) ? r : new List<string>();
            var rolHedef = i.Roles.TryGetValue(Lang.Fallback, out var rt) ? rt.Count : 0;
            sayi += Math.Max(0, rolHedef - roller.Count) + roller.Count(x => string.IsNullOrWhiteSpace(x));

            // Dile bağlı açıklamalar
            sayi += i.Facts.Count(f => string.IsNullOrWhiteSpace(DilDegeri(f.Label, d)));
            sayi += i.Techs.Count(t => string.IsNullOrWhiteSpace(DilDegeri(t.Note, d)));
            sayi += i.Games.Count(g => string.IsNullOrWhiteSpace(DilDegeri(g.Desc, d)));
            sayi += i.Demos.Count(x => string.IsNullOrWhiteSpace(DilDegeri(x.Desc, d)));
            sayi += i.Repos.Count(x => string.IsNullOrWhiteSpace(DilDegeri(x.Desc, d)));

            sonuc[d] = sayi;
        }
        return sonuc;
    }

    public async Task<int> BoslariTrdenDoldurAsync(string lang, CancellationToken ct = default)
    {
        lang = Lang.Normalize(lang);
        if (lang == Lang.Fallback) return 0;                    // kaynağı kendisinden doldurmak anlamsız

        var i = await _depo.LoadAsync(ct);
        var doldurulan = 0;

        if (!i.I18n.TryGetValue(lang, out var hedef)) i.I18n[lang] = hedef = new Dictionary<string, string>();
        var taban = i.I18n.TryGetValue(Lang.Fallback, out var t) ? t : new Dictionary<string, string>();

        foreach (var k in AdminSections.TumKopyaAnahtarlari)
            if ((!hedef.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v)) &&
                taban.TryGetValue(k, out var trDeger) && !string.IsNullOrWhiteSpace(trDeger))
            { hedef[k] = trDeger; doldurulan++; }

        // Roller: TR listesindeki karşılığı boşsa kopyala
        if (i.Roles.TryGetValue(Lang.Fallback, out var trRoller))
        {
            if (!i.Roles.TryGetValue(lang, out var hedefRoller)) i.Roles[lang] = hedefRoller = new List<string>();
            while (hedefRoller.Count < trRoller.Count) { hedefRoller.Add(""); }
            for (var ix = 0; ix < trRoller.Count; ix++)
                if (string.IsNullOrWhiteSpace(hedefRoller[ix]) && !string.IsNullOrWhiteSpace(trRoller[ix]))
                { hedefRoller[ix] = trRoller[ix]; doldurulan++; }
        }

        doldurulan += Doldur(i.Facts.Select(f => f.Label), lang);
        doldurulan += Doldur(i.Techs.Select(t => t.Note), lang);
        doldurulan += Doldur(i.Games.Select(g => g.Desc), lang);
        doldurulan += Doldur(i.Demos.Select(d => d.Desc), lang);
        doldurulan += Doldur(i.Repos.Select(r => r.Desc), lang);

        if (doldurulan > 0) await _depo.SaveAsync(i, ct);
        return doldurulan;

        static int Doldur(IEnumerable<Localized> hepsi, string lang)
        {
            var n = 0;
            foreach (var l in hepsi)
                if (string.IsNullOrWhiteSpace(DilDegeri(l, lang)) && !string.IsNullOrWhiteSpace(l.Tr))
                { DilDegeriYaz(l, lang, l.Tr); n++; }
            return n;
        }
    }

    // ---------------------------------------------------------------- JSON içe/dışa

    private static readonly JsonSerializerOptions DisaSecenek = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
    };

    public async Task<string> DisaAktarAsync(CancellationToken ct = default)
        => JsonSerializer.Serialize(await _depo.LoadAsync(ct), DisaSecenek);

    public async Task IceAktarAsync(string json, CancellationToken ct = default)
    {
        // Önce ayrıştır: geçersizse hiçbir şey YAZILMAZ (bozuk yükleme içeriği uçurmasın)
        var gelen = JsonSerializer.Deserialize<SiteContent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("JSON boş ya da geçersiz.");

        if (gelen.I18n.Count == 0 || !gelen.I18n.ContainsKey(Lang.Fallback))
            throw new InvalidOperationException("JSON geçerli görünmüyor: `i18n.tr` bulunamadı.");

        await _depo.SaveAsync(gelen, ct);
    }

    // ---------------------------------------------------------------- görsel

    public async Task<GorselSonuc> OyunGorseliYukleAsync(int index, Stream akis, long uzunluk, CancellationToken ct = default)
    {
        var sonuc = GorselDogrulayici.Dogrula(akis, uzunluk);
        if (!sonuc.Gecerli) return sonuc;                       // geçersiz → diske HİÇBİR ŞEY yazılmaz

        var i = await _depo.LoadAsync(ct);
        if (index < 0 || index >= i.Games.Count) return GorselSonuc.Olmaz("Oyun bulunamadı.");

        var eski = i.Games[index].Image;
        i.Games[index].Image = await _medya.KaydetAsync(akis, sonuc.Uzanti, ct);
        await _depo.SaveAsync(i, ct);

        await _medya.SilAsync(eski, ct);                        // yeni kayıt sağlamsa eskisini temizle
        return sonuc;
    }

    public async Task OyunGorseliKaldirAsync(int index, CancellationToken ct = default)
    {
        var i = await _depo.LoadAsync(ct);
        if (index < 0 || index >= i.Games.Count) return;

        var eski = i.Games[index].Image;
        i.Games[index].Image = "";
        await _depo.SaveAsync(i, ct);
        await _medya.SilAsync(eski, ct);
    }

    public async Task<string> DemoHtmlAsync(int index, CancellationToken ct = default)
    {
        var i = await _depo.LoadAsync(ct);
        if (index < 0 || index >= i.Demos.Count) return "";
        var d = i.Demos[index];
        return string.IsNullOrWhiteSpace(d.Html) ? ContentService.DemoStubPublic(d.Name) : d.Html;
    }
}
