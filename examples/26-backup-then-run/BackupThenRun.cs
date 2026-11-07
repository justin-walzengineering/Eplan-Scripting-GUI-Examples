// ============================================================
// Example   : 26 - Back up first, then run
// Purpose   : Make a timestamped backup of the project, and only if that worked, run a script or Eplan action you choose.
// You learn : Chaining two actions safely, stopping when the first fails, choosing a script or an action, doing risky work only after a safety step
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Drawing;                  // Point, Size
using System.IO;                       // File, Directory, Path
using System.Windows.Forms;            // Form, controls, OpenFileDialog, FolderBrowserDialog
using Eplan.EplApi.ApplicationFramework;   // CommandLineInterpreter, ActionCallingContext
using Eplan.EplApi.Base;               // PathMap
using Eplan.EplApi.Scripting;          // The [Start] attribute

public class BackupThenRunDemo
{
    [Start]
    public void Run()
    {
        BackupThenRunChoice choice;
        using (BackupThenRunForm form = new BackupThenRunForm())
        {
            if (form.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            choice = form.Choice;
        }

        // Step 1: the backup. If anything goes wrong here, we stop and do NOT run the risky part.
        string archiveName = BtrNaming.Name(BtrNaming.CurrentProjectName());
        try
        {
            Directory.CreateDirectory(choice.Folder);
            ActionCallingContext backup = new ActionCallingContext();
            backup.AddParameter("TYPE", "PROJECT");
            backup.AddParameter("ARCHIVENAME", archiveName);
            backup.AddParameter("DESTINATIONPATH", choice.Folder);
            backup.AddParameter("BACKUPMETHOD", "BACKUP");
            backup.AddParameter("COMMENT", "Automatic backup before: " + choice.Target);
            new CommandLineInterpreter().Execute("backup", backup);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The backup failed, so nothing was run.\r\n\r\n" + ex.Message, "Back up first, then run", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Step 2: only now do the real work.
        try
        {
            CommandLineInterpreter cli = new CommandLineInterpreter();
            if (choice.IsScript)
            {
                ActionCallingContext run = new ActionCallingContext();
                run.AddParameter("ScriptFile", choice.Target);
                cli.Execute("ExecuteScript", run);
            }
            else
            {
                cli.Execute(choice.Target);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("The backup was made, but running your choice failed:\r\n\r\n" + ex.Message + "\r\n\r\nBackup name: " + archiveName,
                "Back up first, then run", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

public class BackupThenRunChoice
{
    public string Folder;
    public bool IsScript;
    public string Target;
}

public static class BtrNaming
{
    // The backup name: project name, date and time. Dots are removed (Eplan does not allow a name ending in a dot and three digits).
    public static string Name(string project)
    {
        string name = project + "_before_run_" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        foreach (char bad in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(bad, '_');
        }
        return name.Replace('.', '_');
    }

    public static string CurrentProjectName()
    {
        try
        {
            string value = PathMap.SubstitutePath("$(PROJECTNAME)");
            if (!string.IsNullOrEmpty(value) && !value.Contains("$("))
            {
                return value;
            }
        }
        catch (Exception) { }
        return "Project";
    }
}

public class BackupThenRunForm : Form
{
    private readonly TextBox folderBox = new TextBox();
    private readonly ComboBox kindBox = new ComboBox();
    private readonly TextBox targetBox = new TextBox();
    private readonly Button browseTarget = new Button();

    public BackupThenRunChoice Choice;

    public BackupThenRunForm()
    {
        Text = "Back up first, then run";
        ClientSize = new Size(500, 240);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        AddLabel("Backup folder", 12, 20);
        folderBox.Location = new Point(110, 17);
        folderBox.Width = 300;
        folderBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EplanBackups");
        Button browseFolder = new Button();
        browseFolder.Text = "Browse...";
        browseFolder.Location = new Point(416, 16);
        browseFolder.Size = new Size(72, 24);
        browseFolder.Click += delegate
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK) { folderBox.Text = dialog.SelectedPath; }
            }
        };

        AddLabel("Then run", 12, 62);
        kindBox.Location = new Point(110, 59);
        kindBox.Width = 120;
        kindBox.DropDownStyle = ComboBoxStyle.DropDownList;
        kindBox.Items.AddRange(new object[] { "A script file", "An Eplan action" });
        kindBox.SelectedIndex = 0;
        kindBox.SelectedIndexChanged += delegate { browseTarget.Enabled = kindBox.SelectedIndex == 0; };

        AddLabel("Target", 12, 100);
        targetBox.Location = new Point(110, 97);
        targetBox.Width = 300;
        browseTarget.Text = "Browse...";
        browseTarget.Location = new Point(416, 96);
        browseTarget.Size = new Size(72, 24);
        browseTarget.Click += delegate
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "C# scripts (*.cs)|*.cs|All files (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK) { targetBox.Text = dialog.FileName; }
            }
        };

        Label hint = new Label();
        hint.Text = "For an action, type its name, for example XGedRedrawAction.";
        hint.Location = new Point(110, 126);
        hint.Size = new Size(380, 18);
        hint.ForeColor = SystemColors.GrayText;

        Label note = new Label();
        note.Text = "If the backup fails, nothing is run.";
        note.Location = new Point(12, 160);
        note.AutoSize = true;

        Button go = new Button();
        go.Text = "Back up, then run";
        go.Location = new Point(262, 198);
        go.Size = new Size(130, 28);
        go.Click += OnGo;
        Button cancel = new Button();
        cancel.Text = "Cancel";
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(398, 198);
        cancel.Size = new Size(90, 28);
        AcceptButton = go;
        CancelButton = cancel;

        Controls.AddRange(new Control[] { folderBox, browseFolder, kindBox, targetBox, browseTarget, hint, note, go, cancel });
    }

    private void AddLabel(string text, int x, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.Location = new Point(x, y);
        label.AutoSize = true;
        Controls.Add(label);
    }

    private void OnGo(object sender, EventArgs e)
    {
        if (folderBox.Text.Trim().Length == 0 || targetBox.Text.Trim().Length == 0)
        {
            MessageBox.Show("Please choose a backup folder and what to run.", "Back up first, then run", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Choice = new BackupThenRunChoice();
        Choice.Folder = folderBox.Text.Trim();
        Choice.IsScript = kindBox.SelectedIndex == 0;
        Choice.Target = targetBox.Text.Trim();
        DialogResult = DialogResult.OK;
    }
}
