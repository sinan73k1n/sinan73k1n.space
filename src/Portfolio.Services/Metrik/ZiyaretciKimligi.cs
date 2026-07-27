using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Services.Metrik;

/// <summary>
/// Çerezsiz ziyaretçi kimliği (karar: Sinan, 2026-07-27).
///
/// <para>
/// <b>Nasıl:</b> kimlik = SHA-256(IP + tarayıcı imzası + <i>o güne özel rastgele tuz</i>),
/// ilk 16 bayt hex. Tuz her gece değişir.
/// </para>
/// <para>
/// <b>Neden böyle:</b> üç şeyi aynı anda istiyoruz — (1) aynı kişi gün içinde bir kez
/// sayılsın, (2) ham IP hiç saklanmasın, (3) çerez onay banneri gerekmesin.
/// Günlük dönen tuz üçünü de veriyor: gün içinde kimlik sabit, gün dönünce aynı kişi
/// başka bir kimliğe düşüyor. Yani <b>günler arası takip yapısal olarak mümkün değil</b>,
/// "unutmayı seçmek" değil — unutmaktan başka seçenek yok.
/// </para>
/// <para>
/// ⚠️ Tuz olmadan da hash geri döndürülemez ama IP uzayı küçük olduğu için
/// <b>kaba kuvvetle</b> denenebilirdi (4 milyar IPv4 × birkaç UA = ucuz). Tuz bunu
/// imkânsız kılıyor; tuz ham veriyle birlikte silindiğinde eski hash'ler kalıcı
/// olarak anonim hâle geliyor.
/// </para>
/// </summary>
public static class ZiyaretciKimligi
{
    /// <summary>Yeni bir günlük tuz üretir (256 bit, kriptografik rastgelelik).</summary>
    public static string TuzUret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Günlük kimliği hesaplar.
    /// </summary>
    /// <param name="ip">İstemci IP'si (ters vekilden gelen gerçek IP).</param>
    /// <param name="tarayici">User-Agent. Yoksa boş geçilebilir.</param>
    /// <param name="gunlukTuz">O güne ait tuz.</param>
    public static string Hesapla(string? ip, string? tarayici, string gunlukTuz)
    {
        // Ayraç ("|") şart: "1.2.3.4" + "5..." ile "1.2.3.45" + "..." aynı girdiye
        // düşmesin. Ayraçsız birleştirme farklı ziyaretçileri birleştirebilirdi.
        var girdi = $"{ip}|{tarayici}|{gunlukTuz}";
        var ozet = SHA256.HashData(Encoding.UTF8.GetBytes(girdi));

        // 16 bayt = 32 hex karakter. Çakışma olasılığı bu ölçekte ihmal edilebilir,
        // ama tam hash'i saklamamak veri en aza indirme ilkesine uyuyor.
        return Convert.ToHexString(ozet.AsSpan(0, 16));
    }
}
