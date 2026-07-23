import type { Metadata } from 'next';
import Image from 'next/image';
import Link from 'next/link';
import {
  HardDrive,
  Camera,
  FileJson,
  Terminal,
  ArrowRight,
  Zap,
  RefreshCw,
  Monitor,
  ShieldCheck,
  ShoppingBag,
} from 'lucide-react';
import { LinkButton } from '@/components/link-button';
import { DownloadButton, type DownloadLabels } from '@/components/download-button';
import { HeroVerb } from '@/components/hero-verb';
import { Reveal } from '@/components/reveal';
import { Logo } from '@/components/logo';
import { getMirrorChyanUrl, getRelease } from '@/lib/releases';
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion';

const GITHUB_URL = 'https://github.com/d3ara1n/Polymerium';
const DOWNLOAD_URL = `${GITHUB_URL}/releases`;

/* ─── i18n dictionaries ─── */

function useDict(lang: string) {
  if (lang === 'zh') {
    return {
      // Hero
      heroVerbs: ['管理', '打包', '交付'],
      heroRest: '你的 Minecraft 体验',
      heroSub:
        '元数据驱动的 Minecraft 启动器，每个模组只存一份——零重复实例、快照系统，以及内置 CLI 与 MCP AI Agent 模式。',
      heroShotAlt: 'Polymerium 主界面截图',
      ctaDownload: '下载',
      ctaCli: '命令行工具',

      // Download split button
      downloadForTemplate: '下载 {name} 版',
      platformName: { windows: 'Windows', macos: 'macOS', linux: 'Linux' },
      appleSilicon: '仅 Apple Silicon',
      allReleases: 'GitHub 全部版本',
      allFilesTemplate: 'GitHub {version} 的全部文件',
      mirrorHint: '已有 Mirror酱 CDK？前往高速下载',

      // Tech badges
      badgeOpenSource: 'MIT 开源',

      // Core value
      coreTitle: '实例不是文件夹，是一份描述',
      coreDesc:
        'Polymerium 用一个轻量的 profile.json 描述你的游戏配置——版本、加载器、模组列表。部署时从共享缓存通过符号链接构建，不复制任何文件。这意味着零重复存储、秒级切换整合包、Git 友好的版本控制。',

      // Features
      featuresTitle: '为高效而生',
      marketShotAlt: 'Polymerium 整合包市场界面截图',
      features: [
        {
          title: '整合包市场',
          desc: '在启动器内直接浏览、安装和更新来自 Modrinth 与 CurseForge 的整合包。',
        },
        {
          title: '零重复存储',
          desc: '每个模组文件只存一份，通过符号链接共享到所有实例。实例越多，节省的磁盘空间越多。',
        },
        {
          title: '秒级切换',
          desc: '在不同整合包之间秒级切换。实例由元数据构建，无需复制任何文件。',
        },
        {
          title: '快照系统',
          desc: '保存、恢复和对比完整游戏状态，放心尝试任何改动。',
        },
        {
          title: 'Git 友好的整合包',
          desc: '一个实例就是一个 JSON 文件，像代码一样用 Git 管理你的整合包。',
        },
        {
          title: '隐私优先',
          desc: '无广告、无遥测、无数据收集，MIT 协议开源。',
        },
      ],

      // MCP highlight
      mcpLabel: '独家功能',
      mcpTitle: '唯一内置 MCP 的 Minecraft 启动器',
      mcpDesc:
        '启动 trident MCP 服务器，AI Agent 可以直接创建实例、添加模组、构建部署、管理快照——30+ 工具覆盖完整工作流。',
      mcpCta: '查看 CLI 文档',

      // Screenshot tour
      tourTitle: '界面一览',
      tourItems: [
        {
          title: '实例总览',
          desc: '部署、启动、游戏时长统计，一屏搞定。',
          alt: 'Polymerium 实例总览界面截图',
        },
        {
          title: '包管理',
          desc: '每个包一个开关，版本由 Pref 稳定引用锁定。',
          alt: 'Polymerium 包管理界面截图',
        },
      ],

      // FAQ
      faqTitle: '常见问题',
      faqItems: [
        {
          q: '支持哪些模组加载器？',
          a: '支持 Fabric、Forge、NeoForge 和 Quilt。你可以在创建实例时选择，也可以随时切换。',
        },
        {
          q: '和 Prism Launcher / HMCL 有什么不同？',
          a: '传统启动器为每个实例完整复制所有文件。Polymerium 采用元数据驱动架构，用符号链接共享模组文件，大幅节省磁盘空间，并且支持快照、MCP AI Agent 模式等高级功能。',
        },
        {
          q: '数据存在哪里？能干净卸载吗？',
          a: '所有数据存储在应用本地目录中的 SQLite 数据库和 JSON 文件中，不写注册表。卸载只需删除应用目录和数据目录即可，干净无残留。',
        },
        {
          q: '能从其他启动器迁移吗？',
          a: '支持从官方 Minecraft 启动器、Prism Launcher 等启动器迁移。详见迁移指南。',
        },
        {
          q: '什么是 MCP 模式？',
          a: 'MCP（Model Context Protocol）是一种让 AI Agent 与应用交互的协议。启动 trident 的 MCP 服务器后，Claude、GPT 等 AI 助手可以直接帮你管理 Minecraft 实例。',
        },
        {
          q: '支持哪些平台？',
          a: '支持 Windows (x64)、Linux (x64) 和 macOS (ARM64)。所有平台功能一致。',
        },
      ],

      // CTA
      ctaTitle: '准备好节省磁盘空间了吗？',
      ctaSub: '下载 Polymerium，体验元数据驱动的实例管理。',

      // Footer
      footerDocs: '文档',
      footerLicense: 'MIT 开源协议',

      // Meta
      metaTitle: 'Polymerium — Minecraft 实例管理器',
      metaDesc: '基于元数据驱动的 Minecraft 实例管理器。零重复存储、快照、CLI、MCP AI Agent 模式。',
    };
  }
  return {
    heroVerbs: ['Manage', 'Pack', 'Deliver'],
    heroRest: ' your Minecraft experience.',
    heroSub:
      'The metadata-driven Minecraft launcher that stores each mod exactly once — zero-duplication instances, snapshots, and a built-in CLI with MCP mode for AI agents.',
    heroShotAlt: 'Screenshot of the Polymerium home screen',
    ctaDownload: 'Download',
    ctaCli: 'CLI Tool',

    // Download split button
    downloadForTemplate: 'Download for {name}',
    platformName: { windows: 'Windows', macos: 'macOS', linux: 'Linux' },
    appleSilicon: 'Apple Silicon only',
    allReleases: 'All releases on GitHub',
    allFilesTemplate: 'All files in {version} on GitHub',
    mirrorHint: 'Already have a Mirror酱 CDK? Fast download',

    badgeOpenSource: 'MIT License',

    coreTitle: 'An Instance Is a Description, Not a Folder Copy',
    coreDesc:
      'Polymerium describes your game setup with a lightweight profile.json — version, loader, mod list. Instances are built on demand from a shared cache using symlinks. No files are copied. That means zero duplication, instant modpack switching, and Git-friendly version control.',

    featuresTitle: 'Built for Efficiency',
    marketShotAlt: 'Screenshot of the Polymerium modpack marketplace',
    features: [
      {
        title: 'Modpack Marketplace',
        desc: 'Browse, install, and update modpacks from Modrinth and CurseForge without leaving the launcher.',
      },
      {
        title: 'Zero Duplication',
        desc: 'Every mod file is stored exactly once and shared across instances via symlinks. The more instances you run, the more disk you save.',
      },
      {
        title: 'Instant Switching',
        desc: 'Switch between modpacks in seconds. Instances are built from metadata, not file copies.',
      },
      {
        title: 'Snapshots',
        desc: 'Save, restore, and diff entire game states. Experiment without fear.',
      },
      {
        title: 'Git-Friendly Modpacks',
        desc: 'An instance is a single JSON file. Version control your modpack like code.',
      },
      {
        title: 'Privacy First',
        desc: 'No ads, no telemetry, no data collection. Open source under the MIT license.',
      },
    ],

    mcpLabel: 'Exclusive',
    mcpTitle: 'The Only Minecraft Launcher with Built-in MCP',
    mcpDesc:
      'Start the trident MCP server and let AI agents create instances, add mods, build deployments, and manage snapshots — 30+ tools covering the full workflow.',
    mcpCta: 'View CLI Docs',

    tourTitle: 'A Look Inside',
    tourItems: [
      {
        title: 'Instance Overview',
        desc: 'Deploy, launch, and track play time from a single screen.',
        alt: 'Screenshot of the instance overview with the launch pad',
      },
      {
        title: 'Package Management',
        desc: 'Every package one toggle away, versions pinned by stable Pref references.',
        alt: 'Screenshot of the package management page',
      },
    ],

    faqTitle: 'Frequently Asked Questions',
    faqItems: [
      {
        q: 'Which mod loaders are supported?',
        a: 'Fabric, Forge, NeoForge, and Quilt. You choose when creating an instance and can switch anytime.',
      },
      {
        q: 'How is this different from Prism Launcher / HMCL?',
        a: 'Traditional launchers copy every file into each instance. Polymerium uses a metadata-driven architecture that shares mod files via symlinks, dramatically reducing disk usage. It also supports snapshots, MCP AI Agent mode, and other advanced features.',
      },
      {
        q: 'Where is my data stored? Can I do a clean uninstall?',
        a: 'All data lives in a local SQLite database and JSON files within the app\'s data directory — no registry entries. To uninstall, simply delete the app and data folders. Clean and complete.',
      },
      {
        q: 'Can I migrate from other launchers?',
        a: 'Yes. Polymerium supports migrating from the vanilla Minecraft launcher, Prism Launcher, and more. See the migration guide for details.',
      },
      {
        q: 'What is MCP mode?',
        a: 'MCP (Model Context Protocol) lets AI agents interact with applications. Start trident\'s MCP server, and AI assistants like Claude or GPT can directly manage your Minecraft instances.',
      },
      {
        q: 'Which platforms are supported?',
        a: 'Windows (x64), Linux (x64), and macOS (ARM64). Full feature parity across all platforms.',
      },
    ],

    ctaTitle: 'Ready to Save Disk Space?',
    ctaSub: 'Download Polymerium and experience metadata-driven instance management.',

    footerDocs: 'Docs',
    footerLicense: 'MIT License',

    metaTitle: 'Polymerium — Minecraft Instance Manager',
    metaDesc: 'Metadata-driven Minecraft instance manager. Zero-duplication storage, snapshots, CLI, MCP AI Agent mode.',
  };
}

/* ─── Icon helpers ─── */

function GitHubIcon(props: React.ComponentProps<'svg'>) {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" {...props}>
      <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0024 12c0-6.63-5.37-12-12-12z" />
    </svg>
  );
}

const featureIcons = [ShoppingBag, HardDrive, RefreshCw, Camera, FileJson, ShieldCheck];

/* ─── profile.json syntax-highlighted sample ─── */

const profileJsonLines: [string, string][][] = [
  [['code-punct', '{']],
  [['code-key', '  "version"'], ['code-punct', ': '], ['code-string', '"1.21.4"'], ['code-punct', ',']],
  [
    ['code-key', '  "loader"'],
    ['code-punct', ': { '],
    ['code-key', '"type"'],
    ['code-punct', ': '],
    ['code-string', '"fabric"'],
    ['code-punct', ', '],
    ['code-key', '"version"'],
    ['code-punct', ': '],
    ['code-string', '"0.16.9"'],
    ['code-punct', ' },'],
  ],
  [['code-key', '  "packages"'], ['code-punct', ': [']],
  [['code-string', '    "modrinth:sodium"'], ['code-punct', ',']],
  [['code-string', '    "modrinth:iris"'], ['code-punct', ',']],
  [['code-string', '    "modrinth:fabric-api"']],
  [['code-punct', '  ]']],
  [['code-punct', '}']],
];

/* ─── Metadata ─── */

export async function generateMetadata(props: PageProps<'/[lang]'>): Promise<Metadata> {
  const params = await props.params;
  const lang = params.lang;
  const d = useDict(lang);

  return {
    title: d.metaTitle,
    description: d.metaDesc,
    alternates: {
      canonical: `https://polymerium.dearain.dev/${lang}`,
      languages: {
        en: 'https://polymerium.dearain.dev/en',
        zh: 'https://polymerium.dearain.dev/zh',
        'x-default': 'https://polymerium.dearain.dev/en',
      },
    },
    openGraph: {
      title: d.metaTitle,
      description: d.metaDesc,
      locale: lang === 'zh' ? 'zh_CN' : 'en_US',
      url: `https://polymerium.dearain.dev/${lang}`,
      images: [{ url: `/og/home?lang=${lang}`, width: 1200, height: 630, type: 'image/png' }],
    },
  };
}

/* ─── Page ─── */

export default async function HomePage(props: PageProps<'/[lang]'>) {
  const params = await props.params;
  const lang = params.lang;
  const d = useDict(lang);

  const release = await getRelease();
  const downloadLabels: DownloadLabels = {
    genericDownloadLabel: d.ctaDownload,
    downloadForTemplate: d.downloadForTemplate,
    platformName: d.platformName,
    appleSilicon: d.appleSilicon,
    allReleases: d.allReleases,
    allFilesTemplate: d.allFilesTemplate,
  };

  const faqSchema = {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: d.faqItems.map((item) => ({
      '@type': 'Question',
      name: item.q,
      acceptedAnswer: {
        '@type': 'Answer',
        text: item.a,
      },
    })),
  };

  const softwareSchema = {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    name: 'Polymerium',
    applicationCategory: 'GameApplication',
    operatingSystem: 'Windows, Linux, macOS',
    offers: {
      '@type': 'Offer',
      price: '0',
      priceCurrency: 'USD',
    },
    description: d.metaDesc,
    url: `https://polymerium.dearain.dev/${lang}`,
    installUrl: 'https://github.com/d3ara1n/Polymerium/releases',
    license: 'https://opensource.org/licenses/MIT',
  };

  const organizationSchema = {
    '@context': 'https://schema.org',
    '@type': 'Organization',
    name: 'Polymerium',
    url: 'https://polymerium.dearain.dev',
    logo: 'https://polymerium.dearain.dev/favicon.png',
  };

  const breadcrumbSchema = {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: [
      {
        '@type': 'ListItem',
        position: 1,
        name: lang === 'zh' ? '首页' : 'Home',
        item: `https://polymerium.dearain.dev/${lang}`,
      },
    ],
  };

  return (
    <div className="flex flex-col flex-1">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(faqSchema) }}
      />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(softwareSchema) }}
      />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(organizationSchema) }}
      />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(breadcrumbSchema) }}
      />

      {/* ──── Hero ──── */}
      <section className="relative overflow-hidden">
        <div aria-hidden className="pointer-events-none absolute inset-0 hero-grid" />
        <div aria-hidden className="pointer-events-none absolute -top-40 right-[-8%] hero-glow" />

        <div className="relative mx-auto grid max-w-6xl items-center gap-10 px-6 pt-14 pb-16 md:pt-20 md:pb-24 lg:grid-cols-[1.15fr_1fr] lg:gap-14">
          <div>
            <Reveal>
              <h1 className="text-4xl sm:text-5xl font-bold tracking-tight text-fd-foreground leading-[1.12]">
                <HeroVerb verbs={d.heroVerbs} className="text-primary" />
                {d.heroRest}
              </h1>
            </Reveal>
            <Reveal delay={0.08}>
              <p className="mt-6 max-w-xl text-lg text-fd-muted-foreground leading-relaxed">
                {d.heroSub}
              </p>
            </Reveal>
            <Reveal delay={0.16}>
              <div className="mt-8 flex flex-wrap items-center gap-4">
                <DownloadButton release={release} labels={downloadLabels} />
                <LinkButton
                  href={`/${lang}/docs/advanced/cli`}
                  variant="outline"
                  size="lg"
                  className="rounded-full px-7 text-sm font-semibold"
                >
                  <Terminal className="size-4" />
                  {d.ctaCli}
                </LinkButton>
              </div>
              {lang === 'zh' && (
                <p className="mt-4 flex items-center gap-1.5 text-sm text-muted-foreground">
                  <Zap className="size-3.5 text-primary" />
                  <a
                    href={getMirrorChyanUrl()}
                    className="underline-offset-4 transition-colors hover:text-foreground hover:underline"
                  >
                    {d.mirrorHint}
                  </a>
                </p>
              )}
            </Reveal>
          </div>

          <Reveal delay={0.2} className="relative">
            <div
              aria-hidden
              className="absolute -inset-8 rounded-[2.5rem] bg-primary/15 blur-3xl"
            />
            <Image
              src="/screenshots/landing.webp"
              alt={d.heroShotAlt}
              width={1920}
              height={1098}
              priority
              className="relative rounded-xl border border-border shadow-2xl shadow-primary/10"
            />
          </Reveal>
        </div>
      </section>

      {/* ──── Tech Badges ──── */}
      <section className="border-y border-fd-border py-5">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-center gap-x-10 gap-y-3 px-6">
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Monitor className="size-4 text-primary" />
            <span className="font-medium">Win / Linux / macOS</span>
          </div>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <GitHubIcon className="size-4" />
            <span className="font-medium">{d.badgeOpenSource}</span>
          </div>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Zap className="size-4 text-primary" />
            <span className="font-medium">Fabric · Forge · NeoForge · Quilt</span>
          </div>
        </div>
      </section>

      {/* ──── Core Value Prop ──── */}
      <section className="py-24">
        <div className="mx-auto grid max-w-6xl items-center gap-12 px-6 md:grid-cols-2">
          <Reveal>
            <h2 className="text-3xl md:text-4xl font-bold tracking-tight text-foreground leading-tight">
              {d.coreTitle}
            </h2>
            <p className="mt-6 text-base leading-relaxed text-muted-foreground max-w-[58ch]">
              {d.coreDesc}
            </p>
          </Reveal>
          <Reveal delay={0.1}>
            <div className="terminal-block">
              <div className="terminal-titlebar">
                <span className="window-dot" style={{ background: '#ff5f57' }} />
                <span className="window-dot" style={{ background: '#febc2e' }} />
                <span className="window-dot" style={{ background: '#28c840' }} />
                <span className="ml-3 text-xs font-medium" style={{ color: 'oklch(0.62 0.015 70)' }}>
                  profile.json
                </span>
              </div>
              <div className="terminal-body">
                <pre>
                  <code>
                    {profileJsonLines.map((line, i) => (
                      <span key={i}>
                        {line.map(([cls, text], j) => (
                          <span key={j} className={cls}>
                            {text}
                          </span>
                        ))}
                        {'\n'}
                      </span>
                    ))}
                  </code>
                </pre>
              </div>
            </div>
          </Reveal>
        </div>
      </section>

      {/* ──── Features Bento ──── */}
      <section className="py-24 border-t border-fd-border">
        <div className="mx-auto max-w-6xl px-6">
          <Reveal>
            <h2 className="text-center text-3xl md:text-4xl font-bold tracking-tight text-foreground">
              {d.featuresTitle}
            </h2>
          </Reveal>
          <div className="mt-14 grid gap-5 md:grid-cols-6">
            {d.features.map((f, i) => {
              const Icon = featureIcons[i];
              if (i === 0) {
                return (
                  <Reveal key={f.title} className="md:col-span-4">
                    <div className="group h-full rounded-2xl border border-border bg-card overflow-hidden transition-all duration-200 hover:border-primary/40 hover:shadow-lg hover:shadow-primary/5">
                      <div className="grid h-full items-center sm:grid-cols-2">
                        <div className="p-7">
                          <div className="flex size-10 items-center justify-center rounded-xl bg-primary/10 text-primary transition-colors duration-200 group-hover:bg-primary group-hover:text-primary-foreground">
                            <Icon className="size-5" />
                          </div>
                          <h3 className="mt-4 text-lg font-semibold text-foreground">{f.title}</h3>
                          <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
                            {f.desc}
                          </p>
                        </div>
                        <div className="border-t border-border p-4 sm:border-t-0 sm:border-l sm:p-5">
                          {/* NOTE: aspect-[7/4] matches the source image (1920×1098); swap the screenshot and this ratio must change with it, or object-cover crops again */}
                          <div className="relative aspect-[7/4] w-full overflow-hidden rounded-lg border border-border">
                            <Image
                              src="/screenshots/marketplace.webp"
                              alt={d.marketShotAlt}
                              fill
                              sizes="(min-width: 768px) 22rem, 100vw"
                              className="object-cover object-left-top"
                            />
                          </div>
                        </div>
                      </div>
                    </div>
                  </Reveal>
                );
              }
              if (i === 5) {
                return (
                  <Reveal key={f.title} className="md:col-span-6">
                    <div className="group flex h-full items-center gap-5 rounded-2xl border border-primary/25 bg-primary/8 p-6 transition-all duration-200 hover:border-primary/50">
                      <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/15 text-primary transition-colors duration-200 group-hover:bg-primary group-hover:text-primary-foreground">
                        <Icon className="size-5" />
                      </div>
                      <p className="text-sm leading-relaxed text-muted-foreground">
                        <span className="font-semibold text-foreground">{f.title} — </span>
                        {f.desc}
                      </p>
                    </div>
                  </Reveal>
                );
              }
              return (
                <Reveal key={f.title} delay={0.05 * i} className="md:col-span-2">
                  <div className="group h-full rounded-2xl border border-border bg-card p-7 transition-all duration-200 hover:border-primary/40 hover:shadow-lg hover:shadow-primary/5 hover:-translate-y-0.5">
                    <div className="flex size-10 items-center justify-center rounded-xl bg-primary/10 text-primary transition-colors duration-200 group-hover:bg-primary group-hover:text-primary-foreground">
                      <Icon className="size-5" />
                    </div>
                    <h3 className="mt-4 text-base font-semibold text-foreground">{f.title}</h3>
                    <p className="mt-2 text-sm leading-relaxed text-muted-foreground">{f.desc}</p>
                  </div>
                </Reveal>
              );
            })}
          </div>
        </div>
      </section>

      {/* ──── Screenshot Tour ──── */}
      <section className="py-24 border-t border-fd-border">
        <div className="mx-auto max-w-6xl px-6">
          <Reveal>
            <h2 className="text-center text-3xl md:text-4xl font-bold tracking-tight text-foreground">
              {d.tourTitle}
            </h2>
          </Reveal>
          <div className="mt-14 grid gap-6 md:grid-cols-2">
            {d.tourItems.map((item, i) => (
              <Reveal key={item.title} delay={0.08 * i}>
                <figure className="group overflow-hidden rounded-2xl border border-border bg-card transition-all duration-200 hover:border-primary/40 hover:shadow-lg hover:shadow-primary/5">
                  <div className="p-4 sm:p-5">
                    <div className="overflow-hidden rounded-xl border border-border">
                      <Image
                        src={i === 0 ? '/screenshots/instance.webp' : '/screenshots/setup.webp'}
                        alt={item.alt}
                        width={1920}
                        height={1098}
                        className="transition-transform duration-300 group-hover:scale-[1.02]"
                      />
                    </div>
                  </div>
                  <figcaption className="px-6 pb-6 pt-1">
                    <h3 className="text-base font-semibold text-foreground">{item.title}</h3>
                    <p className="mt-1.5 text-sm leading-relaxed text-muted-foreground">
                      {item.desc}
                    </p>
                  </figcaption>
                </figure>
              </Reveal>
            ))}
          </div>
        </div>
      </section>

      {/* ──── CLI + MCP Highlight ──── */}
      <section className="py-24 border-t border-fd-border">
        <div className="mx-auto grid max-w-6xl items-center gap-12 px-6 md:grid-cols-2">
          <Reveal>
            <div className="terminal-block">
              <div className="terminal-titlebar">
                <span className="window-dot" style={{ background: '#ff5f57' }} />
                <span className="window-dot" style={{ background: '#febc2e' }} />
                <span className="window-dot" style={{ background: '#28c840' }} />
                <span className="ml-3 text-xs font-medium" style={{ color: 'oklch(0.62 0.015 70)' }}>
                  trident — MCP server
                </span>
              </div>
              <div className="terminal-body">
                <div>
                  <span className="terminal-prompt">$ </span>
                  <span className="terminal-cmd">trident</span>{' '}
                  <span className="terminal-flag">instance create</span>{' '}
                  <span className="terminal-cmd">my-pack</span>{' '}
                  <span className="terminal-flag">--version</span>{' '}
                  <span className="terminal-cmd">1.21.4</span>
                </div>
                <div>
                  <span className="terminal-prompt">$ </span>
                  <span className="terminal-cmd">trident</span>{' '}
                  <span className="terminal-flag">package add</span>{' '}
                  <span className="terminal-cmd">modrinth:sodium</span>
                </div>
                <div>
                  <span className="terminal-prompt">$ </span>
                  <span className="terminal-cmd">trident</span>{' '}
                  <span className="terminal-flag">package add</span>{' '}
                  <span className="terminal-cmd">modrinth:iris</span>
                </div>
                <div>
                  <span className="terminal-prompt">$ </span>
                  <span className="terminal-cmd">trident</span>{' '}
                  <span className="terminal-flag">instance build</span>{' '}
                  <span className="terminal-cmd">my-pack</span>
                </div>
                <div>
                  <span className="terminal-prompt">$ </span>
                  <span className="terminal-cmd">trident</span>{' '}
                  <span className="terminal-flag">instance run</span>{' '}
                  <span className="terminal-cmd">my-pack</span>
                </div>
                <div className="mt-2" style={{ color: 'oklch(0.72 0.13 150)' }}>
                  ✓ Instance "my-pack" deployed and launched.
                </div>
                <div className="mt-3">
                  <span className="terminal-prompt">$ </span>
                  <span className="terminal-cmd">trident</span>{' '}
                  <span className="terminal-flag">--mcp</span>
                </div>
                <div style={{ color: 'oklch(0.72 0.11 255)' }}>
                  MCP server listening on stdio... (30+ tools registered)
                </div>
              </div>
            </div>
          </Reveal>

          <Reveal delay={0.1}>
            <span className="inline-block rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary">
              {d.mcpLabel}
            </span>
            <h2 className="mt-4 text-3xl md:text-4xl font-bold tracking-tight text-foreground leading-tight">
              {d.mcpTitle}
            </h2>
            <p className="mt-6 text-base leading-relaxed text-muted-foreground max-w-[58ch]">
              {d.mcpDesc}
            </p>
            <div className="mt-8">
              <LinkButton href={`/${lang}/docs/advanced/cli`} variant="outline" className="rounded-full">
                {d.mcpCta}
                <ArrowRight className="size-4" />
              </LinkButton>
            </div>
          </Reveal>
        </div>
      </section>

      {/* ──── FAQ ──── */}
      <section className="py-24 border-t border-fd-border">
        <div className="mx-auto max-w-3xl px-6">
          <Reveal>
            <h2 className="text-center text-3xl md:text-4xl font-bold tracking-tight text-foreground">
              {d.faqTitle}
            </h2>
          </Reveal>
          <div className="mt-10">
            <Accordion>
              {d.faqItems.map((item, i) => (
                <AccordionItem key={i} value={`faq-${i}`}>
                  <AccordionTrigger>{item.q}</AccordionTrigger>
                  <AccordionContent>{item.a}</AccordionContent>
                </AccordionItem>
              ))}
            </Accordion>
          </div>
        </div>
      </section>

      {/* ──── CTA ──── */}
      <section className="relative overflow-hidden py-24 border-t border-fd-border">
        <div
          aria-hidden
          className="pointer-events-none absolute bottom-[-40%] left-1/2 -translate-x-1/2 hero-glow"
        />
        <Reveal className="relative mx-auto max-w-4xl px-6 text-center">
          <h2 className="text-3xl md:text-4xl font-bold tracking-tight text-foreground">
            {d.ctaTitle}
          </h2>
          <p className="mx-auto mt-4 max-w-xl text-muted-foreground">{d.ctaSub}</p>
          <div className="mt-8 flex justify-center">
            <DownloadButton release={release} labels={downloadLabels} />
          </div>
        </Reveal>
      </section>

      {/* ──── Footer ──── */}
      <footer className="border-t border-fd-border py-10">
        <div className="mx-auto flex max-w-6xl flex-col items-center gap-4 px-6 sm:flex-row sm:justify-between">
          <div className="flex items-center gap-2.5">
            <Logo className="size-5 text-primary" />
            <span className="text-sm font-semibold text-foreground">Polymerium</span>
            <span className="text-sm text-muted-foreground">· {d.footerLicense}</span>
          </div>
          <div className="flex items-center gap-6 text-sm text-muted-foreground">
            <Link href={DOWNLOAD_URL} className="hover:text-foreground transition">
              {d.ctaDownload}
            </Link>
            <Link href={`/${lang}/docs`} className="hover:text-foreground transition">
              {d.footerDocs}
            </Link>
            <Link
              href={GITHUB_URL}
              className="inline-flex items-center gap-1.5 hover:text-foreground transition"
            >
              <GitHubIcon className="size-4" />
              GitHub
            </Link>
          </div>
        </div>
      </footer>
    </div>
  );
}
