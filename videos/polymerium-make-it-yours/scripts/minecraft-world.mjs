export function createMinecraftWorldSvg(){
const blocks=new Map();
const key=(x,y,z)=>`${x},${y},${z}`;
const put=(x,y,z,type)=>blocks.set(key(x,y,z),{x,y,z,type});
const palette={
 grass:['#83B94A','#725038','#906441'], dirt:['#A17A50','#79573E','#936A47'],
 stone:['#AAA99D','#73796F','#92978A'], log:['#B79A65','#685037','#85633F'],
 leaves:['#70A442','#3F7034','#53883A'], planks:['#D5AD70','#A17A48','#BC9259'],
 roof:['#98724B','#60472F','#795B3A'], water:['#67C5D3','#2C8FAA','#3EA7BE'],
 sand:['#DCCB91','#B4A879','#CDBB84'], path:['#B9A169','#967C50','#A78D5A'],
 door:['#A17A48','#4C392B','#59422B'], window:['#BCE5E0','#5C8D8C','#8DC6CB'],
 furnace:['#989B90','#555D57','#737C73'], crafting:['#B78349','#7C4D2D','#9B6338']
};
for(let x=0;x<10;x++)for(let y=0;y<9;y++){
 if((x===0&&y<2)||(x===9&&y<1)||(x>7&&y>7)||(x<2&&y>7))continue;
 if(x>0&&x<9&&y>0&&y<8)put(x,y,0,'stone');
 put(x,y,1,((x+y)%4===0)?'stone':'dirt');
 const pond=x>=6&&x<=8&&y>=5&&y<=7;
 put(x,y,2,pond?'water':(x===9&&y>=4&&y<=7)?'sand':'grass');
}
// A small survival cabin with log corners and a stepped gable roof.
for(let x=1;x<=3;x++)for(let y=3;y<=5;y++)for(let z=3;z<=4;z++){
 const corner=(x===1||x===3)&&(y===3||y===5);
 let type=corner?'log':'planks';
 if(y===5&&x===2)type='door';
 if(x===3&&y===4&&z===4)type='window';
 put(x,y,z,type);
}
for(let x=0;x<=4;x++)for(let y=2;y<=6;y++){
 put(x,y,5,'roof');
 if(x>=1&&x<=3)put(x,y,6,'roof');
 if(x===2)put(x,y,7,'roof');
}
for(const [x,y]of [[2,6],[2,7],[3,7],[4,7],[4,8]])put(x,y,2,'path');
put(4,5,3,'crafting');put(4,4,3,'furnace');
// A stepped oak canopy.
for(let z=3;z<=6;z++)put(7,2,z,'log');
for(let z=5;z<=7;z++)for(let x=5;x<=8;x++)for(let y=0;y<=3;y++){
 if(z===7&&(x===5||x===8||y===0||y===3))continue;
 if(z===5&&((x===5||x===8)&&(y===0||y===3)))continue;
 if(x===7&&y===2&&z<=6)continue;
 put(x,y,z,'leaves');
}
put(6,1,8,'leaves');put(7,1,8,'leaves');put(7,2,8,'leaves');
const sx=43,sy=21.5,sz=43;
const project=(x,y,z)=>[480+(x-y)*sx,350+(x+y)*sy-z*sz];
const poly=(pts,fill,extra='')=>`<path d="M${pts.map(p=>p.map(n=>n.toFixed(2)).join(' ')).join('L')}Z" fill="${fill}" ${extra}/>`;
const facePoint=(a,b,c,u,v)=>[a[0]+(b[0]-a[0])*u+(c[0]-a[0])*v,a[1]+(b[1]-a[1])*u+(c[1]-a[1])*v];
function patch(a,b,c,u,v,w,h,color,opacity=1){return poly([facePoint(a,b,c,u,v),facePoint(a,b,c,u+w,v),facePoint(a,b,c,u+w,v+h),facePoint(a,b,c,u,v+h)],color,`opacity="${opacity}"`);}
function face(a,b,c,d,color,type,index,seed){
 let s=poly([a,b,d,c],color,`stroke="#283421" stroke-opacity=".08" stroke-width=".7" stroke-linejoin="round"`);
 if(type==='water'){
  if(index===0)for(let k=0;k<3;k++)s+=patch(a,b,c,.12+(k%2)*.35,.15+k*.23,.22,.045,'#C0F1DD',.6);
  return s;
 }
 if(type==='planks'||type==='roof'||type==='door'){
  for(let k=1;k<4;k++)s+=patch(a,b,c,0,k*.25-.015,1,.025,'#4B3420',.3);
  for(let k=0;k<3;k++)s+=patch(a,b,c,(k%2)*.44+.16,k*.27+.02,.016,.2,'#4B3420',.22);
 }else if(type==='log'){
  if(index===0){for(let k=1;k<=3;k++)s+=patch(a,b,c,k*.105,k*.105,1-k*.21,1-k*.21,k%2?'#81623C':'#C5A670');}
  else for(let k=0;k<4;k++)s+=patch(a,b,c,k*.25+.055,.05,.08,.86,k%2?'#C5A670':'#403422',.32);
 }else{
  const colors=type==='leaves'?['#A7CB62','#295B2F','#45722E']:type==='grass'&&index===0?['#B4CB68','#4E8835','#72A443']:['#F6E4B4','#312F24',color];
  for(let k=0;k<7;k++){
   const n=((seed+17)*(k+13)*7919)%104729;
   const u=((n%6)+.12)/8,v=((Math.floor(n/11)%6)+.15)/8;
   s+=patch(a,b,c,u,v,(1+(n%2))/8,1/8,colors[k%3],type==='leaves'?.48:.20);
  }
 }
 if(type==='grass'&&index!==0){
  s+=patch(a,b,c,0,0,1,.17,index===1?'#5B8F3B':'#6AA144');
  for(let k=0;k<5;k++)s+=patch(a,b,c,k*.2,.17,.2,k%2?.12:.04,index===1?'#5B8F3B':'#6AA144');
 }
 if(type==='furnace'&&index===2){s+=patch(a,b,c,.2,.15,.6,.23,'#343C37');s+=patch(a,b,c,.15,.57,.7,.28,'#26332D');s+=patch(a,b,c,.26,.69,.48,.12,'#EEA93B');}
 if(type==='crafting'&&index===0){s+=patch(a,b,c,.13,.13,.74,.74,'#4E3425');for(let u=0;u<3;u++)for(let v=0;v<3;v++)s+=patch(a,b,c,.17+u*.23,.17+v*.23,.17,.17,'#C9985C');}
 if(type==='window'&&index===2){s+=patch(a,b,c,.07,.07,.86,.86,'#B5DAD2');s+=patch(a,b,c,.43,.07,.09,.86,'#795B3C');s+=patch(a,b,c,.07,.43,.86,.09,'#795B3C');s+=patch(a,b,c,.14,.15,.2,.09,'#EBF4D9');}
 if(type==='door'&&index===1){s+=patch(a,b,c,.13,.12,.74,.76,'#715133');s+=patch(a,b,c,.2,.18,.27,.55,'#89633D');s+=patch(a,b,c,.75,.56,.1,.1,'#EEA93B');}
 return s;
}
const items=[...blocks.values()].sort((a,b)=>(a.x+a.y)-(b.x+b.y)||a.z-b.z||a.x-b.x);
let shapes='';
for(const [i,b] of items.entries()){
 const {x,y,z,type}=b,c=palette[type];
 shapes+=`<g class="voxel" data-rise="${z*.065+(x+y)*.011}">`;
 const p100=project(x+1,y,z),p010=project(x,y+1,z),p110=project(x+1,y+1,z);
 const p001=project(x,y,z+1),p101=project(x+1,y,z+1),p011=project(x,y+1,z+1),p111=project(x+1,y+1,z+1);
 if(!blocks.has(key(x,y,z+1)))shapes+=face(p001,p101,p011,p111,c[0],type,0,i);
 if(!blocks.has(key(x,y+1,z)))shapes+=face(p011,p111,p010,p110,c[1],type,1,i);
 if(!blocks.has(key(x+1,y,z)))shapes+=face(p101,p111,p100,p110,c[2],type,2,i);
 shapes+='</g>';
}
// Pixel flowers and grass silhouettes keep the front lawn legible at film scale.
for(const [x,y,color]of [[.8,6.5,'#D65D42'],[4.8,7.8,'#F3D774'],[5.3,3.8,'#F3D774'],[8.6,3.5,'#D65D42']]){
 const [px,py]=project(x,y,3);shapes+=`<g class="world-detail"><g transform="translate(${px} ${py})"><path d="M0 0V-25M0-10L-9-17M0-5L9-13" stroke="#365B30" stroke-width="4" fill="none"/><path d="M-4-36H4V-31H10V-23H4V-18H-4V-23H-10V-31H-4Z" fill="${color}"/><rect x="-3" y="-30" width="6" height="6" fill="#F5DB84"/></g></g>`;
}
const svg=`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1040 860" aria-label="Simplified Minecraft scene with a grass island, oak cabin, tree and pond"><ellipse class="world-shadow" cx="525" cy="760" rx="290" ry="36" fill="#754F1C" opacity=".13"/>${shapes}</svg>`;
return svg;
}
