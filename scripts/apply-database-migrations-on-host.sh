#!/usr/bin/env bash
# Apply pending EF migrations on the Oracle/production host (same DB as carehome-api).
# Usage on server: cd /home/ubuntu/api && ./scripts/apply-database-migrations-on-host.sh
# Or from repo during SSH: bash scripts/apply-database-migrations-on-host.sh /home/ubuntu/api
set -euo pipefail

API_DIR="${1:-/home/ubuntu/api}"
cd "$API_DIR"

ENV_VARS="$(sudo systemctl show carehome-api --property=Environment --value 2>/dev/null || true)"
if [ -n "$ENV_VARS" ]; then
  set -a
  # shellcheck disable=SC2086
  export $ENV_VARS
  set +a
fi

exec dotnet CareHome.Api.dll --apply-migrations
