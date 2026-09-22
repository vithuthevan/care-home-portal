#!/usr/bin/env bash
# Apply pending EF migrations on the Oracle/production host (same DB as carehome-api).
# Loads secrets from the systemd unit (EnvironmentFile + inline Environment), same as the service.
# Usage on server: bash /home/ubuntu/api/scripts/apply-database-migrations-on-host.sh
# Or: bash scripts/apply-database-migrations-on-host.sh /home/ubuntu/api
set -euo pipefail

API_DIR="${1:-/home/ubuntu/api}"
UNIT="${CAREHOME_SYSTEMD_UNIT:-carehome-api}"

if [ ! -f "$API_DIR/CareHome.Api.dll" ]; then
  echo "CareHome.Api.dll not found in $API_DIR" >&2
  exit 1
fi

sudo bash -s "$UNIT" "$API_DIR" <<'EOS'
set -eo pipefail
UNIT="$1"
API_DIR="$2"

load_env_files() {
  local show=""
  show="$(systemctl show "$UNIT" --property=EnvironmentFiles --value 2>/dev/null || true)"
  if [ -z "$show" ]; then
    show="$(systemctl show "$UNIT" --property=EnvironmentFile --value 2>/dev/null || true)"
  fi
  [ -z "$show" ] && return 0

  local path
  for path in $show; do
    case "$path" in
      -*)
        path="${path#-}"
        [ -f "$path" ] || continue
        ;;
      *)
        if [ ! -f "$path" ]; then
          echo "EnvironmentFile not found: $path (unit $UNIT)" >&2
          exit 1
        fi
        ;;
    esac
    set -a
    # shellcheck disable=SC1090
    . "$path"
    set +a
  done
}

load_inline_environment() {
  local show=""
  show="$(systemctl show "$UNIT" --property=Environment --value 2>/dev/null || true)"
  [ -z "$show" ] && return 0
  set -a
  # systemd-quoted KEY=VALUE pairs (handles spaces in values better than bare export)
  eval "export $show"
  set +a
}

load_env_files
load_inline_environment

cd "$API_DIR"
exec dotnet CareHome.Api.dll --apply-migrations
EOS
