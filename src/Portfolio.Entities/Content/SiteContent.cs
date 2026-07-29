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
[System.Text.Json.Serialization.JsonConverter(typeof(LocalizedJsonConverter))]
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

/// <summary>
/// <see cref="Localized"/> okurken DÜZ METNİ de kabul eder: `"note": "oyun motoru"`
/// → <c>Tr</c>'ye yazılır. Neden: teknoloji notu 2026-07-29'da düz metinden üç dilliye
/// geçti; ondan önce alınmış JSON yedekleri (admin → dışa aktar) içe aktarılırken
/// patlamasın. Yazma her zaman nesne biçimindedir.
/// </summary>
public sealed class LocalizedJsonConverter : System.Text.Json.Serialization.JsonConverter<Localized>
{
    public override Localized Read(ref System.Text.Json.Utf8JsonReader okuyucu, Type tip,
                                   System.Text.Json.JsonSerializerOptions secenek)
    {
        if (okuyucu.TokenType == System.Text.Json.JsonTokenType.String)
            return new Localized { Tr = okuyucu.GetString() ?? "" };

        if (okuyucu.TokenType == System.Text.Json.JsonTokenType.Null)
            return new Localized();

        var sonuc = new Localized();
        if (okuyucu.TokenType != System.Text.Json.JsonTokenType.StartObject)
            throw new System.Text.Json.JsonException("Çeviri alanı metin ya da nesne olmalı.");

        while (okuyucu.Read() && okuyucu.TokenType != System.Text.Json.JsonTokenType.EndObject)
        {
            if (okuyucu.TokenType != System.Text.Json.JsonTokenType.PropertyName) continue;
            var ad = okuyucu.GetString() ?? "";
            okuyucu.Read();
            var deger = okuyucu.TokenType == System.Text.Json.JsonTokenType.String ? okuyucu.GetString() ?? "" : "";
            switch (ad.ToLowerInvariant())
            {
                case "tr": sonuc.Tr = deger; break;
                case "en": sonuc.En = deger; break;
                case "ru": sonuc.Ru = deger; break;
            }
        }
        return sonuc;
    }

    public override void Write(System.Text.Json.Utf8JsonWriter yazici, Localized deger,
                               System.Text.Json.JsonSerializerOptions secenek)
    {
        // Anahtar adları seçeneklerdeki adlandırma politikasına uyar (camelCase → tr/en/ru zaten aynı).
        static string Ad(string a, System.Text.Json.JsonSerializerOptions s)
            => s.PropertyNamingPolicy?.ConvertName(a) ?? a;

        yazici.WriteStartObject();
        yazici.WriteString(Ad("Tr", secenek), deger.Tr);
        yazici.WriteString(Ad("En", secenek), deger.En);
        yazici.WriteString(Ad("Ru", secenek), deger.Ru);
        yazici.WriteEndObject();
    }
}

/// <summary>Sayaç kutusu: değer dilden bağımsız, etiket dile bağlı.</summary>
public sealed class Fact
{
    public int Value { get; set; }
    public Localized Label { get; set; } = new();
}

/// <summary>
/// Teknoloji çipi: ad dilden BAĞIMSIZ (Unity/ASP.NET marka adıdır, çevrilmez),
/// not dile BAĞLI — "oyun motoru" EN sayfada "game engine" olmalı (karar: 2026-07-29).
/// Liste tek olduğu için çip SAYISI üç dilde aynıdır; değişen yalnız nottur.
/// </summary>
public sealed class Tech
{
    public string Name { get; set; } = "";
    public Localized Note { get; set; } = new();
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
