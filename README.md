# Eplan Scripting GUI Examples

Learn how to build custom windows and tools inside Eplan Electric P8 with practical, commented C# scripting examples. Whether you want a quick input form, a progress window, a data grid, or a ribbon button for your own project utilities, each example is small enough to read in one sitting and ready to copy and adapt.

> **Contributions, ideas, and example requests are welcome.** If there is a GUI pattern you would like to see, [open an issue](../../issues/new/choose) and describe what you need.

> ⚠️ **Unofficial community project.** This repository is not affiliated with, endorsed by, or supported by EPLAN. "EPLAN" is a trademark of its owner. All code here is original and uses only the scripting API that ships with Eplan. No Eplan source code, proprietary documentation, training content, or other copyrighted material is included.

## Why this exists

Eplan is a powerful platform, and scripting is one more way to shape it around how you work. Many people can automate a repetitive task with a script, and then wonder how to give it a window of its own. These examples show common GUI patterns in small, focused pieces. Each one is designed to be copied, modified, and used as a starting point for your own tools.

## Who this is for

- Eplan engineers and designers learning scripting
- Electrical designers automating repetitive tasks
- Application engineers building internal productivity tools
- Teams moving from simple scripts to scripts with their own windows

Beginner examples explain the basic C# they use. Later examples cover data grids, saved settings, and ribbon buttons.

## Examples

| # | Example | What you can do with it | Level | Status |
|---|---------|-------------------------|-------|--------|
| 01 | [Reusable input dialog](examples/01-input-dialog/) | Ask the user for text, confirm an action, show a message | Beginner | Available |
| 02 | [Progress bar](examples/02-progress-bar/) | Show progress during a long task, with Cancel | Beginner | Available |
| 03 | [Window placement](examples/03-window-placement/) | Open dialogs centered on Eplan, on a visible monitor | Intermediate | Available |
| 04 | [Light and dark theme](examples/04-light-dark-theme/) | A "Dark mode" check box that restyles your window, plus matching Eplan's own color scheme | Intermediate | Available |
| 05 | [Ribbon tab, group and buttons](examples/05-ribbon-tab/) | Add your own tab, group and buttons (with icons) that run your scripts | Beginner | Available |
| 06 | [Remember size and position](examples/06-remember-window/) | Reopen windows where the user left them | Intermediate | Available |
| 07 | [Themed message window](examples/07-themed-message/) | Messages and questions that fit dark mode | Beginner | Available |
| 08 | [Settings dialog](examples/08-settings-dialog/) | Save and restore user preferences | Beginner | Draft |
| 09 | [Page list](examples/09-page-list/) | Search, sort and export pages to CSV | Advanced | Draft |
| 10 | [Property viewer](examples/10-property-viewer/) | See every property of the project or your selection | Advanced | Draft |
| 11 | [Parts viewer](examples/11-parts-viewer/) | Search the parts database as you type | Intermediate | Draft |
| 12 | [Please-wait window](examples/12-please-wait-window/) | Keep a moving window up while Eplan is busy | Advanced | Available |
| 13 | [Dark-mode-safe controls](examples/13-dark-safe-controls/) | Date field, drop-down, check box and progress bar for dark mode | Intermediate | Draft |
| 14 | [Selection explorer](examples/14-selection-explorer/) | Write the properties of your selection to a text file | Intermediate | Draft |
| 15 | [Hand a value between scripts](examples/15-hand-off-value/) | Pass a short-lived value from one script to another | Advanced | Draft |
| 16 | [Modern folder picker](examples/16-modern-folder-picker/) | The Explorer-style "choose a folder" window | Advanced | Draft |
| 17 | [Export launcher](examples/17-export-launcher/) | PDF export with a file name pattern | Intermediate | Draft |
| 18 | [Device list export](examples/18-device-list-export/) | CSV, XML or text, opened in Excel | Intermediate | Draft |
| 19 | [Renumber helper](examples/19-renumber-helper/) | A safe front end for the renumber action | Intermediate | Draft |
| 20 | [Timestamped backup](examples/20-timestamped-backup/) | A backup with the date and time in its name | Intermediate | Draft |
| 21 | [Favorites launcher](examples/21-favorites-launcher/) | A searchable list of your scripts and actions | Intermediate | Draft |
| 22 | [Results window](examples/22-results-window/) | One report window instead of many message boxes | Intermediate | Draft |
| 23 | [Bulk edit pages](examples/23-bulk-edit/) | Preview, then change many pages at once | Advanced | Draft |
| 24 | [CSV preview and import](examples/24-csv-device-import/) | Check a CSV file before importing it | Advanced | Draft |
| 25 | [Settings finder](examples/25-settings-finder/) | Search Eplan's settings (read-only) | Intermediate | Draft |
| 26 | [Back up first, then run](examples/26-backup-then-run/) | A safety net in front of risky work | Intermediate | Draft |

**Draft** examples are written and compile-checked, but have not been run in Eplan yet. Try them on a copy of a project and tell us what you find. More ideas are in [IDEAS.md](IDEAS.md). Request one by opening an issue.

## Quick start

1. Read [docs/getting-started.md](docs/getting-started.md).
2. Open an example folder under [examples/](examples/).
3. In Eplan, choose File > Extras > Interfaces > **Run** and select the `.cs` file. See [How to run a script](docs/running-scripts.md).

## Tested with

Compile-checked against Eplan Electric P8 **2026** (2026.0.3). Each example's README states whether it has also been run inside Eplan.

## Using these examples

- The code is [MIT licensed](LICENSE): use it, change it, and share it, including in your own work.
- Try each script on a copy of a project first. Scripts run inside Eplan and can change project data.
- The code is provided as is, without warranty. Read a script before you run it.

## Documentation

- [Getting started](docs/getting-started.md)
- [How to run a script](docs/running-scripts.md) (with a Quick Access toolbar tip)
- [Reaching Eplan's data model](docs/reaching-the-data-model.md) (for examples that read project data)
- [C# basics used in these examples](docs/csharp-basics.md)
- [How Eplan loads scripts](docs/how-eplan-loads-scripts.md)
- [Troubleshooting](docs/troubleshooting.md)

## Contributing

Questions and ideas go in [Discussions](../../discussions) and Issues. Pull requests are welcome too, with one rule: only submit your own original work. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE).

---

Maintained by Justin Walz, [Walz Engineering](https://www.walzengineering.com). Need a custom Eplan interface built for your team? [Get in touch](https://www.walzengineering.com).
