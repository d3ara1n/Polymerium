'use client';

import { Menu } from '@base-ui/react/menu';
import {
  Apple,
  Check,
  ChevronDown,
  Download,
  ExternalLink,
  Monitor,
  Terminal,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import {
  type AssetInfo,
  type Platform,
  type ReleaseInfo,
  getReleasesUrl,
} from '@/lib/releases';

export interface DownloadLabels {
  detectedDownloadLabel: string;
  genericDownloadLabel: string;
  platformName: Record<Platform, string>;
  appleSilicon: string;
  allReleases: string;
}

const PLATFORM_ICON: Record<Platform, typeof Monitor> = {
  windows: Monitor,
  macos: Apple,
  linux: Terminal,
};

interface DownloadButtonProps {
  release: ReleaseInfo | null;
  platform: Platform | null;
  labels: DownloadLabels;
  className?: string;
}

export function DownloadButton({
  release,
  platform,
  labels,
  className,
}: DownloadButtonProps) {
  const detectedAsset =
    release && platform
      ? (release.assets.find((a) => a.platform === platform) ?? null)
      : null;

  const mainHref = detectedAsset ? detectedAsset.url : (release?.htmlUrl ?? getReleasesUrl());
  const mainLabel = detectedAsset
    ? labels.detectedDownloadLabel
    : labels.genericDownloadLabel;

  return (
    <div className={cn('inline-flex overflow-hidden rounded-full shadow-lg', className)}>
      <a
        href={mainHref}
        className="inline-flex h-9 items-center gap-1.5 bg-primary px-5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/80"
      >
        <Download className="size-4" />
        <span className="sm:hidden">{labels.genericDownloadLabel}</span>
        <span className="hidden sm:inline">
          {mainLabel}
          {detectedAsset ? ` · ${detectedAsset.sizeLabel}` : ''}
        </span>
      </a>
      <Menu.Root>
        <Menu.Trigger
          aria-label={labels.allReleases}
          className="inline-flex h-9 items-center justify-center border-l border-primary-foreground/20 bg-primary px-3 text-primary-foreground transition-colors hover:bg-primary/80 aria-expanded:bg-primary/70"
        >
          <ChevronDown className="size-4" />
        </Menu.Trigger>
        <Menu.Portal>
          <Menu.Positioner align="end" sideOffset={8}>
            <Menu.Popup className="z-50 min-w-[264px] rounded-xl border border-border bg-popover p-1.5 text-popover-foreground shadow-md outline-none">
              {release?.assets.map((asset) => (
                <PlatformItem
                  key={asset.platform}
                  asset={asset}
                  labels={labels}
                  isCurrent={asset.platform === platform}
                />
              ))}
              <Menu.Separator className="my-1 h-px bg-border" />
              <Menu.LinkItem
                href={release?.htmlUrl ?? getReleasesUrl()}
                className="group flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-sm text-muted-foreground outline-none transition-colors data-[highlighted]:bg-accent data-[highlighted]:text-accent-foreground"
              >
                <ExternalLink className="size-4 shrink-0 text-muted-foreground group-data-[highlighted]:text-accent-foreground" />
                <span className="flex-1">{labels.allReleases}</span>
              </Menu.LinkItem>
            </Menu.Popup>
          </Menu.Positioner>
        </Menu.Portal>
      </Menu.Root>
    </div>
  );
}

function PlatformItem({
  asset,
  labels,
  isCurrent,
}: {
  asset: AssetInfo;
  labels: DownloadLabels;
  isCurrent: boolean;
}) {
  const Icon = PLATFORM_ICON[asset.platform];
  return (
    <Menu.LinkItem
      href={asset.url}
      className="group flex items-center gap-2.5 rounded-lg px-2.5 py-2 outline-none transition-colors data-[highlighted]:bg-accent"
    >
      <Icon className="size-4 shrink-0 text-muted-foreground group-data-[highlighted]:text-accent-foreground" />
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-1.5 text-sm font-medium text-foreground">
          {labels.platformName[asset.platform]}
          {isCurrent && <Check className="size-3.5 text-primary" />}
        </div>
        <div className="truncate text-xs text-muted-foreground">
          {asset.name} · {asset.sizeLabel}
          {asset.platform === 'macos' ? ` · ${labels.appleSilicon}` : ''}
        </div>
      </div>
    </Menu.LinkItem>
  );
}
