import { NextRequest, NextResponse } from 'next/server';
import {
  isMarkdownPreferred,
  rewritePath,
  getNegotiator,
} from 'fumadocs-core/negotiation';
import { docsContentRoute, docsRoute } from '@/lib/shared';
import { i18n } from '@/lib/i18n';

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
  const pathLocale = url.pathname.split('/')[1] || undefined;

  // i18n: redirect to locale-prefixed URL (e.g. / → /en, /docs → /en/docs)
  if (!pathLocale || !(i18n.languages as readonly string[]).includes(pathLocale)) {
    const preferred =
      getNegotiator(request).languages(i18n.languages as string[])[0] ??
      i18n.defaultLanguage;
    const target = new URL(url);
    target.pathname = `/${preferred}${url.pathname}`.replaceAll(/\/+/g, '/');
    return NextResponse.redirect(target);
  }

  // Markdown content negotiation for LLMs
  const suffixResult = rewriteSuffix(request.nextUrl.pathname);
  if (suffixResult) {
    return NextResponse.rewrite(new URL(suffixResult, request.nextUrl));
  }

  if (isMarkdownPreferred(request)) {
    const docsResult = rewriteDocs(request.nextUrl.pathname);
    if (docsResult) {
      return NextResponse.rewrite(new URL(docsResult, request.nextUrl));
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico).*)'],
};
