namespace Portfolio.Entities.Content;

/// <summary>
/// Sitenin tüm içeriği — tek kök nesne.
/// Şema kaynağı: design_handoff_portfolio/source-dc/site-data.js → SEED (tek doğruluk kaynağı).
/// Faz 4'te aynı model EF Core ile SQL'e taşınacak (bkz. vault wiki/content-model.md).
/// </summary>
public sealed class SiteContent
{
    public SiteMeta Meta { get; set; } = new();

    /// <summary>Dil kodu → (anahtar → metin). Örn. I18n["tr"]["heroLead"].</summary>
    public Dictionary<string, Dictionary<string, string>> I18n { get; set; } = new();

    /// <summary>Dil kodu → typewriter rol listesi (dile bağlı, dizi olduğu için I18n'den ayrı).</summary>
    public Dictionary<string, List<string>> Roles { get; set; } = new();

    /// <summary>Terminal mockup satırları — dilden BAĞIMSIZ.</summary>
    public List<string> Logs { get; set; } = new();

    public List<Fact> Facts { get; set; } = new();
    public List<Tech> Techs { get; set; } = new();
    public List<Game> Games { get; set; } = new();
    public List<Demo> Demos { get; set; } = new();
    public List<RepoItem> Repos { get; set; } = new();

    /// <summary>Kapak degrade preset'leri: ad → [üst renk, alt renk]. Görsel yoksa kullanılır.</summary>
    public Dictionary<string, List<string>> Covers { get; set; } = new();
}

/// <summary>Dilden bağımsız site künyesi.</summary>
public sealed class SiteMeta
{
    public string Name { get; set; } = "";
    public string Handle { get; set; } = "";
    public string Mail { get; set; } = "";
    public string Github { get; set; } = "";
    public string GithubUser { get; set; } = "";
    public string Play { get; set; } = "";
}

/// <summary>
/// Üç dilli metin. Eksik dil TR'ye düşer — prototipin davranışı da bu.
/// </summary>
public sealed class Localized
{
    public string Tr { get; set; } = "";
    public string En { get; set; } = "";
    public string Ru { get; set; } = "";

    /// <summary>İstenen dildeki metin; boşsa TR'ye düşer.</summary>
    public string Get(string lang)
    {
        var deger = lang switch
        {
            Lang.En => En,
            Lang.Ru => Ru,
            _ => Tr
        };
        return string.IsNullOrWhiteSpace(deger) ? Tr : deger;
    }
}

/// <summary>Sayaç kutusu: değer dilden bağımsız, etiket dile bağlı.</summary>
public sealed class Fact
{
    public int Value { get; set; }
    public Localized Label { get; set; } = new();
}

/// <summary>Teknoloji çipi — ad ve not dilden BAĞIMSIZ (SEED kararı).</summary>
public sealed class Tech
{
    public string Name { get; set; } = "";
    public string Note { get; set; } = "";
}

public sealed class Game
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    /// <summary>URL ya da statik dosya yolu. Boşsa <see cref="Cover"/> degradesi gösterilir.</summary>
    public string Image { get; set; } = "";
    /// <summary>Kapak preset adı (violet/cyan/magenta/mint/amber).</summary>
    public string Cover { get; set; } = "violet";
    public Localized Desc { get; set; } = new();
}

public sealed class Demo
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    /// <summary>
    /// Tek dosya HTML/JS — KULLANICI İÇERİĞİ. Asla sayfaya gömülmez;
    /// yalnız iframe içinde, sandbox altında çalıştırılır (bkz. vault workflow-rules §3).
    /// </summary>
    public string Html { get; set; } = "";
    public Localized Desc { get; set; } = new();
}

/// <summary>GitHub deposu. (Adı Repo değil RepoItem — "Repository" katman adıyla karışmasın.)</summary>
public sealed class RepoItem
{
    public string Name { get; set; } = "";
    /// <summary>Programlama dili (dilden bağımsız, i18n ile ilgisi yok).</summary>
    public string Lang { get; set; } = "";
    public string Updated { get; set; } = "";
    public string Url { get; set; } = "";
    public Localized Desc { get; set; } = new();
}
