# 27 - Compile canary

**Level:** Beginner  **File:** [CompileCanary.cs](CompileCanary.cs)  **Status:** Run in Eplan 2026

## What it does

A tiny template for every script you write. Its first statement writes a line with the current time to a log file in `Documents\EplanScriptLogs`. If a script fails to compile, Eplan does not show an error, so the script just seems to do nothing. A log file that did not appear (or did not change) tells you the script never started.

## What you will learn

- Why a script that does not compile gives no message
- Writing a log file as the first statement
- Writing a line before each step, so you can see how far a script got
- Catching an error while the script runs (a different problem from a compile error)

## How to run it

1. Copy `CompileCanary.cs` to a folder on your computer.
2. In Eplan, go to File > Extras > Interfaces, choose **Run** (under Scripts), and select the file.
3. A message shows the log path. Open the log: it has a "started" line and "step 1".
4. To see the canary work, remove the two slashes in front of `int broken = ;` and run it again. Nothing appears, and the log keeps the old time.

New to scripts? See [How to run a script](../../docs/running-scripts.md).

## Usual causes when a script does nothing

- Newer C# syntax than Eplan accepts (this repository's notes list what was verified: string interpolation, `?.` and `nameof` each stop a script)
- A missing `using` line, for example `using System.IO;` for `File` and `Path`
- Naming a data model type directly instead of reaching it another way (see [Reaching the data model](../../docs/reaching-the-data-model.md))
- A name used twice, or a missing semicolon or bracket
- A file saved in an unusual encoding

## Compatibility

| Eplan version | Compiles | Run in Eplan |
|---------------|----------|--------------|
| 2026.0.3 | Yes | Yes (log written, message shown; broken copy: no message and the log did not change) |

Checked in Eplan 2026: the script wrote the log and showed the message. A copy with the `int broken = ;` line active ran silently: no message, and the log kept its old time. The same script from the same folder, without the broken line, ran normally right afterwards.

## Ideas to extend

- Add the script version to the first log line.
- Keep only the newest ten logs.
- Show the log path in a themed window (see example 07).
