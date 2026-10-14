const fs = require('fs');
const path = require('path');
const https = require('https');
const { execSync } = require('child_process');

const OWNER = 'Anupam1707';
const REPO = 'RageBaitYourself';
const releasesDir = path.resolve(__dirname, '../dist-game-releases');

// 1. Get Tag from CLI argument or default to v0.1
const tag = process.argv[2] || 'v0.1';
const releaseTitle = process.argv[3] || `RageBait Yourself ${tag}`;
const releaseNotes = process.argv[4] || `Game update ${tag} for macOS, Windows, and Linux.`;

console.log(`\n🚀 RageBait CLI Release Publisher`);
console.log(`- Target Repository: ${OWNER}/${REPO}`);
console.log(`- Release Tag: ${tag}`);
console.log(`- Release Title: ${releaseTitle}\n`);

// 2. Obtain GitHub Token
function getGitHubToken() {
  if (process.env.GITHUB_TOKEN) {
    return process.env.GITHUB_TOKEN;
  }
  try {
    const creds = execSync('printf "protocol=https\\nhost=github.com\\n\\n" | git credential fill', {
      encoding: 'utf8',
      stdio: ['pipe', 'pipe', 'ignore']
    });
    const match = creds.match(/password=(.+)/);
    if (match && match[1]) {
      return match[1].trim();
    }
  } catch (err) {
    // ignore
  }
  return null;
}

const token = getGitHubToken();
if (!token) {
  console.error('❌ Error: Could not find GitHub token.');
  console.error('Please set GITHUB_TOKEN environment variable or log in with git.');
  process.exit(1);
}

// 3. Always package fresh release zips from FinalBuilds
console.log('📦 Packaging fresh release archives from FinalBuilds...');
execSync('npm run package-game', { cwd: path.resolve(__dirname, '..'), stdio: 'inherit' });

const zipFiles = fs.readdirSync(releasesDir).filter(f => f.endsWith('.zip'));
if (zipFiles.length === 0) {
  console.error('❌ No .zip files found in dist-game-releases to upload.');
  process.exit(1);
}

function githubRequest(endpoint, method = 'GET', data = null, customHeaders = {}) {
  return new Promise((resolve, reject) => {
    const url = new URL(endpoint.startsWith('http') ? endpoint : `https://api.github.com${endpoint}`);
    const headers = {
      'User-Agent': 'RageBait-Release-CLI',
      'Authorization': `token ${token}`,
      'Accept': 'application/vnd.github.v3+json',
      ...customHeaders
    };

    if (data && typeof data === 'string') {
      headers['Content-Length'] = Buffer.byteLength(data);
    }

    const req = https.request(url, { method, headers }, (res) => {
      let responseBody = '';
      res.on('data', chunk => { responseBody += chunk; });
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          try {
            resolve(JSON.parse(responseBody || '{}'));
          } catch (_) {
            resolve(responseBody);
          }
        } else {
          reject(new Error(`GitHub API Error (${res.statusCode}): ${responseBody}`));
        }
      });
    });

    req.on('error', reject);
    if (data) req.write(data);
    req.end();
  });
}

function getMimeType(filename) {
  if (filename.endsWith('.dmg')) return 'application/x-apple-diskimage';
  if (filename.endsWith('.pkg')) return 'application/octet-stream';
  if (filename.endsWith('.AppImage')) return 'application/x-executable';
  if (filename.endsWith('.tar.gz')) return 'application/gzip';
  if (filename.endsWith('.zip')) return 'application/zip';
  return 'application/octet-stream';
}

async function uploadAsset(uploadUrlTemplate, filePath) {
  const fileName = path.basename(filePath);
  const fileStats = fs.statSync(filePath);
  const fileSize = fileStats.size;
  const mb = (fileSize / (1024 * 1024)).toFixed(2);
  const mimeType = getMimeType(fileName);

  // Upload url comes in format: https://uploads.github.com/repos/.../releases/{id}/assets{?name,label}
  const baseUrl = uploadUrlTemplate.replace(/\{\?name,label\}/, '');
  const uploadUrl = `${baseUrl}?name=${encodeURIComponent(fileName)}`;

  console.log(`📤 Uploading ${fileName} (${mb} MB)...`);

  return new Promise((resolve, reject) => {
    const url = new URL(uploadUrl);
    const req = https.request(url, {
      method: 'POST',
      headers: {
        'User-Agent': 'RageBait-Release-CLI',
        'Authorization': `token ${token}`,
        'Content-Type': mimeType,
        'Content-Length': fileSize
      }
    }, (res) => {
      let body = '';
      res.on('data', chunk => { body += chunk; });
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          console.log(`✅ Uploaded ${fileName}`);
          resolve(JSON.parse(body || '{}'));
        } else {
          reject(new Error(`Failed to upload ${fileName} (${res.statusCode}): ${body}`));
        }
      });
    });

    req.on('error', reject);
    const readStream = fs.createReadStream(filePath);
    readStream.pipe(req);
  });
}

async function main() {
  try {
    // Collect all candidate assets: Launchers + Game builds
    const filesToUpload = [];

    // 1. Game Archives
    if (fs.existsSync(releasesDir)) {
      const gameFiles = fs.readdirSync(releasesDir).filter(f => f.endsWith('.zip'));
      for (const f of gameFiles) {
        filesToUpload.push(path.join(releasesDir, f));
      }
    }

    // 2. Launcher Installers from Launcher/dist
    const launcherDistDir = path.resolve(__dirname, '../dist');
    if (fs.existsSync(launcherDistDir)) {
      const allowedExts = ['.dmg', '.pkg', '.AppImage', '.tar.gz', '.zip'];
      const launcherFiles = fs.readdirSync(launcherDistDir).filter(f => {
        const lower = f.toLowerCase();
        if (lower.endsWith('.blockmap') || lower.endsWith('.yml') || lower.endsWith('.yaml') || lower.endsWith('.plist')) {
          return false;
        }
        return allowedExts.some(ext => lower.endsWith(ext));
      });

      for (const f of launcherFiles) {
        filesToUpload.push(path.join(launcherDistDir, f));
      }
    }

    console.log(`\nFound ${filesToUpload.length} total release assets to publish (Launchers + Game Builds).`);

    // 1. Check if release already exists for this tag
    let release;
    try {
      release = await githubRequest(`/repos/${OWNER}/${REPO}/releases/tags/${tag}`);
      console.log(`ℹ️ Found existing release for tag ${tag} (ID: ${release.id})`);
    } catch (_) {
      console.log(`🆕 Creating new GitHub release for tag: ${tag}...`);
      release = await githubRequest(`/repos/${OWNER}/${REPO}/releases`, 'POST', JSON.stringify({
        tag_name: tag,
        target_commitish: 'main',
        name: releaseTitle,
        body: releaseNotes,
        draft: false,
        prerelease: false
      }), { 'Content-Type': 'application/json' });
      console.log(`✅ Created GitHub release: "${release.name}" (ID: ${release.id})`);
    }

    // 2. Fetch existing assets to avoid duplicate upload errors
    const existingAssets = release.assets || [];

    for (const filePath of filesToUpload) {
      const fileName = path.basename(filePath);
      const existing = existingAssets.find(a => a.name === fileName);
      if (existing) {
        console.log(`🗑️ Removing previous version of ${fileName} (ID: ${existing.id})...`);
        await githubRequest(`/repos/${OWNER}/${REPO}/releases/assets/${existing.id}`, 'DELETE');
      }

      await uploadAsset(release.upload_url, filePath);
    }


    console.log(`\n🎉 Success! Release published to:`);
    console.log(release.html_url);
    console.log(`\nPlayers using RageBait Launcher can now update directly from this release!`);
  } catch (err) {
    console.error(`\n❌ Release failed: ${err.message}`);
    process.exit(1);
  }
}

main();
