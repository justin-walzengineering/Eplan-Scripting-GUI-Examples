# 06 - Remember window size and position

**Level:** Intermediate  **File:** [RememberWindow.cs](RememberWindow.cs)  **Status:** Run in Eplan 2026

![The remembering demo window](screenshot-window.png)

## What it does

Opens a resizable demo window. Move it, resize it, maximize it, then close it. The next time you run the script, it reopens where you left it. A **Forget saved position** button clears the saved position.

It is also safe: if the saved position is no longer on any screen (for example you unplugged a monitor), the window opens in the center of the screen instead of out of reach.

The reusable part is the `WindowMemory` class. Copy it into your own scripts, then add one line after you set a form up:

```csharp
WindowMemory.Attach(myForm, "MyWindowName");
```

## What you will learn

- Saving small per-user settings in the Windows **registry**, without administrator rights
- Reacting to the form's **FormClosed** event
- Why a maximized window must save `RestoreBounds`, not `Bounds`
- Checking the saved spot against all the **screens** that exist now
- Writing code that fails gracefully, because remembering a position is only a convenience

## How to run it

1. Copy `RememberWindow.cs` to a folder on your computer.
2. In Eplan, go to File > Extras > Interfaces, choose **Run** (under Scripts), and select the file.
3. Move and resize the window, close it, and run the script again.

New to scripts? See [How to run a script](../../docs/running-scripts.md).

## Good to know

- Values are stored under `HKEY_CURRENT_USER\Software\EplanScriptExamples\Windows\<key>`. Give each of your windows its own key.
- The window is saved when it closes normally. A window that is closed by a crash saves nothing.
- Example 03 shows how to center a window on Eplan the first time, before anything is saved.

## Compatibility

| Eplan version | Compiles | Run in Eplan |
|---------------|----------|--------------|
| 2026.0.3 | Yes | Yes (saved, restored, off-screen fallback, maximized, monitors on both sides, Forget then Close) |

Checked in Eplan 2026: the window reopened at the saved position and size after being moved and resized, opened centered when the saved position was far off every screen (a stand-in for an unplugged monitor), and the saved entry was removed when 'Forget saved position' was clicked. A later run (2026-11-07) added three cases. Maximized: the window was moved, maximized and closed, and the next run opened at the saved normal size and position, not maximized. Second monitor: after the window was moved to a monitor on the right and closed, it reopened at the same spot; the same happened on a monitor to the left, where the position has a negative X value. Found in that run: closing the window after clicking 'Forget saved position' saved the position again, so the next run opened at the last position instead of the center. The script was changed so that a window that was forgotten is not saved when it closes, and it was run again in Eplan: after Forget and Close nothing was saved, the next run opened centered, and closing that window saved its position again as before. The test used a PC with three monitors; the window was dragged between them, and no monitor was unplugged.

The save and restore logic, including an off-screen saved position, was also tested outside Eplan.

## Ideas to extend

- Remember the position of a splitter or the width of a grid column too.
- Store the values in the JSON settings file from example 08 instead of the registry.
- Remember whether the window was maximized.
