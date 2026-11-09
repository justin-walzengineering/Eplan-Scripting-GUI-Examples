// ============================================================
// Example   : 20 - Timestamped backup
// Purpose   : Back up the current project with a file name that includes the date and time, to a folder you choose, with a comment.
// You learn : The backup action, name patterns, safe file names, naming rules for Eplan archives, remembering choices
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Diagnostics;              // Process
using System.Drawing;                  // Point, Size
using System.IO;                       // File, Directory, Path
using System.Text.RegularExpressions;  // Regex
using System.Windows.Forms;            // Form, controls
using Newtonsoft.Json;                 // JsonConvert
using Eplan.EplApi.ApplicationFramework;   // CommandLineInterpreter, ActionCallingContext
using Eplan.EplApi.Base;               // PathMap
using Eplan.EplApi.Scripting;          // The [Start] attribute

public class TimestampedBackupDemo
{
    [Start]
    public void Run()
    {
        BackupOptions options = BackupOptions.Load();

        using (BackupForm form = new BackupForm(options))
        {
            if (form.ShowDialog() != DialogResult.OK)
            {
                return;
            }
        }
        options.Save();

        string archiveName = BackupNaming.Apply(options.Pattern, BackupNaming.CurrentProjectName());

        try
        {
            Directory.CreateDirectory(options.Folder);

            // "backup" is Eplan's backup action. TYPE=PROJECT backs up the current project.
            // ARCHIVENAME is just the name (no folder). DESTINATIONPATH is the folder.
            ActionCallingContext context = new ActionCallingContext();
            context.AddParameter("TYPE", "PROJECT");
            context.AddParameter("ARCHIVENAME", archiveName);
            context.AddParameter("DESTINATIONPATH", options.Folder);
            context.AddParameter("BACKUPMETHOD", "BACKUP");
            context.AddParameter("INCLEXTDOCS", options.IncludeDocuments ? "1" : "0");
            context.AddParameter("INCLIMAGES", options.IncludeImages ? "1" : "0");
            if (options.Comment.Length > 0)
            {
                context.AddParameter("COMMENT", options.Comment);
            }
            new CommandLineInterpreter().Execute("backup", context);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The backup did not finish:\r\n\r\n" + ex.Message, "Timestamped backup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Eplan chooses the extension, so look for any file that starts with our name.
        string[] found = Directory.GetFiles(options.Folder, archiveName + ".*");
        if (found.Length == 0)
        {
            MessageBox.Show("The action ran, but I cannot see a file called " + archiveName + ".* in\r\n" + options.Folder,
                "Timestamped backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Process.Start("explorer.exe", "/select,\"" + found[0] + "\"");
    }
}

public static class BackupNaming
{
    // Replaces {project}, {date} and {time}, removes characters Windows does not allow, and avoids a trailing ".nnn".
    public static string Apply(string pattern, string project)
    {
        string name = (pattern ?? "").Replace("{project}", project).Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd")).Replace("{time}", DateTime.Now.ToString("HHmmss"));
        foreach (char bad in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(bad, '_');
        }
        name = name.Replace('.', '_').Trim();          // no dots at all, which also keeps us clear of Eplan's ".nnn" rule
        return name.Length == 0 ? "backup" : name;
    }

    // Eplan does not allow archive names that end in a dot and three digits (it uses that form for split backups).
    // Because Apply removes all dots, this can never happen. The check is kept to document the rule.
    public static bool EndsWithThreeDigitExtension(string name)
    {
        return Regex.IsMatch(name, @"\.\d{3}$");
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

public class BackupOptions
{
    public string Folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public string Pattern = "{project}_{date}_{time}";
    public string Comment = "";
    public bool IncludeDocuments = true;
    public bool IncludeImages = true;

    private static string FilePath
    {
        get { return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EplanScriptExamples"), "timestamped-backup.json"); }
    }

    public static BackupOptions Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                BackupOptions loaded = JsonConvert.DeserializeObject<BackupOptions>(File.ReadAllText(FilePath));
                if (loaded != null) { return loaded; }
            }
        }
        catch (Exception) { }
        return new BackupOptions();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch (Exception) { }
    }
}

public class BackupForm : Form
{
    private readonly BackupOptions options;
    private readonly TextBox folderBox = new TextBox();
    private readonly TextBox patternBox = new TextBox();
    private readonly TextBox commentBox = new TextBox();
    private readonly CheckBox docsCheck = new CheckBox();
    private readonly CheckBox imagesCheck = new CheckBox();
    private readonly Label preview = new Label();

    public BackupForm(BackupOptions options)
    {
        this.options = options;

        Text = "Timestamped backup";
        ClientSize = new Size(480, 290);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        AddLabel("Folder", 12, 18);
        folderBox.Location = new Point(100, 15);
        folderBox.Width = 290;
        Button browse = new Button();
        browse.Text = "Browse...";
        browse.Location = new Point(396, 14);
        browse.Size = new Size(72, 24);
        browse.Click += delegate
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = folderBox.Text;
                if (dialog.ShowDialog() == DialogResult.OK) { folderBox.Text = dialog.SelectedPath; }
            }
        };

        AddLabel("Name", 12, 52);
        patternBox.Location = new Point(100, 49);
        patternBox.Width = 368;
        patternBox.TextChanged += delegate { preview.Text = "Result:  " + BackupNaming.Apply(patternBox.Text, BackupNaming.CurrentProjectName()) + ".*"; };

        Label help = new Label();
        help.Text = "Use {project}, {date} and {time}. Dots are removed.";
        help.Location = new Point(100, 74);
        help.AutoSize = true;
        help.ForeColor = SystemColors.GrayText;

        preview.Location = new Point(100, 96);
        preview.Size = new Size(368, 20);

        AddLabel("Comment", 12, 130);
        commentBox.Location = new Point(100, 127);
        commentBox.Width = 368;

        docsCheck.Text = "Include external documents";
        docsCheck.Location = new Point(100, 162);
        docsCheck.AutoSize = true;
        imagesCheck.Text = "Include images";
        imagesCheck.Location = new Point(100, 188);
        imagesCheck.AutoSize = true;

        Button backup = new Button();
        backup.Text = "Back up";
        backup.Location = new Point(312, 248);
        backup.Size = new Size(75, 28);
        backup.Click += OnBackup;
        Button cancel = new Button();
        cancel.Text = "Cancel";
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(393, 248);
        cancel.Size = new Size(75, 28);
        AcceptButton = backup;
        CancelButton = cancel;

        Controls.AddRange(new Control[] { folderBox, browse, patternBox, help, preview, commentBox, docsCheck, imagesCheck, backup, cancel });

        folderBox.Text = options.Folder;
        patternBox.Text = options.Pattern;
        commentBox.Text = options.Comment;
        docsCheck.Checked = options.IncludeDocuments;
        imagesCheck.Checked = options.IncludeImages;
    }

    private void AddLabel(string text, int x, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.Location = new Point(x, y);
        label.AutoSize = true;
        Controls.Add(label);
    }

    private void OnBackup(object sender, EventArgs e)
    {
        if (folderBox.Text.Trim().Length == 0 || patternBox.Text.Trim().Length == 0)
        {
            MessageBox.Show("Please choose a folder and a name.", "Timestamped backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        options.Folder = folderBox.Text.Trim();
        options.Pattern = patternBox.Text.Trim();
        options.Comment = commentBox.Text.Trim();
        options.IncludeDocuments = docsCheck.Checked;
        options.IncludeImages = imagesCheck.Checked;
        DialogResult = DialogResult.OK;
    }
}
