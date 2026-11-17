// ============================================================
// Example   : 17 - Export launcher (PDF)
// Purpose   : A small window to export the current project to PDF with a file name you choose from a pattern, then open the folder.
// You learn : Running an Eplan action with parameters, file name patterns, sanitizing names, remembering the last choices, confirm before overwriting
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 and 2027.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Diagnostics;              // Process
using System.Drawing;                  // Point, Size
using System.IO;                       // File, Directory, Path
using System.Windows.Forms;            // Form, controls, FolderBrowserDialog
using Newtonsoft.Json;                 // JsonConvert
using Eplan.EplApi.ApplicationFramework;   // CommandLineInterpreter, ActionCallingContext
using Eplan.EplApi.Base;               // PathMap
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs.
// ---------------------------------------------------------------
public class ExportLauncherDemo
{
    [Start]
    public void Run()
    {
        ExportOptions options = ExportOptions.Load();

        using (ExportForm form = new ExportForm(options))
        {
            if (form.ShowDialog() != DialogResult.OK)
            {
                return;
            }
        }
        options.Save();                                          // remember the choices for next time

        // The file name WITHOUT an extension: Eplan adds ".pdf" itself.
        string baseName = ExportNaming.Apply(options.Pattern, ExportNaming.CurrentProjectName());
        string target = Path.Combine(options.Folder, baseName);

        // Do not overwrite silently.
        if (File.Exists(target + ".pdf"))
        {
            if (MessageBox.Show("The file already exists:\r\n" + target + ".pdf\r\n\r\nReplace it?", "Export launcher",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }
        }

        DateTime started = DateTime.Now.AddSeconds(-2);   // used below to recognise the file this run creates

        try
        {
            Directory.CreateDirectory(options.Folder);
            if (File.Exists(target + ".pdf")) { File.Delete(target + ".pdf"); }   // the user chose Replace above

            // Build the action's parameters, as you would type them on Eplan's command line.
            // "export" is the action. TYPE=PDFPROJECTSCHEME exports the whole project to one PDF file.
            ActionCallingContext context = new ActionCallingContext();
            context.AddParameter("TYPE", "PDFPROJECTSCHEME");
            context.AddParameter("EXPORTFILE", target);
            if (options.Language.Length > 0)
            {
                context.AddParameter("LANGUAGE", options.Language);       // for example en_US (case matters)
            }
            if (options.Scheme.Length > 0)
            {
                context.AddParameter("EXPORTSCHEME", options.Scheme);     // blank = Eplan uses the scheme you used last
            }

            new CommandLineInterpreter().Execute("export", context);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The export did not finish:\r\n\r\n" + ex.Message, "Export launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // In Eplan 2026, the action chooses its own file name (the project name plus the date and time) and ignores the
        // name we asked for, so we find the file it just made and give it the name from the pattern.
        if (!File.Exists(target + ".pdf"))
        {
            ExportNaming.RenameNewestPdf(options.Folder, started, target + ".pdf");
        }

        if (!File.Exists(target + ".pdf"))
        {
            MessageBox.Show("The action ran, but I cannot see the file:\r\n" + target + ".pdf", "Export launcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (options.OpenFolder)
        {
            // "/select," makes Explorer open the folder with the new file highlighted.
            Process.Start("explorer.exe", "/select,\"" + target + ".pdf\"");
        }
        else
        {
            MessageBox.Show("Exported to:\r\n" + target + ".pdf", "Export launcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

// ---------------------------------------------------------------
// Part 2: naming helpers and saved options.
// ---------------------------------------------------------------
public static class ExportNaming
{
    // Finds the newest PDF in the folder that was written after "since" and renames it to "newPath".
    // Does nothing (and does not throw) if there is no such file.
    public static void RenameNewestPdf(string folder, DateTime since, string newPath)
    {
        try
        {
            string newest = null;
            DateTime newestTime = since;
            foreach (string file in Directory.GetFiles(folder, "*.pdf"))
            {
                DateTime written = File.GetLastWriteTime(file);
                if (written >= newestTime)
                {
                    newest = file;
                    newestTime = written;
                }
            }
            if (newest != null)
            {
                File.Move(newest, newPath);
            }
        }
        catch (Exception)
        {
        }
    }

    // Replaces {project}, {date} and {time} in the pattern, then removes characters Windows does not allow in a file name.
    public static string Apply(string pattern, string project)
    {
        string name = (pattern ?? "").Replace("{project}", project).Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd")).Replace("{time}", DateTime.Now.ToString("HHmm"));
        foreach (char bad in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(bad, '_');
        }
        name = name.Trim().TrimEnd('.');
        return name.Length == 0 ? "export" : name;
    }

    // The name of the selected project, from Eplan's path variable $(PROJECTNAME). Returns "Project" if it is not available.
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
        catch (Exception)
        {
        }
        return "Project";
    }
}

public class ExportOptions
{
    public string Folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public string Pattern = "{project}_{date}";
    public string Language = "";
    public string Scheme = "";
    public bool OpenFolder = true;

    private static string FilePath
    {
        get { return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EplanScriptExamples"), "export-launcher.json"); }
    }

    public static ExportOptions Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                ExportOptions loaded = JsonConvert.DeserializeObject<ExportOptions>(File.ReadAllText(FilePath));
                if (loaded != null) { return loaded; }
            }
        }
        catch (Exception) { }
        return new ExportOptions();
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

// ---------------------------------------------------------------
// Part 3: the window.
// ---------------------------------------------------------------
public class ExportForm : Form
{
    private readonly ExportOptions options;
    private readonly TextBox folderBox = new TextBox();
    private readonly TextBox patternBox = new TextBox();
    private readonly TextBox languageBox = new TextBox();
    private readonly TextBox schemeBox = new TextBox();
    private readonly CheckBox openCheck = new CheckBox();
    private readonly Label preview = new Label();

    public ExportForm(ExportOptions options)
    {
        this.options = options;

        Text = "Export launcher (PDF)";
        ClientSize = new Size(480, 270);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        AddLabel("Folder", 12, 18);
        folderBox.Location = new Point(110, 15);
        folderBox.Width = 280;
        Button browse = new Button();
        browse.Text = "Browse...";
        browse.Location = new Point(396, 14);
        browse.Size = new Size(72, 24);
        browse.Click += OnBrowse;

        AddLabel("File name", 12, 52);
        patternBox.Location = new Point(110, 49);
        patternBox.Width = 358;
        patternBox.TextChanged += delegate { UpdatePreview(); };

        Label help = new Label();
        help.Text = "Use {project}, {date} and {time} in the name.";
        help.Location = new Point(110, 74);
        help.AutoSize = true;
        help.ForeColor = SystemColors.GrayText;

        preview.Location = new Point(110, 96);
        preview.Size = new Size(358, 32);

        AddLabel("Language", 12, 138);
        languageBox.Location = new Point(110, 135);
        languageBox.Width = 90;
        AddLabel("(optional, for example en_US)", 206, 138);

        AddLabel("Export scheme", 12, 170);
        schemeBox.Location = new Point(110, 167);
        schemeBox.Width = 200;
        AddLabel("(optional)", 316, 170);

        openCheck.Text = "Show the file in Explorer afterwards";
        openCheck.Location = new Point(110, 198);
        openCheck.AutoSize = true;

        Button export = new Button();
        export.Text = "Export";
        export.Location = new Point(312, 230);
        export.Size = new Size(75, 28);
        export.Click += OnExport;
        Button cancel = new Button();
        cancel.Text = "Cancel";
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(393, 230);
        cancel.Size = new Size(75, 28);
        AcceptButton = export;
        CancelButton = cancel;

        Controls.AddRange(new Control[] { folderBox, browse, patternBox, help, preview, languageBox, schemeBox, openCheck, export, cancel });

        folderBox.Text = options.Folder;
        patternBox.Text = options.Pattern;
        languageBox.Text = options.Language;
        schemeBox.Text = options.Scheme;
        openCheck.Checked = options.OpenFolder;
        UpdatePreview();
        ExportSkin.Apply(this);
    }

    private void AddLabel(string text, int x, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.Location = new Point(x, y);
        label.AutoSize = true;
        Controls.Add(label);
    }

    // Shows the file name the current pattern would give.
    private void UpdatePreview()
    {
        preview.Text = "Result:  " + ExportNaming.Apply(patternBox.Text, ExportNaming.CurrentProjectName()) + ".pdf";
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

    private void OnExport(object sender, EventArgs e)
    {
        if (folderBox.Text.Trim().Length == 0 || patternBox.Text.Trim().Length == 0)
        {
            MessageBox.Show("Please choose a folder and a file name.", "Export launcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        options.Folder = folderBox.Text.Trim();
        options.Pattern = patternBox.Text.Trim();
        options.Language = languageBox.Text.Trim();
        options.Scheme = schemeBox.Text.Trim();
        options.OpenFolder = openCheck.Checked;
        DialogResult = DialogResult.OK;
    }
}

// ---------------------------------------------------------------
// Optional: follow Eplan's dark color scheme.
// Call ExportSkin.Apply(form) once your window is built. When Eplan is set to a light color scheme this does nothing,
// so the window keeps the normal Windows look. When Eplan is dark, the window, its title bar and its controls turn dark.
// You can copy this class into your own scripts. (Example 04 explains the idea step by step.)
// ---------------------------------------------------------------
public static class ExportSkin
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
