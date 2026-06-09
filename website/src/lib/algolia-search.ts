'use client';

import type { Hit, LiteClient } from 'algoliasearch/lite';
import type { BaseIndex } from 'fumadocs-core/search/algolia';
import {
  createContentHighlighter,
  type SortedResult,
} from 'fumadocs-core/search';
import { useEffect, useRef, useState } from 'react';

// ---------------------------------------------------------------------------
// Debounce
// ---------------------------------------------------------------------------

function useDebounce<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    if (delayMs === 0) {
      setDebounced(value);
      return;
    }
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);

  return delayMs === 0 ? value : debounced;
}

// ---------------------------------------------------------------------------
// groupResults – same logic as fumadocs-core but exported so we can use it
// outside the (buggy) algoliaClient wrapper.
// ---------------------------------------------------------------------------

export function groupResults(hits: Hit<BaseIndex>[]): SortedResult[] {
  const grouped: SortedResult[] = [];
  const scannedUrls = new Set<string>();

  for (const hit of hits) {
    if (!scannedUrls.has(hit.url)) {
      scannedUrls.add(hit.url);
      grouped.push({
        id: hit.url,
        type: 'page',
        breadcrumbs: hit.breadcrumbs,
        url: hit.url,
        content: hit.title,
      });
    }

    grouped.push({
      id: hit.objectID,
      type: hit.content === hit.section ? 'heading' : 'text',
      url: hit.section_id ? `${hit.url}#${hit.section_id}` : hit.url,
      content: hit.content,
    });
  }

  return grouped;
}

// ---------------------------------------------------------------------------
// useAlgoliaSearch – drop-in replacement for useDocsSearch({ type: 'algolia' })
// that returns ALL result types (page, heading, text) instead of only page.
// ---------------------------------------------------------------------------

export interface UseAlgoliaSearchOptions {
  client: LiteClient;
  indexName: string;
  /** Current locale, used as a `tag` facet filter (e.g. `'en'`, `'zh'`). */
  locale?: string;
  /** Debounce delay in ms. Default 300. */
  delayMs?: number;
}

export function useAlgoliaSearch(options: UseAlgoliaSearchOptions) {
  const { client, indexName, locale, delayMs = 300 } = options;

  const [search, setSearch] = useState('');
  const [results, setResults] = useState<SortedResult[] | 'empty'>('empty');
  const [isLoading, setIsLoading] = useState(false);

  const debouncedSearch = useDebounce(search, delayMs);

  // Track the latest query to avoid stale results
  const latestIdRef = useRef(0);

  useEffect(() => {
    const query = debouncedSearch.trim();
    if (query.length === 0) {
      setResults('empty');
      setIsLoading(false);
      return;
    }

    setIsLoading(true);

    const currentId = ++latestIdRef.current;
    let cancelled = false;

    async function run() {
      try {
        const result = await client.searchForHits<BaseIndex>({
          requests: [
            {
              type: 'default',
              indexName,
              query,
              distinct: 5,
              hitsPerPage: 10,
              filters: locale ? `tag:${locale}` : undefined,
            },
          ],
        });

        if (cancelled || currentId !== latestIdRef.current) return;

        const hits = result.results[0].hits;
        const highlighter = createContentHighlighter(query);

        // groupResults + highlight for EVERY type (the fix)
        const grouped = groupResults(hits).map((hit) => ({
          ...hit,
          content: highlighter.highlightMarkdown(hit.content as string),
        }));

        setResults(grouped);
      } catch (err) {
        if (!cancelled && currentId === latestIdRef.current) {
          console.error('Algolia search error:', err);
          setResults('empty');
        }
      } finally {
        if (!cancelled && currentId === latestIdRef.current) {
          setIsLoading(false);
        }
      }
    }

    run();

    return () => {
      cancelled = true;
    };
  }, [debouncedSearch, client, indexName, locale]);

  return { search, setSearch, query: { isLoading, data: results } };
}
