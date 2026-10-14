#!/usr/bin/env bash
set -e

# Version tag (default to v0.3)
TAG="${1:-v0.3}"
MODE="${2:-all}"
export GAME_VERSION="$TAG"

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
UNITY_PROJECT_DIR="/Users/anupamkanoongo/Documents/Developer's Drive/RageBaitYourself-Unity"

echo "=================================================="
echo "🎮 RageBait Yourself — Master Build & Release"
echo "📌 Version Tag: $TAG"
echo "📂 Unity Source: $UNITY_PROJECT_DIR"
echo "📂 Release Hub:  $REPO_DIR"
echo "=================================================="

# 1. Verify Unity Source Directory
if [ ! -d "$UNITY_PROJECT_DIR" ]; then
  echo "❌ Error: Unity project directory not found at $UNITY_PROJECT_DIR"
  exit 1
fi

# 2. Locate Unity Editor Binary
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

METHOD="BuildScript.BuildAll"
if [ "$MODE" = "--mac-only" ] || [ "$MODE" = "mac" ]; then
  METHOD="BuildScript.BuildMacOS"
  echo "⚡ Mode: Building macOS only..."
else
  echo "🌍 Mode: Building all platforms (macOS Universal, Windows x64, Windows ia32, Linux x64)..."
fi

# 3. Trigger Headless Unity Batchmode Compilation
echo "⚙️ Compiling game binaries via Unity batchmode..."
"$UNITY_BIN" -batchmode -quit -projectPath "$UNITY_PROJECT_DIR" -executeMethod "$METHOD" -logFile "$REPO_DIR/unity_build.log"

echo "✅ Unity compilation successful!"

# 4. Sync Fresh Binaries to FinalBuilds
echo "📦 Syncing compiled binaries from Developer's Drive to FinalBuilds/..."
mkdir -p "$REPO_DIR/FinalBuilds"
rsync -a "$UNITY_PROJECT_DIR/FinalBuilds/" "$REPO_DIR/FinalBuilds/"

# 5. Run Packaging & Publish GitHub Release via release.sh
echo "🚀 Packaging Launcher & Publishing GitHub Release..."
cd "$REPO_DIR"
./release.sh "$TAG"

echo ""
echo "🎉 Build & Release $TAG successfully finished!"
