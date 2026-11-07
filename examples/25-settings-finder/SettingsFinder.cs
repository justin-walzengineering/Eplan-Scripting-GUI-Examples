// ============================================================
// Example   : 25 - Settings finder (read-only)
// Purpose   : Search Eplan's settings by name or description, and see each one's default and current value, to answer "which setting controls this?".
// You learn : Reading XML with XmlReader, building a searchable list, ListView, the Settings class, why read-only tools are safe to try
// Level     : Intermediate
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (DRAFT: not yet run in Eplan)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Collections.Generic;      // List<T>, Stack<T>
using System.Diagnostics;              // Process
using System.Drawing;                  // Point, Size
using System.IO;                       // Directory, Path
using System.Text;                     // StringBuilder
using System.Windows.Forms;            // Form, ListView, TextBox, Clipboard
using System.Xml;                      // XmlReader
using Eplan.EplApi.Base;               // Settings: read the current value of a setting
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs. It never changes a setting: it only reads and shows.
// ---------------------------------------------------------------
public class SettingsFinderDemo
{
    [Start]
    public void Run()
    {
        string cfg = SettingsReader.FindConfigFolder();
        if (cfg == null)
        {
            MessageBox.Show("I could not find Eplan's Cfg folder.", "Settings finder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Cursor.Current = Cursors.WaitCursor;                  // reading hundreds of small files takes a moment
        List<SettingInfo> all = SettingsReader.ReadAll(cfg);
        Cursor.Current = Cursors.Default;

        using (SettingsFinderForm form = new SettingsFinderForm(all, cfg))
        {
            form.ShowDialog();
        }
    }
}

// One setting as described in Eplan's configuration files.
public class SettingInfo
{
    public string Path;       // for example USER.MF.GuiColorScheme
    public string Type;       // int, bool, string ...
    public string Info;       // the description text, if the file has one
    public string Default;    // the default value in the file
    public string File;       // which file it came from
}

// ---------------------------------------------------------------
// Part 2: reading the files.
// Eplan keeps the definition of its settings in XML files in its Cfg folder. They look like this:
//   <CAT name="USER"> <MOD name="MF"> <LEV1 name="..."> <Setting name="GuiColorScheme" type="int"> <Val>1</Val> ...
// The setting path is the names joined with dots:  USER.MF.GuiColorScheme
// ---------------------------------------------------------------
public static class SettingsReader
{
    // The program is in ...\Platform\<version>\Bin, and the Cfg folder is beside it.
    public static string FindConfigFolder()
    {
        try
        {
            string exe = Process.GetCurrentProcess().MainModule.FileName;
            string binFolder = Path.GetDirectoryName(exe);
            string cfg = Path.Combine(Path.GetDirectoryName(binFolder), "Cfg");
            return Directory.Exists(cfg) ? cfg : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static List<SettingInfo> ReadAll(string cfgFolder)
    {
        List<SettingInfo> result = new List<SettingInfo>();
        foreach (string file in Directory.GetFiles(cfgFolder, "*.xml", SearchOption.TopDirectoryOnly))
        {
            try
            {
                ReadFile(file, result);
            }
            catch (Exception)
            {
                // One unreadable file should not stop the rest.
            }
        }
        result.Sort(delegate(SettingInfo a, SettingInfo b) { return string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase); });
        return result;
    }

    private static void ReadFile(string file, List<SettingInfo> result)
    {
        XmlReaderSettings readerSettings = new XmlReaderSettings();
        readerSettings.DtdProcessing = DtdProcessing.Prohibit;      // we never need DTDs, and refusing them is safer
        readerSettings.IgnoreComments = true;

        // The "path so far": one entry per level we are inside, with the depth it was found at.
        List<KeyValuePair<int, string>> levels = new List<KeyValuePair<int, string>>();

        using (XmlReader reader = XmlReader.Create(file, readerSettings))
        {
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                string element = reader.Name;
                string name = reader.GetAttribute("name");
                int depth = reader.Depth;

                if (name != null && (element == "CAT" || element == "MOD" || element.StartsWith("LEV")))
                {
                    // Going down a level, or sideways: forget the levels at this depth or deeper first.
                    levels.RemoveAll(delegate(KeyValuePair<int, string> l) { return l.Key >= depth; });
                    levels.Add(new KeyValuePair<int, string>(depth, name));
                }
                else if (element == "Setting" && name != null)
                {
                    levels.RemoveAll(delegate(KeyValuePair<int, string> l) { return l.Key >= depth; });

                    SettingInfo info = new SettingInfo();
                    StringBuilder path = new StringBuilder();
                    foreach (KeyValuePair<int, string> l in levels)
                    {
                        path.Append(l.Value).Append('.');
                    }
                    info.Path = path.Append(name).ToString();
                    info.Type = reader.GetAttribute("type") ?? "";
                    info.Info = reader.GetAttribute("info") ?? "";
                    info.File = Path.GetFileName(file);
                    info.Default = reader.IsEmptyElement ? "" : FirstValue(reader);
                    result.Add(info);
                }
            }
        }
    }

    // Reads the text of the first <Val> inside the current <Setting> element.
    private static string FirstValue(XmlReader reader)
    {
        using (XmlReader sub = reader.ReadSubtree())
        {
            while (sub.Read())
            {
                if (sub.NodeType == XmlNodeType.Element && sub.Name == "Val")
                {
                    return sub.IsEmptyElement ? "" : sub.ReadElementContentAsString();
                }
            }
        }
        return "";
    }

    // Asks Eplan for the value a setting has RIGHT NOW. This only reads.
    public static string CurrentValue(string path)
    {
        try
        {
            Settings settings = new Settings();
            if (!settings.ExistSetting(path))
            {
                return "(not set in this session)";
            }
            return settings.GetStringSetting(path, 0);
        }
        catch (Exception ex)
        {
            return "(cannot be read: " + ex.Message + ")";
        }
    }
}

// ---------------------------------------------------------------
// Part 3: the window.
// ---------------------------------------------------------------
public class SettingsFinderForm : Form
{
    private readonly List<SettingInfo> all;
    private readonly List<SettingInfo> shown = new List<SettingInfo>();
    private readonly TextBox searchBox = new TextBox();
    private readonly ListView list = new ListView();
    private readonly TextBox details = new TextBox();
    private readonly Label countLabel = new Label();

    public SettingsFinderForm(List<SettingInfo> all, string cfgFolder)
    {
        this.all = all;

        Text = "Settings finder (read-only)";
        ClientSize = new Size(760, 500);
        MinimumSize = new Size(560, 360);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        Label searchLabel = new Label();
        searchLabel.Text = "Search";
        searchLabel.Location = new Point(12, 16);
        searchLabel.AutoSize = true;

        searchBox.Location = new Point(64, 12);
        searchBox.Width = 560;
        searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        searchBox.TextChanged += delegate { Refill(); };

        countLabel.Location = new Point(632, 16);
        countLabel.Size = new Size(116, 18);
        countLabel.TextAlign = ContentAlignment.MiddleRight;
        countLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        list.View = View.Details;
        list.FullRowSelect = true;
        list.HideSelection = false;
        list.Columns.Add("Setting", 360);
        list.Columns.Add("Type", 60);
        list.Columns.Add("Default", 120);
        list.Columns.Add("Description", 400);
        list.Location = new Point(12, 44);
        list.Size = new Size(736, 308);
        list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        list.SelectedIndexChanged += delegate { ShowDetails(); };

        details.Multiline = true;
        details.ReadOnly = true;
        details.Location = new Point(12, 360);
        details.Size = new Size(736, 96);
        details.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Button copy = new Button();
        copy.Text = "Copy path";
        copy.Location = new Point(12, 462);
        copy.Size = new Size(90, 28);
        copy.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        copy.Click += delegate
        {
            if (list.SelectedIndices.Count > 0)
            {
                Clipboard.SetText(shown[list.SelectedIndices[0]].Path);
            }
        };

        Button close = new Button();
        close.Text = "Close";
        close.DialogResult = DialogResult.OK;
        close.Location = new Point(673, 462);
        close.Size = new Size(75, 28);
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        CancelButton = close;

        Controls.AddRange(new Control[] { searchLabel, searchBox, countLabel, list, details, copy, close });
        Shown += delegate { searchBox.Focus(); };
        Refill();
    }

    // Shows the settings that match every word typed in the search box (in the path or the description).
    private void Refill()
    {
        string[] words = (searchBox.Text ?? "").Trim().ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        shown.Clear();
        foreach (SettingInfo s in all)
        {
            string haystack = (s.Path + " " + s.Info).ToLowerInvariant();
            bool matches = true;
            foreach (string word in words)
            {
                if (!haystack.Contains(word))
                {
                    matches = false;
                    break;
                }
            }
            if (matches)
            {
                shown.Add(s);
                if (shown.Count >= 1500)
                {
                    break;                                   // a very long list is slow to draw and no use to read
                }
            }
        }

        list.BeginUpdate();                                  // draw once at the end, not after every row
        list.Items.Clear();
        foreach (SettingInfo s in shown)
        {
            list.Items.Add(new ListViewItem(new string[] { s.Path, s.Type, s.Default, s.Info }));
        }
        list.EndUpdate();
        countLabel.Text = shown.Count + (shown.Count >= 1500 ? "+" : "") + " settings";
        details.Text = "";
    }

    private void ShowDetails()
    {
        if (list.SelectedIndices.Count == 0)
        {
            return;
        }
        SettingInfo s = shown[list.SelectedIndices[0]];
        details.Text = s.Path + "\r\n" +
                       "Type: " + s.Type + "     Default in file: " + s.Default + "     File: " + s.File + "\r\n" +
                       "Current value: " + SettingsReader.CurrentValue(s.Path) + "\r\n" +
                       (s.Info.Length > 0 ? s.Info : "(no description in the file)");
    }
}
