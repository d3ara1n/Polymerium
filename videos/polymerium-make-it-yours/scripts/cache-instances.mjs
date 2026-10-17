export function createCacheInstancesHtml(){
  const rows=instances.map(v=>`<div class="row" id="${v.id}"><span class="row-bar"></span><span class="row-icon">${cube}</span><span class="row-name">${v.name}</span><dl class="row-facts"><div><dt>Minecraft</dt><dd>${v.version}</dd></div><div><dt>Loader</dt><dd>${v.loader}</dd></div><div><dt>Source</dt><dd>${v.sourceLabel}</dd></div></dl>${v.pinned?`<span class="row-pin">${pin}</span>`:''}<span class="row-open">${arrow}</span></div>`).join('');
  const ports=instances.map(v=>`<span class="port" id="port-${v.id}"></span>`).join('');
  return {rows,ports};
}

// Card content mirrors the real instance cards in the dashboard screenshot.
export const instances=[
  {id:'node-create',sourceLabel:'Modrinth',name:'Fabulously Optimized',loader:'Fabric',version:'26.2',pinned:true},
  {id:'node-explore',sourceLabel:'Local',name:'Cherry Picks',loader:'NeoForge',version:'1.21.1',pinned:true},
  {id:'node-experiment',sourceLabel:'Instance profile',name:'Devin’s Overhaul',loader:'Fabric',version:'1.20.1',pinned:false}
];

const cube='<svg viewBox="0 0 64 64" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linejoin="round"><path d="M32 7L54 20V45L32 58L10 45V20Z M10 20L32 33L54 20 M32 33V58 M21 13L43 26"/></svg>';
const pin='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M14 3L21 10L17 11L13 16L8 11L13 7Z M10 14L4 20"/></svg>';
const arrow='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"><path d="M9 4L17 12L9 20"/></svg>';
