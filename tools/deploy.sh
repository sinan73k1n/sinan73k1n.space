#!/usr/bin/env bash
# tools/deploy.sh — portfolyoyu sunucuya yayınlar.
#
# NE YAPAR: Release publish (Mac'te) → rsync (sunucu) → servisi yeniden başlat → doğrula.
# NE YAPMAZ: sudo gerektiren ilk kurulum (systemd birimi, nginx, auth env). Onlar bir
# kereliktir ve Sinan kendi terminalinde yapar (bkz. vault wiki/deploy-and-infra.md).
#
# ⛔ KRİTİK: rsync'te --delete YOK.
#    Sunucuda repoda BULUNMAYAN iki şey var ve silinirlerse kaybolurlar:
#      · wwwroot/uploads/  → admin'den yüklenen oyun kapakları
#      · (canlı içerik zaten publish DIŞINDA — yolu systemd'deki Content__FilePath belirler)
#
set -euo pipefail

# ⛔ Sunucu adresi REPOYA YAZILMAZ (kural: vault wiki/workflow-rules §1).
# Değeri depo dışındaki ortam notlarımda
# Kullanım:  PORTFOLIO_SUNUCU=kullanici@adres ./tools/deploy.sh
#   ya da kalıcı olarak:  echo 'export PORTFOLIO_SUNUCU=...' >> ~/.zshrc
: "${PORTFOLIO_SUNUCU:?PORTFOLIO_SUNUCU tanımlı değil. Sunucu adresi repoda tutulmaz; ortam değişkeni olarak ver .}"
SUNUCU="$PORTFOLIO_SUNUCU"
HEDEF="${PORTFOLIO_HEDEF:-~/publish/portfolio/}"
KOK="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CIKTI="$(mktemp -d)/publish"

echo "▶ 1/5  Testler"
dotnet test "$KOK/tests/Portfolio.Tests" --nologo | tail -2

echo "▶ 2/5  Release publish"
dotnet publish "$KOK/src/Portfolio.SITE_UI" -c Release -o "$CIKTI" --nologo | tail -1

echo "▶ 3/5  Sır taraması (publish çıktısında kimlik/sır olmamalı)"
if grep -rlE 'pbkdf2\$|TotpSecret=|Auth__PasswordHash' "$CIKTI" 2>/dev/null; then
  echo "✗ Publish çıktısında sır bulundu — DEPLOY DURDURULDU."; exit 1
fi
echo "  temiz"

echo "▶ 4/5  Kopyalama (--delete YOK)"
rsync -az --stats "$CIKTI/" "$SUNUCU:$HEDEF" | grep -E "Number of files transferred|Total transferred" || true

echo "▶ 5/5  Servisi yeniden başlat + doğrula"
# systemctl restart parola ister (NOPASSWD yok) → Sinan çalıştırır.
echo "  ▶️ Sunucuda çalıştır:  sudo systemctl restart portfolio"
read -r -p "  Yeniden başlatıldıysa Enter'a bas (atlamak için Ctrl-C)… " _

ssh "$SUNUCU" '
  for yol in / /admin /admin/giris /css/variables.css; do
    kod=$(curl -s -o /dev/null -w "%{http_code}" "http://127.0.0.1:5002$yol")
    printf "  %-16s → %s\n" "$yol" "$kod"
  done
  echo "  içerik: $(ls -la ~/portfolio-data/content.json 2>/dev/null | awk "{print \$5\" bayt, \"\$6\" \"\$7\" \"\$8}")"
  echo "  görseller: $(ls ~/publish/portfolio/wwwroot/uploads/games 2>/dev/null | wc -l) dosya (deploy sonrası KORUNMALI)"
'
echo "✔ Bitti."
