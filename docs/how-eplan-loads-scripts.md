# How Eplan loads scripts

> This page records what has been **verified** in Eplan 2025.0.3 and 2026.0.3. Items marked _to verify_ are working assumptions that will be confirmed and updated as examples are tested.

## The basics

- A script is a `.cs` source file. Eplan compiles it when you load it. There is no separate build or installer step.
- Eplan exposes its features to scripts through the public API assemblies in its `Bin` folder (for example `Eplan.EplApi.Baseu`, `Eplan.EplApi.AFu`, `Eplan.EplApi.Guiu`, `Eplan.EplApi.DataModelu`).
- The 2025 and 2026 API assemblies both target **.NET Framework 4.8.1** (verified from the installs).
- A script marks the method Eplan should run with the `[Start]` attribute. Other attributes let scripts register actions, menu items, or event handlers. The examples introduce each one when needed. _Exact attribute usage per example will be confirmed during testing._

## Compiler and language version

_To verify:_ which C# language version Eplan's script compiler accepts. Until confirmed, the examples use only C# 5 syntax (no string interpolation, no `?.`, no `out var`), which is accepted by any compiler Eplan could reasonably use. This page will be updated with the result.

## Where scripts live

You can keep scripts anywhere you like. Many people use a shared folder so a team can load the same files. _To verify:_ whether Eplan 2026 remembers loaded scripts between sessions in your configuration.

## Using a regular Visual Studio project alongside

A local project that references the Eplan DLLs gives you IntelliSense and a quick compile check. It is optional and never shared. See [Getting started](getting-started.md).

## Safety tips

- Test on a copy of a project.
- Scripts run with your permissions inside Eplan and can change project data. Only run scripts you've read and trust.
