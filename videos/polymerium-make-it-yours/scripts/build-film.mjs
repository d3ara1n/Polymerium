import fs from 'node:fs';
import path from 'node:path';
import {createMinecraftWorldSvg} from './minecraft-world.mjs';
import {createCacheInstancesHtml} from './cache-instances.mjs';
import {BEAT, DURATION, SHOTS, SEAMS, b} from './cues.mjs';

const root=path.resolve(import.meta.dirname,'..');
const out=path.join(root,'compositions');
fs.mkdirSync(out,{recursive:true});
fs.writeFileSync(path.join(root,'ledger.json'),JSON.stringify({fps:60,seams:SEAMS},null,2)+'\n');
const letters=s=>[...s].map(c=>`<span class="letter">${c===' '?'&nbsp;':c}</span>`).join('');
const image=(file,cls='',id='')=>`<img ${id?`id="${id}"`:''} class="${cls}" src="../out/screens/${file}.png" alt="Polymerium interface"/>`;
const mark=fs.readFileSync(path.join(root,'assets/brand/logo-dark.svg'),'utf8')
 .replace('width="128" height="128"','id="brand-mark" width="360" height="360"')
 .replaceAll('id="link"','id="final-link"').replaceAll('#link','#final-link')
 .replaceAll('id="weave"','id="final-weave"').replaceAll('#weave','#final-weave')
 .replaceAll('<g transform="translate(-17 17)">','<g class="link-ivory-motion"><g transform="translate(-17 17)">')
 .replace('<g transform="translate(17 -17)">','<g class="link-gold-motion"><g transform="translate(17 -17)">')
 .replace(/(<use href="#final-link" stroke="#[A-Fa-f0-9]+"\/>\s*<\/g>)/g,'$1</g>');
// The clipped overpass belongs to the ivory link and travels with it.
const brandMark=mark.replace('</g></g>\n    </g>','</g>\n    </g></g>');

function sculpture(){
 const blocks=[];
 for(let x=0;x<4;x++)for(let y=0;y<4;y++)for(let z=0;z<3;z++)blocks.push({x,y,z});
 blocks.sort((a,b)=>(a.x+a.y)-(b.x+b.y)||a.z-b.z);
 return `<svg viewBox="0 0 920 860" aria-label="Assembling block sculpture">${blocks.map(({x,y,z},i)=>{
 const s=67,px=460+(x-y)*s,py=310+(x+y)*s*.5-z*s;
 const c=(x+y+z)%7===0?['#F2EEE5','#B9B0A0','#D4CBBA']:['#F7C66C','#B97824','#EEA93B'];
 return `<g transform="translate(${px} ${py})"><g class="voxel" data-x="${x}" data-y="${y}" data-z="${z}"><path d="M0 0L${s} ${s*.5}L0 ${s}L${-s} ${s*.5}Z" fill="${c[0]}"/><path d="M${-s} ${s*.5}L0 ${s}V${s*2}L${-s} ${s*1.5}Z" fill="${c[1]}"/><path d="M0 ${s}L${s} ${s*.5}V${s*1.5}L0 ${s*2}Z" fill="${c[2]}"/></g></g>`;
 }).join('')}</svg>`;
}
const rayField=(id)=>`<svg id="${id}" class="ray-field" viewBox="0 0 1920 1080" aria-hidden="true">${Array.from({length:32},(_,i)=>{
 const a=i/32*Math.PI*2,r=225,R=410+(i%3)*45;
 return `<path d="M${960+Math.cos(a)*r} ${540+Math.sin(a)*r}L${960+Math.cos(a)*R} ${540+Math.sin(a)*R}"/>`;
}).join('')}</svg>`;
const common=`
@font-face{font-family:Geist;src:url('../assets/fonts/geist-latin.woff2');font-weight:100 900}
@font-face{font-family:'Geist Mono';src:url('../assets/fonts/geist-mono-latin.woff2');font-weight:100 900}
*{box-sizing:border-box;margin:0;padding:0}
#root{position:absolute;inset:0;width:100%;height:100%;overflow:hidden;font-family:Geist,sans-serif;color:#F2EEE5}
.stage{position:absolute;inset:0;overflow:hidden;background:#171611}
.paper{background:#F2EEE5;color:#29261F}.gold{background:#EEA93B;color:#29261F}
.title{font-size:200px;line-height:.94;letter-spacing:-.075em;font-weight:850}
.word{display:flex}.letter{display:inline-block;flex:0 0 auto}.mono{font:22px 'Geist Mono',monospace;letter-spacing:.06em}
.screen{overflow:hidden;border-radius:24px;box-shadow:0 30px 65px #0003;background:#F5F5F5}.screen img{width:100%;display:block}
.ray-field{position:absolute;inset:0;width:1920px;height:1080px;fill:none;stroke:#EEA93B;stroke-width:8;pointer-events:none}
`;
const helper=`
const B=${BEAT};
const beat=n=>n*B;
const tl=gsap.timeline({paused:true});
function enter(sel,at,vars={},duration=.48){tl.fromTo(sel,{x:300,y:0,rotation:8,opacity:0,...vars},{x:0,y:0,rotation:0,opacity:1,duration,ease:'power4.out'},at);}
function text(sel,at,d=.5){tl.fromTo(sel,{y:250,rotation:12,opacity:0},{y:0,rotation:0,opacity:1,duration:d,stagger:.035,ease:'power4.out'},at);}
`;
const scenes={};
scenes.world={
 css:`
#impulse{position:absolute;left:900px;top:480px;width:120px;height:120px;border-radius:24px;background:#EEA93B}
#world-rays{stroke:#A66717}
#world-type{position:absolute;left:200px;top:245px;transform-origin:0 0;z-index:3}
#world-your{font-size:250px}#world-word{font-size:285px}
#world-rules{position:absolute;left:205px;top:710px;font-size:68px;font-weight:650;letter-spacing:-.05em}
#world-rules span{display:block}#world-rules span:last-child{color:#94601A}
#tower{position:absolute;left:910px;top:130px;width:900px;height:850px;transform-origin:50% 50%}#tower svg{width:100%;height:100%}
#world-underline{position:absolute;left:207px;top:653px;width:610px;height:10px;background:#EEA93B;transform-origin:left}
`,
 html:`<div class="stage paper"><div id="impulse"></div>${rayField('world-rays')}<h1 id="world-type" class="title"><span id="world-your" class="word">${letters('YOUR')}</span><span id="world-word" class="word">${letters('WORLD.')}</span></h1><div id="world-underline"></div><div id="world-rules"><span>Your rules.</span><span>Your way.</span></div><div id="tower">${sculpture()}</div></div>`,
 motion:`
tl.fromTo('#impulse',{y:-480,rotation:-45,scale:.45},{y:0,rotation:0,scale:1,duration:beat(1),ease:'power4.in'},0);
tl.to('#impulse',{scaleX:12,scaleY:.08,duration:.19,ease:'power4.out'},beat(1));
tl.to('#impulse',{scaleX:16,opacity:0,duration:.2},beat(1)+.19);
tl.fromTo('#world-rays',{scale:.12,opacity:1},{scale:2.7,opacity:0,duration:.8,ease:'power3.out'},beat(1));
text('#world-your .letter',beat(1));text('#world-word .letter',beat(1.5));
tl.to('#world-type',{x:0,y:-40,scale:.64,duration:.6,ease:'power4.inOut'},beat(3));
tl.fromTo('#tower',{x:470,y:150,scale:.65,rotation:-18},{x:0,y:0,scale:1,rotation:0,duration:.65,ease:'power4.out'},beat(3));
[...document.querySelectorAll('#tower .voxel')].forEach((el,i)=>{
 const x=+el.dataset.x,y=+el.dataset.y,z=+el.dataset.z;
 tl.fromTo(el,{x:(x-1.5)*140,y:-220-z*90,opacity:0},{x:0,y:0,opacity:1,duration:.48,ease:'back.out(1.45)'},beat(3)+z*.14+i*.003);
 tl.to(el,{x:(x-1.5)*54,y:(y-1.5)*25-z*40,duration:.42,ease:'power3.inOut'},beat(5.5)+z*.04);
 tl.to(el,{x:(x-1.5)*210-300,y:(y-1.5)*110-z*100,rotation:(x-y)*12,duration:.48,ease:'power3.in'},beat(7));
});
enter('#world-rules span:first-child',beat(4),{x:0,y:100});enter('#world-rules span:last-child',beat(5),{x:0,y:100});
tl.fromTo('#world-underline',{scaleX:0},{scaleX:1,duration:.4,ease:'power4.out'},beat(5));
tl.to('#tower',{rotation:9,scale:1.08,duration:.65,ease:'power2.inOut'},beat(6));
`};
scenes.build={
 css:`
#build-word{position:absolute;left:270px;top:330px;font-size:360px;transform-origin:0 0;color:#EEA93B;z-index:4}
#mix{position:absolute;left:220px;top:370px;font-size:116px;line-height:.95;font-weight:800;letter-spacing:-.06em;width:350px}
#build-rig{position:absolute;left:680px;top:190px;width:1060px;height:760px;perspective:1800px}
#build-panel{height:760px;transform-origin:50% 30%}
#build-focus{position:absolute;left:28px;top:53px;width:165px;height:74px;border:4px solid #EEA93B;border-radius:14px}
#build-rows{position:absolute;left:650px;top:470px;width:1110px;perspective:1600px}.build-row{position:absolute;width:1110px;height:105px;border-radius:18px;box-shadow:0 24px 48px #0005;overflow:hidden}.build-row img{width:100%}#row-apple{top:132px}#row-control{top:264px}
#build-labels{position:absolute;left:228px;top:695px;display:flex;flex-direction:column;gap:25px;color:#D4CBBA}.build-label{display:flex;gap:15px;align-items:center}.build-label i{width:10px;height:10px;background:#EEA93B}
`,
 html:`<div class="stage"><h2 id="build-word" class="title word">${letters('BUILD.')}</h2><div id="mix">YOUR MIX.</div><div id="build-rig"><div id="build-panel" class="screen">${image('setup-main')}<div id="build-focus"></div></div></div><div id="build-rows">${['mouse','apple','control'].map(n=>`<div class="build-row" id="row-${n}">${image('package-'+n)}</div>`).join('')}</div><div id="build-labels" class="mono">${['Minecraft version','Mod loader','Your packages'].map(s=>`<div class="build-label"><i></i>${s}</div>`).join('')}</div></div>`,
 motion:`
text('#build-word .letter',0);
tl.to('#build-word',{x:-50,y:-140,scale:.42,duration:.55,ease:'power4.inOut'},beat(1.5));
enter('#mix',beat(2.5),{x:-280,y:0});
tl.fromTo('#build-rig',{x:900,y:180,rotation:14,scale:.7,opacity:0},{x:0,y:0,rotation:0,scale:1,opacity:1,duration:.7,ease:'power4.out'},beat(1.5));
tl.fromTo('#build-panel',{rotationY:-24,rotationX:10},{rotationY:0,rotationX:0,duration:.7,ease:'power4.out'},beat(1.5));
tl.fromTo('#build-focus',{opacity:0},{opacity:1,duration:.1},beat(3));
tl.to('#build-focus',{x:184,scaleX:1.7,transformOrigin:'left',duration:.25,ease:'power4.inOut'},beat(4));
tl.to('#build-focus',{opacity:0,duration:.15},beat(5));
['#row-mouse','#row-apple','#row-control'].forEach((s,i)=>{
 tl.fromTo(s,{x:950,y:100,rotation:16,rotationY:-30,opacity:0},{x:-30+i*18,y:0,rotation:-3+i*3,rotationY:0,opacity:1,duration:.48,ease:'power4.out'},beat(4+i));
 tl.to(s,{x:0,rotation:0,duration:.3,ease:'power3.inOut'},beat(7)+i*.025);
});
[...document.querySelectorAll('.build-label')].forEach((el,i)=>enter(el,beat(3+i),{x:-80,y:0},.3));
tl.to('#build-panel',{opacity:.34,scale:.97,duration:.35},beat(4));
`};
scenes.organize={
 css:`
#organize-word{position:absolute;left:210px;top:170px;font-size:205px;z-index:5}
#library-camera{position:absolute;inset:0;transform-origin:50% 55%}
#dashboard{position:absolute;left:620px;top:380px;width:1090px;height:623.6px;transform-origin:0 0}
#instance-fo,#instance-cherry{position:absolute;width:650px;border-radius:22px;overflow:hidden;box-shadow:0 24px 65px #493A2838;transform-origin:0 0;z-index:4}
#instance-fo{left:260px;top:440px}#instance-cherry{left:1000px;top:600px}#instance-fo img,#instance-cherry img{width:100%;display:block}
#in-place{position:absolute;left:210px;top:420px;width:280px;font-size:81px;line-height:1;letter-spacing:-.06em;font-weight:750;color:#94601A}
#organize-arrow{position:absolute;left:220px;top:740px;width:170px;height:100px;fill:none;stroke:#94601A;stroke-width:12}
`,
 html:`<div class="stage paper"><h2 id="organize-word" class="title word">${letters('ORGANIZE.')}</h2><div id="library-camera"><div id="dashboard" class="screen">${image('landing')}</div><div id="instance-fo">${image('instance-fo')}</div><div id="instance-cherry">${image('instance-cherry')}</div></div><div id="in-place">EVERY WORLD. IN ITS PLACE.</div><svg id="organize-arrow" viewBox="0 0 170 100"><path d="M0 50H155M110 5L155 50L110 95"/></svg></div>`,
 motion:`
text('#organize-word .letter',0,.4);
enter('#instance-fo',.1,{x:800,y:150,rotation:-22},.6);enter('#instance-cherry',.25,{x:850,y:-150,rotation:22},.6);
tl.to('#instance-fo',{x:670,y:120,rotation:6,duration:.55,ease:'power3.inOut'},beat(2));
tl.to('#instance-cherry',{x:-710,y:-160,rotation:-5,duration:.55,ease:'power3.inOut'},beat(2));
tl.fromTo('#dashboard',{opacity:0,clipPath:'inset(0 78% 0 0 round 24px)'},{opacity:1,duration:.1},beat(3.5));
// Crop coordinates are the exact source positions in landing.png, scaled by 1090/2234.
const factor=1090/2234;
tl.to('#instance-fo',{x:620+20*factor-260,y:380+364*factor-440,scale:510*factor/650,rotation:0,duration:.58,ease:'power4.inOut'},beat(3.5));
tl.to('#instance-cherry',{x:620+20*factor-1000,y:380+547*factor-600,scale:510*factor/650,rotation:0,duration:.58,ease:'power4.inOut'},beat(3.5));
tl.to('#dashboard',{clipPath:'inset(0 0% 0 0 round 24px)',duration:.6,ease:'power4.inOut'},beat(4.5));
tl.set('#instance-fo,#instance-cherry',{opacity:0},beat(5));
enter('#in-place',beat(4.5),{x:-160,y:0},.5);enter('#organize-arrow',beat(5.5),{x:-60,y:0},.35);
tl.to('#library-camera',{x:25,y:-12,scale:1.035,duration:.75,ease:'power2.inOut'},beat(6));
`};
const {rows,ports}=createCacheInstancesHtml();
const paths=[`M510 570H760Q800 570 800 530V445Q800 415 840 415H980`,`M510 570H980`,`M510 570H760Q800 570 800 610V695Q800 725 840 725H980`];
scenes.cache={
 css:`
#share-word{position:absolute;left:220px;top:150px;font-size:210px;color:#EEA93B}
#cache-diagram{position:absolute;inset:0}
#cache-source{position:absolute;left:280px;top:455px;width:230px;height:230px;border-radius:42px;background:#EEA93B;color:#29261F;box-shadow:10px 12px 0 #986622;display:grid;place-items:center}
#cache-source svg{width:140px;height:140px}
#cache-lines{position:absolute;inset:0;width:1920px;height:1080px;fill:none}.wire{stroke:#6B6251;stroke-width:3}.packet{stroke:#EEA93B;stroke-width:9;stroke-linecap:round}
.row{position:absolute;left:980px;width:720px;height:134px;border:2px solid #665B49;border-radius:22px;background:#25231D;overflow:hidden}
#node-create{top:348px}#node-explore{top:503px}#node-experiment{top:658px}
.row-bar{position:absolute;left:0;top:0;bottom:0;width:5px;background:#EEA93B}.row-icon{position:absolute;left:21px;top:30px;width:64px;height:64px;color:#EEA93B}.row-icon svg{width:100%;height:100%}
.row-name{position:absolute;left:111px;top:15px;font-size:35px;font-weight:600;letter-spacing:-.045em}
.row-facts{position:absolute;left:113px;top:66px;display:flex;gap:28px}.row-facts dt{font:14px 'Geist Mono';color:#BDB4A3}.row-facts dd{font-size:20px;line-height:29px}.row-pin{position:absolute;right:20px;top:18px;width:22px;color:#EEA93B}.row-open{position:absolute;right:20px;bottom:25px;width:22px;color:#C9BEAA}
.port{position:absolute;left:973px;width:14px;height:14px;border:3px solid #171611;border-radius:50%;background:#8F826A}#port-node-create{top:408px}#port-node-explore{top:563px}#port-node-experiment{top:718px}
#cache-caption{position:absolute;left:250px;top:755px;font-size:27px;color:#C9BEAA}#freedom{position:absolute;left:218px;top:855px;font-size:73px;font-weight:650;letter-spacing:-.045em}
#file-token{position:absolute;left:540px;top:515px;font:26px 'Geist Mono';color:#EEA93B}
`,
 html:`<div class="stage"><h2 id="share-word" class="title word">${letters('SHARE.')}</h2><div id="cache-diagram"><svg id="cache-lines" viewBox="0 0 1920 1080">${paths.map((d,i)=>`<path id="wire-${i}" class="wire" d="${d}"/>`).join('')}${[0,1].map(w=>paths.map((d,i)=>`<path id="packet-${w}-${i}" class="packet" d="${d}"/>`).join('')).join('')}</svg><div id="cache-source"><svg viewBox="0 0 120 120" fill="none" stroke="currentColor" stroke-width="5" stroke-linejoin="round"><path d="M60 10L105 35V85L60 110L15 85V35Z M15 35L60 60L105 35 M60 60V110 M38 22L83 47"/></svg></div>${rows}${ports}<div id="file-token">.jar</div><div id="cache-caption">One shared cache.</div></div><div id="freedom">Keep the freedom.</div></div>`,
 motion:`
text('#share-word .letter',0,.45);
enter('#cache-source',beat(1),{x:-280,y:80,rotation:-25},.5);
enter('#cache-caption',beat(2),{x:-90,y:0},.35);
const nodes=['#node-create','#node-explore','#node-experiment'];
nodes.forEach((sel,i)=>enter(sel,beat(1)+i*.1,{x:480,y:0,rotation:7-i*3},.55));
enter('.port',beat(2),{x:0,y:0},.15);
tl.fromTo('.wire',{strokeDasharray:1000,strokeDashoffset:1000},{strokeDashoffset:0,duration:.65,stagger:.07,ease:'power3.inOut'},beat(2));
[0,1].forEach(w=>{
 const launch=beat(3+w*2);
 tl.to('#cache-source',{scaleX:.86,scaleY:1.06,duration:.09,ease:'power2.in'},launch-.09);
 tl.to('#cache-source',{scaleX:1,scaleY:1,duration:.3,ease:'back.out(1.6)'},launch);
 nodes.forEach((node,i)=>{
  const p=document.querySelector('#packet-'+w+'-'+i),len=p.getTotalLength(),t=launch+i*.1;
  tl.fromTo(p,{strokeDasharray:'70 '+(len+70),strokeDashoffset:70,opacity:0},{strokeDashoffset:-len,opacity:1,duration:.62,ease:'none'},t);
  tl.set(p,{opacity:0},t+.62);
  tl.to(node,{borderColor:'#EEA93B',backgroundColor:'#352C1D',x:14,duration:.09},t+.62);
  tl.to(node,{x:0,duration:.25,ease:'power3.out'},t+.71);
  tl.to('#port-'+node.slice(1),{backgroundColor:'#EEA93B',duration:.08},t+.62);
 });
});
tl.fromTo('#file-token',{x:0,opacity:0},{x:165,opacity:1,duration:.4,ease:'power2.out'},beat(3));
tl.to('#file-token',{x:280,opacity:0,duration:.2,ease:'power2.in'},beat(4));
enter('#freedom',beat(6),{x:350,y:0},.55);
`};
scenes.play={
 css:`
#play-word{position:absolute;left:230px;top:150px;font-size:230px;z-index:2}
#play-card{position:absolute;left:420px;top:435px;width:1100px;height:477px}
#cursor{position:absolute;left:1350px;top:465px;width:80px;height:100px;z-index:5;filter:drop-shadow(0 7px 4px #0004)}
#click-ring{position:absolute;left:1280px;top:400px;width:160px;height:160px;border:5px solid #94601A;border-radius:50%}
#play-reveal{position:absolute;inset:0;background:#EEA93B;overflow:hidden;z-index:6}
#play-world{position:absolute;left:720px;top:90px;width:1150px;height:930px;transform-origin:50% 65%}#play-world svg{width:100%;height:100%}
#go-type{position:absolute;left:200px;top:230px;font-size:240px;font-weight:850;line-height:.9;letter-spacing:-.08em;color:#29261F}
#go-type span{display:block}#go-caption{position:absolute;left:210px;top:805px;width:500px;font-size:36px;line-height:1.2;letter-spacing:-.035em}
#yours{position:absolute;left:205px;top:770px;font-size:130px;letter-spacing:-.07em;font-weight:850}
`,
 html:`<div class="stage paper"><h2 id="play-word" class="title word">${letters('PLAY.')}</h2><div id="play-card" class="screen">${image('play-panel')}</div><div id="click-ring"></div><svg id="cursor" viewBox="0 0 80 100"><path d="M6 4L70 55L42 59L54 88L39 94L27 64L6 83Z" fill="#F2EEE5" stroke="#29261F" stroke-width="4" stroke-linejoin="round"/></svg><div id="play-reveal"><div id="play-world">${createMinecraftWorldSvg()}</div><h2 id="go-type"><span>GO</span><span>PLAY.</span></h2><div id="go-caption">Make something worth coming back to.</div><div id="yours">YOURS.</div></div></div>`,
 motion:`
text('#play-word .letter',0,.45);
tl.fromTo('#play-card',{x:700,y:170,rotation:-9,scale:.8,opacity:0},{x:0,y:0,rotation:0,scale:1,opacity:1,duration:.65,ease:'power4.out'},beat(1));
tl.fromTo('#cursor',{x:400,y:500,rotation:24,opacity:1},{x:160,y:160,rotation:5,duration:.35,ease:'power2.in'},beat(2.5));
tl.to('#cursor',{x:0,y:0,rotation:0,duration:.35,ease:'power3.out'},beat(2.5)+.35);
tl.to('#cursor',{scale:.8,duration:.08},beat(4));tl.to('#cursor',{scale:1,duration:.2},beat(4)+.08);
tl.to('#play-card',{scale:.97,duration:.08},beat(4));tl.to('#play-card',{scale:1,duration:.2,ease:'power3.out'},beat(4)+.08);
tl.fromTo('#click-ring',{scale:.1,opacity:0},{scale:1.8,opacity:1,duration:.12},beat(4));tl.to('#click-ring',{scale:2.8,opacity:0,duration:.28},beat(4)+.12);
// A full beat of withheld sound and picture makes the launch release land.
tl.fromTo('#play-reveal',{clipPath:'circle(0% at 71% 45%)'},{clipPath:'circle(150% at 71% 45%)',duration:.5,ease:'power4.inOut'},beat(5));
tl.set('#play-word,#play-card,#cursor,#click-ring',{autoAlpha:0},beat(5.5));
enter('#go-type span:first-child',beat(5.5),{x:-500,y:0},.4);enter('#go-type span:last-child',beat(6),{x:-600,y:0},.45);
[...document.querySelectorAll('#play-world .voxel')].forEach(el=>{
 tl.fromTo(el,{y:320,opacity:0},{y:0,opacity:1,duration:.45,ease:'back.out(1.45)'},beat(5)+Number(el.dataset.rise)*1.25);
});
tl.fromTo('#play-world .world-detail',{y:45,opacity:0},{y:0,opacity:1,duration:.3,stagger:.04},beat(7));
tl.fromTo('#play-world .world-shadow',{scaleX:.6,opacity:0,transformOrigin:'50% 50%'},{scaleX:1,opacity:.13,duration:1.1},beat(5));
tl.fromTo('#play-world',{x:140,y:85,scale:.68,rotation:-8},{x:0,y:0,scale:1,rotation:0,duration:beat(3),ease:'power3.out'},beat(5));
enter('#go-caption',beat(7.5),{x:0,y:80},.4);
tl.fromTo('#placed-block',{y:-500,opacity:0},{y:0,opacity:1,duration:.35,ease:'power4.in'},beat(9)-.35);
[...document.querySelectorAll('.puff')].forEach(el=>{
 tl.fromTo(el,{x:0,y:0,opacity:0},{x:+el.dataset.dx*.6,y:-35,opacity:1,duration:.13},beat(9));
 tl.to(el,{y:-65,opacity:0,duration:.22},beat(9)+.13);
});
tl.to('#play-world',{y:10,rotation:1,duration:.08},beat(9));tl.to('#play-world',{y:-10,rotation:0,scale:1.04,duration:.55,ease:'power3.out'},beat(9)+.08);
tl.to('#go-caption',{x:-650,duration:.3,ease:'power3.in'},beat(10));
enter('#yours',beat(10.5),{x:-700,y:0,rotation:-10},.45);
`};
scenes.brand={
 css:`
#brand-mark-wrap{position:absolute;left:780px;top:160px;width:360px;height:360px}#brand-mark{overflow:visible}
#brand-name{position:absolute;left:220px;right:220px;top:530px;display:flex;justify-content:center;font-size:180px;font-weight:650;line-height:1.15;letter-spacing:-.065em}
#brand-tagline{position:absolute;left:0;right:0;top:755px;text-align:center;font-size:58px;letter-spacing:-.045em;color:#EEA93B}
#brand-url{position:absolute;left:0;right:0;top:880px;text-align:center;font:27px 'Geist Mono'}
#brand-halo{position:absolute;left:710px;top:90px;width:500px;height:500px;border:3px solid #EEA93B;border-radius:50%}
#brand-rule{position:absolute;left:460px;top:850px;width:1000px;height:2px;background:#EEA93B;transform-origin:center}
`,
 html:`<div class="stage"><div id="brand-halo"></div><div id="brand-mark-wrap">${brandMark}</div><h2 id="brand-name">${letters('Polymerium')}</h2><div id="brand-tagline">Make it yours.</div><div id="brand-rule"></div><div id="brand-url">polymerium.dearain.dev</div></div>`,
 motion:`
tl.fromTo('#brand-mark-wrap',{y:100,scale:1.3},{y:0,scale:1,duration:.65,ease:'power4.out'},0);
tl.fromTo('.link-ivory-motion',{x:-120,y:80,rotation:-65,svgOrigin:'64 64',opacity:0},{x:0,y:0,rotation:0,opacity:1,duration:.55,ease:'power4.out'},.05);
tl.fromTo('.link-gold-motion',{x:120,y:-80,rotation:65,svgOrigin:'64 64',opacity:0},{x:0,y:0,rotation:0,opacity:1,duration:.55,ease:'power4.out'},.05);
tl.fromTo('#brand-halo',{scale:.35,opacity:0},{scale:1.4,opacity:.7,duration:.13},.48);tl.to('#brand-halo',{scale:2.4,opacity:0,duration:.6,ease:'power3.out'},.61);
text('#brand-name .letter',beat(1.5),.6);
enter('#brand-tagline',beat(3),{x:0,y:100},.5);
tl.fromTo('#brand-rule',{scaleX:0},{scaleX:1,duration:.5,ease:'power3.inOut'},beat(3.5));
enter('#brand-url',beat(4),{x:0,y:50},.45);
`};

for(const [i,shot]of SHOTS.entries()){
 const s=scenes[shot.id],duration=+( (SHOTS[i+1]?b(SHOTS[i+1].start):DURATION)-b(shot.start)).toFixed(4);
 const html=`<!doctype html><html lang="en"><head><meta charset="utf-8"></head><body><template><style>${common}${s.css}</style><div id="root" data-composition-id="scene-${shot.id}" data-width="1920" data-height="1080" data-duration="${duration}">${s.html}</div><script>${helper}${s.motion}\nwindow.__timelines["scene-${shot.id}"]=tl;</script></template></body></html>`;
 fs.writeFileSync(path.join(out,`${shot.id}.html`),html);
}
// The wrapper owns the seam; each mounted scene owns its local choreography.
const hosts=SHOTS.map((s,i)=>{
 const start=b(s.start),end=SHOTS[i+1]?b(SHOTS[i+1].start):DURATION,dur=+(end-start).toFixed(4);
 return `<div id="shot-${s.id}" class="shot"><div id="clip-${s.id}" class="clip" data-composition-id="scene-${s.id}" data-composition-src="compositions/${s.id}.html" data-start="${start}" data-duration="${dur}" data-track-index="${i+1}" data-track-kind="graphics" data-label="${s.id.toUpperCase()}" data-width="1920" data-height="1080"></div></div>`;
}).join('\n');
const html=`<!doctype html><html lang="en"><head><meta charset="UTF-8"><title>Polymerium — Make it yours</title><script src="assets/gsap.min.js"></script><style>
@font-face{font-family:'Geist Mono';src:url('assets/fonts/geist-mono-latin.woff2');font-weight:100 900}
*{box-sizing:border-box}html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#171611}#root{width:100%;height:100%;position:relative;overflow:hidden;background:#171611}.clip,.shot{position:absolute;inset:0}.shot{opacity:0;overflow:hidden}#hud{position:absolute;inset:0;pointer-events:none;color:#29261F;z-index:50}#hud-label{position:absolute;left:96px;top:58px;font:20px 'Geist Mono';letter-spacing:.07em}#hud-clock{position:absolute;right:96px;top:58px;font:20px 'Geist Mono';height:26px;overflow:hidden}#clock-strip span{display:block;position:absolute;width:100%;height:26px;line-height:26px;opacity:0}#hud-clock{width:84px}#progress{position:absolute;left:0;bottom:0;width:100%;height:7px;background:#EEA93B;transform-origin:left}
</style></head><body><div id="root" data-composition-id="main" data-width="1920" data-height="1080" data-duration="${DURATION}">
${hosts}
<div id="hud"><div id="hud-label">POLYMERIUM / MAKE IT YOURS</div><div id="hud-clock"><div id="clock-strip" data-layout-allow-overflow>${Array.from({length:26},(_,i)=>`<span id="count-${i}" data-layout-allow-overflow>${String(i).padStart(2,'0')} : 25</span>`).join('')}</div></div><div id="progress"></div></div>
<audio id="score" src="out/score.wav" data-start="0" data-duration="${DURATION}" data-track-index="10" data-volume="1"></audio>
</div><script>
const tl=gsap.timeline({paused:true});
tl.fromTo('#progress',{scaleX:0},{scaleX:1,duration:${DURATION},ease:'none'},0);
tl.set('#count-0',{autoAlpha:1},0);
${Array.from({length:25},(_,i)=>`tl.fromTo('#count-${i+1}',{y:26,autoAlpha:1},{y:0,autoAlpha:1,duration:.18,ease:'power4.inOut',immediateRender:false},${i+1-.18});tl.to('#count-${i}',{y:-26,duration:.18,ease:'power4.inOut'},${i+1-.18});tl.set('#count-${i}',{autoAlpha:0},${i+1});`).join('\n')}
${SHOTS.map(s=>`tl.set('#hud',{color:'${s.theme==='paper'?'#29261F':'#F2EEE5'}'},${b(s.start)});`).join('\n')}
// <seams:auto>
// </seams:auto>
window.__timelines["main"]=tl;
</script></body></html>`;
fs.writeFileSync(path.join(root,'index.html'),html);
console.log(`Built six modular scenes, ${DURATION}s, ${BEAT*60} frames per beat.`);
