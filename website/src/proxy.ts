import { NextRequest, NextResponse } from 'next/server';
import { isMarkdownPreferred, rewritePath } from 'fumadocs-core/negotiation';
import { docsContentRoute, docsRoute } from '@/lib/shared';

const COOKIE_NAME = 'FD_LOCALE';
const LOCALES = ['en', 'zh'] as const;
const DEFAULT_LOCALE = 'en';

function getLocaleFromCookie(request: NextRequest): string {
  const cookie = request.cookies.get(COOKIE_NAME)?.value;
  if (cookie && LOCALES.includes(cookie as never)) return cookie;
  return DEFAULT_LOCALE;
}

function getLocaleFromPath(url: URL): { locale: string; pathname: string } {
  const parts = url.pathname.split('/').filter(Boolean);
  const first = parts[0];
  if (LOCALES.includes(first as never)) {
    const rest = '/' + parts.slice(1).join('/');
    return { locale: first, pathname: rest || '/' };
  }
  return { locale: DEFAULT_LOCALE, pathname: url.pathname };
}

const { rewrite: rewriteDocs } = rewritePath(
  `${docsRoute}{/*path}`,
  `${docsContentRoute}{/*path}/content.md`,
);
const { rewrite: rewriteSuffix } = rewritePath(
  `${docsRoute}{/*path}.md`,
  `${docsContentRoute}{/*path}/content.md`,
);

export default function proxy(request: NextRequest) {
  const url = request.nextUrl;

  // Step 1: Detect locale from path
  const { locale, pathname } = getLocaleFromPath(url);

  // If a non-default locale is in the path, rewrite internally
  if (locale !== DEFAULT_LOCALE) {
    url.pathname = pathname;
    const response = NextResponse.rewrite(url);
    response.cookies.set(COOKIE_NAME, locale, { path: '/' });
    return response;
  }

  // Step 2: Set cookie for default locale if not set
  const cookie = request.cookies.get(COOKIE_NAME)?.value;
  if (!cookie || cookie !== DEFAULT_LOCALE) {
    const response = NextResponse.next();
    response.cookies.set(COOKIE_NAME, DEFAULT_LOCALE, { path: '/' });
    return response;
  }

  // Step 3: Handle markdown content negotiation
  const suffixResult = rewriteSuffix(url.pathname);
  if (suffixResult) {
    return NextResponse.rewrite(new URL(suffixResult, url));
  }

  if (isMarkdownPreferred(request)) {
    const docsResult = rewriteDocs(url.pathname);
    if (docsResult) {
      return NextResponse.rewrite(new URL(docsResult, url));
    }
  }

  return NextResponse.next();
}
