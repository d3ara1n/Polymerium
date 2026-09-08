// Ping IndexNow with every sitemap URL so participating engines (Bing, Yandex,
// Naver, ...) recrawl the site immediately. Run after the deployment is live —
// the key file must be reachable at BASE/<KEY>.txt for validation.

const BASE = 'https://polymerium.dearain.dev';
const KEY = '7dbc28eaf0387607124ab7e642b7088d';

const res = await fetch(`${BASE}/sitemap.xml`);
if (!res.ok) {
  console.error(`Failed to fetch sitemap: ${res.status} ${res.statusText}`);
  process.exit(1);
}

const xml = await res.text();
const urls = [...xml.matchAll(/<loc>([^<]+)<\/loc>/g)].map((m) => m[1]);

if (urls.length === 0) {
  console.error('Sitemap contains no URLs');
  process.exit(1);
}

const ping = await fetch('https://api.indexnow.org/indexnow', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json; charset=utf-8' },
  body: JSON.stringify({
    host: new URL(BASE).host,
    key: KEY,
    keyLocation: `${BASE}/${KEY}.txt`,
    urlList: urls,
  }),
});

// 200 = key validated and accepted, 202 = accepted, key validated later.
if (ping.status === 200 || ping.status === 202) {
  console.log(`Submitted ${urls.length} URLs, IndexNow responded ${ping.status}`);
} else {
  console.error(`IndexNow rejected the submission: ${ping.status} ${ping.statusText}`);
  console.error(await ping.text());
  process.exit(1);
}
