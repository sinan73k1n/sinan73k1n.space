using Portfolio.Entities.Content;

namespace Portfolio.Repositories.Sql;

/// <summary>
/// MSSQL satır tipleri (Faz 4). Bunlar **kalıcılık modelidir**, alan adları
/// tabloya bakar; sitenin gördüğü model <see cref="SiteContent"/>'tir.
/// Çeviri, dönüşüm ve sıralama <see cref="SqlContentStore"/>'da yapılır —
/// böylece view/servis/admin katmanı DB'yi hiç görmez.
///
/// <para>
/// ⚠️ ŞEMA KARARI (2026-07-27, vault content-model.md'deki ilk planın revizyonu):
/// Fact/Game/Demo/Repo açıklamalarının çevirileri **sahip satırın kolonlarında**
/// durur (owned type → DescTr/DescEn/DescRu). İlk plan ayrı bir polimorfik
/// `LocalizedText(OwnerType, OwnerId, …)` tablosuydu; gerekçesi "dil eklemek
/// şema değiştirmesin"di. Ama <see cref="Localized"/> zaten üç alanda SABİT:
/// 4. dil eklemek Localized.Get'i, Lang'i, admin sekmelerini ve fallback'i
/// nasılsa değiştiriyor. Buna karşılık polimorfik tablo FOREIGN KEY kurmayı
/// imkânsız kılıyordu (yetim çeviri satırı). Kolon yaklaşımında yetim satır
/// **fiziksel olarak mümkün değil** ve okuma tek sorgu.
/// </para>
/// <para>
/// Bu revizyon <c>i18n</c> sözlüğünü KAPSAMAZ: orada anahtar kümesi dinamik
/// olduğu için satır bazlı <see cref="CopyEntry"/> doğru kalıp.
/// </para>
/// </summary>
public sealed class SiteMetaRow
{
    /// <summary>Her zaman 1 — künye tek satırdır (tablo, tekil kayıt).</summary>
    public int Id { get; set; } = 1;

    public string Name { get; set; } = "";
    public string Handle { get; set; } = "";
    public string Mail { get; set; } = "";
    public string Github { get; set; } = "";
    public string GithubUser { get; set; } = "";
    public string Play { get; set; } = "";
}

/// <summary>Sitenin sabit kopyası: (Anahtar, Dil) → metin. Çift anahtar UNIQUE.</summary>
public sealed class CopyEntry
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Lang { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>Typewriter rol listesi — dile bağlı olduğu için CopyEntry'e sığmaz.</summary>
public sealed class HeroRoleRow
{
    public int Id { get; set; }
    public string Lang { get; set; } = "";
    public int Order { get; set; }
    public string Text { get; set; } = "";
}

/// <summary>Terminal mockup satırı — dilden BAĞIMSIZ (SEED kararı).</summary>
public sealed class TerminalLogRow
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = "";
}

public sealed class FactRow
{
    public int Id { get; set; }
    public int Order { get; set; }
    public int Value { get; set; }
    public Localized Label { get; set; } = new();
}

public sealed class TechRow
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = "";
    public string Note { get; set; } = "";
}

public sealed class GameRow
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";

    /// <summary>Statik dosya yolu ya da URL. Prototipteki data URL production'a taşınmaz.</summary>
    public string Image { get; set; } = "";

    public string Cover { get; set; } = "violet";
    public Localized Desc { get; set; } = new();
}

public sealed class DemoRow
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>EF Core ilkel koleksiyonu → JSON kolonu. Ayrı tablo etmeyecek kadar küçük.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// KULLANICI İÇERİĞİ, nvarchar(max). Kaydederken sanitize EDİLMEZ;
    /// güvenlik sınırı çalıştırma anında (ayrı origin + sandbox).
    /// </summary>
    public string Html { get; set; } = "";

    public Localized Desc { get; set; } = new();
}

/// <summary>GitHub deposu. <c>Lang</c> = programlama dili, i18n ile ilgisi YOK.</summary>
public sealed class RepoRow
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = "";
    public string Lang { get; set; } = "";
    public string Updated { get; set; } = "";
    public string Url { get; set; } = "";
    public Localized Desc { get; set; } = new();
}

/// <summary>Kapak degrade preset'i (violet/cyan/…): görsel yoksa gösterilen renk çifti.</summary>
public sealed class CoverPresetRow
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = "";
    public string Top { get; set; } = "";
    public string Bottom { get; set; } = "";
}
