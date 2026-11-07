# Eplan Scripting GUI Examples

Free, original C# script examples that add **custom windows and tools** to Eplan Electric P8 (2025 and 2026), written for people who are new to C# and Eplan, with room to grow into more advanced data-driven interfaces.

> **Unofficial community resource.** This project is not affiliated with, endorsed by, or supported by EPLAN. "EPLAN" is a trademark of its owner. All code here is original work and uses only the publicly available Eplan scripting API. No Eplan-owned code, documentation, or training material is included.

## Why this exists

Eplan's own screens are great, but sometimes you want to see and edit your data in a way they don't offer: a filterable grid of project properties, a quick input dialog, a progress bar for a long task. Eplan scripting lets you build those screens yourself. Good English-language examples are hard to find, so this repo collects small, well-explained ones you can read, run, and adapt.

## Who it's for

- **Beginners** to C# and Eplan scripting. Each beginner example explains the basic C# it uses.
- **Experienced script writers** who want ready-made GUI patterns (dialogs, grids, progress, settings, ribbon and context-menu launchers).

## Quick start

1. Read [docs/getting-started.md](docs/getting-started.md).
2. Open an example folder under [examples/](examples/).
3. Load the `.cs` file in Eplan (Utilities > Scripts) and run it.

## Examples

| # | Example | What you learn | Level |
|---|---------|----------------|-------|
| 01 | [Reusable input dialog](examples/01-input-dialog/) | Windows Forms basics, returning user input | Beginner |
| 02 | _Progress bar_ (planned) | Showing progress for long actions | Beginner |
| 03 | _Project property viewer/editor_ (planned) | Data grid, reading and writing properties | Intermediate |
| 04 | _Page list with search and CSV export_ (planned) | Sorting, filtering, exporting | Intermediate |
| 05 | _Parts viewer with filter_ (planned) | Working with part data in a grid | Intermediate |
| 06 | _Ribbon and context-menu launcher_ (planned) | Adding your own buttons to Eplan | Intermediate |
| 07 | _Settings dialog_ (planned) | Saving and restoring user preferences | Intermediate |

See [IDEAS.md](IDEAS.md) for what's planned next.

## Tested with

Compile-checked against Eplan Electric P8 **2025** (2025.0.3) and **2026** (2026.0.3). Both use the .NET Framework 4.8.1 API. Each example's README lists whether it has also been run inside Eplan.

## Documentation

- [Getting started](docs/getting-started.md)
- [C# basics used in these examples](docs/csharp-basics.md)
- [How Eplan loads scripts](docs/how-eplan-loads-scripts.md)
- [Troubleshooting](docs/troubleshooting.md)

## Contributing

Questions, bug reports, and ideas are welcome through Issues and Discussions. Pull requests are welcome too, with one rule: only submit your own original work. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE). Use it, change it, share it.

---

Maintained by Justin Walz, [Walz Engineering](https://www.walzengineering.com).
