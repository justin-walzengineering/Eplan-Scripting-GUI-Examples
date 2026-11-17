// ============================================================
// Example   : 19 - Renumber helper
// Purpose   : A simple window in front of Eplan's renumber action: choose what to renumber, set the start and step, and confirm before it runs.
// You learn : Running the renumber action, enabling a button only when a safety box is checked, enabling controls based on the user's choices, confirming a change
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 and 2027.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
//
// THIS CHANGES YOUR PROJECT. Try it on a copy first.
// ============================================================

using System;
using System.Drawing;                  // Point, Size, Color
using System.Windows.Forms;            // Form, controls
using Eplan.EplApi.ApplicationFramework;   // CommandLineInterpreter, ActionCallingContext
using Eplan.EplApi.Scripting;          // The [Start] attribute

public class RenumberHelperDemo
{
    [Start]
    public void Run()
    {
        RenumberChoice choice;
        using (RenumberForm form = new RenumberForm())
        {
            if (form.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            choice = form.Choice;
        }

        // One more chance to cancel before anything changes.
        string summary = "Renumber " + choice.TypeText.ToLowerInvariant() +
                         (choice.OnlySelected ? " (only the selected ones)" : " (all of them)") +
                         ", starting at " + choice.Start + " in steps of " + choice.Step + "." +
                         (choice.TypeCode == "DEVICES" ? "\r\n" + (choice.OnlyUnnumbered ? "Only tags that still contain a \"?\"." : "ALL device tags.") : "");
        if (MessageBox.Show(summary + "\r\n\r\nThis changes your project. Continue?", "Renumber helper",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            // "renumber" is Eplan's numbering action. The parameters mirror the options in the window.
            ActionCallingContext context = new ActionCallingContext();
            context.AddParameter("TYPE", choice.TypeCode);
            context.AddParameter("USESELECTION", choice.OnlySelected ? "1" : "0");
            context.AddParameter("STARTVALUE", choice.Start.ToString());
            context.AddParameter("STEPVALUE", choice.Step.ToString());
            if (choice.TypeCode == "DEVICES")
            {
                context.AddParameter("POSTNUMERATE", choice.OnlyUnnumbered ? "1" : "0");   // 1 = only tags that still have a "?"
            }
            new CommandLineInterpreter().Execute("renumber", context);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The renumbering did not finish:\r\n\r\n" + ex.Message, "Renumber helper", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show("The renumber action finished.", "Renumber helper", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

// What the user chose, in one small object.
public class RenumberChoice
{
    public string TypeCode;        // value for the TYPE parameter
    public string TypeText;        // words for the user
    public bool OnlySelected;
    public bool OnlyUnnumbered;    // devices only
    public int Start;
    public int Step;
}

public class RenumberForm : Form
{
    // The choices: what the user sees and the TYPE value Eplan expects.
    private static readonly string[][] Types = new string[][]
    {
        new string[] { "Devices", "DEVICES" },
        new string[] { "Pages", "PAGES" },
        new string[] { "Terminals", "TERMINALS" },
        new string[] { "Cables", "CABLES" },
        new string[] { "Connections", "CONNECTIONS" }
    };

    private readonly ComboBox typeBox = new ComboBox();
    private readonly CheckBox selectedCheck = new CheckBox();
    private readonly CheckBox unnumberedCheck = new CheckBox();
    private readonly NumericUpDown startBox = new NumericUpDown();
    private readonly NumericUpDown stepBox = new NumericUpDown();
    private readonly CheckBox safeCheck = new CheckBox();
    private readonly Button run = new Button();

    public RenumberChoice Choice;

    public RenumberForm()
    {
        Text = "Renumber helper";
        ClientSize = new Size(440, 330);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        AddLabel("Renumber", 16, 20);
        typeBox.Location = new Point(130, 17);
        typeBox.Width = 160;
        typeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (string[] t in Types) { typeBox.Items.Add(t[0]); }
        typeBox.SelectedIndex = 0;
        typeBox.SelectedIndexChanged += delegate { UpdateEnabled(); };

        selectedCheck.Text = "Only the objects I have selected";
        selectedCheck.Location = new Point(130, 52);
        selectedCheck.AutoSize = true;

        unnumberedCheck.Text = "Devices: only tags that still contain a \"?\"";
        unnumberedCheck.Location = new Point(130, 80);
        unnumberedCheck.AutoSize = true;
        unnumberedCheck.Checked = true;

        AddLabel("Start at", 16, 118);
        startBox.Location = new Point(130, 115);
        startBox.Width = 80;
        startBox.Minimum = 0;
        startBox.Maximum = 99999;
        startBox.Value = 1;

        AddLabel("Step", 16, 150);
        stepBox.Location = new Point(130, 147);
        stepBox.Width = 80;
        stepBox.Minimum = 1;
        stepBox.Maximum = 1000;
        stepBox.Value = 1;

        Label warning = new Label();
        warning.Text = "This changes your project.\r\nTry it on a copy first.";
        warning.ForeColor = Color.Firebrick;
        warning.Font = new Font(Font, FontStyle.Bold);
        warning.Location = new Point(16, 196);
        warning.Size = new Size(400, 40);

        safeCheck.Text = "I am working on a copy, or I have a backup";
        safeCheck.Location = new Point(16, 244);
        safeCheck.AutoSize = true;
        safeCheck.CheckedChanged += delegate { UpdateEnabled(); };

        run.Text = "Renumber...";
        run.Location = new Point(262, 286);
        run.Size = new Size(90, 28);
        run.Click += OnRun;
        Button cancel = new Button();
        cancel.Text = "Cancel";
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(358, 286);
        cancel.Size = new Size(70, 28);
        CancelButton = cancel;

        Controls.AddRange(new Control[] { typeBox, selectedCheck, unnumberedCheck, startBox, stepBox, warning, safeCheck, run, cancel });
        UpdateEnabled();
        RenumberSkin.Apply(this);
    }

    private void AddLabel(string text, int x, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.Location = new Point(x, y);
        label.AutoSize = true;
        Controls.Add(label);
    }

    // The device-only option only makes sense for devices, and the button waits for the safety box.
    private void UpdateEnabled()
    {
        unnumberedCheck.Enabled = typeBox.SelectedIndex == 0;
        run.Enabled = safeCheck.Checked;
    }

    private void OnRun(object sender, EventArgs e)
    {
        string[] type = Types[typeBox.SelectedIndex];
        Choice = new RenumberChoice();
        Choice.TypeText = type[0];
        Choice.TypeCode = type[1];
        Choice.OnlySelected = selectedCheck.Checked;
        Choice.OnlyUnnumbered = unnumberedCheck.Checked;
        Choice.Start = (int)startBox.Value;
        Choice.Step = (int)stepBox.Value;
        DialogResult = DialogResult.OK;
    }
}

// ---------------------------------------------------------------
// Optional: follow Eplan's dark color scheme.
// Call RenumberSkin.Apply(form) once your window is built. When Eplan is set to a light color scheme this does nothing,
// so the window keeps the normal Windows look. When Eplan is dark, the window, its title bar and its controls turn dark.
// You can copy this class into your own scripts. (Example 04 explains the idea step by step.)
// ---------------------------------------------------------------
public static class RenumberSkin
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
