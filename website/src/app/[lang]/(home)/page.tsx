import Link from 'next/link';
import {
  HardDrive,
  Camera,
  FileJson,
  Terminal,
  ArrowRight,
  Download,
  Zap,
  RefreshCw,
  Monitor,
  ShieldCheck,
} from 'lucide-react';
import { LinkButton } from '@/components/link-button';
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
      heroTagline: '每个模组只存一份的 Minecraft 启动器',
      heroSub:
        '基于元数据驱动的实例管理器——零重复存储、快照、内置 CLI 与 MCP AI Agent 模式。',
      ctaDownload: '下载',
      ctaCli: '命令行工具',
      ctaViewDocs: '查看文档',

      // Tech badges
      badgeCrossPlatform: '跨平台',
      badgeOpenSource: 'MIT 开源',
      badgeLoaders: '支持所有主流加载器',

      // Core value
      coreTitle: '实例不是文件夹，是一份描述',
      coreDesc:
        'Polymerium 用一个轻量的 profile.json 描述你的游戏配置——版本、加载器、模组列表。部署时从共享缓存通过符号链接构建，不复制任何文件。这意味着零重复存储、秒级切换整合包、Git 友好的版本控制。',
      coreJson: `{
  "version": "1.21.4",
  "loader": { "type": "fabric", "version": "0.16.9" },
  "modloader": true,
  "packages": [
    "modrinth:sodium",
    "modrinth:iris",
    "modrinth:fabric-api"
  ]
}`,
      coreJsonLabel: 'profile.json',

      // Features
      featuresTitle: '为高效而生',
      features: [
        {
          title: '零重复存储',
          desc: '每个模组文件只存一份，通过符号链接共享到所有实例。节省 60–80% 磁盘空间。',
        },
        {
          title: '秒级切换',
          desc: '秒级切换不同整合包，无需等待文件复制。',
        },
        {
          title: '快照系统',
          desc: '保存、恢复和对比完整游戏状态。安全地尝试任何改动。',
        },
        {
          title: 'Git 友好的整合包',
          desc: '实例 = 一个 JSON 文件。用 Git 管理你的整合包开发。',
        },
        {
          title: '命令行 + MCP 模式',
          desc: '30+ 命令自动化全流程。让 AI Agent 直接管理你的实例。',
        },
        {
          title: '隐私优先',
          desc: '无广告、无遥测、无数据收集。MIT 开源。',
        },
      ],

      // MCP highlight
      mcpLabel: '独家功能',
      mcpTitle: '唯一内置 MCP 的 Minecraft 启动器',
      mcpDesc:
        '启动 trident MCP 服务器，AI Agent 可以直接创建实例、添加模组、构建部署、管理快照——30+ 工具覆盖完整工作流。',
      mcpCta: '查看 CLI 文档',

      // Quick start
      quickTitle: '三步上手',
      quickSteps: [
        { step: '01', title: '下载安装', desc: '从 GitHub Releases 下载对应平台版本，解压即用。' },
        { step: '02', title: '创建实例', desc: '选择游戏版本和加载器，添加你需要的模组。' },
        { step: '03', title: '开始游戏', desc: '一键构建部署，启动 Minecraft。' },
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
          a: '支持从 Prism Launcher 导入实例。详见迁移文档。',
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
      footerMadeWith: '用 ❤️ 制作',

      // Meta
      metaTitle: 'Polymerium — Minecraft 实例管理器',
      metaDesc: '基于元数据驱动的 Minecraft 实例管理器。零重复存储、快照、CLI、MCP AI Agent 模式。',
    };
  }
  return {
    heroTagline: 'The Minecraft Launcher That Stores Each Mod Exactly Once',
    heroSub:
      'Metadata-driven instance manager with zero-duplication storage, snapshots, and a built-in CLI with MCP mode for AI agents.',
    ctaDownload: 'Download',
    ctaCli: 'CLI Tool',
    ctaViewDocs: 'View Docs',

    badgeCrossPlatform: 'Cross-platform',
    badgeOpenSource: 'MIT License',
    badgeLoaders: 'All major loaders supported',

    coreTitle: 'An Instance Is a Description, Not a Folder Copy',
    coreDesc:
      'Polymerium describes your game setup with a lightweight profile.json — version, loader, mod list. Instances are built on demand from a shared cache using symlinks. No files are copied. That means zero duplication, instant modpack switching, and Git-friendly version control.',
    coreJson: `{
  "version": "1.21.4",
  "loader": { "type": "fabric", "version": "0.16.9" },
  "modloader": true,
  "packages": [
    "modrinth:sodium",
    "modrinth:iris",
    "modrinth:fabric-api"
  ]
}`,
    coreJsonLabel: 'profile.json',

    featuresTitle: 'Built for Efficiency',
    features: [
      {
        title: 'Zero Duplication',
        desc: 'Each mod file stored once, symlinked everywhere. Save 60–80% disk space.',
      },
      {
        title: 'Instant Switching',
        desc: 'Change modpacks in seconds, not minutes. No file copying needed.',
      },
      {
        title: 'Snapshots',
        desc: 'Save, restore, and diff entire game states. Experiment safely.',
      },
      {
        title: 'Git-Friendly Modpacks',
        desc: 'Instance = one JSON file. Version control your modpack development.',
      },
      {
        title: 'CLI + MCP Mode',
        desc: '30+ commands for automation. Let AI agents manage your instances.',
      },
      {
        title: 'Privacy First',
        desc: 'No ads, no telemetry, no data collection. Open source (MIT).',
      },
    ],

    mcpLabel: 'Exclusive',
    mcpTitle: 'The Only Minecraft Launcher with Built-in MCP',
    mcpDesc:
      'Start the trident MCP server and let AI agents create instances, add mods, build deployments, and manage snapshots — 30+ tools covering the full workflow.',
    mcpCta: 'View CLI Docs',

    quickTitle: 'Up and Running in 3 Steps',
    quickSteps: [
      { step: '01', title: 'Download', desc: 'Grab the latest release for your platform from GitHub. No installer needed.' },
      { step: '02', title: 'Create Instance', desc: 'Pick a Minecraft version, choose a loader, add the mods you want.' },
      { step: '03', title: 'Play', desc: 'One-click deploy and launch. That\'s it.' },
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
        a: 'Yes. Polymerium supports importing instances from Prism Launcher. See the migration guide for details.',
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
    footerMadeWith: 'Made with ❤️',

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

const featureIcons = [HardDrive, RefreshCw, Camera, FileJson, Terminal, ShieldCheck];

/* ─── Page ─── */

export default async function HomePage(props: PageProps<'/[lang]'>) {
  const params = await props.params;
  const lang = params.lang;
  const d = useDict(lang);

  return (
    <div className="flex flex-col flex-1">
      {/* ──── Hero ──── */}
      <section className="relative overflow-hidden pt-24 pb-20 md:pt-36 md:pb-28">
        <div aria-hidden className="pointer-events-none absolute inset-0 hero-grid" />
        <div aria-hidden className="pointer-events-none absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 hero-glow" />

        <div className="relative mx-auto max-w-5xl px-6 text-center">
          <h1 className="text-4xl sm:text-5xl md:text-6xl font-extrabold tracking-tight text-fd-foreground leading-tight">
            {d.heroTagline}
          </h1>
          <p className="mx-auto mt-6 max-w-2xl text-lg text-fd-muted-foreground leading-relaxed">
            {d.heroSub}
          </p>

          {/* CTA buttons */}
          <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
            <LinkButton href={DOWNLOAD_URL} size="lg" className="rounded-full px-7 text-sm font-semibold shadow-lg">
                <Download className="size-4" />
                {d.ctaDownload}
            </LinkButton>
            <LinkButton href={`/${lang}/docs/cli`} variant="outline" size="lg" className="rounded-full px-7 text-sm font-semibold">
                <Terminal className="size-4" />
                {d.ctaCli}
            </LinkButton>
            <LinkButton href={`/${lang}/docs`} variant="ghost" size="lg" className="rounded-full px-7 text-sm font-semibold">
                {d.ctaViewDocs}
                <ArrowRight className="size-4" />
            </LinkButton>
          </div>

          {/* App schematic — simplified Polymerium UI mockup */}
          <div className="mx-auto mt-16 max-w-4xl">
            <div className="app-schematic rounded-xl overflow-hidden border border-border bg-card shadow-2xl">
              {/* Titlebar */}
              <div className="flex items-center gap-2 px-4 py-2.5 border-b border-border bg-muted">
                <span className="size-3 rounded-full bg-[#ff5f57]" />
                <span className="size-3 rounded-full bg-[#febc2e]" />
                <span className="size-3 rounded-full bg-[#28c840]" />
                <span className="ml-3 text-[11px] font-medium text-muted-foreground">Polymerium</span>
              </div>

              {/* App body — 3-column layout */}
              <div className="flex h-[280px] sm:h-[320px]">
                {/* Left sidebar */}
                <div className="hidden sm:flex w-12 flex-col items-center gap-4 py-4 border-r border-border bg-muted/50">
                  <div className="size-6 rounded-md bg-primary/10 flex items-center justify-center">
                    <svg className="size-3.5 text-primary" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/></svg>
                  </div>
                  <div className="size-6 rounded-md bg-muted flex items-center justify-center">
                    <svg className="size-3.5 text-muted-foreground" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/></svg>
                  </div>
                  <div className="size-6 rounded-md bg-muted flex items-center justify-center">
                    <svg className="size-3.5 text-muted-foreground" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/></svg>
                  </div>
                  <div className="mt-auto size-6 rounded-md bg-muted flex items-center justify-center">
                    <svg className="size-3.5 text-muted-foreground" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>
                  </div>
                </div>

                {/* Main content */}
                <div className="flex-1 flex flex-col p-4 gap-3 overflow-hidden">
                  {/* Top row — banner + setup panel */}
                  <div className="flex gap-3 h-[100px]">
                    {/* Instance banner */}
                    <div className="flex-[2] rounded-lg overflow-hidden relative bg-gradient-to-br from-primary/20 to-muted border border-border">
                      <div className="absolute inset-0 opacity-30" style={{ backgroundImage: 'radial-gradient(circle at 30% 70%, hsl(var(--primary) / 0.2) 0%, transparent 60%)' }} />
                      <div className="relative p-3 h-full flex flex-col justify-between">
                        <div className="text-[10px] text-muted-foreground font-medium">Overview</div>
                        <div className="text-sm font-bold text-foreground truncate">Fabulously Optimized</div>
                      </div>
                    </div>
                    {/* Setup panel */}
                    <div className="flex-1 rounded-lg border border-border bg-muted p-3 flex flex-col justify-between">
                      <div className="flex items-center justify-between">
                        <span className="text-[10px] text-muted-foreground">Setup</span>
                        <span className="text-[10px] text-muted-foreground/60">Details →</span>
                      </div>
                      <div className="text-lg font-bold text-foreground">43<span className="text-[10px] font-normal text-muted-foreground ml-1">packages</span></div>
                      <div className="flex gap-3 text-[10px]">
                        <span className="text-muted-foreground">Fabric</span>
                        <span className="text-muted-foreground">1.21.5</span>
                      </div>
                    </div>
                  </div>

                  {/* Middle row — stats */}
                  <div className="flex gap-3 flex-1">
                    {/* Play Time */}
                    <div className="flex-1 rounded-lg border border-border bg-muted p-3 flex flex-col items-center justify-center gap-1">
                      <div className="relative size-14 text-primary">
                        <svg className="size-14 -rotate-90" viewBox="0 0 36 36">
                          <circle cx="18" cy="18" r="15.9" fill="none" stroke="var(--border)" strokeWidth="2" />
                          <circle cx="18" cy="18" r="15.9" fill="none" stroke="currentColor" strokeWidth="2" strokeDasharray="60 100" />
                        </svg>
                      </div>
                      <div className="text-[9px] text-muted-foreground">Play Time</div>
                      <div className="text-sm font-bold text-foreground">8.3<span className="text-[9px] font-normal text-muted-foreground">h</span></div>
                    </div>
                    {/* Snapshots */}
                    <div className="flex-1 rounded-lg border border-border bg-muted p-3 flex flex-col items-center justify-center gap-1">
                      <svg className="size-6 text-muted-foreground" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"><path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"/><circle cx="12" cy="13" r="4"/></svg>
                      <div className="text-[9px] text-muted-foreground">Snapshots</div>
                      <div className="text-sm font-bold text-foreground">3</div>
                    </div>
                    {/* Storage Saved */}
                    <div className="hidden sm:flex flex-1 rounded-lg border border-border bg-muted p-3 flex-col items-center justify-center gap-1">
                      <svg className="size-6 text-muted-foreground" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"><line x1="22" y1="12" x2="2" y2="12"/><path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"/><line x1="6" y1="16" x2="6.01" y2="16"/><line x1="10" y1="16" x2="10.01" y2="16"/></svg>
                      <div className="text-[9px] text-muted-foreground">Storage Saved</div>
                      <div className="text-sm font-bold text-foreground">4.2<span className="text-[9px] font-normal text-muted-foreground">GB</span></div>
                    </div>
                  </div>

                  {/* Bottom row — launch pad */}
                  <div className="flex items-center gap-3">
                    <div className="flex-1 flex items-center gap-2 text-[10px] text-emerald-600 dark:text-emerald-400">
                      <span className="size-2 rounded-full bg-emerald-500/60" />
                      Everything is ready
                    </div>
                    <div className="rounded-md bg-primary px-4 py-1.5 text-[11px] font-bold text-primary-foreground">LAUNCH</div>
                    <div className="flex items-center gap-2 text-[10px] text-muted-foreground">
                      <div className="size-5 rounded bg-muted flex items-center justify-center text-[8px] font-bold text-muted-foreground">ST</div>
                      <span>Stewie</span>
                    </div>
                  </div>
                </div>

                {/* Right sidebar */}
                <div className="hidden md:flex w-40 flex-col gap-2 p-3 border-l border-border bg-muted/50">
                  <div className="flex items-center gap-2 rounded-md bg-muted px-3 py-2 text-[10px] text-muted-foreground">
                    <svg className="size-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
                    Home
                  </div>
                  <div className="flex items-center gap-2 rounded-md bg-primary px-3 py-2 text-[10px] font-medium text-primary-foreground">
                    <svg className="size-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/><line x1="3" y1="6" x2="21" y2="6"/><path d="M16 10a4 4 0 0 1-8 0"/></svg>
                    Marketplace
                  </div>
                  {/* Instance list */}
                  <div className="mt-1 flex-1 rounded-md border border-border bg-card p-2 flex flex-col gap-1.5">
                    <div className="rounded border border-border bg-muted/50 p-1.5 text-left">
                      <div className="text-[8px] text-muted-foreground/60 truncate text-left">CURSEFORGE#fabulously_optimized</div>
                      <div className="text-[9px] text-foreground truncate text-left">Fabulously Optimized</div>
                      <div className="flex gap-2 mt-1">
                        <span className="text-[7px] text-muted-foreground">Fabric</span>
                        <span className="text-[7px] text-muted-foreground">1.21.5</span>
                      </div>
                    </div>
                    <div className="rounded border border-border bg-muted/50 p-1.5 text-left">
                      <div className="text-[8px] text-muted-foreground/60 truncate text-left">MODRINTH#create</div>
                      <div className="text-[9px] text-foreground truncate text-left">Create: Above and Beyond</div>
                      <div className="flex gap-2 mt-1">
                        <span className="text-[7px] text-muted-foreground">Forge</span>
                        <span className="text-[7px] text-muted-foreground">1.20.1</span>
                      </div>
                    </div>
                    <div className="mt-auto rounded-md border border-dashed border-muted-foreground/30 bg-muted/30 p-2 flex items-center justify-center gap-1.5 text-muted-foreground/50 hover:text-primary hover:border-primary/30 transition-colors cursor-pointer">
                      <svg className="size-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
                      <span className="text-[10px]">Add Instance</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ──── Tech Badges ──── */}
      <section className="border-t border-fd-border py-12">
        <div className="mx-auto flex max-w-4xl flex-wrap items-center justify-center gap-x-8 gap-y-4 px-6">
          {/* Cross-platform */}
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Monitor className="size-4" />
            <span className="font-medium">Win / Linux / macOS</span>
          </div>
          {/* Separator */}
          <span className="hidden sm:block size-1 rounded-full bg-border" />
          {/* Open source */}
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <GitHubIcon className="size-4" />
            <span className="font-medium">{d.badgeOpenSource}</span>
          </div>
          {/* Separator */}
          <span className="hidden sm:block size-1 rounded-full bg-border" />
          {/* Loaders */}
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Zap className="size-4" />
            <span className="font-medium">Fabric · Forge · NeoForge · Quilt</span>
          </div>
        </div>
      </section>

      {/* ──── Core Value Prop ──── */}
      <section className="py-20 border-t border-fd-border">
        <div className="mx-auto grid max-w-5xl items-center gap-12 px-6 md:grid-cols-2">
          <div>
            <h2 className="text-3xl md:text-4xl font-bold tracking-tight text-foreground leading-tight">
              {d.coreTitle}
            </h2>
            <p className="mt-6 text-base leading-relaxed text-muted-foreground">
              {d.coreDesc}
            </p>
          </div>
          <div className="rounded-2xl border border-border bg-card overflow-hidden shadow-sm">
            <div className="flex items-center gap-2 border-b border-border bg-muted px-4 py-2.5">
              <span className="text-xs font-medium text-muted-foreground">{d.coreJsonLabel}</span>
            </div>
            <pre className="p-5 text-[13px] leading-relaxed text-foreground overflow-x-auto">
              <code>{d.coreJson}</code>
            </pre>
          </div>
        </div>
      </section>

      {/* ──── Features Grid ──── */}
      <section className="py-20 border-t border-fd-border">
        <div className="mx-auto max-w-5xl px-6">
          <h2 className="text-center text-3xl font-bold text-foreground">{d.featuresTitle}</h2>
          <div className="mt-14 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {d.features.map((f, i) => {
              const Icon = featureIcons[i];
              return (
                <div
                  key={f.title}
                  className="group rounded-2xl border border-border bg-card p-7 transition-all duration-200 hover:border-primary/40 hover:shadow-lg hover:shadow-primary/5 hover:-translate-y-0.5"
                >
                  <div className="flex size-10 items-center justify-center rounded-xl bg-primary/10 text-primary transition-colors duration-200 group-hover:bg-primary group-hover:text-primary-foreground">
                    <Icon className="size-5" />
                  </div>
                  <h3 className="mt-4 text-base font-semibold text-foreground">{f.title}</h3>
                  <p className="mt-2 text-sm leading-relaxed text-muted-foreground">{f.desc}</p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* ──── MCP Highlight ──── */}
      <section className="py-20 border-t border-fd-border">
        <div className="mx-auto grid max-w-5xl items-center gap-12 px-6 md:grid-cols-2">
          {/* Terminal placeholder — replace with asciinema/VHS embed later */}
          <div className="terminal-block">
            <div className="terminal-titlebar">
              <span className="window-dot" style={{ background: '#ff5f57' }} />
              <span className="window-dot" style={{ background: '#febc2e' }} />
              <span className="window-dot" style={{ background: '#28c840' }} />
              <span className="ml-3 text-xs font-medium" style={{ color: 'oklch(0.556 0 0)' }}>trident — MCP server</span>
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
              <div className="mt-2" style={{ color: 'oklch(0.7 0.15 145)' }}>
                ✓ Instance "my-pack" deployed and launched.
              </div>
              <div className="mt-3">
                <span className="terminal-prompt">$ </span>
                <span className="terminal-cmd">trident</span>{' '}
                <span className="terminal-flag">--mcp</span>
              </div>
              <div style={{ color: 'oklch(0.7 0.12 260)' }}>
                MCP server listening on stdio... (30+ tools registered)
              </div>

            </div>
          </div>

          <div>
            <span className="inline-block rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary">
              {d.mcpLabel}
            </span>
            <h2 className="mt-4 text-3xl md:text-4xl font-bold tracking-tight text-foreground leading-tight">
              {d.mcpTitle}
            </h2>
            <p className="mt-6 text-base leading-relaxed text-muted-foreground">
              {d.mcpDesc}
            </p>
            <div className="mt-8">
              <LinkButton href={`/${lang}/docs/cli`} variant="outline" className="rounded-full">
                  {d.mcpCta}
                  <ArrowRight className="size-4" />
              </LinkButton>
            </div>
          </div>
        </div>
      </section>

      {/* ──── Quick Start ──── */}
      <section className="py-20 border-t border-fd-border">
        <div className="mx-auto max-w-4xl px-6">
          <h2 className="text-center text-3xl font-bold text-foreground">{d.quickTitle}</h2>
          <div className="mt-14 grid gap-10 md:grid-cols-3">
            {d.quickSteps.map((step) => (
              <div key={step.step} className="flex flex-col items-center text-center">
                {/* Screenshot placeholder */}
                <div className="mb-6 w-full overflow-hidden rounded-xl border border-border bg-muted">
                  <img
                    src={`https://placehold.co/400x240/f5f5f5/a3a3a3?text=Step+${step.step}`}
                    alt={step.title}
                    className="w-full object-cover"
                    loading="lazy"
                  />
                </div>
                <span className="text-xs font-bold uppercase tracking-widest text-primary">{step.step}</span>
                <h3 className="mt-2 text-lg font-semibold text-foreground">{step.title}</h3>
                <p className="mt-2 text-sm text-muted-foreground leading-relaxed">{step.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ──── FAQ ──── */}
      <section className="py-20 border-t border-fd-border">
        <div className="mx-auto max-w-3xl px-6">
          <h2 className="text-center text-3xl font-bold text-foreground">{d.faqTitle}</h2>
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
      <section className="py-20 border-t border-fd-border">
        <div className="mx-auto max-w-4xl px-6 text-center">
          <h2 className="text-3xl font-bold text-foreground">{d.ctaTitle}</h2>
          <p className="mx-auto mt-4 max-w-xl text-muted-foreground">{d.ctaSub}</p>
          <div className="mt-8 flex flex-wrap items-center justify-center gap-4">
            <LinkButton href={DOWNLOAD_URL} size="lg" className="rounded-full px-7 text-sm font-semibold shadow-lg">
                <Download className="size-4" />
                {d.ctaDownload}
            </LinkButton>
            <LinkButton href={`/${lang}/docs/cli`} variant="outline" size="lg" className="rounded-full px-7 text-sm font-semibold">
                <Terminal className="size-4" />
                {d.ctaCli}
            </LinkButton>
          </div>
        </div>
      </section>

      {/* ──── Footer ──── */}
      <footer className="border-t border-fd-border py-10">
        <div className="mx-auto flex max-w-5xl flex-col items-center gap-4 px-6 sm:flex-row sm:justify-between">
          <div className="flex items-center gap-6 text-sm text-muted-foreground">
            <Link href={DOWNLOAD_URL} className="hover:text-foreground transition">
              {d.ctaDownload}
            </Link>
            <Link href={`/${lang}/docs`} className="hover:text-foreground transition">
              {d.footerDocs}
            </Link>
            <span>{d.footerLicense}</span>
          </div>
          <p className="text-sm text-muted-foreground">{d.footerMadeWith}</p>
        </div>
      </footer>
    </div>
  );
}
