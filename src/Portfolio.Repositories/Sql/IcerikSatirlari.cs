using Portfolio.Entities.Content;

namespace Portfolio.Repositories.Sql;

/// <summary>
/// İçeriğin **satır hâli** — tabloların bellek içi karşılığı, ve iki yöne dönüşüm.
///
/// <para>
/// Neden ayrı sınıf: dönüşümün riskli kısmı SQL değil, <b>eşleme</b> —
/// sıralamanın (<c>Order</c>) korunması, i18n'in dile göre gruplanması,
/// kapak renklerinin çift olarak geri kurulması, etiket listesi. Bunlar
/// DbContext'ten bağımsız saf fonksiyonlarda durursa **veritabanı olmadan**
/// test edilebilir; testlerin sunucudaki mssql'e (ve RAM'ine) ihtiyacı kalmaz.
/// <see cref="SqlContentStore"/> geriye yalnız okuma/yazma işini yapar.
/// </para>
/// </summary>
public sealed class IcerikSatirlari
{
    public SiteMetaRow? Meta { get; set; }
    public List<CopyEntry> Copy { get; set; } = new();
    public List<HeroRoleRow> HeroRoles { get; set; } = new();
    public List<TerminalLogRow> Logs { get; set; } = new();
    public List<FactRow> Facts { get; set; } = new();
    public List<TechRow> Techs { get; set; } = new();
    public List<GameRow> Games { get; set; } = new();
    public List<DemoRow> Demos { get; set; } = new();
    public List<RepoRow> Repos { get; set; } = new();
    public List<CoverPresetRow> Covers { get; set; } = new();

    /// <summary>
    /// Çeviri nesnesinin KOPYASI. EF'te "owned" bir örnek iki sahibe ait olamaz;
    /// çağıran aynı <see cref="Localized"/> örneğini iki kayıtta paylaşmışsa
    /// (JSON içe aktarma sonrası olabilir) kopyalamadan eklemek çalışma anında patlar.
    /// </summary>
    private static Localized Kopya(Localized? k) =>
        k is null ? new Localized() : new Localized { Tr = k.Tr, En = k.En, Ru = k.Ru };

    /// <summary>SiteContent → satırlar. Liste sırası <c>Order</c> kolonuna yazılır.</summary>
    public static IcerikSatirlari Ayristir(SiteContent i)
    {
        ArgumentNullException.ThrowIfNull(i);
        var s = new IcerikSatirlari
        {
            Meta = new SiteMetaRow
            {
                Id = 1,
                Name = i.Meta.Name,
                Handle = i.Meta.Handle,
                Mail = i.Meta.Mail,
                Github = i.Meta.Github,
                GithubUser = i.Meta.GithubUser,
                Play = i.Meta.Play
            }
        };

        foreach (var (dil, sozluk) in i.I18n)
            foreach (var (anahtar, deger) in sozluk)
                s.Copy.Add(new CopyEntry { Key = anahtar, Lang = dil, Value = deger ?? "" });

        foreach (var (dil, roller) in i.Roles)
            for (var n = 0; n < roller.Count; n++)
                s.HeroRoles.Add(new HeroRoleRow { Lang = dil, Order = n, Text = roller[n] ?? "" });

        for (var n = 0; n < i.Logs.Count; n++)
            s.Logs.Add(new TerminalLogRow { Order = n, Text = i.Logs[n] ?? "" });

        for (var n = 0; n < i.Facts.Count; n++)
            s.Facts.Add(new FactRow { Order = n, Value = i.Facts[n].Value, Label = Kopya(i.Facts[n].Label) });

        for (var n = 0; n < i.Techs.Count; n++)
            s.Techs.Add(new TechRow { Order = n, Name = i.Techs[n].Name, Note = i.Techs[n].Note });

        for (var n = 0; n < i.Games.Count; n++)
        {
            var o = i.Games[n];
            s.Games.Add(new GameRow
            {
                Order = n,
                Name = o.Name,
                Url = o.Url,
                Image = o.Image,
                Cover = o.Cover,
                Desc = Kopya(o.Desc)
            });
        }

        for (var n = 0; n < i.Demos.Count; n++)
        {
            var o = i.Demos[n];
            s.Demos.Add(new DemoRow
            {
                Order = n,
                Path = o.Path,
                Name = o.Name,
                Tags = new List<string>(o.Tags),
                Html = o.Html,
                Desc = Kopya(o.Desc)
            });
        }

        for (var n = 0; n < i.Repos.Count; n++)
        {
            var o = i.Repos[n];
            s.Repos.Add(new RepoRow
            {
                Order = n,
                Name = o.Name,
                Lang = o.Lang,
                Updated = o.Updated,
                Url = o.Url,
                Desc = Kopya(o.Desc)
            });
        }

        var sira = 0;
        foreach (var (ad, renkler) in i.Covers)
        {
            s.Covers.Add(new CoverPresetRow
            {
                Order = sira++,
                Name = ad,
                Top = renkler.ElementAtOrDefault(0) ?? "",
                Bottom = renkler.ElementAtOrDefault(1) ?? ""
            });
        }

        return s;
    }

    /// <summary>
    /// Satırlar → SiteContent. Listeler <c>Order</c>'a göre sıralanır; DB'den
    /// gelen sıra garanti değildir, sıralamayı burada uyguluyoruz.
    /// </summary>
    public SiteContent Birlestir()
    {
        var i = new SiteContent();

        if (Meta is not null)
        {
            i.Meta = new SiteMeta
            {
                Name = Meta.Name,
                Handle = Meta.Handle,
                Mail = Meta.Mail,
                Github = Meta.Github,
                GithubUser = Meta.GithubUser,
                Play = Meta.Play
            };
        }

        foreach (var satir in Copy)
        {
            if (!i.I18n.TryGetValue(satir.Lang, out var sozluk))
                i.I18n[satir.Lang] = sozluk = new Dictionary<string, string>();
            sozluk[satir.Key] = satir.Value;
        }

        foreach (var grup in HeroRoles.OrderBy(x => x.Order).GroupBy(x => x.Lang))
            i.Roles[grup.Key] = grup.Select(x => x.Text).ToList();

        i.Logs = Logs.OrderBy(x => x.Order).Select(x => x.Text).ToList();

        i.Facts = Facts.OrderBy(x => x.Order)
            .Select(x => new Fact { Value = x.Value, Label = Kopya(x.Label) }).ToList();

        i.Techs = Techs.OrderBy(x => x.Order)
            .Select(x => new Tech { Name = x.Name, Note = x.Note }).ToList();

        i.Games = Games.OrderBy(x => x.Order).Select(x => new Game
        {
            Name = x.Name,
            Url = x.Url,
            Image = x.Image,
            Cover = x.Cover,
            Desc = Kopya(x.Desc)
        }).ToList();

        i.Demos = Demos.OrderBy(x => x.Order).Select(x => new Demo
        {
            Path = x.Path,
            Name = x.Name,
            Tags = new List<string>(x.Tags),
            Html = x.Html,
            Desc = Kopya(x.Desc)
        }).ToList();

        i.Repos = Repos.OrderBy(x => x.Order).Select(x => new RepoItem
        {
            Name = x.Name,
            Lang = x.Lang,
            Updated = x.Updated,
            Url = x.Url,
            Desc = Kopya(x.Desc)
        }).ToList();

        foreach (var kapak in Covers.OrderBy(x => x.Order))
            i.Covers[kapak.Name] = new List<string> { kapak.Top, kapak.Bottom };

        return i;
    }
}
