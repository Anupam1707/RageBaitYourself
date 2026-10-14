// State
let currentConfig = null;
let installedState = null;
let latestRelease = null;
let isBusy = false;
let isGameRunning = false;

// DOM Elements
const navTabs = document.querySelectorAll('.nav-tab');
const tabContents = document.querySelectorAll('.tab-content');

const gameClientStatus = document.getElementById('gameClientStatus');
const installedVersionText = document.getElementById('installedVersionText');
const latestVersionText = document.getElementById('latestVersionText');
const btnOpenGameDir = document.getElementById('btnOpenGameDir');

const progressContainer = document.getElementById('progressContainer');
const progressBarFill = document.getElementById('progressBarFill');
const progressStepText = document.getElementById('progressStepText');
const progressStatsText = document.getElementById('progressStatsText');

const btnMainAction = document.getElementById('btnMainAction');
const btnMainActionText = document.getElementById('btnMainActionText');

const patchNotesMeta = document.getElementById('patchNotesMeta');
const patchNotesContent = document.getElementById('patchNotesContent');
const btnRefreshReleases = document.getElementById('btnRefreshReleases');
const toastNotice = document.getElementById('toastNotice');

// Tab Navigation
navTabs.forEach(tab => {
  tab.addEventListener('click', () => {
    const target = tab.dataset.tab;
    navTabs.forEach(t => t.classList.toggle('active', t === tab));
    tabContents.forEach(c => c.classList.toggle('active', c.id === `tab-${target}`));
  });
});

// Mini Level Cards Click -> Switch to Levels Tab
document.querySelectorAll('.mini-card').forEach(card => {
  card.addEventListener('click', () => {
    const levelsTab = document.querySelector('.nav-tab[data-tab="levels"]');
    if (levelsTab) levelsTab.click();
  });
});

// Toast notification helper
function showToast(msg, duration = 3500) {
  toastNotice.textContent = msg;
  toastNotice.classList.add('show');
  setTimeout(() => {
    toastNotice.classList.remove('show');
  }, duration);
}

// Format bytes
function formatBytes(bytes) {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}

// Compare semantic version tags (e.g. "v0.2" vs "v0.1")
function isNewerVersion(remoteTag, localTag) {
  if (!localTag) return true;
  if (!remoteTag) return false;

  const cleanRemote = remoteTag.replace(/^v/i, '').trim();
  const cleanLocal = localTag.replace(/^v/i, '').trim();

  if (cleanRemote === cleanLocal) return false;

  const rParts = cleanRemote.split('.').map(n => parseInt(n, 10) || 0);
  const lParts = cleanLocal.split('.').map(n => parseInt(n, 10) || 0);

  for (let i = 0; i < Math.max(rParts.length, lParts.length); i++) {
    const r = rParts[i] || 0;
    const l = lParts[i] || 0;
    if (r > l) return true;
    if (r < l) return false;
  }
  return false;
}

// Update UI button and status indicators
function updateLauncherUI() {
  if (isBusy) {
    btnMainAction.disabled = true;
    return;
  }

  if (isGameRunning) {
    btnMainAction.disabled = true;
    btnMainAction.classList.remove('play-state');
    btnMainActionText.textContent = 'GAME RUNNING...';
    gameClientStatus.textContent = 'ACTIVE';
    gameClientStatus.style.color = '#00e676';
    return;
  }

  const isInstalled = installedState && installedState.isInstalled;
  const localVer = installedState ? installedState.installedVersion : null;
  const remoteVer = latestRelease && latestRelease.success ? latestRelease.tag : null;

  installedVersionText.textContent = isInstalled ? `Installed: ${localVer || 'v0.1'}` : 'Installed: Not Found';
  latestVersionText.textContent = remoteVer ? `Latest: ${remoteVer}` : 'Latest: N/A';

  if (!isInstalled) {
    gameClientStatus.textContent = 'NOT INSTALLED';
    gameClientStatus.style.color = '#ff9d00';
    btnMainAction.disabled = false;
    btnMainAction.classList.remove('play-state');

    if (latestRelease && latestRelease.success && latestRelease.asset) {
      btnMainActionText.textContent = `INSTALL GAME (${latestRelease.tag})`;
    } else {
      btnMainActionText.textContent = 'INSTALL LOCAL BUILD';
    }
  } else {
    // Game is installed
    if (remoteVer && isNewerVersion(remoteVer, localVer)) {
      // Update available!
      gameClientStatus.textContent = 'UPDATE AVAILABLE';
      gameClientStatus.style.color = '#ff2a4b';
      btnMainAction.disabled = false;
      btnMainAction.classList.remove('play-state');
      btnMainActionText.textContent = `UPDATE TO ${remoteVer}`;
    } else {
      // Up to date!
      gameClientStatus.textContent = 'UP TO DATE';
      gameClientStatus.style.color = '#00e676';
      btnMainAction.disabled = false;
      btnMainAction.classList.add('play-state');
      btnMainActionText.textContent = 'PLAY GAME';
    }
  }
}

// Initial Sync & Check
async function refreshState() {
  try {
    btnMainActionText.textContent = 'CHECKING...';
    btnMainAction.disabled = true;

    // Load installed state
    installedState = await window.launcherAPI.checkInstalledStatus();

    // Check remote releases
    latestRelease = await window.launcherAPI.fetchLatestRelease();

    if (latestRelease && latestRelease.success) {
      // Populate patch notes
      patchNotesMeta.textContent = `Release: ${latestRelease.name} • Tag: ${latestRelease.tag} • ${new Date(latestRelease.publishedAt).toLocaleDateString()}`;
      patchNotesContent.textContent = latestRelease.body || 'No release notes provided.';
    } else {
      patchNotesMeta.textContent = 'RageBait Yourself Patch History';
      patchNotesContent.textContent = 'Welcome to RageBait Yourself!\n\nNo online patch updates yet. When new content, performance enhancements, or new levels are released, patch notes will be displayed right here.';
    }

    updateLauncherUI();
  } catch (err) {
    console.error('Sync error:', err);
    updateLauncherUI();
  }
}

// Download & Install Handlers
window.launcherAPI.onDownloadProgress((data) => {
  progressContainer.style.display = 'flex';
  progressBarFill.style.width = `${data.percent}%`;
  progressStepText.textContent = `Downloading update... ${data.percent}%`;
  progressStatsText.textContent = `${formatBytes(data.receivedBytes)} / ${formatBytes(data.totalBytes)} (${formatBytes(data.speedBytesPerSec)}/s)`;
});

window.launcherAPI.onInstallStatus((status) => {
  progressStepText.textContent = status.message;
  if (status.step === 'done') {
    progressBarFill.style.width = '100%';
    setTimeout(() => {
      progressContainer.style.display = 'none';
    }, 2000);
  }
});

window.launcherAPI.onGameStateChange((state) => {
  isGameRunning = state.isRunning;
  if (!state.isRunning && state.error) {
    showToast('Failed to start game: ' + state.error);
  }
  updateLauncherUI();
});

// Main Action Button Click
btnMainAction.addEventListener('click', async () => {
  if (isBusy || isGameRunning) return;

  const isInstalled = installedState && installedState.isInstalled;
  const localVer = installedState ? installedState.installedVersion : null;
  const remoteVer = latestRelease && latestRelease.success ? latestRelease.tag : null;

  // Case 1: PLAY GAME
  if (isInstalled && (!remoteVer || !isNewerVersion(remoteVer, localVer))) {
    showToast('Launching RageBait Yourself...');
    const res = await window.launcherAPI.launchGame();
    if (!res.success) {
      showToast('Error: ' + res.error);
    }
    return;
  }

  // Case 2: ONLINE DOWNLOAD & INSTALL / UPDATE
  if (latestRelease && latestRelease.success && latestRelease.asset) {
    isBusy = true;
    btnMainAction.disabled = true;
    btnMainActionText.textContent = 'DOWNLOADING...';
    progressContainer.style.display = 'flex';
    progressBarFill.style.width = '0%';
    progressStepText.textContent = 'Connecting to download stream...';
    progressStatsText.textContent = '';

    const res = await window.launcherAPI.downloadAndInstallUpdate({
      downloadUrl: latestRelease.asset.downloadUrl,
      versionTag: latestRelease.tag,
      releaseName: latestRelease.name
    });

    isBusy = false;
    if (res.success) {
      showToast('Update installed successfully!');
      await refreshState();
    } else {
      showToast('Install failed: ' + res.error);
      progressContainer.style.display = 'none';
      updateLauncherUI();
    }
    return;
  }

  // Case 3: OFFLINE / LOCAL FALLBACK INSTALL
  isBusy = true;
  btnMainAction.disabled = true;
  btnMainActionText.textContent = 'INSTALLING LOCAL BUILD...';
  progressContainer.style.display = 'flex';
  progressBarFill.style.width = '50%';
  progressStepText.textContent = 'Unpacking local FinalBuilds files...';

  const localRes = await window.launcherAPI.installFromLocalBuild();
  isBusy = false;
  progressContainer.style.display = 'none';

  if (localRes.success) {
    showToast('Local build installed successfully!');
    await refreshState();
  } else {
    showToast('Failed to install local build: ' + localRes.error);
    updateLauncherUI();
  }
});

btnOpenGameDir.addEventListener('click', () => {
  window.launcherAPI.openGameDir();
});

btnRefreshReleases.addEventListener('click', () => {
  refreshState();
});

// About Dev button handlers
const btnDevGithub = document.getElementById('btnDevGithub');
const btnDevIssues = document.getElementById('btnDevIssues');

if (btnDevGithub) {
  btnDevGithub.addEventListener('click', () => {
    window.launcherAPI.openExternal(`https://github.com/${currentConfig?.githubOwner || 'Anupam1707'}`);
  });
}

if (btnDevIssues) {
  btnDevIssues.addEventListener('click', () => {
    window.launcherAPI.openExternal(`https://github.com/${currentConfig?.githubOwner || 'Anupam1707'}/${currentConfig?.githubRepo || 'RageBaitYourself'}/issues`);
  });
}

// App Startup
async function init() {
  currentConfig = await window.launcherAPI.getConfig();
  await refreshState();
}

init();
