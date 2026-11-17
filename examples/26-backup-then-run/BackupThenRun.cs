// ============================================================
// Example   : 26 - Back up first, then run
// Purpose   : Make a timestamped backup of the project, and only if that worked, run a script or Eplan action you choose.
// You learn : Chaining two actions safely, stopping when the first fails, choosing a script or an action, doing risky work only after a safety step
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 and 2027.0.3 (Run in Eplan 2026)
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

        // Step 2: run the script or action you chose.
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
        BackupFirstSkin.Apply(this);
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

// ---------------------------------------------------------------
// Optional: follow Eplan's dark color scheme.
// Call BackupFirstSkin.Apply(form) once your window is built. When Eplan is set to a light color scheme this does nothing,
// so the window keeps the normal Windows look. When Eplan is dark, the window, its title bar and its controls turn dark.
// You can copy this class into your own scripts. (Example 04 explains the idea step by step.)
// ---------------------------------------------------------------
public static class BackupFirstSkin
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
