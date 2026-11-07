# 12 - "Please wait" window on its own thread

**Level:** Advanced  **File:** [PleaseWait.cs](PleaseWait.cs)  **Status:** DRAFT, not yet run in Eplan

## What it does

Shows a small "Please wait" window with a moving bar, does slow work, then closes the window. Because the window runs on **its own thread**, the bar keeps moving even while Eplan's own screen is busy and cannot redraw.

```csharp
PleaseWaitWindow wait = PleaseWaitWindow.Open("Working on it...");
try
{
    // slow work
}
finally
{
    wait.Close();
}
```

## What you will learn

- Why a normal window freezes when your script is busy
- Starting a **thread** and giving it its own window and message loop
- Using a `ManualResetEvent` so one thread can wait until another is ready
- Why a window must be closed by **its own thread** (`BeginInvoke`)
- Using `try` / `finally` so the window can never be left on screen

## How to run it

1. Copy `PleaseWait.cs` to a folder on your computer.
2. In Eplan, go to File > Extras > Interfaces, choose **Run** (under Scripts), and select the file.
3. Watch the bar move for about four seconds, then see the final message.

New to scripts? See [How to run a script](../../docs/running-scripts.md).

## Good to know

- Use this when you do **one long piece of work** that blocks Eplan. If your work happens in steps you control, Eplan's own progress window (example 02) is simpler.
- Always close the window **before** showing a message, or the message can appear behind it.
- Always close it in `finally`.
- Do not touch Eplan's data from the window's thread. It is only for showing text and a bar.
- Time Eplan spends compiling a script before it starts cannot be covered by the script itself.

## Compatibility

| Eplan version | Compiles | Run in Eplan |
|---------------|----------|--------------|
| 2026.0.3 | Yes | Pending |

The open, close and double-close behavior was also tested outside Eplan.

## Ideas to extend

- Pass a changing text, such as "Step 2 of 5", with a method that updates the label (using `BeginInvoke`).
- Centre the window on Eplan using example 03.
- Match Eplan's color scheme using example 04.
