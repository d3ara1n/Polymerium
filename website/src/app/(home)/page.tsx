import Link from 'next/link';
import { getDownloadUrl } from '@/lib/shared';

const features = {
  en: [
    {
      icon: '💾',
      title: 'Zero Duplication',
      description:
        'Each mod file stored once, symlinked everywhere. Save 60-80% disk space compared to traditional launchers.',
    },
    {
      icon: '⚡',
      title: 'Instant Switching',
      description:
        'Change between completely different modpacks in seconds. No waiting for file copying.',
    },
    {
      icon: '📸',
      title: 'Snapshots & History',
      description:
        'Save, restore, and diff entire game states. Experiment freely — roll back any change.',
    },
    {
      icon: '📦',
      title: 'Git-Friendly Modpacks',
      description:
        'Your entire game setup is a portable JSON file. Version control your modpack development with Git.',
    },
    {
      icon: '🤖',
      title: 'CLI + MCP Mode',
      description:
        '30+ commands for automation. Built-in MCP server mode lets AI agents manage your Minecraft instances.',
    },
    {
      icon: '🔒',
      title: 'Privacy First',
      description:
        'No ads, no telemetry, no data collection. Open source (MIT). Delete two folders to uninstall completely.',
    },
  ],
  zh: [
    {
      icon: '💾',
      title: '零重复',
      description: '每个模组文件只存一份，通过符号链接共享。节省 60-80% 的磁盘空间。',
    },
    {
      icon: '⚡',
      title: '秒级切换',
      description: '在完全不同的整合包之间切换只需几秒，无需等待文件复制。',
    },
    {
      icon: '📸',
      title: '快照与历史',
      description: '保存、恢复、对比完整的游戏状态。放心实验，随时回退。',
    },
    {
      icon: '📦',
      title: 'Git 友好',
      description: '整个游戏配置就是一个可移植的 JSON 文件，可以用 Git 进行版本控制。',
    },
    {
      icon: '🤖',
      title: 'CLI + MCP 模式',
      description: '30+ 命令行工具支持自动化。内置 MCP 服务器模式，让 AI Agent 管理你的 Minecraft 实例。',
    },
    {
      icon: '🔒',
      title: '隐私优先',
      description: '无广告、无遥测、无数据收集。开源（MIT）。删除两个文件夹即可完全卸载。',
    },
  ],
} as const;

export default function HomePage() {
  // Default to English on the home page.
  // The /zh route will serve the Chinese version via middleware.
  const locale = 'en';
  const t = features[locale];

  return (
    <main className="flex-1">
      {/* Hero */}
      <section className="flex flex-col items-center justify-center text-center px-6 pt-32 pb-20">
        <h1 className="text-4xl sm:text-5xl md:text-6xl font-bold tracking-tight max-w-4xl">
          The Minecraft launcher that
          <br />
          <span className="text-fd-primary">
            stores each mod exactly once.
          </span>
        </h1>
        <p className="mt-6 text-lg text-fd-muted-foreground max-w-2xl">
          Prism and MultiMC copy your mods into every instance. Polymerium
          keeps one copy on disk and shares it via smart symlinks.
          Same mods. Half the disk space.
        </p>
        <div className="flex flex-wrap items-center justify-center gap-4 mt-10">
          <Link
            href={getDownloadUrl(locale)}
            className="inline-flex items-center rounded-full bg-fd-primary text-fd-primary-foreground px-6 py-3 text-sm font-medium hover:opacity-90 transition-opacity"
          >
            Download for your platform
          </Link>
          <Link
            href="/docs"
            className="inline-flex items-center rounded-full border border-fd-border px-6 py-3 text-sm font-medium hover:bg-fd-accent hover:text-fd-accent-foreground transition-colors"
          >
            Get Started →
          </Link>
        </div>
      </section>

      {/* Features */}
      <section className="px-6 pb-32">
        <div className="max-w-6xl mx-auto">
          <h2 className="text-2xl font-bold text-center mb-12">
            Why Polymerium?
          </h2>
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {t.map((feature) => (
              <div
                key={feature.title}
                className="rounded-xl border border-fd-border p-6 hover:bg-fd-accent/50 transition-colors"
              >
                <div className="text-2xl mb-3">{feature.icon}</div>
                <h3 className="font-semibold mb-2">{feature.title}</h3>
                <p className="text-sm text-fd-muted-foreground">
                  {feature.description}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>
    </main>
  );
}
