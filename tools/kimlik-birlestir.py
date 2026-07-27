#!/usr/bin/env python3
"""
Yeni kimlik bilgilerini mevcut auth.env'e BİRLEŞTİRİR (üzerine yazmaz).

Neden gerekli: `--setup-auth` yalnız 3 anahtar üretiyor (Username/PasswordHash/
TotpSecret) ama /etc/portfolio/auth.env'de dördüncü bir anahtar daha var:
ConnectionStrings__Portfolio. Dosyayı olduğu gibi kopyalamak o satırı siler ve
uygulama sessizce JSON deposuna düşer — hata vermeden yanlış içerik sunar.

Yazmadan önce doğrular; bir şey eksikse HİÇBİR ŞEY yazmaz.

⚠️ /etc/portfolio KLASÖRÜ root'a ait: servis kullanıcısı oradaki mevcut dosyaya yazabilir ama
   YENİ dosya oluşturamaz. Bu yüzden yedek ev dizinine alınır ve hedef dosyaya
   yerinde yazılır (geçici dosya + rename yapılamaz).
"""
import os
import sys

YENI = os.path.expanduser("~/deploy-staging/portfolio-auth.env")
HEDEF = "/etc/portfolio/auth.env"
BEKLENEN = {"Auth__Username", "Auth__PasswordHash", "Auth__TotpSecret", "ConnectionStrings__Portfolio"}


def oku(yol):
    d = {}
    with open(yol) as f:
        for satir in f:
            satir = satir.strip()
            if not satir or satir.startswith("#") or "=" not in satir:
                continue
            k, v = satir.split("=", 1)
            d[k] = v
    return d


mevcut = oku(HEDEF)
yeni = oku(YENI)

birlesik = dict(mevcut)
birlesik.update(yeni)          # kimlik alanları yenisiyle değişir
                               # ConnectionStrings__Portfolio mevcuttan KORUNUR

eksik = BEKLENEN - set(birlesik)
if eksik:
    print("✗ DURDURULDU — şu anahtarlar eksik:", ", ".join(sorted(eksik)))
    print("  Dosyaya DOKUNULMADI.")
    sys.exit(1)

for k, v in birlesik.items():
    if not v.strip():
        print(f"✗ DURDURULDU — {k} boş. Dosyaya DOKUNULMADI.")
        sys.exit(1)

# Yedek EV DİZİNİNE (o klasörde yeni dosya oluşturulamıyor)
yedek = os.path.expanduser("~/auth.env.onceki")
with open(HEDEF) as kaynak, open(yedek, "w") as hedef:
    hedef.write(kaynak.read())
os.chmod(yedek, 0o600)

# İçeriği ÖNCE tam olarak hazırla, sonra tek seferde yaz: dosya açıkken
# hesaplama yapıp yarıda kalırsak kimlik dosyası bozulurdu.
icerik = "".join(
    f"{k}={birlesik[k]}\n"
    for k in ("Auth__Username", "Auth__PasswordHash", "Auth__TotpSecret", "ConnectionStrings__Portfolio")
)
with open(HEDEF, "w") as f:
    f.write(icerik)
os.chmod(HEDEF, 0o600)

print("✓ Birleştirildi. Dosyadaki anahtarlar:")
for k in oku(HEDEF):
    print("   ", k)
print()
print("  Kimlik alanları YENİLENDİ, bağlantı dizesi KORUNDU.")
print("  Eski hâli:", yedek)
