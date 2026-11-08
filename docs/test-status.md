# Test status

Every example has been run in Eplan Electric P8 2026 (2026.0.3), and examples 06 to 26 on a copy of a demo project. For eight examples, some options or cases have not been tried yet. This page lists them in one place so you can see what was checked and what is still open.

"Partly tested" means exactly what the example's README says was run. Nothing here is a claim that the open cases fail; they have not been tried.

Each README has a Compatibility section with the full details.

## What is still open

| # | Example | Checked in Eplan 2026 | Not tried yet | Kind of test |
|---|---------|-----------------------|---------------|--------------|
| 04 | [Light and dark theme](../examples/04-light-dark-theme/) | Dark, light and automatic (Windows in light mode) | Automatic with Windows set to dark mode | Changes a Windows setting |
| 06 | [Remember size and position](../examples/06-remember-window/) | Saved and restored position and size, off-screen fallback, maximized, monitors on the right and left (run 2026-11-07) | A monitor that is actually unplugged. Forget then Close was fixed and retested | Only changes the demo's own entry in the Windows registry |
| 07 | [Themed message window](../examples/07-themed-message/) | Dark scheme: message, Yes/No question, Esc as No, answer window | Eplan set to light or automatic | Changes an Eplan setting |
| 14 | [Selection explorer](../examples/14-selection-explorer/) | Three pages selected in a navigator, written to a text file | Objects selected on an open drawing page still reported nothing selected (all objects, and one text object, 2026-11-07); start from a Quick Access button | Reads only; the Quick Access button needs a change to the toolbar |
| 18 | [Device list export](../examples/18-device-list-export/) | Action ran; CSV and XML files written | Real columns and content (the demo project's device list was empty) | Needs a project with device list data |
| 19 | [Renumber helper](../examples/19-renumber-helper/) | Pages type, only the selected objects, three pages | Devices, terminals, cables and connections | Writes to a copy |
| 23 | [Bulk edit pages](../examples/23-bulk-edit/) | "Replace text in the description" on one page | "Set the description to"; several pages at once | Writes to a copy |
| 24 | [CSV preview and import](../examples/24-csv-device-import/) | Preview, checks, confirmation; import action finished without an error | That the rows reach the device list (a test file with made-up column names changed nothing) | Needs a project with device list data; writes to a copy |

## Fully run examples

01, 02, 03, 05, 08, 09 (sorting run 2026-11-07), 10, 11, 12, 13, 15, 16, 17, 20, 21, 22, 25 and 26 have no open cases listed.

## Helping out

If you try one of the open cases, work on a copy of a project, not the original. Then tell us what you saw, whether it worked or not. You can open an issue on the repository.
