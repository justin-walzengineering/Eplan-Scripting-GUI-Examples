# How Eplan loads scripts

> This page records what has been **verified** in Eplan 2026.0.3. Items marked _to verify_ are working assumptions that will be confirmed and updated as examples are tested.

## The basics

- A script is a `.cs` source file. Eplan compiles it when you run or load it. There is no separate build or installer step. **Run** (File > Extras > Interfaces > Scripts) executes it once; **Load** registers it so it stays available, and Eplan registers loaded scripts again at the next start. See [How to run a script](running-scripts.md).
- Eplan exposes its features to scripts through the public API assemblies in its `Bin` folder (for example `Eplan.EplApi.Baseu`, `Eplan.EplApi.AFu`, `Eplan.EplApi.Guiu`, `Eplan.EplApi.DataModelu`).
- The 2026 API assemblies target **.NET Framework 4.8.1** (verified from the installs).
- A script marks the method Eplan should run with the `[Start]` attribute. Other attributes let scripts register actions, menu items, or event handlers. The examples introduce each one when needed. _Exact attribute usage per example will be confirmed during testing._

## What the script compiler references (to verify)

Community reports say Eplan's script compiler does **not** reference the data model and services assemblies, so a script that names those types directly fails to compile (error CS0234). Scripts that need them reach the types by reflection. It is also reported that Eplan adds some of its own namespaces to every script, which can make a short name such as `EventHandler` ambiguous (error CS0104). Write `System.EventHandler` in full to avoid it. These are not yet confirmed for 2026 in this repo; the local compile-check is set up to follow them so the examples stay safe either way.

## Compiler and language version

_To verify:_ which C# language version Eplan's script compiler accepts. Until confirmed, the examples use only C# 5 syntax (no string interpolation, no `?.`, no `out var`), which is accepted by any compiler Eplan could reasonably use. This page will be updated with the result.

## Where scripts live

You can keep scripts anywhere you like. Many people use a shared folder so a team can run the same files. Scripts you register with **Load** are registered again each time Eplan starts, so keep those files where they will stay.

## Using a regular Visual Studio project alongside

A local project that references the Eplan DLLs gives you IntelliSense and a quick compile check. It is optional and never shared. See [Getting started](getting-started.md).

## Safety tips

- Test on a copy of a project.
- Scripts run with your permissions inside Eplan and can change project data. Only run scripts you've read and trust.
