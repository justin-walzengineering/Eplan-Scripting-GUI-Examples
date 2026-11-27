// ============================================================
// Example   : 18 - Device list export (CSV, XML or text)
// Purpose   : Export the current project's device list in the format you pick, with a file name from a pattern, and open it (for example in Excel).
// You learn : The devicelist action, a format drop-down, opening a file with its default program, reusing the naming pattern idea
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 and 2027.0.3 (run in Eplan 2026; no device list data to check)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Diagnostics;              // Process
using System.Drawing;                  // Point, Size
using System.IO;                       // File, Directory, Path
using System.Windows.Forms;            // Form, controls
using Newtonsoft.Json;                 // JsonConvert
using Eplan.EplApi.ApplicationFramework;   // CommandLineInterpreter, ActionCallingContext
using Eplan.EplApi.Base;               // PathMap
using Eplan.EplApi.Scripting;          // The [Start] attribute

public class DeviceListExportDemo
{
    // True when the exported file holds no device list entries: a file of 0 bytes, or an XML file that says count="0".
    private static bool IsEmptyExport(string path)
    {
        try
        {
            FileInfo info = new FileInfo(path);
            if (info.Length == 0)
            {
                return true;
            }
            if (info.Length < 300 && path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return File.ReadAllText(path).Contains("count=\"0\"");
            }
        }
        catch (Exception)
        {
            // If we cannot tell, treat the export as fine.
        }
        return false;
    }

    [Start]
    public void Run()
    {
        DeviceListOptions options = DeviceListOptions.Load();

        using (DeviceListForm form = new DeviceListForm(options))
        {
            if (form.ShowDialog() != DialogResult.OK)
            {
                return;
            }
        }
        options.Save();

        // Each choice in the drop-down holds three values: the text the user sees, the exporter name Eplan expects, and the file extension.
        DeviceListFormat format = DeviceListFormat.All[options.FormatIndex];
        string fileName = DeviceListNaming.Apply(options.Pattern, DeviceListNaming.CurrentProjectName()) + format.Extension;
        string target = Path.Combine(options.Folder, fileName);

        if (File.Exists(target) && MessageBox.Show("The file already exists:\r\n" + target + "\r\n\r\nReplace it?", "Device list export",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(options.Folder);

            // "devicelist" is the action. TYPE=EXPORT writes the list; FORMAT picks the exporter.
            ActionCallingContext context = new ActionCallingContext();
            context.AddParameter("TYPE", "EXPORT");
            context.AddParameter("EXPORTFILE", target);
            context.AddParameter("FORMAT", format.ExporterName);
            new CommandLineInterpreter().Execute("devicelist", context);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The export did not finish:\r\n\r\n" + ex.Message, "Device list export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!File.Exists(target))
        {
            MessageBox.Show("The action ran, but I cannot see the file:\r\n" + target, "Device list export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // A project can have an empty device list. Then Eplan writes an empty file (or an XML file with count="0").
        // Say so, instead of announcing a successful export of nothing.
        if (IsEmptyExport(target))
        {
            MessageBox.Show("The export ran, but the device list of this project is empty, so the file has no entries:\r\n" + target,
                "Device list export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (options.OpenFile)
        {
            // UseShellExecute lets Windows open the file with its default program (for example Excel for .csv files).
            ProcessStartInfo start = new ProcessStartInfo(target);
            start.UseShellExecute = true;
            Process.Start(start);
        }
        else
        {
            MessageBox.Show("Exported to:\r\n" + target, "Device list export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

// A format the user can pick.
public class DeviceListFormat
{
    public string Display;        // text in the drop-down
    public string ExporterName;   // value for the FORMAT parameter
    public string Extension;      // file extension to give the file

    public static readonly DeviceListFormat[] All = new DeviceListFormat[]
    {
        new DeviceListFormat { Display = "CSV (opens in Excel)", ExporterName = "XDLCsvImporterExporter", Extension = ".csv" },
        new DeviceListFormat { Display = "XML", ExporterName = "XDLXmlExporter", Extension = ".xml" },
        new DeviceListFormat { Display = "Text", ExporterName = "XDLTxtImporterExporter", Extension = ".txt" }
    };
}

public static class DeviceListNaming
{
    public static string Apply(string pattern, string project)
    {
        string name = (pattern ?? "").Replace("{project}", project).Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd")).Replace("{time}", DateTime.Now.ToString("HHmm"));
        foreach (char bad in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(bad, '_');
        }
        name = name.Trim().TrimEnd('.');
        return name.Length == 0 ? "devicelist" : name;
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

public class DeviceListOptions
{
    public string Folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public string Pattern = "{project}_devicelist_{date}";
    public int FormatIndex = 0;
    public bool OpenFile = true;

    private static string FilePath
    {
        get { return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EplanScriptExamples"), "device-list-export.json"); }
    }

    public static DeviceListOptions Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                DeviceListOptions loaded = JsonConvert.DeserializeObject<DeviceListOptions>(File.ReadAllText(FilePath));
                if (loaded != null && loaded.FormatIndex >= 0 && loaded.FormatIndex < DeviceListFormat.All.Length) { return loaded; }
            }
        }
        catch (Exception) { }
        return new DeviceListOptions();
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

public class DeviceListForm : Form
{
    private readonly DeviceListOptions options;
    private readonly TextBox folderBox = new TextBox();
    private readonly TextBox patternBox = new TextBox();
    private readonly ComboBox formatBox = new ComboBox();
    private readonly CheckBox openCheck = new CheckBox();
    private readonly Label preview = new Label();

    public DeviceListForm(DeviceListOptions options)
    {
        this.options = options;

        Text = "Device list export";
        ClientSize = new Size(480, 230);
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

        AddLabel("File name", 12, 52);
        patternBox.Location = new Point(100, 49);
        patternBox.Width = 368;
        patternBox.TextChanged += delegate { UpdatePreview(); };

        AddLabel("Format", 12, 86);
        formatBox.Location = new Point(100, 83);
        formatBox.Width = 200;
        formatBox.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (DeviceListFormat f in DeviceListFormat.All) { formatBox.Items.Add(f.Display); }
        formatBox.SelectedIndexChanged += delegate { UpdatePreview(); };

        preview.Location = new Point(100, 114);
        preview.Size = new Size(368, 20);
        preview.AutoEllipsis = true;                           // a long name ends in "..." instead of being cut off

        openCheck.Text = "Open the file afterwards";
        openCheck.Location = new Point(100, 142);
        openCheck.AutoSize = true;

        Button export = new Button();
        export.Text = "Export";
        export.Location = new Point(312, 190);
        export.Size = new Size(75, 28);
        export.Click += OnExport;
        Button cancel = new Button();
        cancel.Text = "Cancel";
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(393, 190);
        cancel.Size = new Size(75, 28);
        AcceptButton = export;
        CancelButton = cancel;

        Controls.AddRange(new Control[] { folderBox, browse, patternBox, formatBox, preview, openCheck, export, cancel });

        folderBox.Text = options.Folder;
        patternBox.Text = options.Pattern;
        formatBox.SelectedIndex = options.FormatIndex;
        openCheck.Checked = options.OpenFile;
        UpdatePreview();
        DeviceListSkin.Apply(this);
    }

    private void AddLabel(string text, int x, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.Location = new Point(x, y);
        label.AutoSize = true;
        Controls.Add(label);
    }

    private void UpdatePreview()
    {
        int index = Math.Max(0, formatBox.SelectedIndex);
        preview.Text = "Result:  " + DeviceListNaming.Apply(patternBox.Text, DeviceListNaming.CurrentProjectName()) + DeviceListFormat.All[index].Extension;
    }

    private void OnExport(object sender, EventArgs e)
    {
        if (folderBox.Text.Trim().Length == 0 || patternBox.Text.Trim().Length == 0)
        {
            MessageBox.Show("Please choose a folder and a file name.", "Device list export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        options.Folder = folderBox.Text.Trim();
        options.Pattern = patternBox.Text.Trim();
        options.FormatIndex = Math.Max(0, formatBox.SelectedIndex);
        options.OpenFile = openCheck.Checked;
        DialogResult = DialogResult.OK;
    }
}

// ---------------------------------------------------------------
// Optional: follow Eplan's dark color scheme.
// Call DeviceListSkin.Apply(form) once your window is built. When Eplan is set to a light color scheme this does nothing,
// so the window keeps the normal Windows look. When Eplan is dark, the window, its title bar and its controls turn dark.
// You can copy this class into your own scripts. (Example 04 explains the idea step by step.)
// ---------------------------------------------------------------
public static class DeviceListSkin
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
