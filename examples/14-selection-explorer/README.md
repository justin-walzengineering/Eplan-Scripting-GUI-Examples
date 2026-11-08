# 14 - Selection explorer

**Level:** Intermediate  **File:** [SelectionExplorer.cs](SelectionExplorer.cs)  **Status:** Run in Eplan 2026 (pages selected in the page navigator)

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

1. Open a project in Eplan. **Select** something first: a page in the page navigator, or a device in the drawing.
2. Copy `SelectionExplorer.cs` to a folder on your computer.
3. Go to File > Extras > Interfaces, choose **Run** (under Scripts), and select the file. It works best from a ribbon button or the Quick Access toolbar, so the selection is kept.
4. Choose where to save the text file.

New to scripts? See [How to run a script](../../docs/running-scripts.md).

## Good to know

- Properties without a value are left out, to keep the report short.
- A report is limited to 100 objects.
- If you see "Nothing is selected" even though something is, try running from a ribbon button or the Quick Access toolbar instead of the File menu, so the drawing page keeps focus.
- This example uses reflection and is **likely to need a small adjustment** after the first run. Please report any error message exactly.

## Compatibility

| Eplan version | Compiles | Run in Eplan |
|---------------|----------|--------------|
| 2026.0.3 | Yes | Partly (three selected pages written to a text file) |

Checked in Eplan 2026: with three pages selected in the page navigator, the script wrote one block per page to a text file and opened it. Not working yet from a drawing selection: after selecting everything on an open page with Ctrl+A, the script reported that nothing was selected, both when started from File > Extras > Interfaces and from the command search box, so for now select pages (or other items) in a navigator before running it. A second run (2026-11-07) clicked one text object on the open page, which enabled Cut, Copy and Delete in the ribbon, and started the script from File > Extras > Interfaces: it again reported that nothing was selected. Running it from a Quick Access toolbar button, as suggested above, has not been tried. The file lists project data such as the last editor's name and e-mail, so there is no screenshot here; check a file before you share it.

The member names used were checked against the installed 2026 assemblies. The reflection helper was tested outside Eplan against stand-in objects.

## Ideas to extend

- Offer a choice of "only properties with a value" and "all properties".
- Save as CSV instead of text.
- Add the cross-references of each selected object.
