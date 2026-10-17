# Film project

- Canonical composition source is `scripts/build-film.mjs`, with instance cards in `scripts/cache-instances.mjs` and the Minecraft scene in `scripts/minecraft-world.mjs`.
- Rebuild `index.html` with `npm run build:film`; direct edits to the generated HTML will be overwritten.
- Use Studio for review and iteration. Export an MP4 only when the user requests a deliverable.
- Keep motion deterministic and seekable on the single paused timeline. Scene boundaries are defined by `ledger.json`.
- Run `npm run check` after composition edits; verify affected boundaries with `scripts/verify-seams.mjs` when timing or transitions change.
- Preserve the official logo geometry and gold color. Keep instance content consistent with the source screenshots.
- Temporary renders and drafts stay in ignored directories. The reviewed master has the stable path `delivery/polymerium-make-it-yours.mp4`.
