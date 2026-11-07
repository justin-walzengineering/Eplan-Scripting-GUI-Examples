# 18 - Device list export (CSV, XML or text)

**Level:** Intermediate  **File:** [DeviceListExport.cs](DeviceListExport.cs)  **Status:** DRAFT, not yet run in Eplan

## What it does

A small window that exports the **current project's device list** with Eplan's own `devicelist` action. Choose the folder, build the file name from `{project}`, `{date}` and `{time}`, pick a format (**CSV**, **XML** or **Text**), and optionally open the result straight away. A CSV file opens in Excel, which makes it easy to filter, sort and share the list.

It remembers your choices, and asks before replacing a file that already exists.

## What you will learn

- Running the `devicelist` action with `TYPE=EXPORT`, `EXPORTFILE` and `FORMAT`
- Filling a drop-down from a small table of choices (a text, an exporter name and an extension)
- Opening a file with the program Windows has set for it (`UseShellExecute`)
- Building a file name from a pattern safely

## How to run it

1. Open a **copy of a project** in Eplan.
2. Copy `DeviceListExport.cs` to a folder on your computer.
3. Go to File > Extras > Interfaces, choose **Run** (under Scripts), and select the file.
4. Choose CSV and press **Export**. Excel (or your default program) opens the list.

New to scripts? See [How to run a script](../../docs/running-scripts.md).

## Good to know

- The three format names are the ones Eplan documents for this action. Your Eplan may also have a custom format; you can add it to the `DeviceListFormat.All` table.
- The project name comes from Eplan's path variable `$(PROJECTNAME)`. If it is not supplied, the name shows `Project`.
- Example 24 imports an edited CSV back into the device list.
- CSV files from Eplan are usually semicolon-separated. Excel normally opens them correctly when double-clicked.

## Compatibility

| Eplan version | Compiles | Run in Eplan |
|---------------|----------|--------------|
| 2026.0.3 | Yes | Pending |

## Ideas to extend

- Add a check box "Only the selected devices".
- Offer a custom exporter format by name.
- Open the folder instead of the file.
