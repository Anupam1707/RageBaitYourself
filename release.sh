#!/usr/bin/env bash
set -e

# Specify tag argument or default to next version
TAG="${1:-v0.3}"
TITLE="${2:-RageBait Yourself $TAG}"
NOTES="${3:-Game update $TAG for macOS, Windows, and Linux.}"

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT_DIR/Launcher"

echo "🚀 Packaging & Releasing RageBait Yourself $TAG..."
npm run release-game "$TAG" "$TITLE" "$NOTES"
