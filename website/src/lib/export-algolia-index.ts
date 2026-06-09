import { source } from '@/lib/source';
import type { DocumentRecord } from 'fumadocs-core/search/algolia';
import { i18n } from '@/lib/i18n';

export async function exportAlgoliaIndex(): Promise<DocumentRecord[]> {
  const results: DocumentRecord[] = [];

  for (const page of source.getPages()) {
    results.push({
      _id: page.url,
      structured: page.data.structuredData,
      url: page.url,
      title: page.data.title,
      description: page.data.description,
      // Tag each record with its locale so the search dialog can filter by
      // the current language. Default-locale pages have `locale` undefined;
      // normalise to the configured default.
      tag: page.locale ?? i18n.defaultLanguage,
    });
  }

  return results;
}
