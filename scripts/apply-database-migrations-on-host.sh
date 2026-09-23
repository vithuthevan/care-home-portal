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

# systemd EnvironmentFile values (e.g. SQL connection strings with ";Initial Catalog=")
# must not be loaded with bash "source" — unquoted ";" runs extra commands.
read_env_file() {
  local path="$1"
  local line key val
  while IFS= read -r line || [[ -n "$line" ]]; do
    line="${line#"${line%%[![:space:]]*}"}"
    line="${line%"${line##*[![:space:]]}"}"
    [[ -z "$line" || "$line" == \#* ]] && continue

    key="${line%%=*}"
    val="${line#*=}"
    key="${key#"${key%%[![:space:]]*}"}"
    key="${key%"${key##*[![:space:]]}"}"
    [[ -z "$key" ]] && continue

    if [[ "$val" == \"*\" ]]; then
      val="${val:1}"
      val="${val%\"}"
    fi

    printf -v "$key" '%s' "$val"
    export "$key"
  done < "$path"
}

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
    read_env_file "$path"
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
