// ============================================================
// Example   : 08 - Settings dialog that saves preferences
// Purpose   : Let the user change a few options in a window and keep them between runs, in a small JSON file.
// You learn : A settings class, JSON with Newtonsoft.Json, defaults, handling a missing or damaged file, OK / Cancel / Reset
// Level     : Beginner
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Drawing;                  // Point, Size
using System.IO;                       // File, Directory, Path
using System.Windows.Forms;            // Form, controls, FolderBrowserDialog
using Newtonsoft.Json;                 // JsonConvert: turns an object into text and back (Eplan lets scripts use it)
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs.
// ---------------------------------------------------------------
public class SettingsDialogDemo
{
    [Start]
    public void Run()
    {
        // 1. Load the saved settings (or the defaults if there is no file yet).
        ScriptSettings settings = ScriptSettings.Load();

        // 2. Show them in a window and let the user change them.
        using (SettingsForm form = new SettingsForm(settings))
        {
            if (form.ShowDialog() != DialogResult.OK)
            {
                return;                                        // Cancel: nothing is saved
            }
        }

        // 3. Save, then say where the file is so the user can find it.
        settings.Save();
        MessageBox.Show("Settings saved to:\r\n" + ScriptSettings.FilePath, "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

// ---------------------------------------------------------------
// Part 2: the settings themselves. Each public field below becomes one entry in the file.
// ---------------------------------------------------------------
public class ScriptSettings
{
    // Defaults: the values used when there is no file yet, or when a value is missing.
    public string Initials = "";
    public string OutputFolder = "";
    public bool AskBeforeOverwrite = true;
    public int RecentCount = 10;

    // The file lives in the user's own AppData folder, so no administrator rights are needed
    // and every Windows user gets their own settings.
    public static string FilePath
    {
        get
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EplanScriptExamples");
            return Path.Combine(folder, "settings.json");
        }
    }

    // Reads the file. If it is missing or damaged we quietly use the defaults, so the script never fails at startup.
    public static ScriptSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                ScriptSettings loaded = JsonConvert.DeserializeObject<ScriptSettings>(File.ReadAllText(FilePath));
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch (Exception)
        {
            // If the file cannot be read, fall back to the defaults.
        }
        return new ScriptSettings();
    }

    // Writes the file. "Indented" makes it easy for a person to read and edit.
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}

// ---------------------------------------------------------------
// Part 3: the window.
// ---------------------------------------------------------------
public class SettingsForm : Form
{
    private readonly ScriptSettings settings;
    private readonly TextBox initialsBox = new TextBox();
    private readonly TextBox folderBox = new TextBox();
    private readonly CheckBox overwriteCheck = new CheckBox();
    private readonly NumericUpDown recentUpDown = new NumericUpDown();

    public SettingsForm(ScriptSettings settings)
    {
        this.settings = settings;

        Text = "Settings";
        ClientSize = new Size(440, 230);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        AddLabel("Your initials", 16, 18);
        initialsBox.Location = new Point(160, 15);
        initialsBox.Width = 80;
        initialsBox.MaxLength = 4;

        AddLabel("Output folder", 16, 52);
        folderBox.Location = new Point(160, 49);
        folderBox.Width = 200;
        Button browse = new Button();
        browse.Text = "Browse...";
        browse.Location = new Point(366, 48);
        browse.Size = new Size(64, 24);
        browse.Click += OnBrowse;

        overwriteCheck.Text = "Ask before overwriting files";
        overwriteCheck.Location = new Point(160, 84);
        overwriteCheck.AutoSize = true;

        AddLabel("Recent items to keep", 16, 120);
        recentUpDown.Location = new Point(160, 117);
        recentUpDown.Width = 60;
        recentUpDown.Minimum = 1;
        recentUpDown.Maximum = 50;

        Button reset = new Button();
        reset.Text = "Reset to defaults";
        reset.Location = new Point(16, 190);
        reset.Size = new Size(120, 28);
        reset.Click += OnReset;

        Button ok = new Button();
        ok.Text = "OK";
        ok.DialogResult = DialogResult.OK;
        ok.Location = new Point(274, 190);
        ok.Size = new Size(75, 28);
        ok.Click += OnOk;                                       // copy the boxes back into the settings

        Button cancel = new Button();
        cancel.Text = "Cancel";
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(355, 190);
        cancel.Size = new Size(75, 28);

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.AddRange(new Control[] { initialsBox, folderBox, browse, overwriteCheck, recentUpDown, reset, ok, cancel });

        ShowSettings(settings);
    }

    private void AddLabel(string text, int x, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.Location = new Point(x, y);
        label.AutoSize = true;
        Controls.Add(label);
    }

    // Settings -> boxes.
    private void ShowSettings(ScriptSettings s)
    {
        initialsBox.Text = s.Initials ?? "";
        folderBox.Text = s.OutputFolder ?? "";
        overwriteCheck.Checked = s.AskBeforeOverwrite;
        recentUpDown.Value = Math.Max(recentUpDown.Minimum, Math.Min(recentUpDown.Maximum, s.RecentCount));
    }

    // Boxes -> settings. Only called when the user presses OK.
    private void OnOk(object sender, EventArgs e)
    {
        settings.Initials = initialsBox.Text.Trim().ToUpperInvariant();
        settings.OutputFolder = folderBox.Text.Trim();
        settings.AskBeforeOverwrite = overwriteCheck.Checked;
        settings.RecentCount = (int)recentUpDown.Value;
    }

    private void OnReset(object sender, EventArgs e)
    {
        ShowSettings(new ScriptSettings());                     // show the defaults, but save nothing until OK
    }

    private void OnBrowse(object sender, EventArgs e)
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.SelectedPath = folderBox.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                folderBox.Text = dialog.SelectedPath;
            }
        }
    }
}
