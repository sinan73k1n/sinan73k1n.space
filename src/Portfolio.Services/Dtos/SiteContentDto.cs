namespace Portfolio.Services.Dtos;

/// <summary>
/// Sunuma HAZIR içerik: dil çözümlenmiş, fallback uygulanmış, kapak renkleri hesaplanmış.
/// ⛔ Katman kuralı: Controller/View Entity görmez — yalnız bu DTO'ları alır.
/// </summary>
public sealed class SiteContentDto
{
    public string Lang { get; init; } = "";
    public MetaDto Meta { get; init; } = new();

    /// <summary>Çözümlenmiş kopya: anahtar → o dildeki metin (eksikse TR).</summary>
    public IReadOnlyDictionary<string, string> Copy { get; init; } = new Dictionary<string, string>();

    /// <summary>Typewriter rolleri (o dildeki).</summary>
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<LogLineDto> Logs { get; init; } = Array.Empty<LogLineDto>();
    public IReadOnlyList<FactDto> Facts { get; init; } = Array.Empty<FactDto>();
    public IReadOnlyList<TechDto> Techs { get; init; } = Array.Empty<TechDto>();
    public IReadOnlyList<GameDto> Games { get; init; } = Array.Empty<GameDto>();
    public IReadOnlyList<DemoDto> Demos { get; init; } = Array.Empty<DemoDto>();
    public IReadOnlyList<RepoDto> Repos { get; init; } = Array.Empty<RepoDto>();

    /// <summary>Kopya anahtarını getirir; yoksa boş string (view patlamasın).</summary>
    public string T(string anahtar) => Copy.TryGetValue(anahtar, out var v) ? v : "";
}

public sealed class MetaDto
{
    public string Name { get; init; } = "";
    public string Handle { get; init; } = "";
    public string Mail { get; init; } = "";
    public string Github { get; init; } = "";
    public string GithubUser { get; init; } = "";
    public string Play { get; init; } = "";
}

/// <summary>Terminal satırı + rengi (renk ilk karakterden türetilir — site-data.js logColor).</summary>
public sealed class LogLineDto
{
    public string Text { get; init; } = "";
    /// <summary>CSS sınıfı: term__line--cmd / --out / --ok / --acc (boş = sessiz).</summary>
    public string CssClass { get; init; } = "";
}

public sealed class FactDto
{
    public int Value { get; init; }
    public string Label { get; init; } = "";
}

public sealed class TechDto
{
    public string Name { get; init; } = "";
    public string Note { get; init; } = "";
    /// <summary>Nokta rengi — sırayla --acc / --acc2.</summary>
    public string Dot { get; init; } = "";
}

public sealed class GameDto
{
    public string No { get; init; } = "";          // "01", "02" …
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
    public string Image { get; init; } = "";
    public bool HasImage => !string.IsNullOrWhiteSpace(Image);
    /// <summary>Kapak degradesi (görsel yoksa görünür).</summary>
    public string CoverFrom { get; init; } = "";
    public string CoverTo { get; init; } = "";
    public string Desc { get; init; } = "";
}

public sealed class DemoDto
{
    public int Index { get; init; }
    public string Path { get; init; } = "";
    public string Name { get; init; } = "";
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public string Desc { get; init; } = "";
    /// <summary>
    /// Çalıştırılacak HTML. Demo boşsa yer tutucu stub döner (asla boş iframe).
    /// ⚠️ Yalnız `iframe sandbox="allow-scripts"` içine verilir.
    /// </summary>
    public string Html { get; init; } = "";
}

public sealed class RepoDto
{
    public string Name { get; init; } = "";
    public string Lang { get; init; } = "";
    public string Updated { get; init; } = "";
    public string Url { get; init; } = "";
    public string Desc { get; init; } = "";
    /// <summary>Dil noktası rengi.</summary>
    public string Dot { get; init; } = "";
}
