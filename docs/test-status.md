# Test status

Every example has been run in Eplan Electric P8 2026 (2026.0.3), and examples 06 to 26 on a copy of a demo project. For five examples, some options or cases have not been tried yet. This page lists them in one place so you can see what was checked and what is still open.

"Partly tested" means exactly what the example's README says was run. Nothing here is a claim that the open cases fail; they have not been tried.

Each README has a Compatibility section with the full details.

## What is still open

| # | Example | Checked in Eplan 2026 | Not tried yet | Kind of test |
|---|---------|-----------------------|---------------|--------------|
| 04 | [Light and dark theme](../examples/04-light-dark-theme/) | Dark, light, and automatic with Windows in light mode (run 2026-11-07) | Automatic with Windows set to dark mode (needs the Windows theme changed) | Changes a Windows setting |
| 06 | [Remember size and position](../examples/06-remember-window/) | Saved and restored position and size, off-screen fallback, maximized, monitors on the right and left, Forget then Close (run 2026-11-07) | A monitor that is actually unplugged | Only changes the demo's own entry in the Windows registry |
| 07 | [Themed message window](../examples/07-themed-message/) | Dark, light, and automatic with Windows in light mode (run 2026-11-07) | Automatic with Windows set to dark mode | Changes a Windows setting |
| 14 | [Selection explorer](../examples/14-selection-explorer/) | Three pages selected in a navigator, written to a text file; the open-page fallback, from the Quick Access Run script button, once (run 2026-11-07) | Objects selected on an open drawing page still reported nothing selected (all objects, and one text object, from the File menu and from the button) | Reads only |
| 19 | [Renumber helper](../examples/19-renumber-helper/) | Pages and cables changed as expected; devices, terminals and connections ran with no error (run 2026-11-07) | An effect for devices, terminals and connections (no visible change for one selected object each); devices with only tags that contain a ? | Writes to a copy |

## Fully run examples

01, 02, 03, 05, 08, 09, 10, 11, 12, 13, 15, 16, 17, 18, 20, 21, 22, 23, 24, 25 and 26 have no open cases listed. Example 18 was run with a CSV export of a device list that had four entries, and example 24 imported a CSV in that layout, with the new rows showing in a second export (both 2026-11-07).

## Helping out

If you try one of the open cases, work on a copy of a project, not the original. Then tell us what you saw, whether it worked or not. You can open an issue on the repository.
