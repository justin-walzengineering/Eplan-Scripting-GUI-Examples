# CLAUDE.md

Guidance for Claude Code (and human contributors) working in this repository.

## What this repo is

A free, public list of **original** C# script examples for Eplan Electric P8 2026, focused on GUI (Windows Forms) tools. Audience: beginners first, growing toward experienced script writers. Maintained by Justin Walz, Walz Engineering. It is a community resource, not self-promotion.

## Hard rules

1. **Original work only.** Never copy, paraphrase, or use as a template any Eplan-owned material: training books, class example code, the Eplan API help text, template zips. Write every example from scratch against the public API, using generic data.
2. **No Eplan files in the repo.** Never commit `Eplan.EplApi*.dll`, API XML docs, PDFs, zips, or project/template files. The `.gitignore` blocks these; do not work around it.
3. **No customer or employer material.** Scripts from client projects may inspire an idea, but only the concept travels. Rewrite from scratch with made-up data. No client names, project names, part numbers, or company data.
4. **Unofficial.** Keep the disclaimer in the README. Do not imply Eplan endorsement.
5. **English only.**
5a. **Positive tone toward Eplan.** Present scripting as one more way to build on an already excellent product. Never suggest Eplan is lacking, and never frame examples as fixing or replacing Eplan features. Do not describe the repo as filling a gap or as the only good English resource; it is simply a list of examples people can use.
6. **Low-key branding.** Credit appears as a short "Maintained by Justin Walz, Walz Engineering" footer, with one short line inviting people to get in touch about custom work, and in file headers. No other sales pitches.
7. **Never `git push`, create remotes, or publish** without Justin's explicit OK in that conversation.
8. **Blog community spotlights (see `blog/README.md`).** Each blog post carries a Community spotlight section, and a community highlight post (`type: highlight`) goes out only on a Friday and only when no new script post is ready. Link to the source and describe it in original words. Never copy others' text, thumbnails or screenshots, and never copy Eplan documentation or marketing images; only screenshots Justin took himself in his own Eplan. Keep it positive and specific, and only feature something whose link has been opened and marked `verified` in `blog/community/`. Do not invent creators, videos or links.

## Target

- Eplan 2026. Installed at `C:\Program Files\EPLAN\Platform\2026.0.3\Bin`. Every example must compile and work there, and future versions should keep working. Other versions are not tested and not mentioned.
- .NET Framework 4.8.1 (the 2026 API targets it). Scripts are loaded by Eplan as `.cs` files and compiled at load time; there is no compiled DLL to distribute.
- Eplan compiles scripts with its own compiler, and **verified in Eplan 2026: it accepts C# 5 only** (string interpolation, `?.` and `nameof` each stop a script from running). Write C# 5: no string interpolation (`$""`), no `?.`, no `out var`, no expression-bodied members, no `nameof`, no auto-property initializers. The compile-check enforces this with `LangVersion` 5.
- The compile-check also mimics Eplan: it references only the Base, AF and Gui assemblies (**not** DataModel or HEServices; **verified in Eplan 2026** that a script cannot name data model types directly), and it prepends namespaces so ambiguous names such as `EventHandler` fail locally. Verified: `System.Windows.Forms` is added by Eplan, `System.IO` is not, and a bare `EventHandler` does not compile. The other prepended namespaces are still assumptions (listed in `IDEAS.md`). If an example needs the data model, reach it with reflection (see `docs/reaching-the-data-model.md`) and say so in the example README.
- Write `System.EventHandler` in full when you need it.
- Give helper classes a unique prefix per example (for example `Theme...`, `Dialog...`). Eplan compiles each script alone, but the local compile-check builds all examples into one assembly, so two examples must not define the same type name. Record what was verified in `docs/how-eplan-loads-scripts.md`.

## Layout

```
examples/NN-short-name/   one folder per example
    ExampleName.cs        the script
    README.md             what it does, what you learn, how to load it, screenshot/GIF
docs/                     guides written in our own words
tools/CompileCheck/       local-only compile check (references the Eplan install by path)
IDEAS.md                  ideas for future examples
```

## Draft status and safety

- A new example is a **Draft** until the maintainer has run it in Eplan. README tables say "Draft", the example README says "DRAFT, not yet run in Eplan", and the compatibility table says "Pending".
- Never push a Draft to GitHub without the maintainer's explicit OK. Drafts are developed on a local branch (`drafts`).
- Any example that **changes project data or files** must offer a preview, ask for confirmation, tell the user to try a copy first, and say what it changed (or failed to change) afterwards.
- Examples that read the data model use reflection (see `docs/reaching-the-data-model.md`) and wrap every read in `try`/`catch`. Check member names against the installed assemblies before using them.
- Screenshots must show only the dialog or window being demonstrated, never a customer drawing or project.

## Example conventions

Every `.cs` file begins with this header:

```csharp
// ============================================================
// Example   : <name>
// Purpose   : <one sentence>
// You learn : <concepts, comma separated>
// Level     : Beginner | Intermediate | Advanced
// Tested in : Eplan Electric P8 2026.0.3
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================
```

- Beginner examples explain basic C# in comments (what a class, method, variable, `using`, and event are, when they first appear).
- Prefer small, readable code over clever code. Use generic sample data.
- Keep one idea per example. Name files in PascalCase, folders as `NN-kebab-case`.
- Each example folder gets a README with: What it does, What you will learn, How to load it, Screenshot or GIF, Ideas to extend.
- Always handle Cancel, empty input, and "no project open" cases, and show a friendly message instead of crashing Eplan.
- Add each new example to the table in the root README. Include a screenshot.png in each example folder.

## Testing

1. Compile-check against the installed version: `pwsh tools/check-all.ps1` (or `powershell -File tools/check-all.ps1`). One version only: `dotnet build tools/CompileCheck -p:EplanBin="<path>\Bin"`. Run `dotnet build` from PowerShell, not bash, because of path quoting.
2. Where possible, exercise the logic outside Eplan against the compiled DLL (see how example 01 was checked).
3. Run the script inside Eplan 2026 on a throwaway demo project and confirm it works before marking the example "Tested". Update the compatibility table in the example README.
4. Do not claim an example is tested if it has only been compile-checked.

### Running examples in Eplan from Claude (how the 2026-11-07 run was done)

Eplan's remote server was not enabled, so Claude drove the running Eplan with PowerShell 5.1 (screenshots, mouse, keys, UI Automation). Lessons:

- **Use a copy of a project** (File > Project > Copy), never the original. Confirm the title bar shows the copy before anything that writes.
- **Find controls with UI Automation** (`AutomationElement`, by name, then use its bounding rectangle). Coordinates guessed from a scaled screenshot were wrong several times. Wait for the window by name (for example "Script file selection") instead of sleeping. Make the process DPI-aware, or every rectangle is off.
- **A modal message box blocks all clicks.** After every step list the process's windows, and take a screenshot before clicking.
- **Type script paths as long paths.** The 8.3 short form of the temp folder did not open in the file dialog.
- **A script that fails to compile just does not run**, with no window. Test with a marker file the script writes, plus an identical control script that does run.
- **Selection:** pages selected in a navigator reach a script; objects selected on an open drawing page did not.
- **Do not type into the drawing editor** (a stray keystroke can open a coordinate dialog). Use the ribbon or File menu.
- **Screenshots:** crop to the dialog, then look at the image before saving it. Hide navigation panes that show employer names, and anything that shows a project's last editor, e-mail or customer data. Check that no sliver of Eplan's start-up artwork is in the crop.
- **Keep the PC awake** (a locked or sleeping PC stops the run) and keep each command under about 90 seconds.
- A local copy of the helper script lives in the ignored `_local` folder (`eplan-ui-test.ps1`).

## Capturing ideas while working on other scripts

When Justin is working on other Eplan scripting projects and a pattern comes up that would make a good general example, propose adding it to `IDEAS.md` as a short **description only** (what it does, what it teaches, difficulty). Ask first, never add silently, and never copy the source code or any customer data. See rule 3.
