// ============================================================
// Example   : 11 - Parts viewer with filter box
// Purpose   : Browse the parts database in a table and narrow it down as you type, to find a part number, type or manufacturer quickly.
// You learn : Using Eplan's MasterData parts classes directly (no reflection), filtering a large list, protecting against huge results
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026)
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
                    Safe(delegate { return part.Properties.ARTICLE_DESCR1.ToString(); }),
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
