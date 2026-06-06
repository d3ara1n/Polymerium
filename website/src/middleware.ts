import { createI18nMiddleware } from 'fumadocs-core/i18n/middleware';

export const middleware = createI18nMiddleware({
  defaultLanguage: 'en',
  languages: ['en', 'zh'],
  // Hide locale prefix for English (default), show /zh for Chinese
  hideLocale: 'default-locale',
});

export const config = {
  // Match all pathnames except Next.js internals and API routes
  matcher: ['/((?!api|_next|_vercel|.*\\..*).*)'],
};
