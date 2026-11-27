// ============================================================
// Example   : 21 - Favorites launcher
// Purpose   : A searchable list of your own favorite scripts and Eplan actions. Double-click one to run it.
// You learn : Filtering a list as you type, saving a list as JSON, running a script or an action from code, dialog result patterns
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 and 2027.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Collections.Generic;      // List<T>
using System.Drawing;                  // Point, Size
using System.IO;                       // File, Directory, Path
using System.Windows.Forms;            // Form, ListBox, TextBox, OpenFileDialog
using Newtonsoft.Json;                 // JsonConvert
using Eplan.EplApi.ApplicationFramework;   // CommandLineInterpreter, ActionCallingContext
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs.
// ---------------------------------------------------------------
public class FavoritesLauncherDemo
{
    [Start]
    public void Run()
    {
        FavoriteItem chosen;
        List<FavoriteItem> favorites = FavoritesStore.Load();

        // Show the window. It returns when the user closes it. If they pressed Run, "chosen" holds their choice.
        using (FavoritesForm form = new FavoritesForm(favorites))
        {
            form.ShowDialog();
            chosen = form.Chosen;
        }

        // The window may have added or removed favorites, so save the list either way.
        FavoritesStore.Save(favorites);

        if (chosen == null)
        {
            return;
        }

        // IMPORTANT: run the favorite only AFTER the window has closed. Running other scripts while our own window
        // is still open and modal can make them appear behind it or freeze.
        try
        {
            FavoritesRunner.Run(chosen);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not run \"" + chosen.Name + "\":\r\n\r\n" + ex.Message, "Favorites", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

// ---------------------------------------------------------------
// Part 2: the data and its storage.
// ---------------------------------------------------------------
public class FavoriteItem
{
    public string Name;
    public string Kind;      // "Script" (a .cs file) or "Action" (an Eplan action, such as XGedRedrawAction)
    public string Target;    // the script file path, or the action text
}

public static class FavoritesStore
{
    private static string FilePath
    {
        get
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EplanScriptExamples");
            return Path.Combine(folder, "favorites.json");
        }
    }

    public static List<FavoriteItem> Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                List<FavoriteItem> loaded = JsonConvert.DeserializeObject<List<FavoriteItem>>(File.ReadAllText(FilePath));
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch (Exception)
        {
        }

        // First run (or damaged file): start with one harmless sample so the list is not empty.
        List<FavoriteItem> sample = new List<FavoriteItem>();
        sample.Add(new FavoriteItem { Name = "Redraw the screen", Kind = "Action", Target = "XGedRedrawAction" });
        return sample;
    }

    public static void Save(List<FavoriteItem> items)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(items, Formatting.Indented));
        }
        catch (Exception)
        {
            // Not being able to save the list is annoying but should not stop the script.
        }
    }
}

public static class FavoritesRunner
{
    public static void Run(FavoriteItem item)
    {
        CommandLineInterpreter cli = new CommandLineInterpreter();

        if (item.Kind == "Script")
        {
            // Start a script file the same way a ribbon button does.
            ActionCallingContext context = new ActionCallingContext();
            context.AddParameter("ScriptFile", item.Target);
            cli.Execute("ExecuteScript", context);
        }
        else
        {
            // An Eplan action, written like on the command line: the name, then optional /PARAMETER:value pairs.
            cli.Execute(item.Target);
        }
    }
}

// ---------------------------------------------------------------
// Part 3: the windows.
// ---------------------------------------------------------------
public class FavoritesForm : Form
{
    private readonly List<FavoriteItem> favorites;
    private readonly TextBox searchBox = new TextBox();
    private readonly ListBox list = new ListBox();
    private readonly List<FavoriteItem> shown = new List<FavoriteItem>();   // what the list currently displays

    // The favorite the user chose to run (null if they just closed the window).
    public FavoriteItem Chosen;

    public FavoritesForm(List<FavoriteItem> favorites)
    {
        this.favorites = favorites;

        Text = "Favorites";
        ClientSize = new Size(440, 360);
        MinimumSize = new Size(360, 260);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        Label searchLabel = new Label();
        searchLabel.Text = "Search";
        searchLabel.Location = new Point(12, 16);
        searchLabel.AutoSize = true;

        searchBox.Location = new Point(64, 12);
        searchBox.Width = 364;
        searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        searchBox.TextChanged += delegate { Refill(); };                   // refilter on every key press
        searchBox.KeyDown += OnSearchKeyDown;

        list.Location = new Point(12, 44);
        list.Size = new Size(416, 262);
        list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        list.DoubleClick += delegate { RunSelected(); };

        Button run = MakeButton("Run", new Point(12, 320), AnchorStyles.Bottom | AnchorStyles.Left);
        run.Click += delegate { RunSelected(); };
        Button add = MakeButton("Add...", new Point(94, 320), AnchorStyles.Bottom | AnchorStyles.Left);
        add.Click += OnAdd;
        Button remove = MakeButton("Remove", new Point(176, 320), AnchorStyles.Bottom | AnchorStyles.Left);
        remove.Click += OnRemove;
        Button close = MakeButton("Close", new Point(353, 320), AnchorStyles.Bottom | AnchorStyles.Right);
        close.DialogResult = DialogResult.Cancel;
        CancelButton = close;

        Controls.AddRange(new Control[] { searchLabel, searchBox, list, run, add, remove, close });
        Shown += delegate { searchBox.Focus(); };
        Refill();
        FavoritesSkin.Apply(this);
    }

    private static Button MakeButton(string text, Point at, AnchorStyles anchor)
    {
        Button button = new Button();
        button.Text = text;
        button.Location = at;
        button.Size = new Size(76, 28);
        button.Anchor = anchor;
        return button;
    }

    // Rebuilds the list from the favorites that match the search text.
    private void Refill()
    {
        string needle = (searchBox.Text ?? "").Trim().ToLowerInvariant();
        shown.Clear();
        list.Items.Clear();
        foreach (FavoriteItem item in favorites)
        {
            string haystack = ((item.Name ?? "") + " " + (item.Kind ?? "") + " " + (item.Target ?? "")).ToLowerInvariant();
            if (needle.Length == 0 || haystack.Contains(needle))
            {
                shown.Add(item);
                list.Items.Add(item.Name + "   (" + item.Kind + ")");
            }
        }
        if (list.Items.Count > 0)
        {
            list.SelectedIndex = 0;
        }
    }

    private void RunSelected()
    {
        if (list.SelectedIndex < 0 || list.SelectedIndex >= shown.Count)
        {
            return;
        }
        Chosen = shown[list.SelectedIndex];
        DialogResult = DialogResult.OK;                                    // closes the window
    }

    // Enter runs the selected favorite. Down arrow moves from the search box into the list.
    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            RunSelected();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Down && list.Items.Count > 0)
        {
            list.Focus();
        }
    }

    private void OnAdd(object sender, EventArgs e)
    {
        using (AddFavoriteForm dialog = new AddFavoriteForm())
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                favorites.Add(dialog.Item);
                searchBox.Text = "";
                Refill();
            }
        }
    }

    private void OnRemove(object sender, EventArgs e)
    {
        if (list.SelectedIndex < 0 || list.SelectedIndex >= shown.Count)
        {
            return;
        }
        FavoriteItem item = shown[list.SelectedIndex];
        if (MessageBox.Show("Remove \"" + item.Name + "\" from your favorites?", "Favorites", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            favorites.Remove(item);
            Refill();
        }
    }
}

public class AddFavoriteForm : Form
{
    private readonly TextBox nameBox = new TextBox();
    private readonly ComboBox kindBox = new ComboBox();
    private readonly TextBox targetBox = new TextBox();
    private readonly Button browse = new Button();

    public FavoriteItem Item;

    public AddFavoriteForm()
    {
        Text = "Add a favorite";
        ClientSize = new Size(420, 170);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        AddLabel("Name", 12, 18);
        nameBox.Location = new Point(80, 15);
        nameBox.Width = 330;

        AddLabel("Type", 12, 52);
        kindBox.Location = new Point(80, 49);
        kindBox.DropDownStyle = ComboBoxStyle.DropDownList;
        kindBox.Items.AddRange(new object[] { "Script", "Action" });
        kindBox.SelectedIndex = 0;
        kindBox.SelectedIndexChanged += delegate { browse.Enabled = kindBox.Text == "Script"; };

        AddLabel("Target", 12, 86);
        targetBox.Location = new Point(80, 83);
        targetBox.Width = 250;
        browse.Text = "Browse...";
        browse.Location = new Point(336, 82);
        browse.Size = new Size(74, 24);
        browse.Click += OnBrowse;

        Label hint = new Label();
        hint.Text = "Script: the .cs file.   Action: the action name, for example XGedRedrawAction";
        hint.Location = new Point(12, 112);
        hint.Size = new Size(398, 18);

        Button ok = new Button();
        ok.Text = "OK";
        ok.Location = new Point(254, 134);
        ok.Size = new Size(75, 26);
        ok.Click += OnOk;
        Button cancel = new Button();
        cancel.Text = "Cancel";
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(335, 134);
        cancel.Size = new Size(75, 26);
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange(new Control[] { nameBox, kindBox, targetBox, browse, hint, ok, cancel });
        FavoritesSkin.Apply(this);
    }

    private void AddLabel(string text, int x, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.Location = new Point(x, y);
        label.AutoSize = true;
        Controls.Add(label);
    }

    private void OnBrowse(object sender, EventArgs e)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Filter = "C# scripts (*.cs)|*.cs|All files (*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                targetBox.Text = dialog.FileName;
                if (nameBox.Text.Trim().Length == 0)
                {
                    nameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                }
            }
        }
    }

    private void OnOk(object sender, EventArgs e)
    {
        // Validate before closing. Setting DialogResult only when the input is good keeps the window open otherwise.
        if (nameBox.Text.Trim().Length == 0 || targetBox.Text.Trim().Length == 0)
        {
            MessageBox.Show("Please enter a name and a target.", "Add a favorite", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Item = new FavoriteItem { Name = nameBox.Text.Trim(), Kind = kindBox.Text, Target = targetBox.Text.Trim() };
        DialogResult = DialogResult.OK;
    }
}

// ---------------------------------------------------------------
// Optional: follow Eplan's dark color scheme.
// Call FavoritesSkin.Apply(form) once your window is built. When Eplan is set to a light color scheme this does nothing,
// so the window keeps the normal Windows look. When Eplan is dark, the window, its title bar and its controls turn dark.
// You can copy this class into your own scripts. (Example 04 explains the idea step by step.)
// ---------------------------------------------------------------
public static class FavoritesSkin
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
