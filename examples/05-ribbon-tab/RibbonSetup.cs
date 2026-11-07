// ============================================================
// Example   : 05 - Ribbon tab, group and buttons with icons (setup script)
// Purpose   : Create your own tab on Eplan's ribbon, with a group and two buttons that run scripts, each with an icon from an .svg file.
// You learn : RibbonBar, RibbonTab, RibbonCommandGroup, RibbonCommandInfo, RibbonIcon, ExecuteScript, safe re-running
// Level     : Beginner
// Tested in : Compile-checked against Eplan Electric P8 2025.0.3 and 2026.0.3; run in 2026.0.3
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
//
// HOW IT FITS TOGETHER
//   RibbonSetup.cs   (this file)  builds the tab, group and buttons. Run it once.
//   ButtonHello.cs                the script behind the first button.
//   ButtonTime.cs                 the script behind the second button.
//   RibbonRemove.cs               removes the tab again (a clean way to undo this example).
//   icons\hello.svg, icons\time.svg   the button images.
// Keep all of these together in one folder. If this script cannot work out where it is saved, it asks
// you to select ButtonHello.cs, and uses that file's folder.
// ============================================================

using System;
using System.Diagnostics;                          // StackTrace: which code is running, and from which file
using System.IO;                                   // Path, File, Directory
using System.Runtime.CompilerServices;             // CallerFilePath: lets a script ask "which file am I?"
using System.Windows.Forms;                        // MessageBox
using Eplan.EplApi.Gui;                            // RibbonBar, RibbonTab, RibbonCommandGroup, RibbonIcon
using Eplan.EplApi.Scripting;                      // The [Start] attribute

public class RibbonSetupDemo
{
    // ===== Options: change these to suit you =====

    // The folder that holds the scripts and the "icons" folder.
    // Leave it empty ("") and the script tries to work it out, and asks you to select ButtonHello.cs if it cannot.
    // To skip the question, type the folder here, for example @"C:\EplanScripts\Ribbon".
    private const string ScriptFolder = "";

    // The names shown on the ribbon. Keep TabName the same in RibbonRemove.cs, so it can find the tab.
    private const string TabName = "Demo Tools";
    private const string GroupName = "Examples";

    // =============================================

    [Start]
    public void Run()
    {
        // 1. Find the folder with our scripts and icons.
        //    "how" is filled in with a few words about how the folder was found. We show it at the end.
        string how;
        string folder = FindScriptFolder(out how);
        if (folder == null)
        {
            Say("Folder not found",
                "I could not find ButtonHello.cs.\r\n\r\n" +
                "Put all the files of this example in one folder (RibbonSetup.cs, ButtonHello.cs, ButtonTime.cs, RibbonRemove.cs and the icons folder), then run this script again and select ButtonHello.cs when asked.");
            return;
        }

        try
        {
            // 2. Open the ribbon for changes. "true" asks Eplan to refresh it after each change.
            RibbonBar bar = new RibbonBar(true);

            // 3. Is our tab already there from an earlier run? If so, ask before replacing it,
            //    so running this script twice never creates two tabs.
            RibbonTab existing = FindTab(bar, TabName);
            if (existing != null)
            {
                DialogResult answer = MessageBox.Show(
                    "The tab \"" + TabName + "\" already exists. Replace it?",
                    "Ribbon setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                {
                    return;
                }
                existing.Remove();
            }

            // 4. A ribbon is built in three levels: TAB > GROUP > BUTTON.
            RibbonTab tab = bar.AddTab(TabName);
            RibbonCommandGroup group = tab.AddCommandGroup(GroupName);

            // 5. Add the buttons. Each one runs a script file when clicked.
            AddButton(group, "Say hello", folder, "ButtonHello.cs", "hello.svg",
                "Say hello", "Shows a friendly message. This button runs ButtonHello.cs.");
            AddButton(group, "Show time", folder, "ButtonTime.cs", "time.svg",
                "Show the time", "Shows the current date and time. This button runs ButtonTime.cs.");

            Say("Ribbon created",
                "The tab \"" + TabName + "\" was added.\r\n\r\nScripts folder:\r\n" + folder +
                "\r\n(found by: " + how + ")" +
                "\r\n\r\nIf you do not see the tab, restart Eplan.");
        }
        catch (Exception ex)
        {
            Say("Ribbon setup failed", ex.Message);
        }
    }

    // Adds one button to a group.
    //   group       - the group to add the button to
    //   text        - the words under the button
    //   folder      - where the script and icon files are
    //   scriptName  - the script file the button runs
    //   iconName    - the .svg file in the "icons" folder
    //   tooltip     - the short text shown when the mouse rests on the button
    //   description - the longer text under the tooltip
    private static void AddButton(RibbonCommandGroup group, string text, string folder,
                                  string scriptName, string iconName, string tooltip, string description)
    {
        // The "command line" tells Eplan what to do on a click. ExecuteScript runs a script file.
        // The path goes in quotes, in case a folder name contains a space.
        string scriptPath = Path.Combine(folder, scriptName);
        string commandLine = "ExecuteScript /ScriptFile:\"" + scriptPath + "\"";

        // RibbonCommandInfo bundles everything about a button into one object.
        RibbonCommandInfo info = new RibbonCommandInfo(text, commandLine);
        info.Tooltip = tooltip;
        info.Description = description;

        // The icon: read the .svg file and hand its text to Eplan.
        // If the file is missing we still add the button, just without an image.
        RibbonIcon icon = LoadIcon(Path.Combine(Path.Combine(folder, "icons"), iconName));
        if (icon != null)
        {
            group.AddCommand(info, icon);
        }
        else
        {
            group.AddCommand(info);
        }
    }

    // An SVG file is plain text that describes a picture. Eplan wants that text.
    private static RibbonIcon LoadIcon(string svgPath)
    {
        try
        {
            if (!File.Exists(svgPath))
            {
                return null;
            }
            return new RibbonIcon(File.ReadAllText(svgPath));
        }
        catch (Exception)
        {
            return null;                              // a missing picture should never stop the setup
        }
    }

    // GetTab may throw or return nothing when the tab does not exist, so we wrap it.
    private static RibbonTab FindTab(RibbonBar bar, string name)
    {
        try
        {
            return bar.GetTab(name);
        }
        catch (Exception)
        {
            return null;
        }
    }

    // --- Finding our folder ------------------------------------------------------------
    // The buttons need the full path of the scripts, so the setup has to know where they are saved.
    // Eplan does not hand a running script its own location, so we try several things, easiest first,
    // and finally simply ask the user. Asking always works.

    // Try 2 helper. [CallerFilePath] is a C# feature: the compiler fills in the path of this source
    // file for us. (Eplan compiles scripts its own way, so this can come back empty or wrong.)
    private static string ThisFile([CallerFilePath] string path = "")
    {
        return path;
    }

    // Try 3 helper. A "stack trace" lists the code that is running, and can include the source file name.
    private static string FileFromStackTrace()
    {
        try
        {
            StackFrame frame = new StackTrace(true).GetFrame(0);
            return frame == null ? "" : (frame.GetFileName() ?? "");
        }
        catch (Exception)
        {
            return "";
        }
    }

    // Try 4 helper. Ask the user, with the normal Windows "Open" window.
    // We ask for ButtonHello.cs (the Filter hides every other file), then use the folder it is in.
    // This looks and works like every other Windows "Open" window, with the address bar, Quick access and search.
    private static string AskForFolder()
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Title = "Select ButtonHello.cs in the folder with the example files";
            dialog.Filter = "ButtonHello.cs|ButtonHello.cs";     // text shown | what is listed
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;
            return dialog.ShowDialog() == DialogResult.OK ? FolderOf(dialog.FileName) : "";
        }
    }

    // The right folder is the one that contains ButtonHello.cs.
    private static bool LooksRight(string folder)
    {
        try
        {
            return !string.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, "ButtonHello.cs"));
        }
        catch (Exception)
        {
            return false;
        }
    }

    // A file path becomes a folder path. Returns "" if the path is empty or odd.
    private static string FolderOf(string file)
    {
        try
        {
            return string.IsNullOrEmpty(file) ? "" : Path.GetDirectoryName(file);
        }
        catch (Exception)
        {
            return "";
        }
    }

    // Returns the folder that contains our scripts, or null if we cannot find it.
    // "how" is set to a few words describing which method worked.
    private static string FindScriptFolder(out string how)
    {
        // 1. The ScriptFolder option at the top of this file.
        if (LooksRight(ScriptFolder))
        {
            how = "the ScriptFolder option";
            return ScriptFolder;
        }

        // 2. The compiler tells us where the source file is.
        string folder = FolderOf(ThisFile());
        if (LooksRight(folder))
        {
            how = "the compiler (CallerFilePath)";
            return folder;
        }

        // 3. The stack trace tells us.
        folder = FolderOf(FileFromStackTrace());
        if (LooksRight(folder))
        {
            how = "the stack trace";
            return folder;
        }

        // 4. Ask the user to select ButtonHello.cs.
        folder = AskForFolder();
        if (LooksRight(folder))
        {
            how = "you chose it";
            return folder;
        }

        how = "";
        return null;
    }

    private static void Say(string title, string text)
    {
        MessageBox.Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
