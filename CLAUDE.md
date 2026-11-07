# CLAUDE.md

Guidance for Claude Code (and human contributors) working in this repository.

## What this repo is

A free, public list of **original** C# script examples for Eplan Electric P8 2025 and 2026, focused on GUI (Windows Forms) tools. Audience: beginners first, growing toward experienced script writers. Maintained by Justin Walz, Walz Engineering. It is a community resource, not self-promotion.

## Hard rules

1. **Original work only.** Never copy, paraphrase, or use as a template any Eplan-owned material: training books, class example code, the Eplan API help text, template zips. Write every example from scratch against the public API, using generic data.
2. **No Eplan files in the repo.** Never commit `Eplan.EplApi*.dll`, API XML docs, PDFs, zips, or project/template files. The `.gitignore` blocks these; do not work around it.
3. **No customer or employer material.** Scripts from client projects may inspire an idea, but only the concept travels. Rewrite from scratch with made-up data. No client names, project names, part numbers, or company data.
4. **Unofficial.** Keep the disclaimer in the README. Do not imply Eplan endorsement.
5. **English only.**
5a. **Positive tone toward Eplan.** Present scripting as one more way to build on an already excellent product. Never suggest Eplan is lacking, and never frame examples as fixing or replacing Eplan features. Do not describe the repo as filling a gap or as the only good English resource; it is simply a list of examples people can use.
6. **Low-key branding.** Credit appears as a short "Maintained by Justin Walz, Walz Engineering" footer, with one short line inviting people to get in touch about custom work, and in file headers. No other sales pitches.
7. **Never `git push`, create remotes, or publish** without Justin's explicit OK in that conversation.

## Target

- Eplan 2025 and 2026. Installed at `C:\Program Files\EPLAN\Platform\2025.0.3\Bin` and `...\2026.0.3\Bin`. Every example must compile and work on both, and future versions should keep working.
- .NET Framework 4.8.1 (both the 2025 and 2026 APIs target it). Scripts are loaded by Eplan as `.cs` files and compiled at load time; there is no compiled DLL to distribute.
- Eplan compiles scripts with its own compiler. Until verified, write **C# 5 compatible syntax**: no string interpolation (`$""`), no `?.`, no `out var`, no expression-bodied members, no `nameof`, no auto-property initializers. The compile-check enforces this with `LangVersion` 5.
- The compile-check also mimics Eplan: it references only the Base, AF and Gui assemblies (**not** DataModel or HEServices, which reports say Eplan's script compiler does not reference), and it prepends the namespaces Eplan is believed to add, so ambiguous names such as `EventHandler` fail locally. These two assumptions are listed under "Verification to do in Eplan" in `IDEAS.md`. If an example needs the data model, reach it with reflection until that is verified, and say so in the example README.
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
// Tested in : Eplan Electric P8 2025.0.3 and 2026.0.3
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

1. Compile-check against every installed version: `pwsh tools/check-all.ps1` (or `powershell -File tools/check-all.ps1`). One version only: `dotnet build tools/CompileCheck -p:EplanBin="<path>\Bin"`. Run `dotnet build` from PowerShell, not bash, because of path quoting.
2. Where possible, exercise the logic outside Eplan against the compiled DLL (see how example 01 was checked).
3. Run the script inside Eplan 2025 and 2026 on a throwaway demo project and confirm it works before marking the example "Tested". Update the compatibility table in the example README.
4. Do not claim an example is tested if it has only been compile-checked.

## Capturing ideas while working on other scripts

When Justin is working on other Eplan scripting projects and a pattern comes up that would make a good general example, propose adding it to `IDEAS.md` as a short **description only** (what it does, what it teaches, difficulty). Ask first, never add silently, and never copy the source code or any customer data. See rule 3.
