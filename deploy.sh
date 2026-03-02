#!/usr/bin/env bash
set -euo pipefail

APP_DIR="${APP_DIR:-/opt/itu-minitwit}"
COMPOSE_FILE="${COMPOSE_FILE:-$APP_DIR/docker-compose.yml}"
ENV_FILE="${ENV_FILE:-$APP_DIR/.env}"

if [[ ! -d "$APP_DIR" ]]; then
  echo "ERROR: Missing $APP_DIR (create it first)"; exit 1
fi
cd "$APP_DIR"

if [[ ! -f "$COMPOSE_FILE" ]]; then
  echo "ERROR: Missing $COMPOSE_FILE"; exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  if [[ "$(id -u)" -ne 0 ]]; then
    echo "ERROR: Need root to install docker (ssh as root or use sudo)"; exit 1
  fi
  
  export DEBIAN_FRONTEND=noninteractive
  apt-get update -y
  apt-get install -y ca-certificates curl gnupg lsb-release
  
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  chmod a+r /etc/apt/keyrings/docker.gpg

  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
    $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
    > /etc/apt/sources.list.d/docker.list

  apt-get update -y
  apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
  systemctl enable --now docker >/dev/null 2>&1 || true
fi

if ! docker compose version >/dev/null 2>&1; then
  echo "ERROR: docker compose plugin not available"; exit 1
fi

if [[ -n "${TAG:-}" ]]; then
  touch "$ENV_FILE"
  if grep -q '^TAG=' "$ENV_FILE"; then
    sed -i "s/^TAG=.*/TAG=$TAG/" "$ENV_FILE"
  else
    echo "TAG=$TAG" >> "$ENV_FILE"
  fi
fi

docker compose pull
docker compose up -d --remove-orphans
docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Status}}"