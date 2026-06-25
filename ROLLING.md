# Changelog Writing Rules

All changelog entries under `changelogs/` must be written in English in the imperative mood, opening with a base-form verb, as a single self-contained sentence that flows as one continuous clause with minimal punctuation, stating what changed without explaining reasons or technical details and without enumerating parallel items where one summary phrase suffices, and without colons or descriptive parentheticals — although alias-style parenthetical references such as GitHub issue keys (#64), Jira keys (#POLY-38), Sentry IDs, or dependency attributions (Huskui.Avalonia) are required at the end of an entry when the change maps to a tracked item.

Each version file follows the Keep a Changelog convention with the four sections Added, Changed, Removed, and Fixed, plus a ✨ Highlights ✨ block at the top that repeats verbatim up to three entries from the sections below that carry real value to end users, without rewriting or rephrasing them and dropping any trailing issue key or attribution, and stays a single bare dash when nothing below clears that bar.

Every section must always be present, even when empty, using a bare dash `-` as placeholder. Sub-versions within a file are ordered newest-first, and each carries the full four-section structure.

Entries use fixed verb patterns per section: Added uses Add/Introduce/Provide, Changed uses Change/Update/Improve/Adjust/Replace/Rework, Fixed uses Fix/Attempt to fix, and Removed uses Remove/Drop.

Changelog entries must describe only user-visible behavior — never class names, method names, internal services, event names, or implementation reasoning. Write what the user sees change, not how the code implements it.

All changes made to the same module or feature during one release cycle collapse into a single entry describing the final outcome, because users only see the shipped result and never the intermediate iterations — never split development steps into separate entries such as adding an icon in one and then replacing it in another, since each intermediate state never reached a user. The changelog is written for end users, so judge every entry from their perspective: it must state what a reader of the release notes will observe, not the sequence of changes made while building it.
