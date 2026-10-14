# 🔥 RageBait Yourself — Official Release & Portal

[![Release](https://img.shields.io/github/v/release/Anupam1707/RageBaitYourself?style=for-the-badge&color=e58e26&labelColor=1a162b)](https://github.com/Anupam1707/RageBaitYourself/releases)
[![Website](https://img.shields.io/badge/Live_Portal-GitHub_Pages-2ed573?style=for-the-badge&labelColor=1a162b)](https://anupam1707.github.io/RageBaitYourself/)
[![Platforms](https://img.shields.io/badge/Platforms-macOS_%7C_Windows_%7C_Linux-00d2d3?style=for-the-badge&labelColor=1a162b)](https://github.com/Anupam1707/RageBaitYourself/releases)
[![License](https://img.shields.io/badge/License-Proprietary-ff4757?style=for-the-badge&labelColor=1a162b)](LICENSE)

> **Where your own frustration becomes your greatest obstacle.**  
> A high-octane 3D psychological speed-running maze game where your microphone actively listens to your emotional rage, turning spikes in frustration into treacherous, real-time in-game hazards.

---

## 🌐 Official Web Portal

Visit the official live landing page for live patch notes, system requirements, speedrun leaderboards, and direct downloads:  
👉 **[https://anupam1707.github.io/RageBaitYourself/](https://anupam1707.github.io/RageBaitYourself/)**

---

## 📂 Repository Contents

This public repository serves as the official distribution and client hub for **RageBait Yourself**:

| Directory / File | Description |
| :--- | :--- |
| [`index.html`](index.html) | High-performance interactive landing page and release portal |
| [`site-assets/`](site-assets/) | Production stylesheet, ambient audio cues, particles, and branding |
| [`Launcher/`](Launcher/) | Official multi-platform Electron desktop launcher source code |
| [`LAUNCHER_GUIDE.md`](LAUNCHER_GUIDE.md) | Comprehensive build & distribution guide for the desktop launcher |
| [`release.sh`](release.sh) | Automated GitHub release publishing script |

> [!NOTE]  
> The core Unity game source code is maintained in the creator's private development drive. This repository hosts public distribution assets, desktop launcher clients, release pipelines, and the web portal.

---

## 🚀 Desktop Launcher & Game Downloads

The recommended way to play **RageBait Yourself** is via the official **Desktop Launcher**, which features:
- **One-Click Auto Updates**: Automatically detects, downloads, and patches the newest game builds.
- **Microphone Sensitivity Calibration**: Pre-flight mic testing and gain calibration before launching.
- **Integrated Leaderboard & Sync**: Real-time cloud sync for speedrun records and rage statistics.
- **Hardware Acceleration**: Optimized native Electron wrapper with custom window chrome.

### Latest Release: [v0.2](https://github.com/Anupam1707/RageBaitYourself/releases/tag/v0.2)

| Platform | Recommended Launcher | Standalone Game Direct Build |
| :--- | :--- | :--- |
| **macOS (Apple Silicon & Intel)** | [Download `.dmg`](https://github.com/Anupam1707/RageBaitYourself/releases/download/v0.2/RageBait.Yourself.Launcher-0.2.0-universal.dmg) | [Download `.zip`](https://github.com/Anupam1707/RageBaitYourself/releases/download/v0.2/RageBaitYourself-macOS-Universal.zip) |
| **Windows (x64)** | [Download `.exe` Installer](https://github.com/Anupam1707/RageBaitYourself/releases/download/v0.2/RageBait-Yourself-Launcher-Setup-0.2.0.exe) | [Download `.zip`](https://github.com/Anupam1707/RageBaitYourself/releases/download/v0.2/RageBaitYourself-Windows-x64.zip) |
| **Linux (x64)** | [Download `.AppImage`](https://github.com/Anupam1707/RageBaitYourself/releases/download/v0.2/RageBait.Yourself.Launcher-0.2.0.AppImage) | [Download `.tar.gz`](https://github.com/Anupam1707/RageBaitYourself/releases/download/v0.2/RageBaitYourself-Linux-x64.tar.gz) |

*Full package listings and checksums are available on the [Releases Page](https://github.com/Anupam1707/RageBaitYourself/releases).*

---

## 🛠️ Launcher Development

To build or contribute to the Desktop Launcher:

```bash
# Navigate to launcher source
cd Launcher

# Install dependencies
npm install

# Run launcher in development mode
npm start

# Package for current platform
npm run dist

# Package for all platforms (macOS universal, Windows, Linux)
npm run dist:all
```

For full details on cross-compiling, icons, and launcher release automation, refer to [LAUNCHER_GUIDE.md](LAUNCHER_GUIDE.md).

---

## 👨‍💻 Creator

Crafted by **[Anupam Kanoongo (@Anupam1707)](https://github.com/Anupam1707)**  
*Game Engine: Unity 3D & C# | Launcher: Electron & Node.js | Realtime Services: Custom Game Cloud*
