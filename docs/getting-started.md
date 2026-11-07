# Getting started

This guide takes you from "I have Eplan" to "I ran my first script". No programming experience required.

## What you need

- Eplan Electric P8 2025 or 2026 installed.
- A text editor. Any will do, but **Visual Studio Community** (free) or **Visual Studio Code** gives you color, hints, and error underlines, which helps a lot while learning.
- A throwaway demo project in Eplan. Practice on a copy, never on live customer data.

## What is an Eplan script?

A script is a plain text file ending in `.cs` (C# source code). You don't build or install anything. You tell Eplan to **run** the file, and Eplan compiles it on the spot and executes it. See [How Eplan loads scripts](how-eplan-loads-scripts.md) for the details.

## Run your first script

1. Download or copy an example `.cs` file from the [examples](../examples/) folder to a folder on your computer, for example `C:\EplanScripts\`.
2. Open Eplan and open your demo project.
3. Choose **File > Extras > Interfaces**. Under **Scripts**, choose **Run**, then pick the `.cs` file. (Step-by-step, with screenshots: [How to run a script](running-scripts.md).)
4. If the script adds a button or window, use it.
5. If Eplan reports an error, read the message. It usually names the line that has the problem. See [Troubleshooting](troubleshooting.md).

Tip: add **Run script** to your Quick Access toolbar so it is one click away. [How to run a script](running-scripts.md) shows how.

## Edit and re-run

Change the file in your editor, save it, and run it again in Eplan. There is no build step.

## Set up Visual Studio for IntelliSense (optional)

Eplan doesn't need Visual Studio, but a project that points at the Eplan assemblies gives you auto-complete and error checking while you type:

1. Create a **Class Library (.NET Framework)** project targeting **.NET Framework 4.8.1**.
2. Add references to the Eplan assemblies in your install folder, for example `C:\Program Files\EPLAN\Platform\2026.0.3\Bin\Eplan.EplApi.Baseu.dll` (the folder number matches your version), `Eplan.EplApi.AFu.dll`, `Eplan.EplApi.Guiu.dll`, and `Eplan.EplApi.DataModelu.dll`. Set **Copy Local** to **False**.
3. Add `System.Windows.Forms` and `System.Drawing` references.
4. Add the example `.cs` file to the project.

This project is only for your editor. You still run the plain `.cs` file in Eplan. Never share or commit the Eplan DLLs.

## Next steps

- Read [C# basics used in these examples](csharp-basics.md).
- Open example 01 and change a label or button text, then reload and see the result.
