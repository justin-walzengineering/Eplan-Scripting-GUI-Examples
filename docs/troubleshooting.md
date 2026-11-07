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

## Still stuck?

Open an Issue with your Eplan version, what you did, and the exact error text. Please remove any customer or project data first.
