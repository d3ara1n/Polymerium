import { gitConfig } from './shared';

export type Platform = 'windows' | 'macos' | 'linux';

export interface AssetInfo {
  platform: Platform;
  name: string;
  url: string;
  size: number;
  sizeLabel: string;
}

export interface ReleaseInfo {
  version: string;
  htmlUrl: string;
  assets: AssetInfo[];
}

const GITHUB_RELEASES_URL = `https://github.com/${gitConfig.user}/${gitConfig.repo}/releases`;

// Whitelist: only the user-facing installer for each platform. Velopack
// internals (.nupkg, RELEASES*, *.json manifests, portable zips) are skipped —
// users who want those reach them via the "All releases" link.
const ASSET_RULES: Record<Platform, RegExp> = {
  windows: /win-Setup\.exe$/i,
  macos: /osx-Setup\.pkg$/i,
  linux: /\.AppImage$/i,
};

export function detectPlatform(ua: string): Platform | null {
  if (!ua) return null;
  // Desktop-only product; mobile/tablet visitors get the fallback.
  if (/Android|iPhone|iPad|iPod|Mobile|Tablet/i.test(ua)) return null;
  if (/Windows/i.test(ua)) return 'windows';
  if (/Macintosh|Mac OS X/i.test(ua)) return 'macos';
  if (/Linux|X11|FreeBSD/i.test(ua)) return 'linux';
  return null;
}

function formatSize(bytes: number): string {
  const mb = bytes / (1024 * 1024);
  if (mb >= 1024) return `${(mb / 1024).toFixed(1)} GB`;
  return `${Math.round(mb)} MB`;
}

async function fetchLatestRelease(): Promise<ReleaseInfo | null> {
  try {
    const res = await fetch(
      `https://api.github.com/repos/${gitConfig.user}/${gitConfig.repo}/releases/latest`,
      {
        headers: { Accept: 'application/vnd.github+json' },
        cache: 'force-cache',
        // Unauthenticated GitHub API is rate-limited to 60 req/h per IP; ISR
        // keeps us well under that even under traffic spikes.
        next: { revalidate: 3600 },
      },
    );
    if (!res.ok) return null;
    const data = (await res.json()) as {
      tag_name: string;
      html_url: string;
      assets?: Array<{ name: string; browser_download_url: string; size: number }>;
    };

    const assets: AssetInfo[] = [];
    for (const platform of ['windows', 'macos', 'linux'] as Platform[]) {
      const rule = ASSET_RULES[platform];
      const match = data.assets?.find((a) => rule.test(a.name));
      if (match) {
        assets.push({
          platform,
          name: match.name,
          url: match.browser_download_url,
          size: match.size,
          sizeLabel: formatSize(match.size),
        });
      }
    }

    return {
      version: data.tag_name,
      htmlUrl: data.html_url || GITHUB_RELEASES_URL,
      assets,
    };
  } catch {
    return null;
  }
}

export async function getRelease(): Promise<ReleaseInfo | null> {
  return fetchLatestRelease();
}

// Mirror酱 is a China CDN that mirrors GitHub release assets behind a CDK.
// The `source` param attributes this homepage entry so it isn't confused with
// the README's github-readme entry or the in-app updater's own source tag.
// rid/channel are the project's registered identifiers on the Mirror酱 side,
// not derived from gitConfig — they are independent of the GitHub repo name.
const MIRRORCHYAN_URL = 'https://mirrorchyan.com/zh/projects?rid=Polymerium&channel=Polymerium_setup&source=homepage';

export function getMirrorChyanUrl(): string {
  return MIRRORCHYAN_URL;
}

export function getReleasesUrl(): string {
  return GITHUB_RELEASES_URL;
}
