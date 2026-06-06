export const appName = 'Polymerium';
export const appDescription =
  'A metadata-driven Minecraft launcher that stores each mod exactly once. Zero duplication, instant switching, snapshots, and built-in CLI.';

export const docsRoute = '/docs';
export const docsImageRoute = '/og/docs';
export const docsContentRoute = '/llms.mdx/docs';

export const gitConfig = {
  user: 'd3ara1n',
  repo: 'Polymerium',
  branch: 'main',
};

// Navigation links for Fumadocs UI
// Just text + url — external detection is automatic
export const navItems = [
  {
    text: 'Docs',
    url: '/docs',
  },
  {
    text: 'Download',
    url: 'https://github.com/d3ara1n/Polymerium/releases',
  },
  {
    text: 'CLI',
    url: '/docs/cli',
  },
];

export const socialLinks = {
  github: `https://github.com/${gitConfig.user}/${gitConfig.repo}`,
  discord: null as string | null, // TODO: add Discord invite link
  qq: null as string | null, // TODO: add QQ group link
};
