#!/bin/sh
set -e

# Populate env vars from Docker Swarm secrets when present.
# Falls back silently to values already in the environment (local/compose use).
if [ -f /run/secrets/db_connection ]; then
  export ConnectionStrings__DefaultConnection=$(cat /run/secrets/db_connection)
fi

if [ -f /run/secrets/redis_connection ]; then
  export ConnectionStrings__Redis=$(cat /run/secrets/redis_connection)
fi

exec dotnet Chirp.Web.dll
