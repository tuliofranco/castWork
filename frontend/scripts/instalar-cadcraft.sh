#!/usr/bin/env bash
# Baixa o CADCraft web (WebAssembly) da release oficial, confere o SHA256 e copia para public/cadcraft.
# Uso: npm run cadcraft:instalar
set -euo pipefail

VERSAO="${CADCRAFT_VERSAO:-0.17.1}"
PACOTE="cadcraft-web-${VERSAO}.zip"
DESTINO="$(cd "$(dirname "$0")/.." && pwd)/public/cadcraft"
TEMP="$(mktemp -d)"
trap 'rm -rf "$TEMP"' EXIT

cd "$TEMP"
gh release download "v${VERSAO}" -R storytold/cadcraft -p "$PACOTE" -p SHA256SUMS.txt
grep " ${PACOTE}\$" SHA256SUMS.txt | shasum -a 256 -c
unzip -q "$PACOTE"

rm -rf "$DESTINO"
mkdir -p "$DESTINO"
cp -R "cadcraft-web-${VERSAO}/." "$DESTINO/"
# Regras de servidor de exemplo (Netlify/Apache) não servem para o Angular.
rm -f "$DESTINO/_headers" "$DESTINO/.htaccess"
echo "CADCraft ${VERSAO} instalado em public/cadcraft"
