import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';

const root = path.resolve(import.meta.dirname, '..');
const shots = path.resolve(root, '../../assets/screenshots');
const crops = [
  ['setup.png','setup-main.png',710,38,1484,1180],
  ['setup.png','package-mouse.png',746,350,1440,136],
  ['setup.png','package-apple.png',746,500,1440,136],
  ['setup.png','package-control.png',746,646,1440,136],
  ['landing.png','instance-fo.png',20,364,510,173],
  ['landing.png','instance-cherry.png',20,547,510,175],
  ['landing.png','play-panel.png',581,195,1136,493]
];
fs.mkdirSync(path.join(root,'out/screens'),{recursive:true});
for(const [input,output,left,top,width,height] of crops){
  await sharp(path.join(shots,input)).extract({left,top,width,height}).png().toFile(path.join(root,'out/screens',output));
}
console.log(`Created ${crops.length} crops in out/screens from repository screenshots.`);
