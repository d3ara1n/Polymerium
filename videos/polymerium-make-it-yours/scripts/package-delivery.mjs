import fs from 'node:fs';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import sharp from 'sharp';
const root=path.resolve(import.meta.dirname,'..');
const film=process.argv[2] || 'delivery/polymerium-make-it-yours.mp4';
const times=[1.5,2.9,8,11.3,14.5,19.9,23.5,25.6,30.5];
fs.mkdirSync(path.join(root,'renders/final-frames'),{recursive:true});
const composites=[];
for(const [i,t] of times.entries()){
 const target=`renders/final-frames/${String(i+1).padStart(2,'0')}-${t}s.png`;
 const r=spawnSync('ffmpeg',['-hide_banner','-y','-ss',String(t),'-i',film,'-frames:v','1',target],{cwd:root,encoding:'utf8'});if(r.status!==0)throw new Error(r.stderr);
 const thumbnail=await sharp(path.join(root,target)).resize(640,360).toBuffer();
 const label=Buffer.from(`<svg width="640" height="36"><rect width="640" height="36" fill="#171611"/><text x="16" y="25" fill="#E9E4D8" font-family="sans-serif" font-size="17">${String(i+1).padStart(2,'0')} / ${t.toFixed(1)} seconds</text></svg>`);
 composites.push({input:thumbnail,left:(i%3)*640,top:Math.floor(i/3)*396+36},{input:label,left:(i%3)*640,top:Math.floor(i/3)*396});
}
await sharp({create:{width:1920,height:1188,channels:3,background:'#171611'}}).composite(composites).jpeg({quality:94}).toFile(path.join(root,'delivery/contact-sheet.jpg'));
fs.copyFileSync(path.join(root,'renders/final-frames/09-30.5s.png'),path.join(root,'delivery/poster.png'));
fs.copyFileSync(path.join(root,'renders/final-frames/01-1.5s.png'),path.join(root,'delivery/key-visual.png'));
console.log('Created contact sheet, poster, and key visual from the delivered MP4.');
