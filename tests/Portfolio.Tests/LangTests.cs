using Portfolio.Entities;

namespace Portfolio.Tests;

/// <summary>
/// Dil kuralları. Kritik nokta: <b>Fallback ile Initial farklı kavramlardır.</b>
/// Biri içeriğin yazıldığı dil (eksik çeviri buraya düşer), diğeri ilk ziyaretçiye
/// açılan dil. Bu testler ikisinin yanlışlıkla birleştirilmesini engeller.
/// </summary>
public class LangTests
{
    [Fact]
    public void Fallback_TR_dir_cunku_icerik_turkce_yaziliyor()
        => Assert.Equal(Lang.Tr, Lang.Fallback);

    [Fact]
    public void Initial_EN_dir_ilk_ziyaretci_ingilizce_gorur()
        => Assert.Equal(Lang.En, Lang.Initial);

    [Fact]
    public void Fallback_ve_Initial_AYRI_kavramlardir()
    {
        // Bu ikisi eşitlenirse ya ilk ziyaretçi Türkçe görür ya da eksik EN
        // çevirileri boş kalır. Bilinçli olarak farklılar.
        Assert.NotEqual(Lang.Fallback, Lang.Initial);
    }

    [Theory]
    [InlineData("tr")] [InlineData("en")] [InlineData("ru")]
    [InlineData("TR")] [InlineData(" en ")]
    public void Gecerli_desteklenen_dilleri_taniyor(string kod) => Assert.True(Lang.Gecerli(kod));

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("  ")]
    [InlineData("zz")] [InlineData("de")] [InlineData("tr-TR")] [InlineData("../etc")]
    public void Gecerli_desteklenmeyeni_reddediyor(string? kod) => Assert.False(Lang.Gecerli(kod));

    [Theory]
    [InlineData("EN", "en")] [InlineData(" ru ", "ru")] [InlineData("tr", "tr")]
    public void Normalize_gecerli_kodu_kucuk_harfe_indiriyor(string girdi, string beklenen)
        => Assert.Equal(beklenen, Lang.Normalize(girdi));

    [Theory]
    [InlineData("zz")] [InlineData(null)] [InlineData("javascript:alert(1)")]
    public void Normalize_bilinmeyeni_FALLBACK_e_dusuruyor(string? girdi)
    {
        // İçerik çözümlemesinde kullanılır: sözlük anahtarı asla ham girdi olmaz.
        Assert.Equal(Lang.Fallback, Lang.Normalize(girdi));
    }

    [Fact]
    public void All_uc_dili_gosterim_sirasiyla_veriyor()
        => Assert.Equal(new[] { "tr", "en", "ru" }, Lang.All);
}
