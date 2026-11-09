# 14 - Selection explorer

**Level:** Intermediate  **File:** [SelectionExplorer.cs](SelectionExplorer.cs)  **Status:** Run in Eplan 2026 (pages selected in a navigator; the open page as a fallback)

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
- Objects selected directly on an open drawing page were not returned to the script during testing, whether it was started from the File menu or from a Quick Access button. When nothing is found, the script asks whether to write the page that is open in the editor instead.
- This example uses reflection and is **likely to need a small adjustment** after the first run. Please report any error message exactly.

## Compatibility

| Eplan version | Compiles | Run in Eplan |
|---------------|----------|--------------|
| 2026.0.3 | Yes | Partly (three selected pages written to a text file) |

Checked in Eplan 2026: with three pages selected in the page navigator, the script wrote one block per page to a text file and opened it. Not working from a drawing selection: after selecting everything on an open page with Ctrl+A, and later after clicking one text object (Cut and Copy were enabled in the ribbon), the script reported that nothing was selected when started from File > Extras > Interfaces, from the command search box, and from a Quick Access toolbar button (the Run script button). A change was then made so that, when nothing is selected, the script asks 'Write the properties of the page that is open in the editor instead?'. Run from the Quick Access button with the answer Yes, it wrote a report with one block for the open page. That path was tried once, with one page. The file lists project data such as the last editor's name and e-mail, so there is no screenshot here; check a file before you share it.

The member names used were checked against the installed 2026 assemblies. The reflection helper was tested outside Eplan against stand-in objects.

## Ideas to extend

- Offer a choice of "only properties with a value" and "all properties".
- Save as CSV instead of text.
- Add the cross-references of each selected object.
