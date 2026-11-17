// ============================================================
// Example   : 11 - Parts viewer with filter box
// Purpose   : Browse the parts database in a table and narrow it down as you type, to find a part number, type or manufacturer quickly.
// You learn : Using Eplan's MasterData parts classes directly (no reflection), filtering a large list, protecting against huge results
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 and 2027.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
//
// Read-only: this example never changes the parts database.
// ============================================================

using System;
using System.Collections.Generic;      // List<T>
using System.Drawing;                  // Point, Size
using System.Text;                     // StringBuilder
using System.Windows.Forms;            // Form, ListView, TextBox, Clipboard
using Eplan.EplApi.MasterData;         // MDPartsManagement, MDPartsDatabase, MDPart, MDObjectFilter (available to scripts by default)
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs.
// ---------------------------------------------------------------
public class PartsViewerDemo
{
    [Start]
    public void Run()
    {
        // A big parts database can hold hundreds of thousands of parts, so ask before loading everything.
        if (MessageBox.Show("Load the parts database into a table?\r\n\r\nA large database can take a while.", "Parts viewer",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        List<string[]> parts;
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            parts = LoadParts();
        }
        catch (Exception ex)
        {
            MessageBox.Show("I could not read the parts database:\r\n\r\n" + ex.Message, "Parts viewer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }

        using (PartsViewerForm form = new PartsViewerForm(parts))
        {
            form.ShowDialog();
        }
    }

    // Reads every part and stores six values for each: part number, type number, description, manufacturer, order number, product group.
    private static List<string[]> LoadParts()
    {
        List<string[]> rows = new List<string[]>();

        MDPartsManagement management = new MDPartsManagement();
        MDPartsDatabase database = management.OpenDatabase();      // the parts database that is currently set up in Eplan
        try
        {
            // An empty filter means "all parts".
            MDPart[] found = database.GetParts(new MDObjectFilter());
            foreach (MDPart part in found)
            {
                rows.Add(new string[]
                {
                    Safe(delegate { return part.Properties.ARTICLE_PARTNR.ToString(); }),
                    Safe(delegate { return part.Properties.ARTICLE_TYPENR.ToString(); }),
                    Safe(delegate { return PartsText.OneLanguage(part.Properties.ARTICLE_DESCR1.ToString()); }),
                    Safe(delegate { return part.Properties.ARTICLE_MANUFACTURER.ToString(); }),
                    Safe(delegate { return part.Properties.ARTICLE_ORDERNR.ToString(); }),
                    Safe(delegate { return part.Properties.ARTICLE_PRODUCTGROUP.ToString(); })
                });
            }
        }
        finally
        {
            database.Close();                                      // always close the database again
        }
        return rows;
    }

    // Reading some properties can throw an exception, so each read is wrapped: a failed read just gives "".
    private static string Safe(Func<string> read)
    {
        try
        {
            return read() ?? "";
        }
        catch (Exception)
        {
            return "";
        }
    }
}

// Eplan can store one text in several languages at once, written like "en_US@Relay;de_DE@Relais".
// For a table we want just one language, so this picks English if it is there, otherwise the first one.
public static class PartsText
{
    public static string OneLanguage(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw.IndexOf('@') < 0)
        {
            return raw ?? "";                                  // a plain text: nothing to do
        }

        string first = null;
        foreach (string piece in raw.Split(';'))
        {
            // A language piece starts with a code like "en_US@" (five characters, then @).
            if (piece.Length > 6 && piece[5] == '@')
            {
                string text = piece.Substring(6).Trim();
                if (piece.StartsWith("en_US@"))
                {
                    return text;
                }
                if (first == null)
                {
                    first = text;
                }
            }
        }
        return first ?? raw;
    }
}

// ---------------------------------------------------------------
// Part 2: the window.
// ---------------------------------------------------------------
public class PartsViewerForm : Form
{
    // Drawing tens of thousands of rows is slow and nobody reads them all, so we show at most this many.
    private const int MaxShown = 2000;

    private readonly List<string[]> all;
    private readonly List<string[]> shown = new List<string[]>();
    private readonly TextBox searchBox = new TextBox();
    private readonly ListView list = new ListView();
    private readonly Label countLabel = new Label();

    public PartsViewerForm(List<string[]> parts)
    {
        all = parts;

        Text = "Parts viewer (read-only)";
        ClientSize = new Size(900, 520);
        MinimumSize = new Size(560, 360);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        Label searchLabel = new Label();
        searchLabel.Text = "Search";
        searchLabel.Location = new Point(12, 16);
        searchLabel.AutoSize = true;

        searchBox.Location = new Point(64, 12);
        searchBox.Width = 600;
        searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        searchBox.TextChanged += delegate { Refill(); };

        countLabel.Location = new Point(672, 16);
        countLabel.Size = new Size(216, 18);
        countLabel.TextAlign = ContentAlignment.MiddleRight;
        countLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        list.View = View.Details;
        list.FullRowSelect = true;
        list.HideSelection = false;
        list.Columns.Add("Part number", 150);
        list.Columns.Add("Type number", 150);
        list.Columns.Add("Description", 280);
        list.Columns.Add("Manufacturer", 110);
        list.Columns.Add("Order number", 110);
        list.Columns.Add("Product group", 100);
        list.Location = new Point(12, 44);
        list.Size = new Size(876, 424);
        list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Button copy = new Button();
        copy.Text = "Copy part number";
        copy.Location = new Point(12, 478);
        copy.Size = new Size(130, 28);
        copy.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        copy.Click += delegate
        {
            if (list.SelectedIndices.Count > 0)
            {
                Clipboard.SetText(shown[list.SelectedIndices[0]][0]);
            }
        };

        Button close = new Button();
        close.Text = "Close";
        close.DialogResult = DialogResult.Cancel;
        close.Location = new Point(813, 478);
        close.Size = new Size(75, 28);
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        CancelButton = close;

        Controls.AddRange(new Control[] { searchLabel, searchBox, countLabel, list, copy, close });
        Shown += delegate { searchBox.Focus(); };
        Refill();
        PartsSkin.Apply(this);
    }

    // Shows the parts that contain every word typed in the search box, in any column.
    private void Refill()
    {
        string[] words = (searchBox.Text ?? "").Trim().ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        shown.Clear();
        int matches = 0;
        foreach (string[] part in all)
        {
            string haystack = string.Join(" ", part).ToLowerInvariant();
            bool ok = true;
            foreach (string word in words)
            {
                if (!haystack.Contains(word)) { ok = false; break; }
            }
            if (!ok)
            {
                continue;
            }
            matches++;
            if (shown.Count < MaxShown)
            {
                shown.Add(part);
            }
        }

        list.BeginUpdate();                                        // draw once at the end
        list.Items.Clear();
        foreach (string[] part in shown)
        {
            list.Items.Add(new ListViewItem(part));
        }
        list.EndUpdate();
        countLabel.Text = matches + " of " + all.Count + " parts" + (matches > MaxShown ? " (first " + MaxShown + " shown)" : "");
    }
}

// ---------------------------------------------------------------
// Optional: follow Eplan's dark color scheme.
// Call PartsSkin.Apply(form) once your window is built. When Eplan is set to a light color scheme this does nothing,
// so the window keeps the normal Windows look. When Eplan is dark, the window, its title bar and its controls turn dark.
// You can copy this class into your own scripts. (Example 04 explains the idea step by step.)
// ---------------------------------------------------------------
public static class PartsSkin
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
