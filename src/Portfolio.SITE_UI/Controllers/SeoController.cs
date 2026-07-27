using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Entities;

namespace Portfolio.SITE_UI.Controllers;

/// <summary>
/// Arama motoru dosyaları (Faz 7). Statik dosya olarak değil ELDE üretiliyorlar
/// çünkü ikisi de adrese bağlı: site tek sayfa + <c>?lang=</c> ile üç dil, ve
/// aynı uygulama iki alan adından (ana site + demo origin) yanıt veriyor.
/// </summary>
public class SeoController : Controller
{
    private readonly DemoOrigin _demoOrigin;

    public SeoController(DemoOrigin demoOrigin) => _demoOrigin = demoOrigin;

    /// <summary>İsteğin geldiği origin — ters vekilin şemasıyla (UseForwardedHeaders).</summary>
    private string Origin() => $"{Request.Scheme}://{Request.Host.Value}";

    /// <summary>
    /// İstek demo alan adına mı geldi? Demo origin'in indekslenecek hiçbir şeyi yok
    /// (orada <c>/d/{n}</c> dışında her şey zaten 404, o da <c>X-Robots-Tag: noindex</c>).
    /// </summary>
    /// <remarks>
    /// ⚠️ Karşılaştırma TAM host eşitliği olmalı, "biter mi" DEĞİL:
    /// <c>demo.sinan73k1n.space</c> zaten <c>sinan73k1n.space</c> ile biter →
    /// EndsWith kullansaydık ana site de demo sanılır, robots.txt her şeyi kapatırdı.
    /// </remarks>
    private bool DemoAlanAdi() =>
        _demoOrigin.Ayri &&
        Uri.TryCreate(_demoOrigin.Deger, UriKind.Absolute, out var demo) &&
        string.Equals(demo.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase);

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var s = new StringBuilder();
        s.AppendLine("User-agent: *");

        if (DemoAlanAdi())
        {
            // Kullanıcı içeriği çalıştıran alan adı — tamamen dışarıda kalsın.
            s.AppendLine("Disallow: /");
            return Content(s.ToString(), "text/plain; charset=utf-8");
        }

        // Admin public bir adreste duruyor: kimlik doğrulaması onu korur ama
        // arama sonuçlarında görünmesinin de bir faydası yok.
        s.AppendLine("Disallow: /admin");
        s.AppendLine("Disallow: /d/");
        s.AppendLine("Allow: /");
        s.AppendLine();
        s.AppendLine($"Sitemap: {Origin()}/sitemap.xml");

        return Content(s.ToString(), "text/plain; charset=utf-8");
    }

    [HttpGet("/sitemap.xml")]
    public IActionResult Sitemap()
    {
        if (DemoAlanAdi()) return NotFound();

        var origin = Origin();
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        XNamespace xhtml = "http://www.w3.org/1999/xhtml";

        // Site TEK sayfa; "sayfalar" dil varyantları. Her varyant diğerlerini
        // alternate olarak gösterir (Google'ın istediği karşılıklı bağlama),
        // x-default ise dilsiz kök adres — oraya gelen çerez/EN kuralına düşer.
        var kok = new XElement(ns + "urlset",
            new XAttribute(XNamespace.Xmlns + "xhtml", xhtml.NamespaceName));

        foreach (var dil in Lang.All)
        {
            var url = new XElement(ns + "url",
                new XElement(ns + "loc", $"{origin}/?lang={dil}"),
                new XElement(ns + "changefreq", "monthly"),
                new XElement(ns + "priority", dil == Lang.Fallback ? "1.0" : "0.8"));

            foreach (var alt in Lang.All)
            {
                url.Add(new XElement(xhtml + "link",
                    new XAttribute("rel", "alternate"),
                    new XAttribute("hreflang", alt),
                    new XAttribute("href", $"{origin}/?lang={alt}")));
            }

            url.Add(new XElement(xhtml + "link",
                new XAttribute("rel", "alternate"),
                new XAttribute("hreflang", "x-default"),
                new XAttribute("href", $"{origin}/")));

            kok.Add(url);
        }

        var belge = new XDocument(new XDeclaration("1.0", "utf-8", null), kok);
        return Content(belge.Declaration + Environment.NewLine + belge, "application/xml; charset=utf-8");
    }
}
