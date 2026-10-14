const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const rootDir = path.resolve(__dirname, '../..');
const finalBuildsDir = path.join(rootDir, 'FinalBuilds');
const outputDir = path.join(__dirname, '../dist-game-releases');

console.log('📦 Starting RageBait Yourself Game Packaging Pipeline...');
console.log(`- Source directory: ${finalBuildsDir}`);
console.log(`- Output directory: ${outputDir}`);

if (!fs.existsSync(finalBuildsDir)) {
  console.error(`❌ FinalBuilds directory not found at ${finalBuildsDir}`);
  process.exit(1);
}

fs.mkdirSync(outputDir, { recursive: true });

const targets = [
  {
    name: 'macOS Universal',
    candidates: ['RageBait Yourself.app', 'RageBaitYourself.app', 'RageBaitYourself-0.1-macos-universal.app'],
    outputZip: 'RageBaitYourself-macos.zip'
  },
  {
    name: 'Windows 64-bit',
    candidates: ['RageBaitYourself-win-x64', 'RageBaitYourself-0.1-win-x64'],
    outputZip: 'RageBaitYourself-win-x64.zip'
  },
  {
    name: 'Windows 32-bit (x86)',
    candidates: ['RageBaitYourself-win-ia32', 'RageBaitYourself-0.1-win-x86'],
    outputZip: 'RageBaitYourself-win-ia32.zip'
  },
  {
    name: 'Linux x86_64',
    candidates: ['RageBaitYourself-linux-x86_64', 'RageBaitYourself-0.1-linux-x86_64'],
    outputZip: 'RageBaitYourself-linux-x86_64.zip'
  }
];

for (const target of targets) {
  const matchedFolder = target.candidates.find(folder => fs.existsSync(path.join(finalBuildsDir, folder)));
  const zipPath = path.join(outputDir, target.outputZip);

  if (!matchedFolder) {
    console.warn(`⚠️ Warning: No source build found for ${target.name}, skipping.`);
    continue;
  }

  const sourcePath = path.join(finalBuildsDir, matchedFolder);
  console.log(`\n🗜️ Compressing ${target.name} (${matchedFolder}) -> ${target.outputZip}...`);
  if (fs.existsSync(zipPath)) {
    fs.unlinkSync(zipPath);
  }

  try {
    // Use system zip for optimal preservation of executable permissions & symlinks on macOS
    execSync(`cd "${finalBuildsDir}" && zip -r -q -9 "${zipPath}" "${matchedFolder}"`);
    const stats = fs.statSync(zipPath);
    const mb = (stats.size / (1024 * 1024)).toFixed(2);
    console.log(`✅ Created ${target.outputZip} (${mb} MB)`);
  } catch (err) {
    console.error(`❌ Failed to compress ${target.folder}:`, err.message);
  }
}

console.log('\n🎉 Game packaging complete! All archives in:');
console.log(outputDir);
console.log('\nUpload these zip files directly to your GitHub Release (e.g. tag v0.1 or v0.2).');
console.log('Players with RageBait Launcher will automatically detect and download them!');
