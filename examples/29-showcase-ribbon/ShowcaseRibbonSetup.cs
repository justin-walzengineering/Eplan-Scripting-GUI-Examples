// ============================================================
// Example   : 29 - Showcase ribbon (setup script)
// Purpose   : Add one ribbon tab with a button for each example in this repository, so you can try them all with one click.
// You learn : Building a ribbon from a table of data, grouping buttons, icons from SVG files, safe re-running
// Level     : Intermediate
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 and 2027.0.3 (run in Eplan 2027: see README)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
//
// HOW IT FITS TOGETHER
//   ShowcaseRibbonSetup.cs   (this file)  builds the "Script Examples" tab. Run it once.
//   ShowcaseRibbonRemove.cs               removes the tab again.
//   icons\*.svg                           one picture per button (01.svg, 02.svg, ...), plus one per group as a fallback.
// The buttons run the example scripts that sit next to this folder (examples\01-..., examples\02-..., and so on).
// ============================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;                          // StackTrace
using System.IO;                                   // Path, File
using System.Runtime.CompilerServices;             // CallerFilePath
using System.Windows.Forms;                        // MessageBox, OpenFileDialog
using Eplan.EplApi.Gui;                            // RibbonBar, RibbonTab, RibbonCommandGroup, RibbonIcon
using Eplan.EplApi.Scripting;                      // The [Start] attribute

// One button on the ribbon. A small class like this keeps the table below easy to read.
public class ShowcaseButton
{
    public string Group;       // the group the button sits in
    public string Text;        // the words under the button
    public string Script;      // the script, relative to the examples folder, for example "07-themed-message\ThemedMessage.cs"
    public string Tip;         // the tooltip

    public ShowcaseButton(string group, string text, string script, string tip)
    {
        Group = group;
        Text = text;
        Script = script;
        Tip = tip;
    }
}

public class ShowcaseRibbonSetup
{
    // ===== Options =====

    // The folder that holds the numbered example folders (the "examples" folder of this repository).
    // Leave it empty ("") and the script works it out, and asks you if it cannot.
    private const string ExamplesFolder = "";

    // Keep TabName the same in ShowcaseRibbonRemove.cs, so it can find the tab.
    private const string TabName = "Script Examples";

    // The four group names. Each button has its own icon (icons.svg for example 01, and so on).
    // A group icon (windows.svg, data.svg, actions.svg, blocks.svg) is the fallback when a button has no icon of its own.
    private const string GroupWindows = "Windows and dialogs";
    private const string GroupData = "Project data";
    private const string GroupActions = "Actions";
    private const string GroupBlocks = "Helpers";

    // ===================

    // The whole ribbon is described by this table: one row per button.
    private static List<ShowcaseButton> Buttons()
    {
        List<ShowcaseButton> list = new List<ShowcaseButton>();

        list.Add(new ShowcaseButton(GroupWindows, "Input dialog", @"01-input-dialog\InputDialog.cs", "01: Ask for text, confirm an action, show a message."));
        list.Add(new ShowcaseButton(GroupWindows, "Progress bar", @"02-progress-bar\ProgressBar.cs", "02: Show progress during a long task, with Cancel."));
        list.Add(new ShowcaseButton(GroupWindows, "Window placement", @"03-window-placement\WindowPlacement.cs", "03: Open dialogs centered on Eplan."));
        list.Add(new ShowcaseButton(GroupWindows, "Light / dark", @"04-light-dark-theme\LightDarkTheme.cs", "04: A Dark mode check box that restyles the window."));
        list.Add(new ShowcaseButton(GroupWindows, "Remember window", @"06-remember-window\RememberWindow.cs", "06: Reopen a window where the user left it."));
        list.Add(new ShowcaseButton(GroupWindows, "Themed message", @"07-themed-message\ThemedMessage.cs", "07: Messages that fit dark mode."));
        list.Add(new ShowcaseButton(GroupWindows, "Settings", @"08-settings-dialog\SettingsDialog.cs", "08: Save and restore user preferences."));
        list.Add(new ShowcaseButton(GroupWindows, "Please wait", @"12-please-wait-window\PleaseWait.cs", "12: A moving window while Eplan is busy."));
        list.Add(new ShowcaseButton(GroupWindows, "Dark-safe controls", @"13-dark-safe-controls\DarkSafeControls.cs", "13: Date field, drop-down and check box for dark mode."));
        list.Add(new ShowcaseButton(GroupWindows, "Folder picker", @"16-modern-folder-picker\ModernFolderPicker.cs", "16: The Explorer-style choose a folder window."));
        list.Add(new ShowcaseButton(GroupWindows, "Results window", @"22-results-window\ResultsWindow.cs", "22: One report window instead of many message boxes."));

        list.Add(new ShowcaseButton(GroupData, "Page list", @"09-page-list\PageList.cs", "09: Search, sort and export pages to CSV."));
        list.Add(new ShowcaseButton(GroupData, "Property viewer", @"10-property-viewer\PropertyViewer.cs", "10: See every property of the project or your selection."));
        list.Add(new ShowcaseButton(GroupData, "Parts", @"11-parts-viewer\PartsViewer.cs", "11: Search the parts database as you type."));
        list.Add(new ShowcaseButton(GroupData, "Selection", @"14-selection-explorer\SelectionExplorer.cs", "14: Write the properties of your selection to a text file."));
        list.Add(new ShowcaseButton(GroupData, "Settings finder", @"25-settings-finder\SettingsFinder.cs", "25: Search Eplan's settings (read-only)."));

        list.Add(new ShowcaseButton(GroupActions, "PDF export", @"17-export-launcher\ExportLauncher.cs", "17: PDF export with a file name pattern."));
        list.Add(new ShowcaseButton(GroupActions, "Device list", @"18-device-list-export\DeviceListExport.cs", "18: Device list as CSV, XML or text."));
        list.Add(new ShowcaseButton(GroupActions, "Renumber", @"19-renumber-helper\RenumberHelper.cs", "19: A safe front end for renumbering. Use a copy of a project."));
        list.Add(new ShowcaseButton(GroupActions, "Backup", @"20-timestamped-backup\TimestampedBackup.cs", "20: A backup with the date and time in its name."));
        list.Add(new ShowcaseButton(GroupActions, "Bulk edit", @"23-bulk-edit\BulkEdit.cs", "23: Preview, then change many pages. Use a copy of a project."));
        list.Add(new ShowcaseButton(GroupActions, "CSV preview", @"24-csv-device-import\CsvDeviceImport.cs", "24: Check a CSV file before importing it."));
        list.Add(new ShowcaseButton(GroupActions, "Back up first", @"26-backup-then-run\BackupThenRun.cs", "26: A safety net in front of risky work."));

        list.Add(new ShowcaseButton(GroupBlocks, "Hand-off value", @"15-hand-off-value\HandOffSender.cs", "15: Pass a value from one script to another."));
        list.Add(new ShowcaseButton(GroupBlocks, "Favorites", @"21-favorites-launcher\FavoritesLauncher.cs", "21: A searchable list of your scripts."));
        list.Add(new ShowcaseButton(GroupBlocks, "Compile canary", @"27-compile-canary\CompileCanary.cs", "27: A log line first, so a script that did not compile is easy to spot."));
        list.Add(new ShowcaseButton(GroupBlocks, "Unit check", @"28-unit-check\UnitCheck.cs", "28: Tell whether a measured width is in millimetres or inches."));

        return list;
    }

    [Start]
    public void Run()
    {
        string how;
        string setupFolder;
        string examples = FindExamplesFolder(out how, out setupFolder);
        if (examples == null)
        {
            Say("Folder not found",
                "I could not find the examples folder.\r\n\r\n" +
                "Run this script again and select ShowcaseRibbonSetup.cs when asked. It sits in the folder 29-showcase-ribbon.");
            return;
        }

        try
        {
            RibbonBar bar = new RibbonBar(true);

            RibbonTab existing = FindTab(bar, TabName);
            if (existing != null)
            {
                DialogResult answer = MessageBox.Show(
                    "The tab \"" + TabName + "\" already exists. Replace it?",
                    "Showcase ribbon", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                {
                    return;
                }
                existing.Remove();
            }

            RibbonTab tab = bar.AddTab(TabName);

            // Groups are created the first time a button names them, in the order of the table.
            Dictionary<string, RibbonCommandGroup> groups = new Dictionary<string, RibbonCommandGroup>();
            int added = 0;
            List<string> missing = new List<string>();

            foreach (ShowcaseButton b in Buttons())
            {
                string scriptPath = Path.Combine(examples, b.Script);
                if (!File.Exists(scriptPath))
                {
                    missing.Add(b.Script);              // skip a button whose script is not there
                    continue;
                }

                RibbonCommandGroup group;
                if (!groups.TryGetValue(b.Group, out group))
                {
                    group = tab.AddCommandGroup(b.Group);
                    groups[b.Group] = group;
                }

                RibbonCommandInfo info = new RibbonCommandInfo(b.Text, "ExecuteScript /ScriptFile:\"" + scriptPath + "\"");
                info.Tooltip = b.Text;
                info.Description = b.Tip;

                string iconFolder = Path.Combine(setupFolder, "icons");
                RibbonIcon icon = LoadIcon(Path.Combine(iconFolder, b.Script.Substring(0, 2) + ".svg"));     // the button's own icon
                if (icon == null)
                {
                    icon = LoadIcon(Path.Combine(iconFolder, IconFor(b.Group)));                              // else the group's icon
                }
                if (icon != null)
                {
                    group.AddCommand(info, icon);
                }
                else
                {
                    group.AddCommand(info);
                }
                added++;
            }

            string text = "The tab \"" + TabName + "\" was added with " + added + " buttons.\r\n\r\nExamples folder:\r\n" + examples +
                          "\r\n(found by: " + how + ")";
            if (missing.Count > 0)
            {
                text += "\r\n\r\nSkipped " + missing.Count + " (script file not found): " + string.Join(", ", missing.ToArray());
            }
            text += "\r\n\r\nIf you do not see the tab, restart Eplan.";
            Say("Showcase ribbon created", text);
        }
        catch (Exception ex)
        {
            Say("Showcase ribbon failed", ex.Message);
        }
    }

    private static string IconFor(string group)
    {
        if (group == GroupWindows) { return "windows.svg"; }
        if (group == GroupData) { return "data.svg"; }
        if (group == GroupActions) { return "actions.svg"; }
        return "blocks.svg";
    }

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
            return null;
        }
    }

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

    // --- Finding the folders ------------------------------------------------------------
    // The setup folder holds this file and the icons. The examples folder is its parent.

    private static string ThisFile([CallerFilePath] string path = "")
    {
        return path;
    }

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

    // The setup folder is right when it contains this script and an icons folder.
    private static bool SetupFolderLooksRight(string folder)
    {
        try
        {
            return !string.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, "ShowcaseRibbonSetup.cs"));
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string AskForSetupFolder()
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Title = "Select ShowcaseRibbonSetup.cs (in the folder 29-showcase-ribbon)";
            dialog.Filter = "ShowcaseRibbonSetup.cs|ShowcaseRibbonSetup.cs";
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;
            return dialog.ShowDialog() == DialogResult.OK ? FolderOf(dialog.FileName) : "";
        }
    }

    // Returns the examples folder, or null. "setupFolder" is the folder with the icons.
    private static string FindExamplesFolder(out string how, out string setupFolder)
    {
        setupFolder = "";

        // 1. The option at the top of this file.
        if (!string.IsNullOrEmpty(ExamplesFolder) && Directory.Exists(ExamplesFolder))
        {
            setupFolder = Path.Combine(ExamplesFolder, "29-showcase-ribbon");
            how = "the ExamplesFolder option";
            return ExamplesFolder;
        }

        // 2. The compiler tells us where this file is.
        string folder = FolderOf(ThisFile());
        if (SetupFolderLooksRight(folder))
        {
            setupFolder = folder;
            how = "the compiler (CallerFilePath)";
            return FolderOf(folder);
        }

        // 3. The stack trace tells us.
        folder = FolderOf(FileFromStackTrace());
        if (SetupFolderLooksRight(folder))
        {
            setupFolder = folder;
            how = "the stack trace";
            return FolderOf(folder);
        }

        // 4. Ask the user.
        folder = AskForSetupFolder();
        if (SetupFolderLooksRight(folder))
        {
            setupFolder = folder;
            how = "you chose it";
            return FolderOf(folder);
        }

        how = "";
        return null;
    }

    private static void Say(string title, string text)
    {
        MessageBox.Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
