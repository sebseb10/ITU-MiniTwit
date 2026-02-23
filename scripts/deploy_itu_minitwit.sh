#!/usr/bin/env bash
set -euo pipefail

# Deploys ITU-MiniTwit to a Linux VM over SSH using Docker Compose.
# Required env vars:
#   DEPLOY_HOST            Public IP or DNS of target VM
# Optional env vars:
#   DEPLOY_USER            Default: root
#   DEPLOY_SSH_KEY         Default: ~/.ssh/id_ed25519
#   REMOTE_APP_DIR         Default: /opt/itu-minitwit
#   APP_PORT               Default: 5001

: "${DEPLOY_HOST:?DEPLOY_HOST is required}"

DEPLOY_USER="${DEPLOY_USER:-root}"
DEPLOY_SSH_KEY="${DEPLOY_SSH_KEY:-$HOME/.ssh/id_ed25519}"
REMOTE_APP_DIR="${REMOTE_APP_DIR:-/opt/itu-minitwit}"
APP_PORT="${APP_PORT:-5001}"

SSH_OPTS=(
  -i "${DEPLOY_SSH_KEY}"
  -o StrictHostKeyChecking=accept-new
)

REMOTE="${DEPLOY_USER}@${DEPLOY_HOST}"
RSYNC_RSH="ssh -i ${DEPLOY_SSH_KEY} -o StrictHostKeyChecking=accept-new"

echo "Installing Docker on remote VM (if missing)..."
ssh "${SSH_OPTS[@]}" "${REMOTE}" 'bash -s' <<'REMOTE_SETUP'
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

apt-get update -y
apt-get install -y ca-certificates curl gnupg lsb-release rsync

if ! command -v docker >/dev/null 2>&1; then
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  chmod a+r /etc/apt/keyrings/docker.gpg

  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
    $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
    > /etc/apt/sources.list.d/docker.list

  apt-get update -y
  apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
fi
REMOTE_SETUP

echo "Syncing repository to ${REMOTE}:${REMOTE_APP_DIR}..."
ssh "${SSH_OPTS[@]}" "${REMOTE}" "mkdir -p '${REMOTE_APP_DIR}'"
rsync -az --delete \
  --exclude '.git' \
  --exclude '.idea' \
  --exclude 'bin' \
  --exclude 'obj' \
  --exclude 'data' \
  -e "${RSYNC_RSH}" \
  ./ "${REMOTE}:${REMOTE_APP_DIR}/"

echo "Starting application with Docker Compose..."
ssh "${SSH_OPTS[@]}" "${REMOTE}" "cd '${REMOTE_APP_DIR}' && mkdir -p data && docker compose up -d --build"

APP_URL="http://${DEPLOY_HOST}:${APP_PORT}"
SIM_API_URL="${APP_URL}/latest"

echo "Checking simulator API endpoint..."
for _ in $(seq 1 20); do
  if curl -fsS "${SIM_API_URL}" >/dev/null 2>&1; then
    echo "Deployment finished."
    echo "App URL: ${APP_URL}"
    echo "Simulator API URL (example endpoint): ${SIM_API_URL}"
    exit 0
  fi
  sleep 2
done

echo "Deployment completed but health check failed: ${SIM_API_URL}" >&2
exit 1
