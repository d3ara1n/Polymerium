import fs from 'node:fs';
import path from 'node:path';
const root=path.resolve(import.meta.dirname,'..');
const sr=48000, duration=32, N=sr*duration;
const L=new Float64Array(N), R=new Float64Array(N);
const TAU=Math.PI*2;
let seed=21916;
function noise(){seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/2147483648-1;}
const hz=m=>440*2**((m-69)/12);
function add(start,len,sample,amp=1,pan=0){const offset=Math.round(start*sr), count=Math.round(len*sr),lp=Math.sqrt((1-pan)/2),rp=Math.sqrt((1+pan)/2);for(let i=0;i<count&&offset+i<N;i++){if(offset+i<0)continue;const s=sample(i/sr,i)*amp;L[offset+i]+=s*lp;R[offset+i]+=s*rp;}}
function pluck(t,n,amp=.15,pan=0,len=.65){const f=hz(n);add(t,len,x=>{const a=Math.min(1,x/.004)*Math.exp(-x*8);return a*(Math.sin(TAU*f*x)+.22*Math.sin(TAU*f*2*x)*Math.exp(-x*10)+.10*Math.sin(TAU*f*3*x));},amp,pan);}
function kick(t,amp=.5){add(t,.4,x=>Math.sin(TAU*(46*x+3.8*(1-Math.exp(-x*37))))*Math.exp(-x*12),amp);}
function hat(t,amp=.07,open=false){add(t,open?.2:.065,x=>noise()*Math.exp(-x*(open?23:80))*Math.min(1,x/.001),amp,.30);}
function snare(t){add(t,.2,x=>noise()*Math.exp(-x*23)*.75+Math.sin(TAU*180*x)*Math.exp(-x*37)*.25,.18,-.12);}
const chords=[[50,57,60,64],[46,53,57,62],[53,60,64,69],[48,55,62,64]];
const motif=[0,2,1,3,2,1,3,2];
for(let bar=0;bar<15;bar++){
  const start=bar*2,c=chords[Math.floor(bar/2)%4];
  for(let voice=0;voice<4;voice++){const f=hz(c[voice]);add(start,2.6,x=>{const env=Math.min(1,x/.18)*Math.min(1,(2.6-x)/.65);return (Math.sin(TAU*f*x)+.18*Math.sin(TAU*f*2*x))*env;},bar<2?.022:.04,(voice-1.5)*.4);}
  for(let s=0;s<8;s++){
    const t=start+s*.25;
    if(t>=23.5&&t<24)continue;
    pluck(t,c[motif[s]]+12,s%2===0?.13:.085,(s%3-1)*.52);
    if(bar>=2)hat(t,s%2===0?.045:.025,s===7);
  }
  if(bar>=1&&bar<14)for(let b=0;b<4;b++){
    const t=start+b*.5;
    if(t>=23.5&&t<24)continue;
    kick(t,b===0?.58:.42);
    const f=hz(c[0]-12);add(t,.38,x=>Math.tanh(1.8*(Math.sin(TAU*f*x)+.2*Math.sin(TAU*f*2*x)))*Math.min(1,x/.009)*Math.exp(-x*7),.2);
    if(b%2===1)snare(t);
  }
}
const cuts=[4,10,16,22,27];
for(const t of cuts){add(t-.35,.35,x=>noise()*Math.pow(x/.35,2)*(.3+.7*Math.sin(TAU*(650*x+1800*x*x))),.12,-.2);kick(t,.48);pluck(t,86,.14,.4,.45);}
for(const t of [5.2,6.0,6.65,7.3,11.1,11.65,18.0,18.5,19.0,23.5,27.55]){
  add(t,.09,x=>(Math.sin(TAU*1350*x)+noise()*.3)*Math.exp(-x*65),.12);
}
for(let k=0;k<4;k++)pluck(24+k*.08,[62,65,69,74][k],.22,(k-1.5)*.3,1.1);
for(const [i,n] of [50,57,62,65,69,76].entries()){const f=hz(n);add(27.5,4.5,x=>Math.sin(TAU*f*x)*Math.min(1,x/.025)*Math.exp(-x*.65)*Math.min(1,(4.5-x)/.6),.075,(i-2.5)*.28);}
// A short withdrawal makes the launch click and reveal distinct from the groove.
for(let i=0;i<N;i++){const t=i/sr;if(t>=23.5&&t<24){const g=.12;L[i]*=g;R[i]*=g;}}
add(23.5,.085,x=>(Math.sin(TAU*1350*x)+.28*Math.sin(TAU*2600*x))*Math.exp(-x*58),.23);
const d1=Math.round(.1875*sr),d2=Math.round(.375*sr);
for(let i=N-1;i>=d2;i--){L[i]+=R[i-d1]*.13+L[i-d2]*.08;R[i]+=L[i-d1]*.13+R[i-d2]*.08;}
let peak=0;for(let i=0;i<N;i++)peak=Math.max(peak,Math.abs(L[i]),Math.abs(R[i]));
const buf=Buffer.alloc(44+N*4);buf.write('RIFF');buf.writeUInt32LE(36+N*4,4);buf.write('WAVE',8);buf.write('fmt ',12);buf.writeUInt32LE(16,16);buf.writeUInt16LE(1,20);buf.writeUInt16LE(2,22);buf.writeUInt32LE(sr,24);buf.writeUInt32LE(sr*4,28);buf.writeUInt16LE(4,32);buf.writeUInt16LE(16,34);buf.write('data',36);buf.writeUInt32LE(N*4,40);
for(let i=0;i<N;i++){const t=i/sr;const fade=Math.min(1,(duration-t)/.7);buf.writeInt16LE(Math.round(L[i]/peak*.88*fade*32767),44+i*4);buf.writeInt16LE(Math.round(R[i]/peak*.88*fade*32767),46+i*4);}
fs.writeFileSync(path.join(root,'assets/score-premaster.wav'),buf);
fs.writeFileSync(path.join(root,'audio_meta.json'),JSON.stringify({duration,bpm:120,key:'D minor',source:'Original deterministic synthesis',narration:false,sceneCues:cuts,launchClick:23.5,launchReveal:24,brandImpact:27.5},null,2));
console.log('Original stereo score generated: 32 seconds, 48 kHz.');
