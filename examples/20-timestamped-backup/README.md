# 20 - Timestamped backup

**Level:** Intermediate  **File:** [TimestampedBackup.cs](TimestampedBackup.cs)  **Status:** DRAFT, not yet run in Eplan

## What it does

A small window that backs up the **current project** with Eplan's own `backup` action, using a file name with the date and time (for example `MyProject_2026-11-07_141530`) so every backup is a new file and none is overwritten. Choose the folder, add a comment, and decide whether to include external documents and images. When it finishes, Explorer opens with the backup file selected.

Losing work to a crash or a power cut is one of the most common stories in the Eplan community. A one-click backup with a clear name makes "back up before I try something" a habit.

## What you will learn

- Running the `backup` action with `TYPE`, `ARCHIVENAME`, `DESTINATIONPATH`, `COMMENT`, `INCLEXTDOCS` and `INCLIMAGES`
- Building a **unique, safe file name** from a pattern
- Following a naming rule of the action (an archive name may not end in a dot and three digits)
- Finding a file when you do not know its extension (`Directory.GetFiles` with `*`)

## How to run it

1. Open a project in Eplan.
2. Copy `TimestampedBackup.cs` to a folder on your computer.
3. Go to File > Extras > Interfaces, choose **Run** (under Scripts), and select the file.
4. Check the folder and name, then press **Back up**.

New to scripts? See [How to run a script](../../docs/running-scripts.md).

## Good to know

- Put the backup folder on a drive that is itself backed up, and not on the same disk as your projects.
- The project name comes from Eplan's path variable `$(PROJECTNAME)`. If it is not supplied, the name shows `Project`.
- All dots are removed from the name on purpose. Eplan does not allow an archive name ending in a dot and three digits, because it uses that form for split backups.
- A backup is only as good as your last test of restoring it. Try opening one now and then.

## Compatibility

| Eplan version | Compiles | Run in Eplan |
|---------------|----------|--------------|
| 2026.0.3 | Yes | Pending |

The name pattern and saved options were tested outside Eplan. The backup itself needs a run in Eplan.

## Ideas to extend

- Keep only the newest ten backups and delete older ones (ask first).
- Run a backup automatically before another script (see example 26).
- Add a "Back up master data" option (`TYPE=MASTERDATA`).
