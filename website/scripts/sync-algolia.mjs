import { algoliasearch } from 'algoliasearch';
import { sync } from 'fumadocs-core/search/algolia';
import { register } from 'fumadocs-mdx/node';
import { parseArgs } from 'node:util';

const { values } = parseArgs({
  options: {
    'dry-run': { type: 'boolean', default: false },
  },
});
const dryRun = values['dry-run'];
const appId = process.env.NEXT_PUBLIC_ALGOLIA_APP_ID;
const apiKey = process.env.ALGOLIA_ADMIN_API_KEY;

if (!dryRun && (!appId || !apiKey)) {
  console.error('Missing ALGOLIA environment variables');
  process.exit(1);
}

register();

// NOTE: Keep this import dynamic so the MDX loader is registered before the document source loads.
const { exportAlgoliaIndex } = await import('../src/lib/export-algolia-index.ts');
const records = await exportAlgoliaIndex();

if (records.length === 0) {
  throw new Error('Refusing to replace the Algolia search index with an empty document source');
}

if (dryRun) {
  console.log(`Algolia search index generated: ${records.length} records without uploading`);
} else {
  const client = algoliasearch(appId, apiKey);

  await sync(client, {
    indexName: process.env.NEXT_PUBLIC_ALGOLIA_INDEX_NAME || 'document',
    documents: records,
  });

  console.log(`Algolia search index updated: ${records.length} records`);
}
