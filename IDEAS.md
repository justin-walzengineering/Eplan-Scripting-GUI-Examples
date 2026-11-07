# Ideas for future examples

A running list of examples that would help the community. Ideas are **descriptions only**: never copied code, customer data, or Eplan-owned material. Every example is written from scratch with generic data.

Status: `idea` > `in progress` > `done`. Have an idea? Open an Issue using the "Example idea" form.

## Build order

| # | Status | Example | What it teaches | Level |
|---|--------|---------|-----------------|-------|
| 01 | done | Reusable input dialog | Forms, controls, returning values, handling Cancel | Beginner |
| 02 | done | Progress bar around a long action | Progress API, `try/finally` | Beginner |
| 03 | done | Place a window well | Open a dialog centered on the Eplan main window, owned by it, hidden from the taskbar, kept on a visible monitor | Intermediate |
| 04 | done | Light and dark theme | A "Dark mode" check box that restyles a window live (colors, controls, title bar), plus matching Eplan's own color scheme by reading its setting. Checked in Eplan 2026 | Intermediate |
| 05 | done | Ribbon tab, group and buttons with icons | Create your own ribbon tab, add a group to it, and add buttons that run scripts. Give each button an icon read from an image file kept next to the script, and re-run safely (replace the tab instead of duplicating it) | Beginner |
| 06 | draft | Remember window size and position | Save to and load from the registry, and open somewhere sensible if a monitor was unplugged | Intermediate |
| 07 | done | Themed message and confirm window | Why a standard message box ignores dark mode, and a themed replacement with OK or Yes/No | Beginner |
| 08 | draft | Settings dialog that saves preferences | Per-user options that survive a restart, stored as JSON | Beginner |
| 09 | draft | Page list with search, sort, CSV export | Reflection, sortable ListView, filtering, writing CSV (read-only) | Advanced |
| 10 | draft | Property viewer | Every property of the project or selection, searchable (read-only) | Advanced |
| 11 | draft | Parts viewer with filter box | MasterData parts classes used directly; filtering a large list (read-only) | Intermediate |
| 12 | draft | "Please wait" window on its own thread | Keep a progress indicator moving while Eplan is busy, and close it safely | Advanced |
| 13 | draft | Dark-mode-safe controls | Custom check box, date field, drop-down and progress bar | Intermediate |
| 14 | draft | Selection explorer | Write the properties of the selected objects to a text file (read-only) | Intermediate |
| 15 | draft | Hand a value from one script to another | Short-lived value shared between two scripts, with a timestamp | Advanced |
| 16 | draft | Modern folder picker | The Explorer-style "choose a folder" window through COM | Advanced |
| 17 | draft | Export launcher (PDF) | The export action with a file name pattern; opens the folder | Intermediate |
| 18 | draft | Device list export | The devicelist action: CSV, XML or text, opened in Excel | Intermediate |
| 19 | draft | Renumber helper | A safe front end for the renumber action | Intermediate |
| 20 | draft | Timestamped backup | The backup action with a date-and-time name and a comment | Intermediate |
| 21 | draft | Favorites launcher | Searchable personal list of scripts and actions, saved as JSON | Intermediate |
| 22 | draft | Results window | A reusable report window with Copy and Save | Intermediate |
| 23 | draft | Bulk edit pages by filter | Preview, tick, confirm and apply; reading back after writing | Advanced |
| 24 | draft | CSV preview and device list import | A CSV parser, a preview grid, checks, then the devicelist import | Advanced |
| 25 | draft | Settings finder (read-only) | Search Eplan's settings by name or description, with current values | Intermediate |
| 26 | draft | Back up first, then run | Backup, then a script or action only if the backup worked | Intermediate |

## Docs to write

| Status | Page | Notes |
|--------|------|-------|
| idea | Pitfalls and fixes | Short table in our own words: selections come back empty unless unlocked, reading an empty property throws, `EventHandler` is ambiguous inside Eplan, clicks below the last item of a list box can throw, don't swap grid cell types at run time. **Verify each item in Eplan 2026 before publishing.** |
| draft | Reaching the data model (docs/reaching-the-data-model.md) | Written from Eplan's 2026 documentation and the draft examples. Update once the data-model examples have been run in Eplan |
| idea | What Eplan adds to every script | Which namespaces Eplan adds and which names become ambiguous. Confirm by test, then document |

## Verification to do in Eplan

- Does `Settings.GetNumericSetting("USER.MF.GuiColorScheme", 0)` return 2 for light and 0 for automatic when run as a script? Confirmed in Eplan 2026: **1 = dark, 2 = light, 0 = automatic** (automatic resolved through Windows light mode). Still to confirm: automatic with Windows in dark mode. (Eplan's configuration files describe 0 = automatic, 1 = dark, 2 = light.)
- Which namespaces does the script compiler add? (The compile-check assumes the application framework, base, GUI and scripting namespaces.)
- Does the script compiler reference the data model and services assemblies? Eplan's 2026 documentation lists the default assemblies and does **not** include them, so the compile-check assumes **not**. Confirm with a one-line test script.
- Is the application framework namespace usable directly from a script? Reports conflict, so test it.
- Which C# language version does the script compiler accept? (The compile-check enforces C# 5.)
- Ribbon example (05): the ribbon API takes icons as **SVG** (text, or a file path in some overloads) or an index of an existing icon, not as PNG files. The example reads an `.svg` file and passes its text. Confirmed in Eplan 2026: the buttons start their scripts (`ExecuteScript` with `ScriptFile`), and the remove script works. `[CallerFilePath]` did **not** give the script folder in Eplan 2026, so the example asks the user to select a file. Still to confirm: how the icons look on a dark and a light ribbon, whether the tab appears immediately or after a restart, what happens when the setup script is run twice.

- **Draft examples 06 to 26** are not yet run in Eplan. Per example, confirm: it runs, the window looks right (light and dark), the main action works, and Cancel/close behave. Special attention: the reflection examples (09, 10, 14, 23: member names, selection, locking, writing and reading back), `$(PROJECTNAME)` in the action examples (17, 18, 20, 26), the action parameter spellings (`export`, `devicelist`, `renumber`, `backup`), parts database loading (11), and whether scripts share one AppDomain (15).

## Where the newer ideas came from

Ideas 17 to 22 come from reading what Eplan's own documentation says users must do by hand (one PDF per export run, one-at-a-time page renaming, repeated menu steps). Ideas 23 to 26 come from themes in public Eplan community discussions (bulk edits with no filter, lost work with no backup, importing from other tools through a list, and "which setting controls this?"). No individual posts are quoted or recorded.

## Suggested while working on other projects

_Nothing yet. New ideas go here with: what it does, what it teaches, difficulty, and why people would want it._
