#!/usr/bin/env bash
# tools/depo-denetim.sh — bir depoyu HERKESE AÇMADAN ÖNCE denetler.
#
# NEDEN VAR: bu depo public yapıldıktan sonra içinde `.claude/hooks` (her prompt'u
# yerel kasaya yazan betikler), `CLAUDE.md` (kasa yolu + makine adı) ve mutlak bir
# `/Users/<ad>/...` yolu bulundu. Hiçbiri kimlik bilgisi değildi — o yüzden klasik
# "sır taraması" temiz sonuç veriyordu. Yanlış soru sorulmuştu: mesele "sır var mı"
# değil, "buradaki her şey herkese açık olmalı mı".
#
# Bu betik İKİ soruyu birden sorar ve ÇALIŞAN AĞACA + TÜM GEÇMİŞE bakar. Geçmiş
# önemli: dosyayı silmek onu eski commit'lerden silmez.
#
# KULLANIM:
#   ./tools/depo-denetim.sh              # bulunduğun depo
#   ./tools/depo-denetim.sh ~/kod/proje  # başka bir depo
#
# ÇIKIŞ KODU: bulgu varsa 1, temizse 0.

set -uo pipefail
KOK="${1:-.}"
cd "$KOK" || { echo "Dizin yok: $KOK"; exit 2; }
git rev-parse --git-dir >/dev/null 2>&1 || { echo "Burası bir git deposu değil: $KOK"; exit 2; }

BULGU=0
baslik() { printf "\n\033[1m%s\033[0m\n" "$1"; }
sorun()  { printf "  \033[31m✗\033[0m %s\n" "$1"; BULGU=1; }
tamam()  { printf "  \033[32m✓\033[0m %s\n" "$1"; }
uyari()  { printf "  \033[33m!\033[0m %s\n" "$1"; }

# --- 1) KİMLİK BİLGİSİ: sızarsa biri içeri girer ------------------------------
SIR='pbkdf2\$[A-Za-z0-9+/]{8,}|ghp_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|xox[baprs]-[A-Za-z0-9-]{10,}|AKIA[0-9A-Z]{16}|BEGIN [A-Z ]*PRIVATE KEY|(Password|Pwd)=[^;"'"'"' ]{4,}|api[_-]?key["'"'"' :=]+[A-Za-z0-9_\-]{16,}'
baslik "1) Kimlik bilgisi (parola · token · anahtar · bağlantı dizesi)"
if git grep -nEI "$SIR" -- . >/tmp/dd.$$ 2>/dev/null && [ -s /tmp/dd.$$ ]; then
  sorun "çalışan ağaçta eşleşme:"; sed 's/^/      /' /tmp/dd.$$ | head -10
else tamam "çalışan ağaç temiz"; fi
rm -f /tmp/dd.$$

# --- 2) KİŞİSEL ÇALIŞMA DÜZENİ: sızarsa mahremiyet gider ---------------------
# Mutlak ev dizini yolları, yapay zekâ araç klasörleri, ikinci beyin/kasa yolları.
KISI='/Users/[a-z0-9._-]+/|/home/[a-z0-9._-]+/|C:\\\\Users\\\\|GenelClaude|Obsidian|second-brain|ikinci beyin'
baslik "2) Kişisel yol ve çalışma düzeni"
if git grep -nEI "$KISI" -- . >/tmp/dd.$$ 2>/dev/null && [ -s /tmp/dd.$$ ]; then
  sorun "mutlak/kişisel yol geçiyor:"; sed 's/^/      /' /tmp/dd.$$ | head -10
else tamam "mutlak ev dizini yolu yok"; fi
rm -f /tmp/dd.$$

for yol in .claude .cursor .aider* .env .envrc .vscode/settings.json CLAUDE.md AGENTS.md .DS_Store; do
  if git ls-files --error-unmatch "$yol" >/dev/null 2>&1 || [ -n "$(git ls-files "$yol" 2>/dev/null)" ]; then
    sorun "takip ediliyor ama depoya ait değil: $yol"
  fi
done

# --- 3) ALTYAPI İZİ: makine adı, özel IP, port -------------------------------
# ⚠️ Makine adlarını KENDİ ortamına göre genişlet.
MAKINE='servernath|@[a-z0-9-]+\.local'
IP4='([^0-9.]|^)(10\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}|192\.168\.[0-9]{1,3}\.[0-9]{1,3}|172\.(1[6-9]|2[0-9]|3[01])\.[0-9]{1,3}\.[0-9]{1,3}|100\.(6[4-9]|[7-9][0-9]|1[0-1][0-9]|12[0-7])\.[0-9]{1,3}\.[0-9]{1,3})([^0-9.]|$)'
ALTYAPI="$MAKINE|/home/[a-z][a-z0-9_-]*/|$IP4"
baslik "3) Altyapı izi (makine adı · özel ağ IP'si)"
if git grep -nEI "$ALTYAPI" -- . ":!demos/" ":!*Tests*" >/tmp/dd.$$ 2>/dev/null && [ -s /tmp/dd.$$ ]; then
  sorun "eşleşme:"; sed 's/^/      /' /tmp/dd.$$ | head -10
else tamam "makine adı / özel IP yok (demolar ve testler hariç: oradakiler bilerek kurgu)"; fi
rm -f /tmp/dd.$$

# --- 4) GEÇMİŞ: silmek yetmez, eski commit'ler duruyor -----------------------
baslik "4) Geçmiş (silinen dosya eski commit'lerde kalır)"
N=$(git rev-list --count HEAD 2>/dev/null || echo 0)
GECMIS=0
while read -r c; do
  if git grep -lIE "$SIR|$KISI|$MAKINE" "$c" -- . ":!demos/" ":!*Tests*" 2>/dev/null | head -1 | grep -q .; then
    GECMIS=$((GECMIS+1))
  fi
done < <(git rev-list HEAD 2>/dev/null)
if [ "$GECMIS" -gt 0 ]; then
  sorun "$N commit'in $GECMIS tanesinde iz var — dosyayı silmek yetmez, geçmiş yeniden yazılmalı"
  uyari "temizlik: git filter-branch (ya da git-filter-repo) + force-push; fork/yıldız yokken maliyeti düşüktür"
else tamam "$N commit'in hiçbirinde iz yok"; fi

# --- 5) Herkese açık bir depoda beklenenler ----------------------------------
baslik "5) Sunum"
[ -n "$(git ls-files 'README*' 2>/dev/null)" ] && tamam "README var" || sorun "README yok — kart tıklanınca çıplak dosya ağacı görünür"
[ -n "$(git ls-files 'LICENSE*' 2>/dev/null)" ] && tamam "LICENSE var" || uyari "LICENSE yok (bilinçli bir tercih olabilir)"
BUYUK=$(git ls-files -z | xargs -0 du -k 2>/dev/null | awk '$1>5000{print $2" ("int($1/1024)"MB)"}' | head -5)
[ -n "$BUYUK" ] && { uyari "5 MB üstü dosyalar:"; echo "$BUYUK" | sed 's/^/      /'; } || tamam "aşırı büyük dosya yok"

baslik "SONUÇ"
[ "$BULGU" -eq 0 ] && { tamam "herkese açmaya hazır"; exit 0; } || { sorun "yukarıdakiler çözülmeden açma"; exit 1; }
