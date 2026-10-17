# Polymerium — Make it yours

The editable film is 25 seconds at 1920×1080 and 60 fps. Weighty block impacts, kinetic type, real interface crops and an original synchronized score carry the viewer through building, organizing, sharing and playing.

## Preview

Run from this directory:

```sh
npm ci
npm run dev
```

Studio supports playback and scrubbing and reloads edited sources. Review the composition there before requesting another export. The files in `delivery/` remain the earlier reviewed **32-second master**, not an export of the current timeline.

The project pins HyperFrames 0.8.168 and GSAP 3.14.2. Node.js 22 or newer, FFmpeg and a supported Chrome binary are required for checks and rendering. All fonts, images, runtime code and sound are local assets.

## Authoring

| Source | Responsibility |
| --- | --- |
| `scripts/build-film.mjs` | Scene composition, local animation, main wrappers and global HUD |
| `scripts/cues.mjs` | Shared duration, beat grid and transition specification for picture and sound |
| `scripts/cache-instances.mjs` | Cache card markup and real instance data |
| `scripts/minecraft-world.mjs` | Procedural island and final player-placed block |
| `scripts/compose-score.mjs` | Original deterministic music and picture-synchronous effects |
| `scripts/master-score.mjs` | Two-pass audio mastering |
| `scripts/prepare-assets.mjs` | Crops from frozen source screenshots |
| `BRIEF.md`, `frame.md`, `STORYBOARD.md` | Intent, art direction and shot plan |
| `ASSETS.md` | Provenance and licensing |

`index.html`, `compositions/*.html` and `ledger.json` are generated and retained for immediate preview. Edit the authoring sources and rebuild; direct edits to generated files are overwritten. Each scene is a separate sub-composition with a local paused timeline. The main timeline controls non-clip seam wrappers and the global HUD. HyperFrames controls host visibility and audio playback.

```sh
npm run build:film
npm run build:score
npm run check
```

Picture and score share one beat every 28 frames, approximately 128.571 BPM. Rebuild both when changing the cue grid. Regenerate screenshot crops with `npm run prepare:assets` only when their definitions change.

After editing transition timing or boundary movement:

```sh
CHROME_PATH="$(npx hyperframes browser path)" node scripts/verify-seams.mjs verify --ledger ledger.json --project . --fps 60
```

## Validation

The 25-second revision passes composition lint, browser runtime, layout and contrast checks with no errors or warnings across a 20-sample sweep. All five scene boundaries pass direction, speed and visibility verification. Twelve selected frames are pixel-identical when sought in forward and reverse order. Scene and action snapshots are inspected separately; the final pacing and sound remain subject to Studio review.

Package rows, the organizing cards and Play are source screenshot crops. Shared-cache cards are stylized reconstructions with real instance names, loader versions and established source labels. The island is conceptual artwork, not recorded gameplay. The film has no narration.

## Export after review

Keep draft exports in the ignored `renders/` directory:

```sh
npm run render -- --output renders/new-cut.mp4
```

Only after approval should a new master replace `delivery/polymerium-make-it-yours.mp4`. `npm run package:delivery` extracts the poster, key visual and contact sheet from the existing MP4; it does not render the composition. Update its selected timestamps and watch-page runtime for the approved cut when packaging a new delivery.
