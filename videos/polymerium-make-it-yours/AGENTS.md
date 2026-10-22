# Film project

- Canonical composition source is `scripts/build-film.mjs`, with instance cards in `scripts/cache-instances.mjs` and the Minecraft scene in `scripts/minecraft-world.mjs`.
- Rebuild `index.html`, `compositions/*.html` and `ledger.json` with `npm run build:film`; direct edits to generated files will be overwritten. `scripts/cues.mjs` owns the beat grid and scene boundaries shared by picture and score.
- Use Studio for review and iteration. Export an MP4 only when the user requests a deliverable.
- Keep motion deterministic and seekable on the paused timelines. The main timeline owns seam wrappers; mounted scenes own their local choreography. `ledger.json` records the generated scene boundaries.
- Run `npm run check` after composition edits; verify affected boundaries with `scripts/verify-seams.mjs` when timing or transitions change.
- Preserve the official logo geometry and gold color. Keep instance content consistent with the source screenshots.
- Generated outputs — `index.html`, `compositions/`, `ledger.json`, `out/` media, draft renders and `delivery/` artifacts — stay in git-ignored directories and are rebuilt from source; `assets/` holds vendored inputs only. The reviewed master has the stable path `delivery/polymerium-make-it-yours.mp4`.
