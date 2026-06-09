import { exportAlgoliaIndex } from '@/lib/export-algolia-index';

export const revalidate = false;

export async function GET() {
  return Response.json(await exportAlgoliaIndex());
}
