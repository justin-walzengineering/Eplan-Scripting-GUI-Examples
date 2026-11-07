// ============================================================
// Example   : 24 - CSV preview and device list import
// Purpose   : Look at a CSV file in a grid, check it for common problems, and only then import it into the device list.
// You learn : Parsing CSV (quotes and delimiters), DataGridView, detecting a delimiter, validating before a risky step, the devicelist import action
// Level     : Advanced
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026 (preview checked; import not confirmed with real data))
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
            status.ForeColor = ragged.Count > 0 ? Color.Firebrick : SystemColors.ControlText;

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
