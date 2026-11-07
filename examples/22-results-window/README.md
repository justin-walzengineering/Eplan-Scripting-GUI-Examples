# 22 - Results window

**Level:** Intermediate  **File:** [ResultsWindow.cs](ResultsWindow.cs)  **Status:** DRAFT, not yet run in Eplan

## What it does

Pretends to check twelve items, collects a line for each result, and shows **everything once** at the end in a window that follows Eplan's color scheme. The user can scroll, **Copy** the whole report to the clipboard, or **Save...** it as a text file.

This replaces the habit of showing a message box after every item, which is slow to click through and impossible to copy.

```csharp
ResultLog log = new ResultLog();
log.Ok("Item 01: checked");
log.Warning("Item 03: the value looks unusual");
log.Error("Item 05: a required value is missing");
ResultsReport.Show("My check", log);
```

## What you will learn

- A small **log class** that collects lines and counts them
- Showing a **report window** with a summary line and a scrollable, fixed-width text box
- Using the **clipboard** (`Clipboard.SetText`) and a **SaveFileDialog**
- Following Eplan's color scheme (see example 04)
- Why one report beats many message boxes

## How to run it

1. Copy `ResultsWindow.cs` to a folder on your computer.
2. In Eplan, go to File > Extras > Interfaces, choose **Run** (under Scripts), and select the file.
3. Look at the summary, press **Copy**, paste somewhere to check it, then try **Save...**.

New to scripts? See [How to run a script](../../docs/running-scripts.md).

## Good to know

- The summary turns red when there is at least one error.
- The window is resizable. The text is in a fixed-width font so columns line up.
- Other examples in this repository (such as the export and backup ones) can use this window to report what they did.

## Compatibility

| Eplan version | Compiles | Run in Eplan |
|---------------|----------|--------------|
| 2026.0.3 | Yes | Pending |

## Ideas to extend

- Color each line by its level (use a `RichTextBox`).
- Add a "Show only errors" check box.
- Add a column for the page or device name, and save the report as CSV.
