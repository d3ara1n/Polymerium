import fs from 'node:fs';
import path from 'node:path';
import {BEAT,BPM,DURATION,b,SEAMS} from './cues.mjs';
const root=path.resolve(import.meta.dirname,'..');
const SR=48000,N=Math.round(SR*DURATION),TAU=Math.PI*2;
const L=new Float64Array(N),R=new Float64Array(N);
let seed=21916;
const noise=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/2147483648-1;};
const hz=n=>440*2**((n-69)/12);
function add(start,dur,fn,amp=1,pan=0){
 const offset=Math.round(start*SR),n=Math.round(dur*SR),lp=Math.sqrt((1-pan)/2),rp=Math.sqrt((1+pan)/2);
 for(let i=0;i<n&&offset+i<N;i++){if(offset+i<0)continue;const v=fn(i/SR,i)*amp;L[offset+i]+=v*lp;R[offset+i]+=v*rp;}
}
function kick(t,g=.6){add(t,.42,x=>(Math.sin(TAU*(47*x+5.4*(1-Math.exp(-x*40))))+.1*noise()*Math.exp(-x*95))*Math.exp(-x*13),g);}
function clap(t){let last=0;add(t,.18,x=>{const n=noise(),h=n-last;last=n;return (h*.45+Math.sin(TAU*180*x)*.2)*Math.exp(-x*25);},.22,.12);}
function hat(t,g=.065){let last=0;add(t,.065,x=>{const n=noise(),h=n-last;last=n;return h*Math.exp(-x*82);},g,.35);}
function bass(t,n,g=.24){const f=hz(n);add(t,BEAT*.85,x=>Math.tanh((Math.sin(TAU*f*x)+.3*Math.sin(TAU*f*2*x)) *1.8)*Math.min(1,x/.008)*Math.exp(-x*9),g);}
function pluck(t,n,g=.11,pan=0){const f=hz(n);add(t,.65,x=>Math.sin(TAU*f*x+1.8*Math.sin(TAU*f*2*x)*Math.exp(-x*16))*Math.min(1,x/.003)*Math.exp(-x*9),g,pan);}
function whoosh(t,d=.22,g=.25){let low=0;add(t-d,d,x=>{low=low*.84+noise()*.16;return (low+.22*noise())*(x/d)**2;},g,-.25);}
function hit(t,g=.3){kick(t,g);add(t,.12,x=>(noise()*.45+Math.sin(TAU*900*x)*.4)*Math.exp(-x*48),.16);}
function tick(t,n=86,pan=0){pluck(t,n,.12,pan);add(t,.055,x=>noise()*Math.exp(-x*95),.06,pan);}

// Picture owns the impact points; the groove leaves a beat of vacuum before Play.
for(let k=0;k<44;k++){
 const t=b(k);if(k===36)continue;
 const chord=[38,34,41,36][Math.floor(k/8)%4];
 if(k>=4){kick(t,k%4===0?.62:.44);bass(t+.025,chord);if(k%2)clap(t);hat(t+BEAT/2);}
 if(k>=8){hat(t+BEAT*.75,.025);pluck(t+BEAT/4,[62,69,65,74,69,65,72,69][k%8],.1,(k%3-1)*.45);}
 if(k%4===0&&k>=4){[chord+24,chord+31,chord+34].forEach((n,i)=>{const f=hz(n);add(t,BEAT*3.6,x=>(Math.sin(TAU*f*x)+.15*Math.sin(TAU*f*2*x))*Math.min(1,x/.035)*Math.exp(-x*2.2),.035,(i-1)*.65);});}
}
for(const s of SEAMS){whoosh(s.cut,.28,.4);hit(s.cut,.65);}
for(const beat of [1,3,5.5,10,12,13,14,18,19.5,22,25,27,29]){
 const t=b(beat);whoosh(t,.13,.15);tick(t,beat%2?86:81,beat%3===0?-.3:.3);
}
// Two cache dispatches, with three separate arrival taps per dispatch.
for(const beat of [27,29])for(let i=0;i<3;i++)tick(b(beat)+i*.1+.62,81+i*4,(i-1)*.45);
for(let i=0;i<7;i++)tick(b(37)+i*.12,62+[0,3,7,12,15,19,24][i],(i%3-1)*.4);
hit(b(41),.42);
// Withdraw already-ringing music too, then add the dry click in that space.
for(let i=Math.round(b(36)*SR);i<Math.round(b(37)*SR);i++){L[i]*=.055;R[i]*=.055;}
add(b(36),.07,x=>(Math.sin(TAU*1500*x)+.3*Math.sin(TAU*2900*x))*Math.exp(-x*70),.24);
whoosh(b(37),.18,.26);hit(b(37),.82);
const join=b(44)+.48;hit(join,.55);
[50,57,62,65,69,76].forEach((n,j)=>{const f=hz(n);add(join,DURATION-join,x=>Math.sin(TAU*f*x)*Math.min(1,x/.02)*Math.exp(-x*.85),.075,(j-2.5)*.28);});
for(const [i,n]of [81,86,93].entries())pluck(b(45.5)+i*.11,n,.12,(i-1)*.4);
const delay=Math.round(BEAT*.5*SR);
for(let i=N-1;i>=delay;i--){const t=i/SR;if(t>=b(36)&&t<b(37))continue;L[i]+=R[i-delay]*.11;R[i]+=L[i-delay]*.11;}
let peak=0;
for(let i=0;i<N;i++){L[i]=Math.tanh(L[i]*1.15);R[i]=Math.tanh(R[i]*1.15);peak=Math.max(peak,Math.abs(L[i]),Math.abs(R[i]));}
const out=Buffer.alloc(44+N*4);out.write('RIFF');out.writeUInt32LE(out.length-8,4);out.write('WAVEfmt ',8);out.writeUInt32LE(16,16);out.writeUInt16LE(1,20);out.writeUInt16LE(2,22);out.writeUInt32LE(SR,24);out.writeUInt32LE(SR*4,28);out.writeUInt16LE(4,32);out.writeUInt16LE(16,34);out.write('data',36);out.writeUInt32LE(N*4,40);
for(let i=0;i<N;i++){const fade=Math.min(1,i/120,(N-1-i)/(SR*.6));out.writeInt16LE(Math.round(L[i]/peak*.88*fade*32767),44+i*4);out.writeInt16LE(Math.round(R[i]/peak*.88*fade*32767),46+i*4);}
fs.mkdirSync(path.join(root,'out'),{recursive:true});fs.writeFileSync(path.join(root,'out/score-premaster.wav'),out);
fs.writeFileSync(path.join(root,'audio_meta.json'),JSON.stringify({duration:DURATION,bpm:BPM,beatFrames:28,source:'Original deterministic synthesis',sceneCues:SEAMS.map(s=>s.cut),click:b(36),launch:b(37),brandImpact:join},null,2));
console.log(`Composed ${DURATION}s at ${BPM.toFixed(3)} BPM with picture-synchronous impacts.`);
