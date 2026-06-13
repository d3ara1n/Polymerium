# Changelog Writing Rules

All changelog entries under `changelogs/` must be written in Chinese with a verb-first opening as a single self-contained sentence that states what was done without elaborating on reasons or technical details and without using colons or descriptive parentheticals although alias-style parenthetical references such as issue keys like (#64) for GitHub issues or (#POLY-38) for Jira issues or Sentry issue IDs or dependency attributions like (Huskui.Avalonia) are required at the end of an entry when the change corresponds to a tracked issue.

Each version file follows the Keep a Changelog convention with the five sections Added, Changed, Removed, and Fixed plus an extra ✨ Highlights ✨ block at the top that repeats verbatim the most important one to three entries from the sections below without rewriting or rephrasing them.

Every section must always be present even when empty using a bare dash `-` as placeholder and sub-versions within a file are ordered newest-first with each sub-version carrying the full five-section structure.

Entries use fixed verb patterns per section: Added uses 添加/新增/提供/增加, Changed uses 优化/修改/调整/更换/重写, Fixed uses 修复/尝试性修复, and Removed uses 移除.
