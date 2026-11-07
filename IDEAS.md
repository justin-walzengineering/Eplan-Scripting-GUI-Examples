# Ideas for future examples

A running list of examples that would help the community. Ideas are **descriptions only**: never copied code, customer data, or Eplan-owned material. Every example is written from scratch with generic data.

Status: `idea` > `in progress` > `done`. Have an idea? Open an Issue using the "Example idea" form.

## Build order

| # | Status | Example | What it teaches | Level |
|---|--------|---------|-----------------|-------|
| 01 | done | Reusable input dialog | Forms, controls, returning values, handling Cancel | Beginner |
| 02 | done | Progress bar around a long action | Progress API, `try/finally` | Beginner |
| 03 | idea | Place a window well | Open a dialog centered on the Eplan main window, owned by it, hidden from the taskbar, kept on a visible monitor | Beginner |
| 04 | idea | Light and dark theme | A "Dark mode" check box that restyles a window live (colors, controls, title bar), then an "Auto" option that detects Eplan's theme. Automatic detection is a heuristic, so test it on 2025 and 2026 | Intermediate |
| 05 | idea | Remember window size and position | Save to and load from user settings, and open somewhere sensible if a monitor was unplugged | Intermediate |
| 06 | idea | Themed message and confirm window | Why a standard message box ignores dark mode, and a themed replacement with OK or Yes/No | Beginner |
| 07 | idea | Settings dialog that saves preferences | Per-user options that survive a restart | Beginner |
| 08 | idea | Page list with search, sort, CSV export | Grids, filtering, sorting, file dialogs, tick boxes in a list | Intermediate |
| 09 | idea | Project property viewer/editor | DataGridView, reading and writing properties, handling empty values | Intermediate |
| 10 | idea | Parts and devices viewer with filter box | Live filtering of larger data sets | Intermediate |
| 11 | idea | Ribbon tab and button creator | Add a tab, group and button that runs a script, with an icon and a safe "replace" when run again | Intermediate |
| 12 | idea | "Please wait" window on its own thread | Keep a progress indicator moving while Eplan is busy, and close it safely | Advanced |
| 13 | idea | Dark-mode-safe controls | Custom check box and date picker, because the standard ones are hard to see on a dark background | Intermediate |
| 14 | idea | Selection explorer | Write the properties of the selected objects to a text file, to learn what an object exposes and find property numbers | Intermediate |
| 15 | idea | Hand a value from one script to another | Pass a short-lived value between two scripts in the same Eplan session | Advanced |

## Docs to write

| Status | Page | Notes |
|--------|------|-------|
| idea | Pitfalls and fixes | Short table in our own words: selections come back empty unless unlocked, reading an empty property throws, `EventHandler` is ambiguous inside Eplan, clicks below the last item of a list box can throw, don't swap grid cell types at run time. **Verify each item on 2025 and 2026 before publishing.** |
| idea | Reaching parts of the API without a compile-time reference | Reports say Eplan's script compiler does not reference the data model assemblies, so scripts use reflection to reach them. Confirm in Eplan 2025 and 2026, then explain the pattern with a tiny generic example |
| idea | What Eplan adds to every script | Which namespaces Eplan adds and which names become ambiguous. Confirm by test, then document |

## Verification to do in Eplan

- Which namespaces does the script compiler add? (The compile-check assumes the application framework, base, GUI and scripting namespaces.)
- Does the script compiler reference the data model and services assemblies? (The compile-check assumes **not**.)
- Is the application framework namespace usable directly from a script? Reports conflict, so test it.
- Which C# language version does the script compiler accept? (The compile-check enforces C# 5.)

## Suggested while working on other projects

_Nothing yet. New ideas go here with: what it does, what it teaches, difficulty, and why people would want it._
