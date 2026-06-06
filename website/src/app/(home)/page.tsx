import Link from 'next/link';

const features = [
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
];

export default function HomePage() {
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
            href="https://github.com/d3ara1n/Polymerium/releases"
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
            {features.map((feature) => (
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
