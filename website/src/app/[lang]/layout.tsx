import { Provider } from '@/app/provider';
import { i18nProvider } from 'fumadocs-ui/i18n';
import { Inter } from 'next/font/google';
import { translations } from '@/lib/layout.shared';
import { i18n } from '@/lib/i18n';
import type { Metadata } from 'next';
import { Analytics } from '@vercel/analytics/react';
import { SpeedInsights } from '@vercel/speed-insights/react';
import '../global.css';

const inter = Inter({
  subsets: ['latin'],
});

export const metadata: Metadata = {
  metadataBase: new URL('https://polymerium.dearain.dev'),
  title: {
    default: 'Polymerium — Minecraft Instance Manager',
    template: '%s | Polymerium',
  },
  description:
    'Metadata-driven Minecraft instance manager with zero-duplication storage, cross-platform symlinks, snapshots, CLI, and MCP AI Agent mode.',
  keywords: [
    'Minecraft',
    'launcher',
    'instance manager',
    'symlink',
    'deduplication',
    'modpack',
    'CurseForge',
    'Modrinth',
  ],
  icons: {
    icon: '/favicon.ico',
    apple: '/favicon.png',
  },
  openGraph: {
    type: 'website',
    locale: 'en_US',
    siteName: 'Polymerium',
  },
};

export function generateStaticParams() {
  return i18n.languages.map((lang) => ({ lang }));
}

export default async function Layout({ params, children }: LayoutProps<'/[lang]'>) {
  const { lang } = await params;
  return (
    <html lang={lang} className={inter.className} suppressHydrationWarning>
      <body className="flex flex-col min-h-screen">
        <Provider i18n={i18nProvider(translations, lang)}>{children}</Provider>
        <Analytics />
        <SpeedInsights />
      </body>
    </html>
  );
}
