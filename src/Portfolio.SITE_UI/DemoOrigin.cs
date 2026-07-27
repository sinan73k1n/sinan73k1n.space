namespace Portfolio.SITE_UI;

/// <summary>
/// Demoların servis edileceği ayrı origin (örn. <c>https://demo.sinan73k1n.space</c>).
/// Boşsa demolar `srcdoc` ile aynı origin'de ama sandbox'lı çalışır (geliştirme).
/// </summary>
/// <remarks>
/// Neden ayrı origin: `sandbox="allow-scripts"` demoyu izole eder ama tek katmandır.
/// Ayrı origin eklendiğinde tarayıcının **same-origin policy**'si ikinci, bağımsız
/// bir duvar olur — demo kodu ana sitenin DOM'una ve çerezlerine yapısal olarak
/// erişemez. Admin çerezi zaten `__Host-` önekli olduğu için alt alan adına gitmez.
/// </remarks>
public sealed class DemoOrigin(string? deger)
{
    public string? Deger { get; } = string.IsNullOrWhiteSpace(deger) ? null : deger!.TrimEnd('/');
    public bool Ayri => Deger is not null;

    /// <summary>Demo çerçevesinin src'i (ayrı origin kuruluysa).</summary>
    public string Url(int index) => $"{Deger}/d/{index}";
}
