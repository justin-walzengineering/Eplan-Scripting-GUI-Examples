// ============================================================
// Example   : 24 - CSV preview and device list import
// Purpose   : Look at a CSV file in a grid, check it for common problems, and only then import it into the device list.
// You learn : Parsing CSV (quotes and delimiters), DataGridView, detecting a delimiter, validating before a risky step, the devicelist import action
// Level     : Advanced
// Tested in : Eplan Electric P8 2026.0.3 and 2027.0.3 (Run in Eplan 2026 (preview checked; import not confirmed with real data))
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
//
// THE IMPORT CHANGES YOUR PROJECT. Try it on a copy first.
// Tip: export a device list from Eplan first (example 18), edit that file in Excel, then check and import it here.
// ============================================================

using System;
using System.Collections.Generic;      // List<T>
using System.Drawing;                  // Point, Size, Color
using System.IO;                       // File, Path
using System.Text;                     // StringBuilder
using System.Windows.Forms;            // Form, DataGridView, OpenFileDialog
using Eplan.EplApi.ApplicationFramework;   // CommandLineInterpreter, ActionCallingContext
using Eplan.EplApi.Scripting;          // The [Start] attribute

public class CsvDeviceImportDemo
{
    [Start]
    public void Run()
    {
        using (CsvImportForm form = new CsvImportForm())
        {
            form.ShowDialog();
        }
    }
}

// ---------------------------------------------------------------
// Part 1: the CSV helper. It has no Eplan code in it, so you can reuse it anywhere.
// ---------------------------------------------------------------
public static class CsvReader
{
    // Looks at the first line and picks the delimiter (; , or tab) that appears most often outside quotes.
    public static char DetectDelimiter(string text)
    {
        string firstLine = text;
        int newline = text.IndexOfAny(new char[] { '\r', '\n' });
        if (newline >= 0)
        {
            firstLine = text.Substring(0, newline);
        }

        char best = ';';
        int bestCount = -1;
        foreach (char candidate in new char[] { ';', ',', '\t' })
        {
            int count = 0;
            bool inQuotes = false;
            foreach (char c in firstLine)
            {
                if (c == '"') { inQuotes = !inQuotes; }
                else if (c == candidate && !inQuotes) { count++; }
            }
            if (count > bestCount)
            {
                best = candidate;
                bestCount = count;
            }
        }
        return best;
    }

    // Splits CSV text into rows of cells. Handles "quoted, cells", doubled quotes ("") and line breaks inside quotes.
    public static List<List<string>> Parse(string text, char delimiter)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> row = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');                        // "" inside quotes means one quote character
                        i++;
                    }
                    else
                    {
                        inQuotes = false;                        // the closing quote
                    }
                }
                else
                {
                    cell.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == delimiter)
            {
                row.Add(cell.ToString());
                cell.Length = 0;
            }
            else if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;                                         // treat \r\n as one line break
                }
                row.Add(cell.ToString());
                cell.Length = 0;
                rows.Add(row);
                row = new List<string>();
            }
            else
            {
                cell.Append(c);
            }
        }

        // The last row, if the file does not end with a line break.
        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }
        return rows;
    }

    // Finds rows whose number of cells differs from the first row's. Returns 1-based row numbers.
    public static List<int> FindRaggedRows(List<List<string>> rows)
    {
        List<int> bad = new List<int>();
        if (rows.Count == 0)
        {
            return bad;
        }
        int expected = rows[0].Count;
        for (int i = 1; i < rows.Count; i++)
        {
            bool blankLine = rows[i].Count == 1 && rows[i][0].Length == 0;
            if (!blankLine && rows[i].Count != expected)
            {
                bad.Add(i + 1);
            }
        }
        return bad;
    }
}

// ---------------------------------------------------------------
// Part 2: the window.
// ---------------------------------------------------------------
public class CsvImportForm : Form
{
    private readonly TextBox pathBox = new TextBox();
    private readonly DataGridView grid = new DataGridView();
    private readonly Label status = new Label();
    private readonly Button importButton = new Button();
    private string loadedFile;

    public CsvImportForm()
    {
        Text = "CSV preview and device list import";
        ClientSize = new Size(780, 480);
        MinimumSize = new Size(560, 360);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        Label fileLabel = new Label();
        fileLabel.Text = "CSV file";
        fileLabel.Location = new Point(12, 18);
        fileLabel.AutoSize = true;

        pathBox.Location = new Point(70, 14);
        pathBox.Width = 600;
        pathBox.ReadOnly = true;
        pathBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        Button browse = new Button();
        browse.Text = "Open...";
        browse.Location = new Point(678, 13);
        browse.Size = new Size(90, 26);
        browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        browse.Click += OnOpen;

        grid.Location = new Point(12, 48);
        grid.Size = new Size(756, 336);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        grid.RowHeadersVisible = true;

        status.Location = new Point(12, 392);
        status.Size = new Size(756, 40);
        status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        status.Text = "Open a CSV file to see a preview.";

        importButton.Text = "Import into device list...";
        importButton.Location = new Point(564, 440);
        importButton.Size = new Size(140, 28);
        importButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        importButton.Enabled = false;
        importButton.Click += OnImport;

        Button close = new Button();
        close.Text = "Close";
        close.DialogResult = DialogResult.Cancel;
        close.Location = new Point(710, 440);
        close.Size = new Size(58, 28);
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        CancelButton = close;

        Controls.AddRange(new Control[] { fileLabel, pathBox, browse, grid, status, importButton, close });
        CsvImportSkin.Apply(this);
    }

    private void OnOpen(object sender, EventArgs e)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            LoadPreview(dialog.FileName);
        }
    }

    private void LoadPreview(string file)
    {
        try
        {
            string text = File.ReadAllText(file, Encoding.Default);    // Eplan's CSV is usually in the Windows ANSI encoding
            char delimiter = CsvReader.DetectDelimiter(text);
            List<List<string>> rows = CsvReader.Parse(text, delimiter);

            grid.Columns.Clear();
            grid.Rows.Clear();
            if (rows.Count == 0)
            {
                status.Text = "The file is empty.";
                importButton.Enabled = false;
                return;
            }

            // Columns first, then rows (adding rows before columns exist causes errors).
            int columnCount = rows[0].Count;
            for (int i = 0; i < columnCount; i++)
            {
                grid.Columns.Add("c" + i, rows[0][i].Length > 0 ? rows[0][i] : "(column " + (i + 1) + ")");
            }

            int shown = Math.Min(rows.Count, 201);                     // header plus up to 200 rows
            for (int r = 1; r < shown; r++)
            {
                object[] cells = new object[columnCount];
                for (int c = 0; c < columnCount; c++)
                {
                    cells[c] = c < rows[r].Count ? rows[r][c] : "";
                }
                grid.Rows.Add(cells);
            }

            List<int> ragged = CsvReader.FindRaggedRows(rows);
            string delimiterName = delimiter == '\t' ? "tab" : delimiter.ToString();
            string message = (rows.Count - 1) + " data rows, " + columnCount + " columns, delimiter \"" + delimiterName + "\".";
            if (rows.Count > 201)
            {
                message += "  Showing the first 200 rows.";
            }
            if (ragged.Count > 0)
            {
                message += "\r\nWARNING: " + ragged.Count + " row(s) have a different number of cells than the header, for example row " + ragged[0] + ". Fix the file before importing.";
            }
            status.Text = message;
            bool dark = CsvImportSkin.IsEplanDark();      // in a dark Eplan the text must be light
            Color normal = dark ? Color.FromArgb(240, 240, 240) : SystemColors.ControlText;
            Color problem = dark ? Color.FromArgb(255, 120, 120) : Color.Firebrick;
            status.ForeColor = ragged.Count > 0 ? problem : normal;

            loadedFile = file;
            pathBox.Text = file;
            importButton.Enabled = ragged.Count == 0;                  // only allow the import when the file looks consistent
        }
        catch (Exception ex)
        {
            MessageBox.Show("I could not read that file:\r\n\r\n" + ex.Message, "CSV preview", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnImport(object sender, EventArgs e)
    {
        if (MessageBox.Show("Import this file into the device list of the current project?\r\n\r\n" + loadedFile +
            "\r\n\r\nThis changes your project. Try it on a copy first.", "Import", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            // "devicelist" with TYPE=IMPORT reads a device list file. The CSV exporter/importer must match the file's layout,
            // which is why you should start from a file Eplan exported itself.
            ActionCallingContext context = new ActionCallingContext();
            context.AddParameter("TYPE", "IMPORT");
            context.AddParameter("IMPORTFILE", loadedFile);
            context.AddParameter("FORMAT", "XDLCsvImporterExporter");
            new CommandLineInterpreter().Execute("devicelist", context);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The import did not finish:\r\n\r\n" + ex.Message, "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        MessageBox.Show("The import action finished. Check the device list in Eplan.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

// ---------------------------------------------------------------
// Optional: follow Eplan's dark color scheme.
// Call CsvImportSkin.Apply(form) once your window is built. When Eplan is set to a light color scheme this does nothing,
// so the window keeps the normal Windows look. When Eplan is dark, the window, its title bar and its controls turn dark.
// You can copy this class into your own scripts. (Example 04 explains the idea step by step.)
// ---------------------------------------------------------------
public static class CsvImportSkin
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
