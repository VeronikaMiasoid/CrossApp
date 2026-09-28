#!/usr/bin/env bash
# Публікує Cli в різних режимах для однієї RID і друкує рядки таблиці для README.
# Використання (з кореня репозиторію):  bash scripts/publish-sizes.sh osx-arm64
set -euo pipefail
cd "$(dirname "$0")/.."

RID="${1:-osx-arm64}"

echo "| RID | Режим | Розмір publish | Файлів | Потрібен runtime | Рядків з warning |"
echo "|---|---|---|---|---|---|"

measure() {
  local label="$1" needs="$2"; shift 2
  local out="artifacts/publish/${RID}-${label// /_}"
  rm -rf "$out"
  local log
  log=$(dotnet publish src/Cli -c Release -r "$RID" -o "$out" "$@" 2>&1 || true)
  if [ ! -d "$out" ]; then
    echo "| $RID | $label | помилка publish | - | $needs | - |"
    return
  fi
  local size files warns
  size=$(du -sh "$out" | cut -f1)
  files=$(find "$out" -type f | wc -l | tr -d ' ')
  warns=$(grep -c "warning" <<< "$log" || true)
  echo "| $RID | $label | $size | $files | $needs | $warns |"
}

measure "self-contained"                          "ні"            --self-contained true
measure "framework-dependent"                     "так (.NET 10)" --self-contained false
measure "self-contained + single-file"            "ні"            --self-contained true -p:PublishSingleFile=true
measure "self-contained + single-file + trimmed"  "ні"            --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
