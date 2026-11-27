# Troubleshooting

## The script won't load / compile error

- Read the message. It normally gives a line number and what's wrong.
- Check for a missing semicolon `;`, a missing closing brace `}`, or a typo in a name. C# is case-sensitive: `Form` is not `form`.
- Check the `using` lines at the top. A missing one causes "type or namespace could not be found".
- If you used newer C# syntax, try a simpler form. See [How Eplan loads scripts](how-eplan-loads-scripts.md).

## Nothing happens when I run it

- Confirm the script has a start method and that you chose **Run** (File > Extras > Interfaces > Scripts), not only **Load**.
- Make sure a project is open if the script needs one.

## My window appears behind Eplan or won't close

- Show dialogs with `ShowDialog()` so they stay on top and wait for the user.
- Always dispose or close forms you create.

## Eplan freezes during a long task

- Long work blocks the screen. Use a progress bar (see the progress example) and keep each step short.

## Changes I made aren't showing up

- Save the file, then **run it again** in Eplan so it recompiles. If you registered the script with **Load**, unload it and load it again to be safe.

## A script that uses the data model says "Exception has been thrown by the target of an invocation"

That message only wraps the real one. Look at the inner exception, or show `ex.InnerException.Message`. Two real causes were found in Eplan 2026: "Failed to generate an object of the LockingStep class" (set `LockSelectionByDefault` to `false` before asking for the project) and "Ambiguous match found" (read properties by scanning the list instead of asking for one by name). See [Reaching the data model](reaching-the-data-model.md).

## My script does not run and nothing is shown

A script that does not compile can simply not run. Check the usual suspects: newer C# syntax (Eplan 2026 accepts C# 5 only), a missing `using System.IO;`, a bare `EventHandler` (write `System.EventHandler`), or a direct use of a data model type. [How Eplan loads scripts](how-eplan-loads-scripts.md) lists what was tested.

## The export action ignored my file name

In Eplan 2026 the PDF export action named the file itself (project name, date and time) whatever name was passed in. Example 17 finds the file the action just made and renames it. If you rely on a name, do the same.

## "Nothing is selected" although I selected things

Objects selected on an open drawing page did not reach a script in the tests (example 14). Pages selected in the page navigator did.

## Still stuck?

Open an Issue with your Eplan version, what you did, and the exact error text. Please remove any customer or project data first.
