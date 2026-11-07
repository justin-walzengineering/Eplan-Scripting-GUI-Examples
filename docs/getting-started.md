# Getting started

This guide takes you from "I have Eplan" to "I ran my first script". No programming experience required.

## What you need

- Eplan Electric P8 2025 or 2026 installed.
- A text editor. Any will do, but **Visual Studio Community** (free) or **Visual Studio Code** gives you color, hints, and error underlines, which helps a lot while learning.
- A throwaway demo project in Eplan. Practice on a copy, never on live customer data.

## What is an Eplan script?

A script is a plain text file ending in `.cs` (C# source code). You don't build or install anything. You tell Eplan to **load** the file, and Eplan compiles it on the spot and makes it available to run. See [How Eplan loads scripts](how-eplan-loads-scripts.md) for the details.

## Run your first script

1. Download or copy an example `.cs` file from the [examples](../examples/) folder to a folder on your computer, for example `C:\EplanScripts\`.
2. Open Eplan and open your demo project.
3. Find the scripting commands in the Eplan menus (look for **Utilities > Scripts**; names can differ slightly between versions and layouts).
4. Choose **Load**, and pick the `.cs` file.
5. If it has a start method, choose **Run** (or use the button the script adds).
6. If Eplan reports an error, read the message. It usually names the line that has the problem. See [Troubleshooting](troubleshooting.md).

## Edit and re-run

Change the file in your editor, save it, and load it again in Eplan. There is no build step.

## Set up Visual Studio for IntelliSense (optional)

Eplan doesn't need Visual Studio, but a project that points at the Eplan assemblies gives you auto-complete and error checking while you type:

1. Create a **Class Library (.NET Framework)** project targeting **.NET Framework 4.8.1**.
2. Add references to the Eplan assemblies in your install folder, for example `C:\Program Files\EPLAN\Platform\2026.0.3\Bin\Eplan.EplApi.Baseu.dll` (the folder number matches your version), `Eplan.EplApi.AFu.dll`, `Eplan.EplApi.Guiu.dll`, and `Eplan.EplApi.DataModelu.dll`. Set **Copy Local** to **False**.
3. Add `System.Windows.Forms` and `System.Drawing` references.
4. Add the example `.cs` file to the project.

This project is only for your editor. You still load the plain `.cs` file in Eplan. Never share or commit the Eplan DLLs.

## Next steps

- Read [C# basics used in these examples](csharp-basics.md).
- Open example 01 and change a label or button text, then reload and see the result.
