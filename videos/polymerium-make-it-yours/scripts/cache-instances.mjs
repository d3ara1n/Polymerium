const instances=[
 {id:'node-create',source:'MODRINTH#fabulously_optimized',name:'Fabulously Optimized',loader:'Fabric',version:'26.2',pinned:true},
 {id:'node-explore',source:'LOCAL#cherry_picks',name:'Cherry Picks',loader:'NeoForge',version:'1.21.1',pinned:true},
 {id:'node-experiment',source:'INSTANCE PROFILE',name:"Devin’s Overhaul",loader:'Fabric',version:'1.20.1',pinned:false}
];
const cube='<svg viewBox="0 0 64 64" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linejoin="round"><path d="M32 7L54 20V45L32 58L10 45V20Z M10 20L32 33L54 20 M32 33V58 M21 13L43 26"/></svg>';
const pin='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M14 3L21 10L17 11L13 16L8 11L13 7Z M10 14L4 20"/></svg>';
const play='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round"><path d="M8 4L20 12L8 20Z"/></svg>';
export function createCacheInstancesHtml(){return instances.map(v=>`<div class="cache-node" id="${v.id}"><span class="instance-port"></span><div class="instance-icon">${cube}</div><div class="instance-copy"><div class="instance-source">${v.source}</div><div class="instance-name">${v.name}</div><div class="instance-badges"><span>${v.loader}</span><span>${v.version}</span></div></div>${v.pinned?`<div class="instance-pin">${pin}</div>`:''}<div class="instance-play">${play}</div></div>`).join('');}
export const cacheInstanceStyles=`
.cache-node{position:absolute;left:1035px;width:760px;height:158px;border:1px solid #655B4A;border-radius:25px;background:#25231D;box-shadow:0 16px 32px #0002}
#node-create{top:470px}#node-explore{top:648px}#node-experiment{top:826px}
.instance-port{position:absolute;left:-7px;top:72px;width:13px;height:13px;border-radius:50%;background:#A89B82;border:3px solid #171611}
.instance-icon{position:absolute;left:29px;top:39px;width:80px;height:80px;border-radius:20px;background:#3A3121;color:#EEA93B;display:flex;align-items:center;justify-content:center}
.instance-icon svg{width:55px;height:55px}
.instance-copy{position:absolute;left:136px;top:17px;right:76px}
.instance-source{font-family:'Geist Mono',monospace;font-size:18px;letter-spacing:.015em;line-height:25px;color:#C0B7A5}
.instance-name{font-size:37px;font-weight:550;letter-spacing:-.04em;line-height:49px;white-space:nowrap}
.instance-badges{display:flex;gap:10px;margin-top:8px}
.instance-badges span{font-size:21px;font-weight:550;line-height:28px;padding:0 13px;border-radius:14px;color:#E1D9CB;background:#423C30}
.instance-pin{position:absolute;right:24px;top:20px;color:#EEA93B;width:27px;height:27px}
.instance-play{position:absolute;right:22px;bottom:19px;width:38px;height:38px;display:flex;align-items:center;justify-content:center;border:1px solid #655B4A;border-radius:12px;color:#D3CABB}
.instance-play svg{width:21px;height:21px}
#cache-tag{position:absolute;left:1065px;top:425px;font-family:'Geist Mono',monospace;font-size:21px;color:#C0B7A5}
`;
