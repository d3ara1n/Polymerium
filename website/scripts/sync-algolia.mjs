import { algoliasearch } from 'algoliasearch';
import { sync } from 'fumadocs-core/search/algolia';
import fs from 'node:fs';

const appId = process.env.NEXT_PUBLIC_ALGOLIA_APP_ID;
const apiKey = process.env.ALGOLIA_ADMIN_API_KEY;

if (!appId || !apiKey) {
  console.error('Missing ALGOLIA environment variables');
  process.exit(1);
}

const filePath = '.next/server/app/static.json.body';

if (!fs.existsSync(filePath)) {
  console.error(`File not found: ${filePath}. Run "npm run build" first.`);
  process.exit(1);
}

const content = fs.readFileSync(filePath, 'utf-8');
const records = JSON.parse(content);

const client = algoliasearch(appId, apiKey);

await sync(client, {
  indexName: process.env.NEXT_PUBLIC_ALGOLIA_INDEX_NAME || 'document',
  documents: records,
});

console.log(`Algolia search index updated: ${records.length} records`);
