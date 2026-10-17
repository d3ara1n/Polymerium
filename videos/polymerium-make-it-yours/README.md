# Polymerium — Make it yours

A 32-second promotional film with real Polymerium interface imagery, rebuilt instance cards, a Minecraft block scene, an original electronic score, and the official interlocking brand mark.

## Watch and preview

Open `delivery/watch.html` or play `delivery/polymerium-make-it-yours.mp4` for the reviewed 1920×1080, 60 fps master. The delivery folder also contains a poster, opening key visual, and nine-frame contact sheet.

For live editing, run from this directory:

```sh
npm ci
npm run dev
```

Studio opens in the browser. It supports playback and scrubbing and refreshes when source files change; exporting a video is not required for review. Review changes in Studio before requesting another export.

The project pins HyperFrames 0.8.168 and GSAP 3.14.2. Node.js 22 or newer is required; checks and rendering additionally need FFmpeg and a supported Chrome binary. Fonts, images, the animation runtime, and the mastered score are local assets.

## Authoring

| Source | Responsibility |
| --- | --- |
| `scripts/build-film.mjs` | Six scenes and the main animation timeline |
| `scripts/cache-instances.mjs` | Rebuilt instance cards using source screenshot content |
| `scripts/minecraft-world.mjs` | Grass island, cabin, oak tree, pond, crafting table, and furnace |
| `ledger.json` | Transition times and movement directions |
| `scripts/compose-score.mjs` | Original deterministic stereo music and sound effects |
| `scripts/master-score.mjs` | Two-pass audio mastering |
| `scripts/prepare-assets.mjs` | Crops used by the film, generated from frozen source screenshots |
| `BRIEF.md`, `frame.md`, `STORYBOARD.md` | Intent, design tokens, and shot plan |
| `ASSETS.md` | Provenance and licensing |

`index.html` is the generated HyperFrames composition, retained so the project is ready to preview. Edit its authoring sources and rebuild; direct edits to `index.html` will be overwritten.

```sh
npm run build:film
npm run check
```

Optional asset regeneration:

```sh
npm run prepare:assets
npm run build:score
```

After changing scene boundaries, run `node scripts/verify-seams.mjs verify --ledger ledger.json --project . --fps 60`. Set `CHROME_PATH` to the path printed by `npx hyperframes browser path` when the verifier cannot discover Chrome.

## Export

Keep iterative exports in the ignored `renders/` directory:

```sh
npm run render -- --output renders/new-cut.mp4
```

After approving a new master, replace `delivery/polymerium-make-it-yours.mp4` and run `npm run package:delivery` to regenerate the poster, key visual, and contact sheet. The packaging command extracts images from the existing MP4; it does not render the composition.

## Validation and scope

The reviewed master is exactly 32 seconds at 1920×1080 and 60 fps, with stereo 48 kHz AAC audio measuring −14.33 LUFS and −1.33 dBTP. Composition checks found no lint, runtime, layout, or contrast errors; all five scene boundaries passed continuity verification. Exported frames were visually inspected. Structural linter warnings for repeated brand assets and monolithic scene groups were reviewed and retained.

Package rows and the organizing scene's instance cards are screenshot crops. The shared-cache cards are rebuilt HTML/CSS with real names, loaders, Minecraft versions, and known source stamps. The block world is original conceptual artwork, not recorded gameplay. The film is unnarrated and has no dialogue captions.
