import { RootProvider } from 'fumadocs-ui/provider/next';
import type { Metadata } from 'next';
import { appName, appDescription } from '@/lib/shared';
import './global.css';

export const metadata: Metadata = {
  title: {
    template: `%s | ${appName}`,
    default: appName,
  },
  description: appDescription,
  openGraph: {
    title: appName,
    description: appDescription,
  },
};

export default function Layout({ children }: LayoutProps<'/'>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className="flex flex-col min-h-screen font-sans antialiased">
        <RootProvider>{children}</RootProvider>
      </body>
    </html>
  );
}
