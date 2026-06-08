import { RootProvider } from 'fumadocs-ui/provider/next';
import { i18nProvider } from 'fumadocs-ui/i18n';
import { Inter } from 'next/font/google';
import { translations } from '@/lib/layout.shared';
import type { Metadata } from 'next';
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

export default async function Layout({ params, children }: LayoutProps<'/[lang]'>) {
  const { lang } = await params;
  return (
    <html lang={lang} className={inter.className} suppressHydrationWarning>
      <body className="flex flex-col min-h-screen">
        <RootProvider i18n={i18nProvider(translations, lang)}>{children}</RootProvider>
      </body>
    </html>
  );
}
