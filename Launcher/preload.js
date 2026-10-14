const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('launcherAPI', {
  // Window controls
  minimize: () => ipcRenderer.send('window-minimize'),
  maximize: () => ipcRenderer.send('window-maximize'),
  close: () => ipcRenderer.send('window-close'),

  // Config
  getConfig: () => ipcRenderer.invoke('get-config'),
  saveConfig: (cfg) => ipcRenderer.invoke('save-config', cfg),
  selectDirectory: () => ipcRenderer.invoke('select-directory'),

  // Status & Updates
  checkInstalledStatus: () => ipcRenderer.invoke('check-installed-status'),
  fetchLatestRelease: () => ipcRenderer.invoke('fetch-latest-release'),
  downloadAndInstallUpdate: (payload) => ipcRenderer.invoke('download-and-install-update', payload),
  installFromLocalBuild: () => ipcRenderer.invoke('install-from-local-build'),

  // Game execution
  launchGame: () => ipcRenderer.invoke('launch-game'),
  openExternal: (url) => ipcRenderer.send('open-external', url),
  openGameDir: () => ipcRenderer.send('open-game-dir'),

  // Listeners
  onDownloadProgress: (callback) => {
    const handler = (event, data) => callback(data);
    ipcRenderer.on('download-progress', handler);
    return () => ipcRenderer.removeListener('download-progress', handler);
  },
  onInstallStatus: (callback) => {
    const handler = (event, data) => callback(data);
    ipcRenderer.on('install-status', handler);
    return () => ipcRenderer.removeListener('install-status', handler);
  },
  onGameStateChange: (callback) => {
    const handler = (event, data) => callback(data);
    ipcRenderer.on('game-state-change', handler);
    return () => ipcRenderer.removeListener('game-state-change', handler);
  }
});
