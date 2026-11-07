# website

This is a Next.js application generated with
[Create Fumadocs](https://github.com/fuma-nama/fumadocs).

Run development server:

```bash
npm run dev
# or
pnpm dev
# or
yarn dev
```

Open http://localhost:3000 with your browser to see the result.

## Build and search indexing

Use Node.js 22.18 or newer so Node can load the shared TypeScript content modules directly.

Build the website with `npm run build` and synchronize the Algolia search index with
`npm run build:post`. Vercel runs these commands in that order.

The index synchronization command regenerates the Fumadocs collection and loads it through
Fumadocs' Node loader using the same source adapter and index exporter as `/static.json`.
It reads the checked-out documentation directly and does not require a Next.js build or
inspect framework-specific build artifacts.

Provide `NEXT_PUBLIC_ALGOLIA_APP_ID` and `ALGOLIA_ADMIN_API_KEY` in the command environment.
`NEXT_PUBLIC_ALGOLIA_INDEX_NAME` defaults to `document`. Missing credentials, an empty document
source, or a failed upload fail the command rather than silently leaving search out of date.

To generate the index without credentials or an upload, run:

```bash
npm run build:post -- --dry-run
```

## Explore

In the project, you can see:

- `lib/source.ts`: Code for content source adapter, [`loader()`](https://fumadocs.dev/docs/headless/source-api) provides the interface to access your content.
- `lib/layout.shared.tsx`: Shared options for layouts, optional but preferred to keep.

| Route                     | Description                                            |
| ------------------------- | ------------------------------------------------------ |
| `app/(home)`              | The route group for your landing page and other pages. |
| `app/docs`                | The documentation layout and pages.                    |
| `app/api/search/route.ts` | The Route Handler for search.                          |

### Fumadocs MDX

A `source.config.ts` config file has been included, you can customise different options like frontmatter schema.

Read the [Introduction](https://fumadocs.dev/docs/mdx) for further details.

## Learn More

To learn more about Next.js and Fumadocs, take a look at the following
resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js
  features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.
- [Fumadocs](https://fumadocs.dev) - learn about Fumadocs
