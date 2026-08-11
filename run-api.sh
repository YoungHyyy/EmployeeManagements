#!/usr/bin/env bash
# Chạy API, tránh treo NuGet restore không cần thiết.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
API_PROJ="$ROOT/src/Api/EmployeeManagement.Api.csproj"
URL="${1:-http://127.0.0.1:5269}"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1
export NUGET_PACKAGES="${NUGET_PACKAGES:-$HOME/.nuget/packages}"

echo "==> Repo: $ROOT"
echo "==> URL:  $URL"

PORT=$(echo "$URL" | sed -n 's/.*:\([0-9][0-9]*\).*/\1/p')
if [ -n "${PORT:-}" ] && ss -ltn 2>/dev/null | grep -q ":${PORT} "; then
  if curl -sf "http://127.0.0.1:${PORT}/health" >/dev/null 2>&1; then
    echo "==> API đã chạy sẵn: http://127.0.0.1:${PORT}/swagger"
    exit 0
  fi
  echo "!! Cổng ${PORT} đang bận nhưng /health không OK. Hãy dừng process cũ hoặc dùng port khác:"
  echo "   $0 http://127.0.0.1:5270"
  exit 1
fi

echo "==> Build (skip restore nếu assets đã có)..."
if [ -f "$ROOT/src/Api/obj/project.assets.json" ]; then
  dotnet build "$API_PROJ" --no-restore -nologo -v q
else
  echo "==> Restore lần đầu (có thể chậm nếu mạng kém)..."
  dotnet restore "$API_PROJ" --ignore-failed-sources -nologo -v q
  dotnet build "$API_PROJ" --no-restore -nologo -v q
fi

echo "==> Run..."
cd "$ROOT/src/Api"
exec dotnet run --no-build --no-restore --urls "$URL"
