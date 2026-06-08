import type { MetadataRoute } from 'next';
import { source } from '@/lib/source';

const BASE = 'https://polymerium.dearain.dev';
const LANGS = ['en', 'zh'];

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const entries: MetadataRoute.Sitemap = [];

  // Homepage per locale
  for (const lang of LANGS) {
    entries.push({
      url: `${BASE}/${lang}`,
      lastModified: new Date(),
      changeFrequency: 'weekly',
      priority: 1,
    });
  }

  // Doc pages
  const pages = source.getPages();
  for (const page of pages) {
    entries.push({
      url: `${BASE}${page.url}`,
      lastModified: new Date(),
      changeFrequency: 'weekly',
      priority: 0.8,
    });
  }

  return entries;
}
