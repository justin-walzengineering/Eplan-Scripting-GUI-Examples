# 05 - Ribbon tab, group and buttons with icons

**Level:** Beginner  **Files:** [RibbonSetup.cs](RibbonSetup.cs), [ButtonHello.cs](ButtonHello.cs), [ButtonTime.cs](ButtonTime.cs), [RibbonRemove.cs](RibbonRemove.cs), [icons/](icons/)

## What it does

Adds your own **tab** to Eplan's ribbon. The tab has one **group**, and the group has two **buttons**, each with its own icon:

| Button | Runs | Icon |
|--------|------|------|
| Say hello | `ButtonHello.cs` | `icons/hello.svg` (a speech bubble) |
| Show time | `ButtonTime.cs` | `icons/time.svg` (a clock) |

Run `RibbonSetup.cs` once and the tab appears. Run it again and it asks before replacing the tab, so you never end up with two. `RibbonRemove.cs` removes the tab, so you can undo the example cleanly.

A ribbon is built in three levels, and the code follows them in order:

```
Tab            "Demo Tools"
  Group          "Examples"
    Button         "Say hello"   (icon, tooltip, runs a script)
    Button         "Show time"
```

## What you will learn

- Creating a **tab**, a **group** and **buttons** with `RibbonBar`, `RibbonTab` and `RibbonCommandGroup`
- How a button starts a script: the command line `ExecuteScript /ScriptFile:"<path>"`
- Giving a button an **icon** from an `.svg` file
- Adding a **tooltip** and a longer description
- Finding the folder a script is saved in with `[CallerFilePath]`
- Making setup **safe to run twice**, and a matching **remove** script

## Keep the files together

Put everything in one folder, for example `C:\EplanScripts\Ribbon\`:

```
RibbonSetup.cs
RibbonRemove.cs
ButtonHello.cs
ButtonTime.cs
icons\
    hello.svg
    time.svg
```

The setup script looks for the other files next to itself. If it cannot work out its own folder, it tells you. Then type the folder into the `ScriptFolder` option at the top of `RibbonSetup.cs`.

## How to load it

1. Copy the whole example folder to your computer.
2. In Eplan, go to Utilities > Scripts, choose **Load**, select `RibbonSetup.cs`, and **Run** it.
3. Look for the **Demo Tools** tab. If it does not appear, restart Eplan.
4. Click the buttons. To undo, run `RibbonRemove.cs` the same way.

See [Getting started](../../docs/getting-started.md) if this is your first script.

## About the icons

Ribbon icons are **SVG** pictures. An SVG file is a small text file that describes a shape, so it stays sharp at any size and works on both light and dark backgrounds. The setup script reads the file as text and gives that text to Eplan.

- Eplan's ribbon icons are given to the script as SVG text (or as the number of an icon Eplan already has). A PNG file cannot be passed in the same way.
- The two icons here are simple shapes drawn from scratch. Open them in a text editor to see how short they are, and change a color or shape to make your own.
- Free tools such as Inkscape can draw SVG icons. If you do not have an icon, the button still works; it just appears without a picture.

## Good to know

- Buttons point to the **script file**, so editing `ButtonHello.cs` takes effect the next time the button is clicked. You only need to run the setup again when you change the ribbon itself (names, buttons, icons or folder).
- Ribbon changes may not show until Eplan is restarted.
- Because the buttons store the full path to each script, move the folder only if you run the setup again afterwards.
- On a shared drive, every user needs to run the setup once on their own computer, because each person's ribbon is their own.

## Compatibility

| Eplan version | Compiles | Run in Eplan |
|---------------|----------|--------------|
| 2025.0.3 | Yes | Not tested |
| 2026.0.3 | Yes | Pending |

The ribbon members used here are the same in 2025 and 2026. Still to confirm in Eplan: that the buttons start their scripts, how the icons look on a dark ribbon, and whether the tab appears immediately or after a restart.

## Ideas to extend

- Add a second group, or a third button.
- Put a button next to Eplan's own commands by adding it to an existing tab.
- Point a button at one of your own scripts from the other examples (for example example 04).
- Add more icons, and try a different color for light and dark ribbons.
