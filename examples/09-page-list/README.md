# 09 - Page list with search, sort and CSV export

**Level:** Advanced  **File:** [PageList.cs](PageList.cs)  **Status:** DRAFT, not yet run in Eplan

*Read-only: this example never changes your project.*

## What it does

Lists every page of the open project (name, description and page type) in a table. Type words in the search box to narrow the list, click a column heading to sort (click again to reverse), and press **Save as CSV...** to write the pages that are currently shown to a file that Excel opens directly.

## What you will learn

- **Reflection**: how a script reaches Eplan's data model (`Project`, `Page`) when it cannot name those types directly (see the box below)
- Filtering a table by **several words** at once
- A `ListView` with **sortable columns**
- Writing a **CSV** file correctly: quoting cells that contain a delimiter, a quote or a line break
- Wrapping every risky read, so one odd page cannot stop the whole list

## Why reflection?

Eplan's script compiler does not reference the data model assemblies, so a script cannot write `Page` or `Project` as a type name. The data model is still loaded inside Eplan, though. The helper class at the end of the file finds the types by their text names and reads their members by name while the script runs. You can copy that helper into your own scripts.

## How to run it

1. Open a project in Eplan.
2. Copy `PageList.cs` to a folder on your computer.
3. Go to File > Extras > Interfaces, choose **Run** (under Scripts), and select the file.

New to scripts? See [How to run a script](../../docs/running-scripts.md).

## Good to know

- Page names and descriptions are read through the data model. A page with an empty description shows an empty cell.
- A project with thousands of pages can take a few seconds to load.
- This is the example **most likely to need a small adjustment** after the first run in Eplan, because it depends on member names found by reflection. If you see an error, please report the exact message.

## Compatibility

| Eplan version | Compiles | Run in Eplan |
|---------------|----------|--------------|
| 2026.0.3 | Yes | Pending |

The member names used (`Project.Pages`, `Page.Name`, `Page.PageTypeName`, `PAGE_NOMINATIOMN`) were checked against the installed 2026 assemblies. The reflection helper was tested outside Eplan against stand-in objects.

## Ideas to extend

- Add structure columns (higher-level function, location).
- Select a row to jump to that page in Eplan.
- Export to Excel with columns for revision and date.
