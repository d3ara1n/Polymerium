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

export const locales = [
  { locale: 'en', name: 'English' },
  { locale: 'zh', name: '简体中文' },
] as const;

export type Locale = (typeof locales)[number]['locale'];

/** Locale-specific download URLs */
export function getDownloadUrl(locale: string): string {
  if (locale === 'zh') {
    return 'https://mirrorchyan.com/zh/projects?rid=Polymerium&channel=Polymerium_setup&source=polymerium-website';
  }
  return 'https://github.com/d3ara1n/Polymerium/releases';
}

export const socialLinks = {
  github: `https://github.com/${gitConfig.user}/${gitConfig.repo}`,
  discord: null as string | null,
  qq: null as string | null,
};
