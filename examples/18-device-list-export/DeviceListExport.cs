// ============================================================
// Example   : 18 - Device list export (CSV, XML or text)
// Purpose   : Export the current project's device list in the format you pick, with a file name from a pattern, and open it (for example in Excel).
// You learn : The devicelist action, a format drop-down, opening a file with its default program, reusing the naming pattern idea
// Level     : Intermediate
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (DRAFT: not yet run in Eplan)
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

        // Each choice in the drop-down is a pair: the text the user sees, the exporter name Eplan expects, and the file extension.
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

        if (options.OpenFile)
        {
            // UseShellExecute lets Windows open the file with whatever program is set for it (Excel for .csv, and so on).
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
