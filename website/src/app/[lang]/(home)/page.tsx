import Link from 'next/link';
import {
  HardDrive,
  Layers,
  Camera,
  FileJson,
  Terminal,
  ShieldCheck,
  ArrowRight,
  Download,
  ExternalLink,
  Zap,
  RefreshCw,
} from 'lucide-react';

function GitHubIcon(props: React.ComponentProps<'svg'>) {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" {...props}>
      <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0024 12c0-6.63-5.37-12-12-12z" />
    </svg>
  );
}

const GITHUB_URL = 'https://github.com/d3ara1n/Polymerium';
const DOWNLOAD_URL = `${GITHUB_URL}/releases`;

function useDict(lang: string) {
  if (lang === 'zh') {
    return {
      heroTagline: '每个模组只存一份的 Minecraft 启动器。',
      heroSub: '基于元数据驱动的 Minecraft 实例管理器——零重复存储、跨平台符号链接、快照系统、内置 CLI 与 MCP AI Agent 模式。',
      ctaDownload: '下载',
      ctaCli: '命令行工具',
      ctaGitHub: 'GitHub',
      painTitle: '磁盘空间的真相',
      painDesc: '大多数启动器为每个实例完整复制所有模组文件。5 个整合包？你的磁盘上就有 5 份 Sodium、5 份 Iris、5 份 Fabric API。Polymerium 改变了这一切。',
      painBefore: '传统方式',
      painAfter: 'Polymerium',
      painTotal: '合计',
      painSymlinks: '符号链接',
      howTitle: '工作原理',
      howStep1Title: '描述',
      howStep1Desc: '你的游戏配置只是一个轻量的元数据文件——模组列表、版本号、加载器类型。',
      howStep2Title: '部署',
      howStep2Desc: 'Polymerium 从共享缓存中构建实例——使用符号链接，不复制文件。',
      howStep3Title: '游戏',
      howStep3Desc: '启动游戏。想切换整合包？秒切，无需等待文件复制。',
      featuresTitle: '为高效而生',
      featuresDesc: '每一个功能都围绕同一个目标：让你以更少的磁盘占用做更多的事。',
      cliTitle: '命令行 + AI Agent 模式',
      cliDesc: 'Polymerium 内置 trident 命令行工具，提供 30+ 命令覆盖全流程。启动 MCP 服务器模式，让 AI Agent 直接管理你的 Minecraft 实例。',
      cliCta: '了解更多',
      footerDownload: '下载',
      footerDocs: '文档',
      footerLicense: 'MIT 开源协议',
      footerMadeWith: '用 ❤️ 制作',
      metaTitle: 'Polymerium — Minecraft 实例管理器',
      metaDesc: '基于元数据驱动的 Minecraft 实例管理器。零重复存储、快照、CLI、MCP AI Agent 模式。',
    };
  }
  return {
    heroTagline: 'The Minecraft Launcher That Stores Each Mod Exactly Once',
    heroSub:
      'Metadata-driven Minecraft instance manager with zero-duplication storage, cross-platform symlinks, snapshots, and a built-in CLI with MCP mode for AI agents.',
    ctaDownload: 'Download',
    ctaCli: 'CLI Tool',
    ctaGitHub: 'GitHub',
    painTitle: 'The Truth About Disk Space',
    painDesc: 'Most launchers copy every mod into every instance. 5 modpacks? That\u2019s 5 copies of Sodium, 5 copies of Iris, 5 copies of Fabric API sitting on your drive. Polymerium changes that.',
    painBefore: 'Traditional',
    painAfter: 'Polymerium',
    painTotal: 'Total',
    painSymlinks: 'symlinks',
    howTitle: 'How It Works',
    howStep1Title: 'Describe',
    howStep1Desc: 'Your game setup is a lightweight metadata file\u2014mod list, version, loader type.',
    howStep2Title: 'Deploy',
    howStep2Desc: 'Polymerium builds instances from a shared cache using symlinks. No file copying.',
    howStep3Title: 'Play',
    howStep3Desc: 'Launch the game. Want to switch modpacks? Instant\u2014no waiting for file copies.',
    featuresTitle: 'Built for Efficiency',
    featuresDesc: 'Every feature revolves around one goal: do more with less disk space.',
    cliTitle: 'CLI + AI Agent Mode',
    cliDesc:
      'Polymerium ships with trident, a 30+ command CLI covering the full workflow. Enable MCP server mode to let AI agents manage your Minecraft instances directly.',
    cliCta: 'Learn More',
    footerDownload: 'Download',
    footerDocs: 'Docs',
    footerLicense: 'MIT License',
    footerMadeWith: 'Made with \u2764\uFE0F',
    metaTitle: 'Polymerium \u2014 Minecraft Instance Manager',
    metaDesc: 'Metadata-driven Minecraft instance manager. Zero-duplication storage, snapshots, CLI, MCP AI Agent mode.',
  };
}

const features = [
  {
    icon: HardDrive,
    titleEn: 'Zero Duplication',
    titleZh: '零重复存储',
    descEn: 'Each mod file stored once, symlinked everywhere. Save 60\u201380% disk space.',
    descZh: '每个模组文件只存一份，通过符号链接共享。节省 60\u201380% 磁盘空间。',
  },
  {
    icon: RefreshCw,
    titleEn: 'Instant Switching',
    titleZh: '秒级切换',
    descEn: 'Change modpacks in seconds, not minutes. No file copying needed.',
    descZh: '秒级切换不同整合包，无需等待文件复制。',
  },
  {
    icon: Camera,
    titleEn: 'Snapshots',
    titleZh: '快照系统',
    descEn: 'Save, restore, and diff entire game states. Experiment safely.',
    descZh: '保存、恢复和对比完整游戏状态。安全地尝试任何改动。',
  },
  {
    icon: FileJson,
    titleEn: 'Git-Friendly Modpacks',
    titleZh: 'Git 友好的整合包',
    descEn: 'Instance = one JSON file. Version control your modpack development.',
    descZh: '实例 = 一个 JSON 文件。用 Git 管理你的整合包开发。',
  },
  {
    icon: Terminal,
    titleEn: 'CLI + MCP Mode',
    titleZh: '命令行 + MCP 模式',
    descEn: '30+ commands for automation. Let AI agents manage your instances.',
    descZh: '30+ 命令自动化全流程。让 AI Agent 直接管理你的实例。',
  },
  {
    icon: ShieldCheck,
    titleEn: 'Privacy First',
    titleZh: '隐私优先',
    descEn: 'No ads, no telemetry, no data collection. Open source (MIT).',
    descZh: '无广告、无遥测、无数据收集。MIT 开源。',
  },
];

export default async function HomePage(props: PageProps<'/[lang]'>) {
  const params = await props.params;
  const lang = params.lang;
  const d = useDict(lang);

  return (
    <div className="flex flex-col flex-1">
      <section className="relative overflow-hidden pt-24 pb-20 md:pt-36 md:pb-28">
        <div aria-hidden className="pointer-events-none absolute inset-0 hero-grid" />
        <div aria-hidden className="pointer-events-none absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 hero-glow" />
        <div className="relative mx-auto max-w-4xl px-6 text-center">
          <h1 className="text-4xl sm:text-5xl md:text-6xl font-extrabold tracking-tight text-fd-foreground leading-tight">
            {d.heroTagline}
          </h1>
          <p className="mx-auto mt-6 max-w-2xl text-lg text-fd-muted-foreground leading-relaxed">
            {d.heroSub}
          </p>
          <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
            <Link
              href={DOWNLOAD_URL}
              className="inline-flex items-center gap-2 rounded-full bg-fd-primary px-7 py-3 text-sm font-semibold text-fd-primary-foreground shadow-lg shadow-fd-primary/25 transition hover:brightness-110"
            >
              <Download className="size-4" />
              {d.ctaDownload}
            </Link>
            <Link
              href={`/${lang}/docs/cli`}
              className="inline-flex items-center gap-2 rounded-full border border-fd-border px-7 py-3 text-sm font-semibold text-fd-foreground transition hover:bg-fd-muted"
            >
              <Terminal className="size-4" />
              {d.ctaCli}
            </Link>
            <Link
              href={GITHUB_URL}
              className="inline-flex items-center gap-2 rounded-full border border-fd-border px-7 py-3 text-sm font-semibold text-fd-foreground transition hover:bg-fd-muted"
            >
              <GitHubIcon className="size-4" />
              {d.ctaGitHub}
            </Link>
          </div>
        </div>
      </section>

      <section className="py-20 border-t border-fd-border">
        <div className="mx-auto max-w-5xl px-6">
          <h2 className="text-center text-3xl font-bold text-fd-foreground">{d.painTitle}</h2>
          <p className="mx-auto mt-4 max-w-2xl text-center text-fd-muted-foreground">{d.painDesc}</p>
          <div className="mt-12 grid gap-6 md:grid-cols-2">
            <div className="rounded-2xl border border-fd-border bg-fd-card p-8">
              <div className="mb-4 flex items-center gap-2">
                <span className="inline-block size-3 rounded-full bg-red-500" />
                <span className="text-sm font-semibold uppercase tracking-wider text-fd-muted-foreground">{d.painBefore}</span>
              </div>
              <ul className="space-y-2 text-sm text-fd-foreground">
                <li className="flex items-center justify-between rounded-lg bg-fd-muted/50 px-4 py-2">
                  <span>Instance A</span><span className="font-mono">2.3 GB</span>
                </li>
                <li className="flex items-center justify-between rounded-lg bg-fd-muted/50 px-4 py-2">
                  <span>Instance B</span><span className="font-mono">2.1 GB</span>
                </li>
                <li className="flex items-center justify-between rounded-lg bg-fd-muted/50 px-4 py-2">
                  <span>Instance C</span><span className="font-mono">2.4 GB</span>
                </li>
                <li className="mt-3 flex items-center justify-between border-t border-fd-border pt-3 text-base font-bold">
                  <span>{d.painTotal}</span><span className="font-mono">6.8 GB</span>
                </li>
              </ul>
              <p className="mt-4 text-xs text-fd-muted-foreground">3x Sodium, 3x Iris, 3x Fabric API...</p>
            </div>
            <div className="rounded-2xl border border-fd-primary/30 bg-fd-card p-8 shadow-lg shadow-fd-primary/5">
              <div className="mb-4 flex items-center gap-2">
                <span className="inline-block size-3 rounded-full bg-fd-primary" />
                <span className="text-sm font-semibold uppercase tracking-wider text-fd-primary">{d.painAfter}</span>
              </div>
              <ul className="space-y-2 text-sm text-fd-foreground">
                <li className="flex items-center justify-between rounded-lg bg-fd-muted/50 px-4 py-2">
                  <span>Shared Cache</span><span className="font-mono">2.3 GB</span>
                </li>
                <li className="flex items-center justify-between rounded-lg bg-fd-muted/50 px-4 py-2">
                  <span>Instance A</span><span className="font-mono text-fd-muted-foreground">{d.painSymlinks}</span>
                </li>
                <li className="flex items-center justify-between rounded-lg bg-fd-muted/50 px-4 py-2">
                  <span>Instance B</span><span className="font-mono text-fd-muted-foreground">{d.painSymlinks}</span>
                </li>
                <li className="flex items-center justify-between rounded-lg bg-fd-muted/50 px-4 py-2">
                  <span>Instance C</span><span className="font-mono text-fd-muted-foreground">{d.painSymlinks}</span>
                </li>
                <li className="mt-3 flex items-center justify-between border-t border-fd-primary/30 pt-3 text-base font-bold text-fd-primary">
                  <span>{d.painTotal}</span><span className="font-mono">2.3 GB</span>
                </li>
              </ul>
              <p className="mt-4 text-xs text-fd-muted-foreground">1x Sodium, 1x Iris, 1x Fabric API</p>
            </div>
          </div>
        </div>
      </section>

      <section className="py-20">
        <div className="mx-auto max-w-4xl px-6">
          <h2 className="text-center text-3xl font-bold text-fd-foreground">{d.howTitle}</h2>
          <div className="mt-14 grid gap-10 md:grid-cols-3">
            {[
              { num: '01', icon: FileJson, title: d.howStep1Title, desc: d.howStep1Desc },
              { num: '02', icon: Layers, title: d.howStep2Title, desc: d.howStep2Desc },
              { num: '03', icon: Zap, title: d.howStep3Title, desc: d.howStep3Desc },
            ].map((step) => (
              <div key={step.num} className="flex flex-col items-center text-center">
                <div className="flex size-14 items-center justify-center rounded-2xl bg-fd-primary/10 text-fd-primary">
                  <step.icon className="size-6" />
                </div>
                <span className="mt-4 text-xs font-bold uppercase tracking-widest text-fd-primary">{step.num}</span>
                <h3 className="mt-2 text-lg font-semibold text-fd-foreground">{step.title}</h3>
                <p className="mt-2 text-sm text-fd-muted-foreground leading-relaxed">{step.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-20 border-t border-fd-border">
        <div className="mx-auto max-w-5xl px-6">
          <h2 className="text-center text-3xl font-bold text-fd-foreground">{d.featuresTitle}</h2>
          <p className="mx-auto mt-4 max-w-2xl text-center text-fd-muted-foreground">{d.featuresDesc}</p>
          <div className="mt-14 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {features.map((f) => {
              const title = lang === 'zh' ? f.titleZh : f.titleEn;
              const desc = lang === 'zh' ? f.descZh : f.descEn;
              return (
                <div
                  key={title}
                  className="group rounded-2xl border border-fd-border bg-fd-card p-7 transition hover:border-fd-primary/40 hover:shadow-lg hover:shadow-fd-primary/5"
                >
                  <div className="flex size-11 items-center justify-center rounded-xl bg-fd-primary/10 text-fd-primary transition group-hover:bg-fd-primary group-hover:text-fd-primary-foreground">
                    <f.icon className="size-5" />
                  </div>
                  <h3 className="mt-4 text-base font-semibold text-fd-foreground">{title}</h3>
                  <p className="mt-2 text-sm leading-relaxed text-fd-muted-foreground">{desc}</p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      <section className="py-20 border-t border-fd-border">
        <div className="mx-auto max-w-4xl px-6">
          <h2 className="text-center text-3xl font-bold text-fd-foreground">{d.cliTitle}</h2>
          <p className="mx-auto mt-4 max-w-2xl text-center text-fd-muted-foreground">{d.cliDesc}</p>
          <div className="mt-10 rounded-2xl border border-fd-border bg-fd-card p-6 overflow-x-auto">
            <pre className="text-sm leading-relaxed text-fd-foreground font-mono">
              <code>{`trident instance create my-pack --version 1.21.4 --loader fabric
trident package add modrinth:sodium
trident package add modrinth:iris
trident instance build my-pack
trident instance run my-pack`}</code>
            </pre>
          </div>
          <div className="mt-6 flex justify-center">
            <Link
              href={`/${lang}/docs/cli`}
              className="inline-flex items-center gap-2 text-sm font-medium text-fd-primary transition hover:underline"
            >
              {d.cliCta}
              <ArrowRight className="size-4" />
            </Link>
          </div>
        </div>
      </section>

      <section className="py-20 border-t border-fd-border">
        <div className="mx-auto max-w-4xl px-6 text-center">
          <h2 className="text-3xl font-bold text-fd-foreground">
            {lang === 'zh' ? '准备好节省磁盘空间了吗？' : 'Ready to save disk space?'}
          </h2>
          <p className="mx-auto mt-4 max-w-xl text-fd-muted-foreground">
            {lang === 'zh'
              ? '下载 Polymerium，体验元数据驱动的实例管理。'
              : 'Download Polymerium and experience metadata-driven instance management.'}
          </p>
          <div className="mt-8 flex flex-wrap items-center justify-center gap-4">
            <Link
              href={DOWNLOAD_URL}
              className="inline-flex items-center gap-2 rounded-full bg-fd-primary px-7 py-3 text-sm font-semibold text-fd-primary-foreground shadow-lg shadow-fd-primary/25 transition hover:brightness-110"
            >
              <Download className="size-4" />
              {d.ctaDownload}
            </Link>
            <Link
              href={GITHUB_URL}
              className="inline-flex items-center gap-2 rounded-full border border-fd-border px-7 py-3 text-sm font-semibold text-fd-foreground transition hover:bg-fd-muted"
            >
              <GitHubIcon className="size-4" />
              {d.ctaGitHub}
            </Link>
          </div>
        </div>
      </section>

      <footer className="border-t border-fd-border py-10">
        <div className="mx-auto flex max-w-5xl flex-col items-center gap-4 px-6 sm:flex-row sm:justify-between">
          <div className="flex items-center gap-6 text-sm text-fd-muted-foreground">
            <Link href={DOWNLOAD_URL} className="hover:text-fd-foreground transition">
              {d.footerDownload}
            </Link>
            <Link href={`/${lang}/docs`} className="hover:text-fd-foreground transition">
              {d.footerDocs}
            </Link>
            <span>{d.footerLicense}</span>
          </div>
          <p className="text-sm text-fd-muted-foreground">{d.footerMadeWith}</p>
        </div>
      </footer>
    </div>
  );
}
