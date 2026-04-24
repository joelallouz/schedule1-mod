#!/usr/bin/env bash
set -euo pipefail

# Schedule 1 Mod — Build, Deploy, and optionally tail logs
# Usage:
#   ./deploy.sh          Build and push DLL to Windows PC
#   ./deploy.sh --tail   Build, push, and live-tail the log file
#   ./deploy.sh --logs   Just pull the latest log (no build)

PC="jlall@192.168.1.141"
MODS_PATH='C:\Program Files (x86)\Steam\steamapps\common\Schedule I\Mods\ClientAssignmentOptimizer.dll'
LOG_PATH='C:\Program Files (x86)\Steam\steamapps\common\Schedule I\MelonLoader\Latest.log'
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
DLL="$PROJECT_DIR/bin/Debug/net6.0/ClientAssignmentOptimizer.dll"

case "${1:-}" in
  --logs)
    echo "==> Pulling Latest.log from PC..."
    ssh "$PC" "type \"${LOG_PATH}\"" > "$PROJECT_DIR/Latest.log"
    echo "==> Saved to $PROJECT_DIR/Latest.log"
    exit 0
    ;;
esac

# Build
echo "==> Building..."
dotnet build "$PROJECT_DIR/ClientAssignmentOptimizer.csproj" -p:CopyToMods=false -v:quiet
echo "==> Build succeeded"

# Deploy
echo "==> Deploying DLL to PC..."
scp "$DLL" "$PC:$MODS_PATH"
echo "==> Deployed"

# Tail
if [[ "${1:-}" == "--tail" ]]; then
  echo "==> Tailing Latest.log (Ctrl+C to stop)..."
  ssh "$PC" "powershell -Command \"Get-Content '${LOG_PATH}' -Wait -Tail 50\""
fi

echo "==> Done"
