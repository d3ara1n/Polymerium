import {existsSync, readFileSync} from 'node:fs';

const assets = JSON.parse(readFileSync('assets.json', 'utf8'));
const missing = assets.screenshots.filter(item => !item.src || !existsSync(item.src));
if (missing.length || !assets.music || !existsSync(assets.music)) {
  console.error('发布片素材尚未齐备：');
  for (const item of missing) console.error(`- 截图：${item.label}（在 assets.json 中指定项目内路径）`);
  if (!assets.music || !existsSync(assets.music)) console.error('- 配乐：待选定并放入项目');
  process.exitCode = 1;
}
