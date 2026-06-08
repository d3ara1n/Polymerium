import { getPageImage, getPageMarkdownUrl, source } from '@/lib/source';
import {
  DocsBody,
  DocsDescription,
  DocsPage,
  DocsTitle,
  MarkdownCopyButton,
  PageLastUpdate,
  ViewOptionsPopover,
} from 'fumadocs-ui/layouts/docs/page';
import { notFound } from 'next/navigation';
import { getMDXComponents } from '@/components/mdx';
import type { Metadata } from 'next';
import { createRelativeLink } from 'fumadocs-ui/mdx';
import { gitConfig } from '@/lib/shared';
import { getBreadcrumbItems } from 'fumadocs-core/breadcrumb';

export default async function Page(props: PageProps<'/[lang]/docs/[[...slug]]'>) {
  const params = await props.params;
  const page = source.getPage(params.slug, params.lang);
  if (!page) notFound();

  const MDX = page.data.body;
  const markdownUrl = getPageMarkdownUrl(page).url;

  // BreadcrumbList schema
  const pageTree = source.getPageTree(params.lang);
  const breadcrumbItems = getBreadcrumbItems(page.url, pageTree, {
    includePage: true,
  });
  // Add root item manually for better naming
  const allBreadcrumbItems = [
    {
      name: params.lang === 'zh' ? '首页' : 'Home',
      url: `/${params.lang}`,
    },
    ...breadcrumbItems,
  ];
  const breadcrumbSchema = {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: allBreadcrumbItems.map((item, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: typeof item.name === 'string' ? item.name : (item.name as any).name,
      item: `https://polymerium.dearain.dev${item.url}`,
    })),
  };

  return (
    <DocsPage toc={page.data.toc} full={page.data.full}>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(breadcrumbSchema) }}
      />
      <DocsTitle>{page.data.title}</DocsTitle>
      <DocsDescription className="mb-0">{page.data.description}</DocsDescription>
      <div className="flex flex-row gap-2 items-center border-b pb-6">
        <MarkdownCopyButton markdownUrl={markdownUrl} />
        <ViewOptionsPopover
          markdownUrl={markdownUrl}
          githubUrl={`https://github.com/${gitConfig.user}/${gitConfig.repo}/blob/${gitConfig.branch}/content/docs/${page.path}`}
        />
      </div>
      <DocsBody>
        {page.data.lastModified && <PageLastUpdate date={page.data.lastModified} />}
        <MDX
          components={getMDXComponents({
            a: createRelativeLink(source, page),
          })}
        />
      </DocsBody>
    </DocsPage>
  );
}

export async function generateStaticParams() {
  return source.generateParams();
}

export async function generateMetadata(props: PageProps<'/[lang]/docs/[[...slug]]'>): Promise<Metadata> {
  const params = await props.params;
  const page = source.getPage(params.slug, params.lang);
  if (!page) notFound();

  const slugPath = params.slug?.join('/') ?? '';
  const canonicalUrl = `https://polymerium.dearain.dev${page.url}`;

  return {
    title: page.data.title,
    description: page.data.description,
    alternates: {
      canonical: canonicalUrl,
      languages: {
        en: `https://polymerium.dearain.dev/en/docs/${slugPath}`,
        zh: `https://polymerium.dearain.dev/zh/docs/${slugPath}`,
        'x-default': `https://polymerium.dearain.dev/en/docs/${slugPath}`,
      },
    },
    openGraph: {
      images: getPageImage(page).url,
      locale: params.lang === 'zh' ? 'zh_CN' : 'en_US',
    },
  };
}
