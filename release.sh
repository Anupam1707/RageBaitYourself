#!/usr/bin/env bash
set -e

# Default to v0.1 if no version tag argument is provided
TAG="${1:-v0.1}"

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT_DIR/Launcher"

echo "🚀 Releasing RageBait Yourself $TAG..."
npm run release-game "$TAG"
