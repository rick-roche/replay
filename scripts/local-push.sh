#!/usr/bin/env bash
set -euo pipefail

GHCR_REPOSITORY="${GHCR_REPOSITORY:-rick-roche/replay}"
IMAGE="${IMAGE:-ghcr.io/${GHCR_REPOSITORY}/server:latest}"

docker buildx build \
  --file Dockerfile \
  --tag "${IMAGE}" \
  --push \
  .
