'use client';

import { RootProvider } from 'fumadocs-ui/provider/next';
import SearchDialog from '@/components/search-dialog';
import type { ReactNode } from 'react';

export function Provider({
  children,
  i18n,
}: {
  children: ReactNode;
  i18n: React.ComponentProps<typeof RootProvider>['i18n'];
}) {
  const hasAlgolia =
    process.env.NEXT_PUBLIC_ALGOLIA_APP_ID &&
    process.env.NEXT_PUBLIC_ALGOLIA_API_KEY;

  return (
    <RootProvider
      i18n={i18n}
      search={
        hasAlgolia
          ? { SearchDialog }
          : undefined
      }
    >
      {children}
    </RootProvider>
  );
}
