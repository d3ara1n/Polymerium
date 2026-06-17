'use client';

import { liteClient } from 'algoliasearch/lite';
import {
  SearchDialog,
  SearchDialogClose,
  SearchDialogContent,
  SearchDialogFooter,
  SearchDialogHeader,
  SearchDialogIcon,
  SearchDialogInput,
  SearchDialogList,
  SearchDialogOverlay,
  type SharedProps,
} from 'fumadocs-ui/components/dialog/search';
import { useAlgoliaSearch } from '@/lib/algolia-search';
import { useI18n } from 'fumadocs-ui/contexts/i18n';

// These are inlined at build time. If missing, the Provider will not render
// this component — see src/app/provider.tsx for the gate.
const appId = process.env.NEXT_PUBLIC_ALGOLIA_APP_ID!;
const apiKey = process.env.NEXT_PUBLIC_ALGOLIA_API_KEY!;
const client = liteClient(appId, apiKey);

export default function CustomSearchDialog(props: SharedProps) {
  const { locale } = useI18n();
  const { search, setSearch, query } = useAlgoliaSearch({
    client,
    indexName: process.env.NEXT_PUBLIC_ALGOLIA_INDEX_NAME || 'document',
    locale,
  });

  return (
    <SearchDialog search={search} onSearchChange={setSearch} isLoading={query.isLoading} {...props}>
      <SearchDialogOverlay />
      <SearchDialogContent>
        <SearchDialogHeader>
          <SearchDialogIcon />
          <SearchDialogInput />
          <SearchDialogClose />
        </SearchDialogHeader>
        <SearchDialogList items={query.data !== 'empty' ? query.data : null} />
        <SearchDialogFooter>
          <a
            href="https://algolia.com"
            rel="noreferrer noopener"
            className="ms-auto text-xs text-fd-muted-foreground"
          >
            Search powered by Algolia
          </a>
        </SearchDialogFooter>
      </SearchDialogContent>
    </SearchDialog>
  );
}
