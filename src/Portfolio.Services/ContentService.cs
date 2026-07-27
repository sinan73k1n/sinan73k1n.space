using Portfolio.Entities;
using Portfolio.Entities.Content;
using Portfolio.Repositories;
using Portfolio.Services.Dtos;

namespace Portfolio.Services;

public interface IContentService
{
    /// <summary>İstenen dilde sunuma hazır içerik (eksik çeviriler TR'ye düşer).</summary>
    Task<SiteContentDto> GetAsync(string lang, CancellationToken ct = default);
}

/// <summary>
/// İçerik iş mantığı: dil çözümleme + fallback, kapak rengi seçimi,
/// terminal satır rengi, boş demo için yer tutucu.
/// ⛔ Entity dışarı çıkmaz — yalnız DTO döner.
/// </summary>
public sealed class ContentService : IContentService
{
    private readonly IContentStore _depo;

    public ContentService(IContentStore depo) => _depo = depo;

    public async Task<SiteContentDto> GetAsync(string lang, CancellationToken ct = default)
    {
        lang = Lang.Normalize(lang);
        var i = await _depo.LoadAsync(ct);

        return new SiteContentDto
        {
            Lang = lang,
            Meta = new MetaDto
            {
                Name = i.Meta.Name, Handle = i.Meta.Handle, Mail = i.Meta.Mail,
                Github = i.Meta.Github, GithubUser = i.Meta.GithubUser, Play = i.Meta.Play
            },
            Copy = KopyaCoz(i, lang),
            Roles = i.Roles.TryGetValue(lang, out var r) && r.Count > 0
                    ? r
                    : (i.Roles.TryGetValue(Lang.Default, out var rt) ? rt : new List<string>()),

            Logs = i.Logs.Select(s => new LogLineDto { Text = s, CssClass = LogSinifi(s) }).ToList(),

            Facts = i.Facts.Select(f => new FactDto { Value = f.Value, Label = f.Label.Get(lang) }).ToList(),

            Techs = i.Techs.Select((t, ix) => new TechDto
            {
                Name = t.Name, Note = t.Note,
                Dot = ix % 2 == 0 ? "var(--acc)" : "var(--acc2)"
            }).ToList(),

            Games = i.Games.Select((g, ix) =>
            {
                var (c1, c2) = KapakRenkleri(i, g.Cover);
                return new GameDto
                {
                    No = (ix + 1).ToString("00"),
                    Name = g.Name, Url = g.Url, Image = g.Image,
                    CoverFrom = c1, CoverTo = c2,
                    Desc = g.Desc.Get(lang)
                };
            }).ToList(),

            Demos = i.Demos.Select((d, ix) => new DemoDto
            {
                Index = ix,
                Path = d.Path, Name = d.Name, Tags = d.Tags,
                Desc = d.Desc.Get(lang),
                Html = string.IsNullOrWhiteSpace(d.Html) ? DemoStub(d.Name) : d.Html
            }).ToList(),

            Repos = i.Repos.Select(r => new RepoDto
            {
                Name = r.Name, Lang = r.Lang, Updated = r.Updated,
                Url = string.IsNullOrWhiteSpace(r.Url) ? $"{i.Meta.Github}/{r.Name}" : r.Url,
                Desc = r.Desc.Get(lang),
                Dot = DilNoktasi(r.Lang)
            }).ToList()
        };
    }

    /// <summary>
    /// Kopya sözlüğü: önce TR (taban), sonra istenen dilin DOLU değerleri üzerine yazılır.
    /// Böylece eksik/boş çeviri otomatik TR'ye düşer — anahtar hiç kaybolmaz.
    /// </summary>
    private static Dictionary<string, string> KopyaCoz(SiteContent i, string lang)
    {
        var sonuc = new Dictionary<string, string>(StringComparer.Ordinal);

        if (i.I18n.TryGetValue(Lang.Default, out var taban))
            foreach (var (k, v) in taban) sonuc[k] = v;

        if (lang != Lang.Default && i.I18n.TryGetValue(lang, out var ust))
            foreach (var (k, v) in ust)
                if (!string.IsNullOrWhiteSpace(v)) sonuc[k] = v;

        return sonuc;
    }

    /// <summary>Terminal satır rengi ilk karakterden türetilir (site-data.js → logColor).</summary>
    private static string LogSinifi(string satir)
    {
        var s = satir.TrimStart();
        if (s.StartsWith('✓')) return "term__line--ok";
        if (s.StartsWith('→')) return "term__line--out";
        if (s.StartsWith("$ open", StringComparison.Ordinal)) return "term__line--acc";
        if (s.StartsWith('$')) return "term__line--cmd";
        return "";
    }

    private static (string, string) KapakRenkleri(SiteContent i, string ad)
    {
        if (i.Covers.TryGetValue(ad ?? "", out var c) && c.Count >= 2) return (c[0], c[1]);
        if (i.Covers.TryGetValue("violet", out var v) && v.Count >= 2) return (v[0], v[1]);
        return ("oklch(0.42 0.15 300)", "oklch(0.26 0.09 280)");   // son çare
    }

    private static string DilNoktasi(string dil) => (dil ?? "").ToLowerInvariant() switch
    {
        "c#" => "var(--acc)",
        "php" => "var(--acc2)",
        "javascript" => "oklch(0.8 0.15 95)",
        "html" or "html / css" => "oklch(0.7 0.16 25)",
        "css" => "oklch(0.74 0.14 210)",
        _ => "var(--acc2)"
    };

    /// <summary>Boş demo için yer tutucu sayfa (site-data.js → demoStub birebir).</summary>
    /// <remarks>Admin önizlemesi de aynı stub'ı kullanır → tek kaynak.</remarks>
    internal static string DemoStubPublic(string baslik) => DemoStub(baslik);

    private static string DemoStub(string baslik) =>
        "<!doctype html><html><head><meta charset=\"utf-8\"><style>\n" +
        " body{margin:0;height:100vh;display:flex;align-items:center;justify-content:center;background:#14131c;color:#e8e8ef;font-family:ui-monospace,SFMono-Regular,monospace}\n" +
        " .c{text-align:center;padding:42px 46px;border:1px solid rgba(255,255,255,.14);border-radius:18px;background:rgba(255,255,255,.03)}\n" +
        " h1{margin:0 0 12px;font-size:18px;letter-spacing:.08em;color:#c9a6ff}\n" +
        " p{margin:0;font-size:13px;line-height:1.8;color:#8f8fa3}\n" +
        "</style></head><body><div class=\"c\"><h1>" + System.Net.WebUtility.HtmlEncode(baslik) +
        "</h1><p>Bu demo için HTML/JS henüz yapıştırılmadı.<br/>Admin panel → Demolar → HTML/JS alanı.</p></div></body></html>";
}
