// RageBait Yourself - Landing Page Engine
const OWNER = 'Anupam1707';
const REPO = 'RageBaitYourself';
const FIREBASE_DB = 'https://ragebaitdb-default-rtdb.firebaseio.com';

document.addEventListener('DOMContentLoaded', () => {
  initAmbientCanvas();
  initOSDetection();
  initTabs();
  initNavbarScroll();
  fetchReleaseAssets();
  fetchDeveloperProfile();
  pingCloudStatus();
});

// 1. Ambient Background Glow Canvas
function initAmbientCanvas() {
  const canvas = document.getElementById('ambient-canvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');

  let width, height;
  let particles = [];

  function resize() {
    width = canvas.width = window.innerWidth;
    height = canvas.height = window.innerHeight;
  }
  window.addEventListener('resize', resize);
  resize();

  for (let i = 0; i < 45; i++) {
    particles.push({
      x: Math.random() * width,
      y: Math.random() * height,
      vx: (Math.random() - 0.5) * 0.4,
      vy: (Math.random() - 0.5) * 0.4,
      radius: Math.random() * 2 + 0.8,
      color: Math.random() > 0.4 ? 'rgba(0, 240, 255, ' : 'rgba(255, 46, 99, ',
      alpha: Math.random() * 0.4 + 0.1
    });
  }

  function loop() {
    ctx.clearRect(0, 0, width, height);

    for (let p of particles) {
      p.x += p.vx;
      p.y += p.vy;

      if (p.x < 0) p.x = width;
      if (p.x > width) p.x = 0;
      if (p.y < 0) p.y = height;
      if (p.y > height) p.y = 0;

      ctx.beginPath();
      ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
      ctx.fillStyle = p.color + p.alpha + ')';
      ctx.fill();
    }
    requestAnimationFrame(loop);
  }
  loop();
}

// 2. Client OS Auto-Detection
function initOSDetection() {
  const userAgent = window.navigator.userAgent || '';
  const platform = window.navigator.platform || '';
  const heroBtn = document.getElementById('hero-download-btn');
  const osText = document.getElementById('detected-os');

  let detected = 'macOS';
  let targetTab = 'mac';
  let downloadUrl = `https://github.com/${OWNER}/${REPO}/releases/latest/download/RageBait.Launcher-1.0.0-universal.dmg`;

  if (/Win/i.test(platform) || /Windows/i.test(userAgent)) {
    detected = 'Windows 64-bit';
    targetTab = 'win';
    downloadUrl = `https://github.com/${OWNER}/${REPO}/releases/latest/download/RageBait.Launcher.Setup.1.0.0.exe`;
  } else if (/Linux/i.test(platform) || /Linux/i.test(userAgent)) {
    detected = 'Linux (x86_64)';
    targetTab = 'linux';
    downloadUrl = `https://github.com/${OWNER}/${REPO}/releases/latest/download/RageBait-Launcher-1.0.0.AppImage`;
  } else if (/Mac/i.test(platform) || /Macintosh/i.test(userAgent)) {
    detected = 'macOS Universal (Apple Silicon & Intel)';
    targetTab = 'mac';
    downloadUrl = `https://github.com/${OWNER}/${REPO}/releases/latest/download/RageBait.Launcher-1.0.0-universal.dmg`;
  }

  if (osText) {
    osText.textContent = `Auto-detected: ${detected}`;
  }

  if (heroBtn) {
    heroBtn.href = downloadUrl;
    heroBtn.setAttribute('data-target-tab', targetTab);
  }

  // Pre-activate detected tab
  switchTab(targetTab);
}

// 3. Tab Navigation
function initTabs() {
  const tabs = document.querySelectorAll('.tab-btn');
  tabs.forEach(tab => {
    tab.addEventListener('click', () => {
      const target = tab.getAttribute('data-tab');
      switchTab(target);
    });
  });
}

function switchTab(target) {
  document.querySelectorAll('.tab-btn').forEach(t => {
    t.classList.toggle('active', t.getAttribute('data-tab') === target);
  });
  document.querySelectorAll('.platform-panel').forEach(p => {
    p.classList.toggle('active', p.id === `tab-${target}`);
  });
}

// 4. Navbar Sticky State
function initNavbarScroll() {
  const nav = document.querySelector('.site-nav');
  window.addEventListener('scroll', () => {
    if (window.scrollY > 40) {
      nav.classList.add('scrolled');
    } else {
      nav.classList.remove('scrolled');
    }
  });
}

// 5. GitHub API Live Asset Fetching & Dynamic Linking
async function fetchReleaseAssets() {
  try {
    const res = await fetch(`https://api.github.com/repos/${OWNER}/${REPO}/releases/latest`);
    if (!res.ok) return;
    const release = await res.json();

    // Dynamically link version tag to release URL
    const tagBadge = document.getElementById('live-release-tag');
    const releaseLink = document.getElementById('live-release-link');
    if (tagBadge && release.tag_name) {
      tagBadge.textContent = `${release.tag_name} Latest`;
    }
    if (releaseLink && release.html_url) {
      releaseLink.href = release.html_url;
      releaseLink.title = `View ${release.name || release.tag_name} on GitHub`;
    }

    if (release.assets && release.assets.length > 0) {
      let macAsset = null;
      let winAsset = null;
      let linuxAsset = null;

      // Map assets to matching card buttons
      release.assets.forEach(asset => {
        const name = asset.name.toLowerCase();
        let targetEl = null;

        if (name.endsWith('.dmg')) {
          targetEl = document.getElementById('dl-mac-dmg');
          macAsset = asset;
        } else if (name.endsWith('.pkg')) {
          targetEl = document.getElementById('dl-mac-pkg');
          if (!macAsset) macAsset = asset;
        } else if (name.endsWith('.exe') || (name.includes('win') && name.endsWith('.zip'))) {
          targetEl = document.getElementById('dl-win-exe');
          winAsset = asset;
        } else if (name.endsWith('.appimage')) {
          targetEl = document.getElementById('dl-linux-appimage');
          linuxAsset = asset;
        } else if (name.includes('macos.zip')) {
          targetEl = document.getElementById('dl-game-mac');
        } else if (name.includes('win-x64.zip')) {
          targetEl = document.getElementById('dl-game-win');
        } else if (name.includes('linux-x86_64.zip')) {
          targetEl = document.getElementById('dl-game-linux');
        }

        if (targetEl) {
          targetEl.href = asset.browser_download_url;
          const mb = (asset.size / (1024 * 1024)).toFixed(1);
          const sizeBadge = targetEl.parentElement.querySelector('.asset-size');
          if (sizeBadge) sizeBadge.textContent = `${mb} MB`;
        }
      });

      // Update hero CTA button dynamically based on detected OS
      const heroBtn = document.getElementById('hero-download-btn');
      if (heroBtn) {
        const targetTab = heroBtn.getAttribute('data-target-tab');
        if (targetTab === 'mac' && macAsset) {
          heroBtn.href = macAsset.browser_download_url;
        } else if (targetTab === 'win' && winAsset) {
          heroBtn.href = winAsset.browser_download_url;
        } else if (targetTab === 'linux' && linuxAsset) {
          heroBtn.href = linuxAsset.browser_download_url;
        }
      }
    }
  } catch (err) {
    console.warn('Could not fetch latest release from GitHub API:', err);
  }
}

// 6. Fetch Developer Profile from GitHub
async function fetchDeveloperProfile() {
  try {
    const res = await fetch(`https://api.github.com/users/${OWNER}`);
    if (!res.ok) return;
    const user = await res.json();

    const avatar = document.getElementById('dev-avatar');
    if (avatar && user.avatar_url) {
      avatar.src = user.avatar_url;
    }

    const nameEl = document.getElementById('dev-name');
    if (nameEl && user.name) {
      nameEl.textContent = user.name;
    }

    const reposEl = document.getElementById('dev-repos');
    if (reposEl && user.public_repos !== undefined) {
      reposEl.textContent = user.public_repos;
    }

    const locEl = document.getElementById('dev-location');
    if (locEl && user.location) {
      locEl.textContent = user.location;
    }
  } catch (err) {
    console.warn('Could not fetch GitHub user info:', err);
  }
}

// 7. Firebase RTDB Cloud Status Ping
async function pingCloudStatus() {
  try {
    const res = await fetch(`${FIREBASE_DB}/.json?shallow=true`, { method: 'HEAD' });
    const cloudBadge = document.getElementById('cloud-sync-status');
    if (cloudBadge && res.ok) {
      cloudBadge.textContent = 'ONLINE • RTDB LIVE';
      cloudBadge.style.color = '#00ff88';
    }
  } catch (_) {}
}

