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
| 06 | idea | Remember window size and position | Save to and load from user settings, and open somewhere sensible if a monitor was unplugged | Intermediate |
| 07 | idea | Themed message and confirm window | Why a standard message box ignores dark mode, and a themed replacement with OK or Yes/No | Beginner |
| 08 | idea | Settings dialog that saves preferences | Per-user options that survive a restart | Beginner |
| 09 | idea | Page list with search, sort, CSV export | Grids, filtering, sorting, file dialogs, tick boxes in a list | Intermediate |
| 10 | idea | Project property viewer/editor | DataGridView, reading and writing properties, handling empty values | Intermediate |
| 11 | idea | Parts and devices viewer with filter box | Live filtering of larger data sets | Intermediate |
| 12 | idea | "Please wait" window on its own thread | Keep a progress indicator moving while Eplan is busy, and close it safely | Advanced |
| 13 | idea | Dark-mode-safe controls | Custom check box and date picker, because the standard ones are hard to see on a dark background | Intermediate |
| 14 | idea | Selection explorer | Write the properties of the selected objects to a text file, to learn what an object exposes and find property numbers | Intermediate |
| 15 | idea | Hand a value from one script to another | Pass a short-lived value between two scripts in the same Eplan session | Advanced |
| 16 | idea | Modern folder picker | The Explorer-style "choose a folder" window (the old tree-style one is what .NET Framework offers by default), using the Windows folder dialog through COM. Reusable helper for any script that needs a folder | Advanced |

## Docs to write

| Status | Page | Notes |
|--------|------|-------|
| idea | Pitfalls and fixes | Short table in our own words: selections come back empty unless unlocked, reading an empty property throws, `EventHandler` is ambiguous inside Eplan, clicks below the last item of a list box can throw, don't swap grid cell types at run time. **Verify each item in Eplan 2026 before publishing.** |
| idea | Reaching parts of the API without a compile-time reference | Reports say Eplan's script compiler does not reference the data model assemblies, so scripts use reflection to reach them. Confirm in Eplan 2026, then explain the pattern with a tiny generic example |
| idea | What Eplan adds to every script | Which namespaces Eplan adds and which names become ambiguous. Confirm by test, then document |

## Verification to do in Eplan

- Does `Settings.GetNumericSetting("USER.MF.GuiColorScheme", 0)` return 2 for light and 0 for automatic when run as a script? Confirmed in Eplan 2026: **1 = dark, 2 = light, 0 = automatic** (automatic resolved through Windows light mode). Still to confirm: automatic with Windows in dark mode. (Eplan's configuration files describe 0 = automatic, 1 = dark, 2 = light.)
- Which namespaces does the script compiler add? (The compile-check assumes the application framework, base, GUI and scripting namespaces.)
- Does the script compiler reference the data model and services assemblies? (The compile-check assumes **not**.)
- Is the application framework namespace usable directly from a script? Reports conflict, so test it.
- Which C# language version does the script compiler accept? (The compile-check enforces C# 5.)
- Ribbon example (05): the ribbon API takes icons as **SVG** (text, or a file path in some overloads) or an index of an existing icon, not as PNG files. The example reads an `.svg` file and passes its text. Confirmed in Eplan 2026: the buttons start their scripts (`ExecuteScript` with `ScriptFile`), and the remove script works. `[CallerFilePath]` did **not** give the script folder in Eplan 2026, so the example asks the user to select a file. Still to confirm: how the icons look on a dark and a light ribbon, whether the tab appears immediately or after a restart, what happens when the setup script is run twice.

## Suggested while working on other projects

_Nothing yet. New ideas go here with: what it does, what it teaches, difficulty, and why people would want it._
