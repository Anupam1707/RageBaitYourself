#!/usr/bin/env bash
set -e

TAG="${1:-v0.2}"
MODE="${2:-all}"

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"

# 1. Locate Unity
UNITY_PATH=$(find /Applications/Unity/Hub/Editor -name "Unity.app" 2>/dev/null | head -n 1)
if [ -z "$UNITY_PATH" ]; then
  UNITY_BIN="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
else
  UNITY_BIN="$UNITY_PATH/Contents/MacOS/Unity"
fi

if [ ! -f "$UNITY_BIN" ]; then
  echo "❌ Error: Unity editor binary not found at $UNITY_BIN"
  exit 1
fi

echo "🎮 Found Unity Editor: $UNITY_BIN"
echo "🚀 Building RageBait Yourself ($TAG)..."

METHOD="BuildScript.BuildAll"
if [ "$MODE" = "--mac-only" ] || [ "$MODE" = "mac" ]; then
  METHOD="BuildScript.BuildMacOS"
  echo "⚡ Building macOS only..."
else
  echo "🌍 Building all platforms (macOS, Windows, Linux)..."
fi

# 2. Run Unity Batchmode Build
"$UNITY_BIN" -batchmode -quit -projectPath "$ROOT_DIR" -executeMethod "$METHOD" -logFile "$ROOT_DIR/unity_build.log"

echo "✅ Unity build completed successfully!"

# 3. Package and publish release via Launcher
cd "$ROOT_DIR"
./release.sh "$TAG"

echo ""
echo "🎉 Build & Release $TAG complete!"
