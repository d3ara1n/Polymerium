import { source, i18n } from '@/lib/source';
import { DocsLayout } from 'fumadocs-ui/layouts/docs';
import { baseOptions } from '@/lib/layout.shared';

export default function Layout({ children }: LayoutProps<'/docs'>) {
  return (
    <DocsLayout
      tree={source.pageTree['en']}
      {...baseOptions()}
      i18n={{
        defaultLanguage: 'en',
        languages: ['en', 'zh'],
      }}
    >
      {children}
    </DocsLayout>
  );
}
