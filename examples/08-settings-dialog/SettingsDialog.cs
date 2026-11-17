// ============================================================
// Example   : 08 - Settings dialog that saves preferences
// Purpose   : Let the user change a few options in a window and keep them between runs, in a small JSON file.
// You learn : A settings class, JSON with Newtonsoft.Json, defaults, handling a missing or damaged file, OK / Cancel / Reset
// Level     : Beginner
// Tested in : Eplan Electric P8 2026.0.3 and 2027.0.3 (Run in Eplan 2026)
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
        // Show the folder with the Windows user name replaced by %USERPROFILE%, so a screenshot does not give the name away.
        string folder = System.IO.Path.GetDirectoryName(ScriptSettings.FilePath);
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(profile) && folder.StartsWith(profile, StringComparison.OrdinalIgnoreCase))
        {
            folder = "%USERPROFILE%" + folder.Substring(profile.Length);
        }
        MessageBox.Show("Your settings were saved.\r\n\r\nFolder:\r\n" + folder + "\r\n\r\nFile: " + System.IO.Path.GetFileName(ScriptSettings.FilePath),
            "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        SettingsSkin.Apply(this);
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

// ---------------------------------------------------------------
// Optional: follow Eplan's dark color scheme.
// Call SettingsSkin.Apply(form) once your window is built. When Eplan is set to a light color scheme this does nothing,
// so the window keeps the normal Windows look. When Eplan is dark, the window, its title bar and its controls turn dark.
// You can copy this class into your own scripts. (Example 04 explains the idea step by step.)
// ---------------------------------------------------------------
public static class SettingsSkin
{
    // A Windows function that colors the title bar. (See example 03 for what [DllImport] means.)
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private static readonly Color Back = Color.FromArgb(32, 32, 32);      // window background
    private static readonly Color Field = Color.FromArgb(45, 45, 45);     // text boxes, buttons, lists
    private static readonly Color Header = Color.FromArgb(55, 55, 55);    // list and grid column headers
    private static readonly Color Text = Color.FromArgb(240, 240, 240);   // text
    private static readonly Color Border = Color.FromArgb(90, 90, 90);    // outlines and grid lines
    private static readonly Color Accent = Color.FromArgb(0, 120, 212);   // selected rows

    // Eplan keeps the color scheme in the setting USER.MF.GuiColorScheme: 0 = automatic (follow Windows), 1 = dark, 2 = light.
    public static bool IsEplanDark()
    {
        try
        {
            int scheme = new Eplan.EplApi.Base.Settings().GetNumericSetting("USER.MF.GuiColorScheme", 0);
            if (scheme == 1) { return true; }
            if (scheme == 2) { return false; }
            object value = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
            return (value is int) && ((int)value == 0);               // automatic: ask Windows
        }
        catch (Exception)
        {
            return false;                                              // cannot tell: keep the normal look
        }
    }

    public static void Apply(Form form)
    {
        try
        {
            if (!IsEplanDark())
            {
                return;
            }
            Style(form);
            if (form.IsHandleCreated) { SetTitleBar(form); }
            else { form.HandleCreated += delegate { SetTitleBar(form); }; }
        }
        catch (Exception)
        {
            // Styling is cosmetic: if it fails, the window simply keeps its normal look.
        }
    }

    // Walks through a control and everything inside it, and sets the colors that suit its kind.
    private static void Style(Control c)
    {
        c.ForeColor = Text;

        if (c is Form)
        {
            c.BackColor = Back;
        }
        else if (c is TextBox)
        {
            c.BackColor = Field;
            ((TextBox)c).BorderStyle = BorderStyle.FixedSingle;
        }
        else if (c is ComboBox)
        {
            c.BackColor = Field;
            ((ComboBox)c).FlatStyle = FlatStyle.Flat;
        }
        else if (c is NumericUpDown)
        {
            c.BackColor = Field;
        }
        else if (c is Button)
        {
            Button b = (Button)c;
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = Field;
            b.FlatAppearance.BorderColor = Border;
            b.UseVisualStyleBackColor = false;
        }
        else if (c is ListView)
        {
            StyleList((ListView)c);
        }
        else if (c is DataGridView)
        {
            StyleGrid((DataGridView)c);
        }

        foreach (Control child in c.Controls)
        {
            Style(child);
        }
    }

    // A list in "Details" view draws its column headers with Windows colors, so we draw them ourselves.
    private static void StyleList(ListView list)
    {
        list.BackColor = Field;
        list.ForeColor = Text;
        list.BorderStyle = BorderStyle.FixedSingle;
        list.OwnerDraw = true;
        list.DrawColumnHeader += delegate(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(Header))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            using (Pen pen = new Pen(Border))
            {
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
            Rectangle area = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, list.Font, area, Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        };
        list.DrawItem += delegate(object sender, DrawListViewItemEventArgs e) { e.DrawDefault = true; };
        list.DrawSubItem += delegate(object sender, DrawListViewSubItemEventArgs e) { e.DrawDefault = true; };
    }

    private static void StyleGrid(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;                       // otherwise Windows ignores our header colors
        grid.BackgroundColor = Back;
        grid.GridColor = Border;
        grid.BorderStyle = BorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Header;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Header;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Text;
        grid.RowHeadersDefaultCellStyle.BackColor = Header;
        grid.DefaultCellStyle.BackColor = Field;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = Accent;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
    }

    // 20 = dark title bar. 35 and 36 = caption and text color (Windows 11). A call may fail on older Windows; that is fine.
    private static void SetTitleBar(Form form)
    {
        try
        {
            int useDark = 1;
            if (DwmSetWindowAttribute(form.Handle, 20, ref useDark, 4) != 0)
            {
                DwmSetWindowAttribute(form.Handle, 19, ref useDark, 4);
            }
            int caption = Back.R | (Back.G << 8) | (Back.B << 16);
            int text = Text.R | (Text.G << 8) | (Text.B << 16);
            DwmSetWindowAttribute(form.Handle, 35, ref caption, 4);
            DwmSetWindowAttribute(form.Handle, 36, ref text, 4);
        }
        catch (Exception)
        {
        }
    }
}
