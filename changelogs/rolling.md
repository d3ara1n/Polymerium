## [Unreleased]

### ✨ Highlights ✨

- Update the package identifier concept from Purl to Pref, persisting instance profiles in the pref key with pref:// values while loading older purl-formatted entries automatically
- Introduce collapsible modpack groupings on the instance package list that gather each modpack's mods under a header showing its icon and name, with manually added mods placed loose at the bottom

### Fixed

-

### Added

- Add support for pipe table and grid table rendering in markdown (Huskui.Avalonia)
- Introduce collapsible modpack groupings on the instance package list that gather each modpack's mods under a header showing its icon and name, with manually added mods placed loose at the bottom (#POLY-119)
- Add a sparkle badge to recently added instances and a subtle background highlight to active instances in the sidebar for at-a-glance status identification (#POLY-23)

### Changed

- Update modals with a frosted blur backdrop
- Update the package identifier concept from Purl to Pref, persisting instance profiles in the pref key with pref:// values while loading older purl-formatted entries automatically (#POLY-123)

### Removed

- Drop the grid layout toggle on the instance package list (#POLY-119)
