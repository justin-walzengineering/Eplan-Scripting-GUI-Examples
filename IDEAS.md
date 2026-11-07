# Ideas for future examples

A running list of examples that would help the community. Ideas are **descriptions only**: never copied code, customer data, or Eplan-owned material. Every example is written from scratch with generic data.

Status: `idea` > `in progress` > `partly` (run in Eplan, some parts still unchecked) > `done`. Have an idea? Open an Issue using the "Example idea" form.

## Build order

| # | Status | Example | What it teaches | Level |
|---|--------|---------|-----------------|-------|
| 01 | done | Reusable input dialog | Forms, controls, returning values, handling Cancel | Beginner |
| 02 | done | Progress bar around a long action | Progress API, `try/finally` | Beginner |
| 03 | done | Place a window well | Open a dialog centered on the Eplan main window, owned by it, hidden from the taskbar, kept on a visible monitor | Intermediate |
| 04 | done | Light and dark theme | A "Dark mode" check box that restyles a window live (colors, controls, title bar), plus matching Eplan's own color scheme by reading its setting. Checked in Eplan 2026 | Intermediate |
| 05 | done | Ribbon tab, group and buttons with icons | Create your own ribbon tab, add a group to it, and add buttons that run scripts. Give each button an icon read from an image file kept next to the script, and re-run safely (replace the tab instead of duplicating it) | Beginner |
| 06 | done | Remember window size and position | Save to and load from the registry, and open somewhere sensible if a monitor was unplugged | Intermediate |
| 07 | done | Themed message and confirm window | Why a standard message box ignores dark mode, and a themed replacement with OK or Yes/No | Beginner |
| 08 | done | Settings dialog that saves preferences | Per-user options that survive a restart, stored as JSON | Beginner |
| 09 | done | Page list with search, sort, CSV export | Reflection, sortable ListView, filtering, writing CSV (read-only) | Advanced |
| 10 | done | Property viewer | Every property of the project or selection, searchable (read-only) | Advanced |
| 11 | done | Parts viewer with filter box | MasterData parts classes used directly; filtering a large list (read-only) | Intermediate |
| 12 | done | "Please wait" window on its own thread | Keep a progress indicator moving while Eplan is busy, and close it safely | Advanced |
| 13 | done | Dark-mode-safe controls | Custom check box, date field, drop-down and progress bar | Intermediate |
| 14 | partly | Selection explorer | Write the properties of the selected objects to a text file (read-only) | Intermediate |
| 15 | done | Hand a value from one script to another | Short-lived value shared between two scripts, with a timestamp | Advanced |
| 16 | done | Modern folder picker | The Explorer-style "choose a folder" window through COM | Advanced |
| 17 | done | Export launcher (PDF) | The export action with a file name pattern; opens the folder | Intermediate |
| 18 | partly | Device list export | The devicelist action: CSV, XML or text, opened in Excel | Intermediate |
| 19 | done | Renumber helper | A safe front end for the renumber action | Intermediate |
| 20 | done | Timestamped backup | The backup action with a date-and-time name and a comment | Intermediate |
| 21 | done | Favorites launcher | Searchable personal list of scripts and actions, saved as JSON | Intermediate |
| 22 | done | Results window | A reusable report window with Copy and Save | Intermediate |
| 23 | done | Bulk edit pages by filter | Preview, tick, confirm and apply; reading back after writing | Advanced |
| 24 | partly | CSV preview and device list import | A CSV parser, a preview grid, checks, then the devicelist import | Advanced |
| 25 | done | Settings finder (read-only) | Search Eplan's settings by name or description, with current values | Intermediate |
| 26 | done | Back up first, then run | Backup, then a script or action only if the backup worked | Intermediate |

## Docs to write

| Status | Page | Notes |
|--------|------|-------|
| partly | Pitfalls and fixes (docs/troubleshooting.md) | Now covers the data model errors, silent compile failures, export file names and the selection limit. Still to test: empty property reads, list box clicks below the last item, swapping grid cell types |
| done | Reaching the data model (docs/reaching-the-data-model.md) | Updated with what running examples 09, 10, 14 and 23 in Eplan 2026 showed |
| done | What Eplan adds to every script (docs/how-eplan-loads-scripts.md) | Tested for `System.Windows.Forms`, `System.IO`, `EventHandler` and the C# version; the rest is listed as still to confirm |

## Verification to do in Eplan

- Does `Settings.GetNumericSetting("USER.MF.GuiColorScheme", 0)` return 2 for light and 0 for automatic when run as a script? Confirmed in Eplan 2026: **1 = dark, 2 = light, 0 = automatic** (automatic resolved through Windows light mode). Still to confirm: automatic with Windows in dark mode. (Eplan's configuration files describe 0 = automatic, 1 = dark, 2 = light.)
- **Confirmed in Eplan 2026:** `System.Windows.Forms` is added to every script (a `MessageBox` compiled with no `using`); `System.IO` is not; a bare `EventHandler` does not compile when `using System;` is present (use `System.EventHandler`). Still to confirm: whether `System`, Base, Gui and the application framework namespaces are also added (the compile-check assumes they are).
- **Confirmed in Eplan 2026:** the data model is **not** referenced; a script naming `Eplan.EplApi.DataModel.Project` directly does not compile, and reflection reaches it (examples 09, 10, 14, 23).
- **Confirmed in Eplan 2026:** the application framework namespace is usable directly (`CommandLineInterpreter`, `ActionCallingContext`), as are `Newtonsoft.Json` and the MasterData parts classes.
- **Confirmed in Eplan 2026:** C# 5 only. String interpolation, `?.` and `nameof` each stop a script from running; `var`, `out` on a separate declaration and LINQ work.
- Ribbon example (05): the ribbon API takes icons as **SVG** (text, or a file path in some overloads) or an index of an existing icon, not as PNG files. The example reads an `.svg` file and passes its text. Confirmed in Eplan 2026: the buttons start their scripts (`ExecuteScript` with `ScriptFile`), and the remove script works. `[CallerFilePath]` did **not** give the script folder in Eplan 2026, so the example asks the user to select a file. Still to confirm: how the icons look on a dark and a light ribbon, whether the tab appears immediately or after a restart, what happens when the setup script is run twice.

- **Examples 06 to 26** were run in Eplan 2026 (Update 5) on 2026-11-07, on a copy of a demo project. Still open: 14 (drawing-page selection did not reach the script), 18 and 24 (the demo project had no device list data, so real contents and the import effect are unchecked), column sorting in 09, light-mode and second-monitor checks for 06 and 07, devices/terminals/cables in 19, and the other change types in 23. Findings from the run: the data-model helpers needed `LockSelectionByDefault` set to false and a property lookup that tolerates repeated names; the PDF export action in 2026 chooses its own file name, so example 17 renames the result.

## Where the newer ideas came from

Ideas 17 to 22 come from reading what Eplan's own documentation says users must do by hand (one PDF per export run, one-at-a-time page renaming, repeated menu steps). Ideas 23 to 26 come from themes in public Eplan community discussions (bulk edits with no filter, lost work with no backup, importing from other tools through a list, and "which setting controls this?"). No individual posts are quoted or recorded.

## Suggested while working on other projects

_Nothing yet. New ideas go here with: what it does, what it teaches, difficulty, and why people would want it._
