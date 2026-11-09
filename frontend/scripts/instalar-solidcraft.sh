#!/usr/bin/env bash
# Instala o SolidCraft web (WebAssembly) em public/solidcraft.
# Uso: npm run solidcraft:instalar
#
# Por padrão baixa a release oficial e confere o SHA256. Com SOLIDCRAFT_DIST apontando para um build local
# (apps/solidcraft-web/dist, de `trunk build --release --public-url ./`), copia de lá: a ponte do Castwork
# usa document.export/import/saved, que só existem em builds da main depois da 0.30.1.
set -euo pipefail

VERSAO="${SOLIDCRAFT_VERSAO:-0.30.1}"
PACOTE="solidcraft-web-${VERSAO}.zip"
DESTINO="$(cd "$(dirname "$0")/.." && pwd)/public/solidcraft"

if [[ -n "${SOLIDCRAFT_DIST:-}" ]]; then
  ORIGEM="$(cd "$SOLIDCRAFT_DIST" && pwd)"
else
  TEMP="$(mktemp -d)"
  trap 'rm -rf "$TEMP"' EXIT
  cd "$TEMP"
  gh release download "v${VERSAO}" -R tangency-labs/solidcraft -p "$PACOTE" -p SHA256SUMS.txt
  awk -v p="$PACOTE" '$2 == p' SHA256SUMS.txt | shasum -a 256 -c
  unzip -q "$PACOTE"
  ORIGEM="$TEMP/solidcraft-web-${VERSAO}"
fi

rm -rf "$DESTINO"
mkdir -p "$DESTINO"
cp -R "$ORIGEM/." "$DESTINO/"
# Regras de servidor de exemplo não servem para o Angular.
rm -f "$DESTINO/_headers" "$DESTINO/.htaccess"
echo "SolidCraft instalado em public/solidcraft (de $ORIGEM)"
