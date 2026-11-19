# 14 - Selection explorer

**Level:** Intermediate  **File:** [SelectionExplorer.cs](SelectionExplorer.cs)  **Status:** Run in Eplan 2026 and 2027 (pages selected in a navigator; the open page as a fallback)

*Read-only: this example never changes your project.*

## What it does

Writes the properties of **everything you have selected** in Eplan to a text file, then opens it. Each selected object gets a heading and one line per property that has a value: the property **ID**, its **name** and its **value**.

Use it to learn what an object holds, to find the number of a property you want to read or write in a script, or to attach to a question when you ask for help.

## What you will learn

- Reading the **current selection**, and why it comes back empty unless you unlock it
- Reaching the data model by reflection (see example 09)
- Building a text report with a `StringBuilder`, with aligned columns
- Saving with a `SaveFileDialog` and opening the result with its default program

## How to run it

1. Open a project in Eplan. **Select** something first: one or more pages in the page navigator. If nothing comes back as selected, the script offers to use the page that is open in the editor.
2. Copy `SelectionExplorer.cs` to a folder on your computer.
3. Go to File > Extras > Interfaces, choose **Run** (under Scripts), and select the file. A Quick Access button (see the running scripts guide) works too.
4. Choose where to save the text file.

New to scripts? See [How to run a script](../../docs/running-scripts.md).

## Good to know

- Properties without a value are left out, to keep the report short.
- A report is limited to 100 objects.
- **Groups:** on a schematic page, one click on a symbol selects a *group*, and a group has almost no properties of its own. The script therefore also writes the objects inside a group (indented, marked "inside a group"). Expect many `Line` and `Arc` blocks, because the drawing of the symbol is part of it; the `Function` blocks hold the device tag and the rest of the symbol's data. Groups are only read, never ungrouped, and a group inside a group inside a group is not opened further.
- Objects selected directly on an open drawing page: in the latest test (Eplan 2026) a selected line on a panel layout page was returned and written to the report. In earlier tests, selections made with Ctrl+A or by clicking one text object were not returned, so this may depend on how the object was selected. When nothing is found, the script asks whether to write the page that is open in the editor instead.
- This example uses reflection and is **likely to need a small adjustment** after the first run. Please report any error message exactly.

## Compatibility

| Eplan version | Compiles | Run in Eplan |
|---------------|----------|--------------|
| 2026.0.3 | Yes | Partly (three selected pages written to a text file) |
| 2027.0.3 | Yes | Yes (a mounting panel selected in a 3D layout, 2026-11-17; a symbol selected on a schematic page, with its group opened, 2026-11-18) |

Checked in Eplan 2026: with three pages selected in the page navigator, the script wrote one block per page to a text file and opened it. Not working from a drawing selection: after selecting everything on an open page with Ctrl+A, and later after clicking one text object (Cut and Copy were enabled in the ribbon), the script reported that nothing was selected when started from File > Extras > Interfaces, from the command search box, and from a Quick Access toolbar button (the Run script button). A change was then made so that, when nothing is selected, the script asks 'Write the properties of the page that is open in the editor instead?'. Run from the Quick Access button with the answer Yes, it wrote a report with one block for the open page. That path was tried once, with one page. The file lists project data such as the last editor's name and e-mail, so there is no screenshot here; check a file before you share it.

A later run in Eplan 2026 (2026-11-18): a line was drawn on an empty panel layout page and left selected, and the script was started from File > Extras > Interfaces > Run. It did not ask the "nothing selected" question; it went straight to the Save dialog, and the report held one block for the Line (object ID, start and end coordinates, page, layer). Not tried yet: several objects at once.

On a schematic page (Eplan 2027.0.3, 2026-11-18, an Eplan sample project, one click on a transformer symbol) the first version of the script wrote only one block, a `Group` with just the object ID and change protection. After the script was changed to read the group's `SubPlacements`, the same click gave 66 blocks: the group, 33 `Arc` and 23 `Line` objects (the drawing), and 9 `Function` objects (the transformer and its connection points, each with its device tag). This was run in Eplan 2027 only; the change compiles for 2026, but the group case was not run there.

The member names used were checked against the installed 2026 assemblies. The reflection helper was tested outside Eplan against stand-in objects.

## Ideas to extend

- Offer a choice of "only properties with a value" and "all properties".
- Save as CSV instead of text.
- Add the cross-references of each selected object.
