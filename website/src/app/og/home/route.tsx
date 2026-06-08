import { ImageResponse } from 'next/og';
import { generate as DefaultImage } from 'fumadocs-ui/og';

export const revalidate = false;

export async function GET(req: Request) {
  const { searchParams } = new URL(req.url);
  const lang = searchParams.get('lang') || 'en';

  const isZh = lang === 'zh';

  const title = isZh
    ? 'Polymerium — Minecraft 实例管理器'
    : 'Polymerium — Minecraft Instance Manager';

  const description = isZh
    ? '基于元数据驱动的 Minecraft 实例管理器。零重复存储、快照、CLI、MCP AI Agent 模式。'
    : 'Metadata-driven Minecraft instance manager. Zero-duplication storage, snapshots, CLI, MCP AI Agent mode.';

  return new ImageResponse(
    <DefaultImage title={title} description={description} site="Polymerium" />,
    {
      width: 1200,
      height: 630,
    },
  );
}