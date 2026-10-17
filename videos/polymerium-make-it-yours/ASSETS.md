# Asset provenance

| Asset | Source | Usage |
| --- | --- | --- |
| Polymerium logos | Repository `assets/brand/logo.svg`, `logo-dark.svg` | Original #EEA93B gold and source geometry preserved; animation wraps source groups |
| Setup screenshot | Repository `assets/screenshots/setup.png` | Main setup panel and three package-row crops |
| Dashboard screenshot | Repository `assets/screenshots/landing.png` | Whole dashboard, two instance-card crops, and Play panel |
| Geist and Geist Mono | Locally installed Next.js font assets; upstream https://github.com/vercel/geist-font | Shipped local WOFF2 files; SIL Open Font License in `assets/fonts/OFL.txt` |
| GSAP 3.14.2 | npm package `gsap` | Local animation runtime; Standard no-charge license https://gsap.com/standard-license |
| HyperFrames 0.8.168 | npm package `hyperframes` | Composition renderer and local preview |
| Text rise and character cascade | HyperFrames registry `text-stagger` and `logo-brand-close` | Adapted text-rise and character-cascade timing primitives in the authored timeline |
| Seam stamping and verification | Installed motion-doctrine skill | Copied into project scripts for reproducibility; local verifier uses current `renderSeek` and seek-completion barrier |
| Block sculpture and cache diagram | Original procedural artwork in `scripts/build-film.mjs` | Created for this film |
| Minecraft scene | Original procedural artwork in `scripts/minecraft-world.mjs` | Animated block geometry with simplified materials; no extracted game textures |
| Shared-cache instance cards | HTML/CSS and SVG in `scripts/cache-instances.mjs` | Actual names, loaders and versions from the screenshots; known source stamps preserved, generic profile label where the source is not established |
| Music and sound effects | Original deterministic synthesis in `scripts/compose-score.mjs` | No third-party audio samples or music tracks |

Brand, interface, Minecraft, and package imagery embedded in repository screenshots retain their respective owners' rights and existing attributions. This project does not assign new licenses to those assets or claim independent clearance of third-party artwork depicted in the application's interface.

The source screenshots supply all incidental version numbers, package counts, and account statistics visible in the movie. None is presented as a benchmark, popularity figure, or promise of performance.
