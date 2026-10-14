// State
let currentConfig = null;
let installedState = null;
let latestRelease = null;
let allReleases = [];
let currentUserProfile = null;
let isBusy = false;
let isGameRunning = false;
let pendingInstallAction = null;

// DOM Elements
const navTabs = document.querySelectorAll('.nav-tab');
const tabContents = document.querySelectorAll('.tab-content');
const navUserName = document.getElementById('navUserName');
const navUserBadge = document.getElementById('navUserBadge');

const gameClientStatus = document.getElementById('gameClientStatus');
const installedVersionText = document.getElementById('installedVersionText');
const latestVersionText = document.getElementById('latestVersionText');
const btnOpenGameDir = document.getElementById('btnOpenGameDir');
const btnHomeRefresh = document.getElementById('btnHomeRefresh');
const homeRefreshIcon = document.getElementById('homeRefreshIcon');

const progressContainer = document.getElementById('progressContainer');
const progressBarFill = document.getElementById('progressBarFill');
const progressStepText = document.getElementById('progressStepText');
const progressStatsText = document.getElementById('progressStatsText');

const btnMainAction = document.getElementById('btnMainAction');
const btnMainActionText = document.getElementById('btnMainActionText');

// Patches View
const patchNotesMeta = document.getElementById('patchNotesMeta');
const patchesTimeline = document.getElementById('patchesTimeline');
const btnRefreshReleases = document.getElementById('btnRefreshReleases');
const refreshReleasesSpin = document.getElementById('refreshReleasesSpin');

// Settings View
const settingsInstallPath = document.getElementById('settingsInstallPath');
const btnSettingsChangePath = document.getElementById('btnSettingsChangePath');
const settingsProfileUsername = document.getElementById('settingsProfileUsername');
const settingsProfileEmail = document.getElementById('settingsProfileEmail');
const btnSettingsEditProfile = document.getElementById('btnSettingsEditProfile');
const settingsAutoCloseToggle = document.getElementById('settingsAutoCloseToggle');

// Modals
const registrationModal = document.getElementById('registrationModal');
const registrationForm = document.getElementById('registrationForm');
const regUsername = document.getElementById('regUsername');
const regEmail = document.getElementById('regEmail');
const regPassword = document.getElementById('regPassword');

const installLocationModal = document.getElementById('installLocationModal');
const modalInstallPathInput = document.getElementById('modalInstallPathInput');
const btnModalBrowsePath = document.getElementById('btnModalBrowsePath');
const btnConfirmInstallModal = document.getElementById('btnConfirmInstallModal');
const btnCancelInstallModal = document.getElementById('btnCancelInstallModal');

const toastNotice = document.getElementById('toastNotice');

// Tab Navigation
navTabs.forEach(tab => {
  tab.addEventListener('click', () => {
    const target = tab.dataset.tab;
    navTabs.forEach(t => t.classList.toggle('active', t === tab));
    tabContents.forEach(c => c.classList.toggle('active', c.id === `tab-${target}`));

    if (target === 'patchnotes') {
      renderPatchesTimeline();
    }
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
  if (!bytes || bytes === 0) return '0 B';
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
      gameClientStatus.textContent = 'UPDATE AVAILABLE';
      gameClientStatus.style.color = '#ff2a4b';
      btnMainAction.disabled = false;
      btnMainAction.classList.remove('play-state');
      btnMainActionText.textContent = `UPDATE TO ${remoteVer}`;
    } else {
      gameClientStatus.textContent = 'UP TO DATE';
      gameClientStatus.style.color = '#00e676';
      btnMainAction.disabled = false;
      btnMainAction.classList.add('play-state');
      btnMainActionText.textContent = 'PLAY GAME';
    }
  }

  // Update Settings View Elements
  if (currentConfig) {
    settingsInstallPath.value = currentConfig.installDir || '';
    settingsAutoCloseToggle.checked = !!currentConfig.autoCloseLauncher;
  }
}

// Render Patches Chronicles View
function renderPatchesTimeline() {
  if (!allReleases || allReleases.length === 0) {
    patchesTimeline.innerHTML = '<div class="timeline-empty">No official deployments recorded yet.</div>';
    return;
  }

  patchNotesMeta.textContent = `${allReleases.length} Official Release${allReleases.length === 1 ? '' : 's'} Published`;

  const localVer = installedState ? installedState.installedVersion : null;
  const cleanLocal = localVer ? localVer.replace(/^v/i, '') : null;

  let html = '';
  allReleases.forEach((release) => {
    const isCurrent = cleanLocal && release.tag.replace(/^v/i, '') === cleanLocal;
    const isLatest = latestRelease && latestRelease.tag === release.tag;

    const dateFormatted = release.publishedAt 
      ? new Date(release.publishedAt).toLocaleDateString(undefined, {
          year: 'numeric',
          month: 'short',
          day: 'numeric',
          hour: '2-digit',
          minute: '2-digit'
        })
      : 'Recent';

    const cardClass = `patch-card ${isCurrent ? 'is-installed-version' : ''} ${isLatest ? 'is-latest-version' : ''}`;

    let badgesHtml = '';
    if (isCurrent) {
      badgesHtml += '<span class="patch-status-pill current">Installed</span>';
    }
    if (isLatest) {
      badgesHtml += '<span class="patch-status-pill latest">Latest Release</span>';
    }

    let assetsHtml = '';
    if (release.assets && release.assets.length > 0) {
      assetsHtml = release.assets.map(a => {
        let archLabel = a.name.replace('RageBaitYourself-', '').replace('.zip', '');
        if (archLabel.includes('macos')) archLabel = 'macOS Universal';
        else if (archLabel.includes('win-x64')) archLabel = 'Windows 64-bit';
        else if (archLabel.includes('win-ia32')) archLabel = 'Windows 32-bit';
        else if (archLabel.includes('linux')) archLabel = 'Linux x86_64';

        return `<div class="patch-asset-chip"><span>📦 ${archLabel}</span><span class="patch-asset-size">(${formatBytes(a.size)})</span></div>`;
      }).join('');
    }

    html += `
      <div class="${cardClass}">
        <div class="patch-card-top">
          <div class="patch-title-group">
            <span class="patch-release-badge">RELEASE #${release.releaseNumber}</span>
            <span class="patch-tag">${release.tag}</span>
            ${badgesHtml}
          </div>
          <span class="patch-date">📅 ${dateFormatted}</span>
        </div>
        ${assetsHtml ? `<div class="patch-assets-row">${assetsHtml}</div>` : ''}
      </div>
    `;
  });

  patchesTimeline.innerHTML = html;
}

// Initial Sync & Check
async function refreshState() {
  try {
    btnMainActionText.textContent = 'CHECKING...';
    btnMainAction.disabled = true;

    if (homeRefreshIcon) homeRefreshIcon.classList.add('spinning');
    if (refreshReleasesSpin) refreshReleasesSpin.classList.add('spinning');

    // Load config
    currentConfig = await window.launcherAPI.getConfig();

    // Load installed state
    installedState = await window.launcherAPI.checkInstalledStatus();

    // Check remote releases
    latestRelease = await window.launcherAPI.fetchLatestRelease();

    // Fetch all releases for patches timeline
    const releasesRes = await window.launcherAPI.fetchAllReleases();
    if (releasesRes && releasesRes.success) {
      allReleases = releasesRes.releases;
    }

    // Load user profile
    currentUserProfile = await window.launcherAPI.getUserProfile();
    updateUserProfileUI();

    updateLauncherUI();
    renderPatchesTimeline();
  } catch (err) {
    console.error('Sync error:', err);
    updateLauncherUI();
  } finally {
    if (homeRefreshIcon) homeRefreshIcon.classList.remove('spinning');
    if (refreshReleasesSpin) refreshReleasesSpin.classList.remove('spinning');
  }
}

// Update User Profile UI
function updateUserProfileUI() {
  if (currentUserProfile && currentUserProfile.username) {
    navUserName.textContent = currentUserProfile.username.toUpperCase();
    settingsProfileUsername.textContent = currentUserProfile.username;
    settingsProfileEmail.textContent = currentUserProfile.email || 'None';
  } else {
    navUserName.textContent = 'REGISTER';
    settingsProfileUsername.textContent = 'Not registered';
    settingsProfileEmail.textContent = '-';
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

  // If NOT installed, ask user for folder confirmation before proceeding
  if (!isInstalled) {
    modalInstallPathInput.value = currentConfig ? currentConfig.installDir : '';
    installLocationModal.style.display = 'flex';
    pendingInstallAction = executeInstallPipeline;
    return;
  }

  // If already installed and update is available, install update directly
  await executeInstallPipeline();
});

// Core install execution
async function executeInstallPipeline() {
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
      showToast('Game installed successfully!');
      await refreshState();
    } else {
      showToast('Install failed: ' + res.error);
      progressContainer.style.display = 'none';
      updateLauncherUI();
    }
    return;
  }

  // Fallback to local
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
}

// Modal Install Path Handlers
btnModalBrowsePath.addEventListener('click', async () => {
  const chosen = await window.launcherAPI.selectDirectory();
  if (chosen) {
    modalInstallPathInput.value = chosen;
  }
});

btnConfirmInstallModal.addEventListener('click', async () => {
  const chosenPath = modalInstallPathInput.value;
  installLocationModal.style.display = 'none';

  if (chosenPath && chosenPath !== currentConfig.installDir) {
    await window.launcherAPI.changeInstallDir(chosenPath);
    currentConfig = await window.launcherAPI.getConfig();
  }

  if (pendingInstallAction) {
    const action = pendingInstallAction;
    pendingInstallAction = null;
    await action();
  }
});

btnCancelInstallModal.addEventListener('click', () => {
  installLocationModal.style.display = 'none';
  pendingInstallAction = null;
});

// Home refresh button handler
btnHomeRefresh.addEventListener('click', async () => {
  await refreshState();
  showToast('Client status refreshed!');
});

btnOpenGameDir.addEventListener('click', () => {
  window.launcherAPI.openGameDir();
});

btnRefreshReleases.addEventListener('click', async () => {
  await refreshState();
  showToast('Releases refreshed!');
});

// Settings: Change directory & move files
btnSettingsChangePath.addEventListener('click', async () => {
  const selected = await window.launcherAPI.selectDirectory();
  if (!selected) return;

  btnSettingsChangePath.disabled = true;
  btnSettingsChangePath.textContent = 'Moving files...';

  const res = await window.launcherAPI.changeInstallDir(selected);
  btnSettingsChangePath.disabled = false;
  btnSettingsChangePath.textContent = 'Browse & Move...';

  if (res.success) {
    settingsInstallPath.value = res.newDir;
    showToast('Game files successfully moved to new location!');
    await refreshState();
  } else {
    showToast('Failed to move files: ' + res.error);
  }
});

// Settings: Edit profile
btnSettingsEditProfile.addEventListener('click', () => {
  if (currentUserProfile) {
    regUsername.value = currentUserProfile.username || '';
    regEmail.value = currentUserProfile.email || '';
    regPassword.value = currentUserProfile.password || '';
  }
  registrationModal.style.display = 'flex';
});

// Nav User Badge Click -> Open Settings or Registration
navUserBadge.addEventListener('click', () => {
  if (!currentUserProfile) {
    registrationModal.style.display = 'flex';
  } else {
    const settingsTab = document.querySelector('.nav-tab[data-tab="settings"]');
    if (settingsTab) settingsTab.click();
  }
});

// Registration Form Submit
registrationForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  const username = regUsername.value.trim();
  const email = regEmail.value.trim();
  const password = regPassword.value;

  if (!username) {
    showToast('Please enter a username.');
    return;
  }

  const res = await window.launcherAPI.saveUserProfile({ username, email, password });
  if (res.success) {
    currentUserProfile = res.profile;
    registrationModal.style.display = 'none';
    updateUserProfileUI();
    showToast(`Welcome, ${username}! Profile saved and synced with game account.`);
  } else {
    showToast('Error saving profile: ' + res.error);
  }
});

// Settings: Auto-close toggle
settingsAutoCloseToggle.addEventListener('change', async (e) => {
  if (!currentConfig) return;
  currentConfig.autoCloseLauncher = e.target.checked;
  await window.launcherAPI.saveConfig(currentConfig);
  showToast(e.target.checked ? 'Launcher will minimize when game runs' : 'Launcher will stay open');
});

// About Dev handlers
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
  currentUserProfile = await window.launcherAPI.getUserProfile();

  // If no user profile exists, prompt user registration immediately on launch!
  if (!currentUserProfile || !currentUserProfile.username) {
    registrationModal.style.display = 'flex';
  }

  await refreshState();
}

init();
