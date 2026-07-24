import { ImageResponse } from 'next/og';
import { generate as DefaultImage } from 'fumadocs-ui/og';
import { i18n } from '@/lib/i18n';

export const revalidate = false;

const COPY: Record<string, { title: string; description: string }> = {
  en: {
    title: 'Polymerium — Minecraft Instance Manager',
    description:
      'Metadata-driven Minecraft instance manager. Zero-duplication storage, snapshots, CLI, MCP AI Agent mode.',
  },
  zh: {
    title: 'Polymerium — Minecraft 实例管理器',
    description:
      '基于元数据驱动的 Minecraft 实例管理器。零重复存储、快照、CLI、MCP AI Agent 模式。',
  },
};

export function generateStaticParams() {
  return i18n.languages.map((lang) => ({ lang }));
}

export async function GET(_req: Request, { params }: RouteContext<'/og/home/[lang]'>) {
  const { lang } = await params;
  const { title, description } = COPY[lang] ?? COPY[i18n.defaultLanguage];

  return new ImageResponse(
    <DefaultImage title={title} description={description} site="Polymerium" />,
    {
      width: 1200,
      height: 630,
    },
  );
}
