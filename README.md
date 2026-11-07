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
| 02 | Progress bar | Show progress during a long task | Beginner | Planned |
| 03 | Window placement | Open dialogs centered on Eplan, on a visible monitor | Beginner | Planned |
| 04 | Light and dark theme | Make your windows match Eplan's theme | Intermediate | Planned |
| 05 | Remember size and position | Reopen windows where the user left them | Intermediate | Planned |
| 06 | Themed message window | Messages and questions that fit dark mode | Beginner | Planned |
| 07 | Settings dialog | Save and restore user preferences | Beginner | Planned |
| 08 | Page list with search and export | Sort, filter, and export pages to CSV | Intermediate | Planned |
| 09 | Project property viewer and editor | Browse and edit project data in a grid | Intermediate | Planned |
| 10 | Parts viewer with filter | Search larger lists as you type | Intermediate | Planned |
| 11 | Ribbon button creator | Add your own tab and buttons to Eplan | Intermediate | Planned |

More are on the list in [IDEAS.md](IDEAS.md). Request one by opening an issue.

## Quick start

1. Read [docs/getting-started.md](docs/getting-started.md).
2. Open an example folder under [examples/](examples/).
3. Load the `.cs` file in Eplan (Utilities > Scripts) and run it.

## Tested with

Compile-checked against Eplan Electric P8 **2025** (2025.0.3) and **2026** (2026.0.3). Each example's README states whether it has also been run inside Eplan, and on which versions.

## Using these examples

- The code is [MIT licensed](LICENSE): use it, change it, and share it, including in your own work.
- Try each script on a copy of a project first. Scripts run inside Eplan and can change project data.
- The code is provided as is, without warranty. Read a script before you run it.

## Documentation

- [Getting started](docs/getting-started.md)
- [C# basics used in these examples](docs/csharp-basics.md)
- [How Eplan loads scripts](docs/how-eplan-loads-scripts.md)
- [Troubleshooting](docs/troubleshooting.md)

## Contributing

Questions and ideas go in [Discussions](../../discussions) and Issues. Pull requests are welcome too, with one rule: only submit your own original work. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE).

---

Maintained by Justin Walz, [Walz Engineering](https://www.walzengineering.com).
