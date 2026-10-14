const { app, BrowserWindow, ipcMain, shell, dialog } = require('electron');
const path = require('path');
const fs = require('fs');
const https = require('https');
const http = require('http');
const { spawn, exec } = require('child_process');
const AdmZip = require('adm-zip');

let mainWindow = null;
let gameProcess = null;

// Determine default install directory
function getDefaultInstallDir() {
  const baseDir = app.getPath('appData');
  return path.join(baseDir, 'RageBaitGame');
}

// Config file management
function getConfigPath() {
  return path.join(app.getPath('userData'), 'launcher_config.json');
}

function loadConfig() {
  const cfgPath = getConfigPath();
  const defaultConfig = {
    githubOwner: 'Anupam1707',
    githubRepo: 'RageBaitYourself',
    installDir: getDefaultInstallDir(),
    autoCloseLauncher: false,
    launchWindowed: false
  };

  try {
    if (fs.existsSync(cfgPath)) {
      const data = JSON.parse(fs.readFileSync(cfgPath, 'utf8'));
      return { ...defaultConfig, ...data };
    }
  } catch (err) {
    console.error('Failed to read config:', err);
  }
  return defaultConfig;
}

function saveConfig(config) {
  try {
    const cfgPath = getConfigPath();
    fs.mkdirSync(path.dirname(cfgPath), { recursive: true });
    fs.writeFileSync(cfgPath, JSON.stringify(config, null, 2), 'utf8');
    return true;
  } catch (err) {
    console.error('Failed to save config:', err);
    return false;
  }
}

function getInstalledVersionInfo(installDir) {
  try {
    const versionFile = path.join(installDir, 'installed_version.json');
    if (fs.existsSync(versionFile)) {
      return JSON.parse(fs.readFileSync(versionFile, 'utf8'));
    }
  } catch (e) {
    console.error('Error reading installed version:', e);
  }
  return null;
}

function saveInstalledVersion(installDir, versionTag, releaseName) {
  try {
    fs.mkdirSync(installDir, { recursive: true });
    const versionFile = path.join(installDir, 'installed_version.json');
    const data = {
      version: versionTag,
      name: releaseName || versionTag,
      installedAt: new Date().toISOString()
    };
    fs.writeFileSync(versionFile, JSON.stringify(data, null, 2), 'utf8');
  } catch (e) {
    console.error('Error saving installed version:', e);
  }
}

// Locate game executable inside install directory
function findGameExecutable(installDir) {
  if (!fs.existsSync(installDir)) return null;

  const platform = process.platform;
  const entries = fs.readdirSync(installDir);

  if (platform === 'darwin') {
    // macOS: find .app
    const appDir = entries.find(f => f.endsWith('.app'));
    if (appDir) {
      return path.join(installDir, appDir);
    }
  } else if (platform === 'win32') {
    // Windows: find .exe (ignore crash handlers / unity helpers)
    const exe = entries.find(f => f.endsWith('.exe') && !f.toLowerCase().includes('unitycrashhandler'));
    if (exe) {
      return path.join(installDir, exe);
    }
  } else {
    // Linux: find .x86_64
    const bin = entries.find(f => f.endsWith('.x86_64') || (fs.statSync(path.join(installDir, f)).mode & 0o111));
    if (bin) {
      return path.join(installDir, bin);
    }
  }

  return null;
}

// Global error listeners to avoid silent crashes
process.on('uncaughtException', (err) => {
  console.error('Unhandled Exception:', err);
});
process.on('unhandledRejection', (reason, promise) => {
  console.error('Unhandled Rejection at:', promise, 'reason:', reason);
});

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1040,
    height: 660,
    minWidth: 980,
    minHeight: 620,
    show: false,
    frame: false,
    titleBarStyle: process.platform === 'darwin' ? 'hiddenInset' : 'hidden',
    trafficLightPosition: { x: 16, y: 14 },
    backgroundColor: '#090a0f',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      nodeIntegration: false,
      contextIsolation: true
    },
    icon: path.join(__dirname, 'assets', 'icon.png')
  });

  mainWindow.loadFile(path.join(__dirname, 'src', 'index.html'));

  mainWindow.once('ready-to-show', () => {
    mainWindow.show();
    mainWindow.focus();
  });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

app.whenReady().then(() => {
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

// Window controls
ipcMain.on('window-minimize', () => {
  if (mainWindow) mainWindow.minimize();
});
ipcMain.on('window-maximize', () => {
  if (mainWindow) {
    if (mainWindow.isMaximized()) mainWindow.unmaximize();
    else mainWindow.maximize();
  }
});
ipcMain.on('window-close', () => {
  if (mainWindow) mainWindow.close();
});

// Config IPC
ipcMain.handle('get-config', async () => {
  return loadConfig();
});

ipcMain.handle('save-config', async (event, newConfig) => {
  return saveConfig(newConfig);
});

ipcMain.handle('select-directory', async () => {
  const result = await dialog.showOpenDialog(mainWindow, {
    properties: ['openDirectory', 'createDirectory']
  });
  if (!result.canceled && result.filePaths.length > 0) {
    return result.filePaths[0];
  }
  return null;
});

// Check installed state
ipcMain.handle('check-installed-status', async () => {
  const config = loadConfig();
  const installDir = config.installDir;
  const installedInfo = getInstalledVersionInfo(installDir);
  const exePath = findGameExecutable(installDir);

  return {
    isInstalled: !!exePath,
    installedVersion: installedInfo ? installedInfo.version : (exePath ? '0.1' : null),
    installDir,
    exePath,
    platform: process.platform
  };
});

// Fetch Latest Release from GitHub
ipcMain.handle('fetch-latest-release', async () => {
  const config = loadConfig();
  const { githubOwner, githubRepo } = config;

  return new Promise((resolve, reject) => {
    const options = {
      hostname: 'api.github.com',
      path: `/repos/${githubOwner}/${githubRepo}/releases/latest`,
      headers: {
        'User-Agent': 'RageBait-Launcher',
        'Accept': 'application/vnd.github.v3+json'
      }
    };

    https.get(options, (res) => {
      let data = '';
      res.on('data', chunk => { data += chunk; });
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          try {
            const release = JSON.parse(data);
            
            // Find platform-specific asset
            const platform = process.platform;
            let asset = null;
            if (platform === 'darwin') {
              asset = release.assets.find(a => /mac|osx|universal/i.test(a.name) && a.name.endsWith('.zip'));
            } else if (platform === 'win32') {
              asset = release.assets.find(a => /win|windows/i.test(a.name) && a.name.endsWith('.zip'));
            } else {
              asset = release.assets.find(a => /linux/i.test(a.name) && a.name.endsWith('.zip'));
            }

            // Fallback to any zip if specific not matched
            if (!asset && release.assets && release.assets.length > 0) {
              asset = release.assets.find(a => a.name.endsWith('.zip')) || release.assets[0];
            }

            resolve({
              success: true,
              tag: release.tag_name,
              name: release.name || release.tag_name,
              body: release.body || 'No release notes provided.',
              publishedAt: release.published_at,
              asset: asset ? {
                name: asset.name,
                size: asset.size,
                downloadUrl: asset.browser_download_url
              } : null
            });
          } catch (e) {
            resolve({ success: false, error: 'Failed to parse GitHub response: ' + e.message });
          }
        } else if (res.statusCode === 404) {
          resolve({ success: false, notFound: true, error: `No releases found yet for ${githubOwner}/${githubRepo}. You can test with Local FinalBuilds or publish a release on GitHub.` });
        } else {
          resolve({ success: false, error: `GitHub API error: HTTP ${res.statusCode}` });
        }
      });
    }).on('error', (err) => {
      resolve({ success: false, error: 'Network error connecting to GitHub: ' + err.message });
    });
  });
});

// Helper: Download a file following redirects
function downloadFile(url, destPath, onProgress) {
  return new Promise((resolve, reject) => {
    function getWithRedirect(currentUrl) {
      const parsedUrl = new URL(currentUrl);
      const protocol = parsedUrl.protocol === 'https:' ? https : http;

      const req = protocol.get(currentUrl, {
        headers: {
          'User-Agent': 'RageBait-Launcher'
        }
      }, (res) => {
        // Follow redirect
        if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
          return getWithRedirect(res.headers.location);
        }

        if (res.statusCode !== 200) {
          return reject(new Error(`Download failed with HTTP ${res.statusCode}`));
        }

        const totalBytes = parseInt(res.headers['content-length'] || '0', 10);
        let receivedBytes = 0;
        let lastReportedTime = Date.now();
        let bytesSinceLastReport = 0;

        fs.mkdirSync(path.dirname(destPath), { recursive: true });
        const fileStream = fs.createWriteStream(destPath);

        res.on('data', (chunk) => {
          receivedBytes += chunk.length;
          bytesSinceLastReport += chunk.length;

          const now = Date.now();
          if (now - lastReportedTime > 200 || receivedBytes === totalBytes) {
            const timeDiff = (now - lastReportedTime) / 1000;
            const speed = timeDiff > 0 ? (bytesSinceLastReport / timeDiff) : 0;
            const percent = totalBytes > 0 ? Math.round((receivedBytes / totalBytes) * 100) : 0;

            onProgress({
              receivedBytes,
              totalBytes,
              percent,
              speedBytesPerSec: Math.round(speed)
            });

            lastReportedTime = now;
            bytesSinceLastReport = 0;
          }
        });

        res.pipe(fileStream);

        fileStream.on('finish', () => {
          fileStream.close(() => resolve());
        });

        fileStream.on('error', (err) => {
          fs.unlink(destPath, () => {});
          reject(err);
        });
      });

      req.on('error', (err) => {
        fs.unlink(destPath, () => {});
        reject(err);
      });
    }

    getWithRedirect(url);
  });
}

// Download and install release asset
ipcMain.handle('download-and-install-update', async (event, { downloadUrl, versionTag, releaseName }) => {
  const config = loadConfig();
  const installDir = config.installDir;
  const tempZipPath = path.join(installDir, '_temp_update.zip');

  try {
    // Step 1: Download
    await downloadFile(downloadUrl, tempZipPath, (progress) => {
      mainWindow.webContents.send('download-progress', progress);
    });

    // Step 2: Extracting
    mainWindow.webContents.send('install-status', { step: 'extracting', message: 'Extracting update files...' });
    
    const zip = new AdmZip(tempZipPath);
    zip.extractAllTo(installDir, true);

    // Clean up zip
    if (fs.existsSync(tempZipPath)) {
      fs.unlinkSync(tempZipPath);
    }

    // Step 3: Platform specific post-install permissions
    if (process.platform === 'darwin') {
      // Clear quarantine attribute & ensure executable bit
      const appPath = findGameExecutable(installDir);
      if (appPath) {
        await new Promise(r => exec(`xattr -cr "${appPath}" && chmod -R +x "${appPath}/Contents/MacOS"`, () => r()));
      }
    } else if (process.platform === 'linux') {
      const binPath = findGameExecutable(installDir);
      if (binPath) {
        await new Promise(r => exec(`chmod +x "${binPath}"`, () => r()));
      }
    }

    // Step 4: Save version
    saveInstalledVersion(installDir, versionTag, releaseName);

    mainWindow.webContents.send('install-status', { step: 'done', message: 'Installation complete!' });
    return { success: true };
  } catch (err) {
    console.error('Update failed:', err);
    if (fs.existsSync(tempZipPath)) {
      try { fs.unlinkSync(tempZipPath); } catch (_) {}
    }
    return { success: false, error: err.message };
  }
});

// Local install helper (copies from workspace FinalBuilds for immediate testing without needing internet/GitHub)
ipcMain.handle('install-from-local-build', async () => {
  const config = loadConfig();
  const installDir = config.installDir;
  const platform = process.platform;
  
  // Look for FinalBuilds relative to project root or workspace
  let finalBuildsDir = path.join(path.resolve(__dirname, '..'), 'FinalBuilds');
  if (!fs.existsSync(finalBuildsDir)) {
    const fallback = '/Users/anupamkanoongo/RageBaitYourself/FinalBuilds';
    if (fs.existsSync(fallback)) {
      finalBuildsDir = fallback;
    }
  }

  if (!fs.existsSync(finalBuildsDir)) {
    return { success: false, error: 'FinalBuilds directory not found at ' + finalBuildsDir };
  }

  try {
    mainWindow.webContents.send('install-status', { step: 'copying', message: 'Installing from local FinalBuilds...' });
    fs.mkdirSync(installDir, { recursive: true });

    let sourceName = '';
    if (platform === 'darwin') {
      sourceName = 'RageBaitYourself-0.1-macos-universal.app';
    } else if (platform === 'win32') {
      sourceName = 'RageBaitYourself-0.1-win-x64';
    } else {
      sourceName = 'RageBaitYourself-0.1-linux-x86_64';
    }

    const sourcePath = path.join(finalBuildsDir, sourceName);
    if (!fs.existsSync(sourcePath)) {
      return { success: false, error: `Local build ${sourceName} not found in FinalBuilds.` };
    }

    // Copy directory recursively
    const destPath = path.join(installDir, sourceName);
    await new Promise((resolve, reject) => {
      exec(`cp -R "${sourcePath}" "${installDir}/"`, (error) => {
        if (error) reject(error);
        else resolve();
      });
    });

    if (platform === 'darwin') {
      await new Promise(r => exec(`xattr -cr "${destPath}" && chmod -R +x "${destPath}/Contents/MacOS"`, () => r()));
    }

    saveInstalledVersion(installDir, '0.1.0', 'RageBait Yourself v0.1 (Local Build)');
    mainWindow.webContents.send('install-status', { step: 'done', message: 'Local installation ready!' });
    return { success: true };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

// Launch Game Executable
ipcMain.handle('launch-game', async () => {
  const config = loadConfig();
  const installDir = config.installDir;
  const exePath = findGameExecutable(installDir);

  if (!exePath) {
    return { success: false, error: 'Game executable not found. Please install or verify files.' };
  }

  const platform = process.platform;

  try {
    if (platform === 'darwin') {
      // Use macOS 'open -W' to wait for game termination
      gameProcess = spawn('open', ['-W', '-n', exePath]);
    } else if (platform === 'win32') {
      gameProcess = spawn(exePath, [], {
        cwd: installDir,
        detached: false
      });
    } else {
      gameProcess = spawn(exePath, [], {
        cwd: installDir,
        detached: false
      });
    }

    mainWindow.webContents.send('game-state-change', { isRunning: true });

    if (config.autoCloseLauncher && mainWindow) {
      mainWindow.hide();
    }

    gameProcess.on('close', (code) => {
      gameProcess = null;
      if (mainWindow) {
        if (config.autoCloseLauncher) {
          mainWindow.show();
        }
        mainWindow.webContents.send('game-state-change', { isRunning: false, exitCode: code });
      }
    });

    gameProcess.on('error', (err) => {
      gameProcess = null;
      if (mainWindow) {
        mainWindow.webContents.send('game-state-change', { isRunning: false, error: err.message });
      }
    });

    return { success: true };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

// Open external link
ipcMain.on('open-external', (event, url) => {
  shell.openExternal(url);
});

// Open local game directory in Finder / File Explorer
ipcMain.on('open-game-dir', () => {
  const config = loadConfig();
  if (fs.existsSync(config.installDir)) {
    shell.openPath(config.installDir);
  } else {
    shell.openPath(path.dirname(config.installDir));
  }
});
