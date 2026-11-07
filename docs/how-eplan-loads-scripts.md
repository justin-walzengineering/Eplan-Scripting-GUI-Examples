# How Eplan loads scripts

> This page records what has been **verified** in Eplan 2026.0.3 (Update 5) by running small test scripts. Anything not yet tested is marked _to verify_.

## The basics

- A script is a `.cs` source file. Eplan compiles it when you run or load it. There is no separate build or installer step. **Run** (File > Extras > Interfaces > Scripts) executes it once; **Load** registers it so it stays available, and Eplan registers loaded scripts again at the next start. See [How to run a script](running-scripts.md).
- Eplan exposes its features to scripts through the public API assemblies in its `Bin` folder (for example `Eplan.EplApi.Baseu`, `Eplan.EplApi.AFu`, `Eplan.EplApi.Guiu`, `Eplan.EplApi.DataModelu`).
- The 2026 API assemblies target **.NET Framework 4.8.1** (verified from the installs).
- A script marks the method Eplan should run with the `[Start]` attribute. Other attributes let scripts register actions, menu items, or event handlers. The examples introduce each one when needed. The `[Start]` method was confirmed in every example that was run.

## What a script can reference

Eplan's 2026 documentation lists what scripts can use without extra references: `System`, `System.Drawing`, `System.Windows.Forms`, `System.Net.Http`, `System.IO.Compression.FileSystem`, `System.Xml.Linq`, `System.Xml`, `System.Core` and **Newtonsoft.Json**, plus the Eplan **Base**, **ApplicationFramework**, **Gui** and **MasterData** assemblies. The data model and services assemblies are not on the list, and extra references cannot be added from a script. Scripts that need the data model reach it by reflection: see [Reaching Eplan's data model](reaching-the-data-model.md).

### Confirmed by test scripts in Eplan 2026

| Question | Result |
|----------|--------|
| Can a script name a data model type such as `Eplan.EplApi.DataModel.Project` directly? | **No.** The script does not compile. Use reflection (see the guide above). The data model is nevertheless already loaded inside Eplan, so reflection finds it. |
| Is the application framework namespace (`Eplan.EplApi.ApplicationFramework`, which holds `CommandLineInterpreter` and `ActionCallingContext`) usable directly? | **Yes.** Examples 17 to 20, 24, 26 and 19 use it. |
| Does `Newtonsoft.Json` work directly? | **Yes** (examples 08, 21). |
| Does the `MasterData` parts API work directly? | **Yes** (example 11). |
| Is `System.Windows.Forms` added to every script? | **Yes.** `MessageBox.Show("x")` compiled with no `using` line for it. |
| Is `System.IO` added to every script? | **No.** `Path.GetTempPath()` without `using System.IO;` did not compile. |
| Is a bare `EventHandler` a problem when `using System;` is present? | **Yes, it did not compile**, while `System.EventHandler` written in full did. Eplan has its own `EventHandler` name. |

The compile errors were seen as a script that did not run; the exact message text was not captured, so the two rows that say "did not compile" are based on the script not running while an identical script without that one change ran.

The local compile-check still adds a longer list of namespaces than the two confirmed above. That keeps it on the cautious side for name clashes, but it can accept a script that is missing a `using` line Eplan needs. Add the `using` lines you need.

## Compiler and language version

Eplan 2026 accepts **C# 5 syntax only**. Test scripts using string interpolation (`$"..."`), the null-conditional `?.` and `nameof` each failed to run, while the same script with a plain `out` variable declared on its own line, `var`, and `System.Linq` ran fine. Write C# 5. Those three features were tested one by one; other newer features (such as `out var` and expression-bodied members) were not tried individually, so keep avoiding them.

## Where scripts live

You can keep scripts anywhere you like. Many people use a shared folder so a team can run the same files. Scripts you register with **Load** are registered again each time Eplan starts, so keep those files where they will stay.

## Using a regular Visual Studio project alongside

A local project that references the Eplan DLLs gives you IntelliSense and a quick compile check. It is optional and never shared. See [Getting started](getting-started.md).

## Safety tips

- Test on a copy of a project.
- Scripts run with your permissions inside Eplan and can change project data. Only run scripts you've read and trust.
