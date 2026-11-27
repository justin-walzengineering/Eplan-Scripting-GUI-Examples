// ============================================================
// Example   : 28 - Unit check
// Purpose   : Compare a width you measured in Eplan with a known dimension from a data sheet, and say whether it matches in millimetres or in inches.
// You learn : A small form with number fields, parsing numbers safely, comparing with a tolerance, millimetres versus inches, separating the logic from the window
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 and 2027.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Drawing;                  // Point, Size
using System.Globalization;            // CultureInfo (numbers must not depend on the computer's language)
using System.Windows.Forms;            // Form, Label, TextBox, Button
using Eplan.EplApi.Scripting;          // The [Start] attribute

public class UnitCheckDemo
{
    [Start]
    public void Run()
    {
        using (UnitCheckForm form = new UnitCheckForm())
        {
            form.ShowDialog();
        }
    }
}

// The logic lives in its own class, so it can be tested without a window.
public static class UnitCheckLogic
{
    public const double MmPerInch = 25.4;

    // Tolerance in percent. 2 percent covers rounding on a data sheet.
    public const double TolerancePercent = 2.0;

    // measured: the number you read in Eplan. known: the width on the data sheet, in mm.
    // Returns one sentence.
    public static string Judge(double measured, double known)
    {
        if (measured <= 0 || known <= 0) return "Enter two numbers larger than zero.";

        // The same number read as millimetres, or read as inches (and converted to mm).
        bool asMm = Close(measured, known);
        bool asInch = Close(measured * MmPerInch, known);

        if (asMm && asInch) return "Both readings match (the sizes are too close to tell apart).";
        if (asMm) return "OK: the number is in millimetres and matches the data sheet.";
        if (asInch) return "The number is in INCHES: " + measured.ToString("0.###", CultureInfo.InvariantCulture) + " in = "
            + (measured * MmPerInch).ToString("0.#", CultureInfo.InvariantCulture) + " mm matches the data sheet. A millimetre project would show it 25.4 times too small.";
        if (Close(measured / MmPerInch, known)) return "The number is about 25.4 times too SMALL: it looks like millimetres read as inches.";
        return "No match in millimetres or in inches. Check the data sheet value, the object you measured, or the import scale.";
    }

    static bool Close(double a, double b)
    {
        return Math.Abs(a - b) <= b * TolerancePercent / 100.0;
    }

    // Reads a number with a dot or a comma as the decimal sign. Returns false for anything else.
    public static bool TryNumber(string text, out double value)
    {
        return double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}

public class UnitCheckForm : Form
{
    TextBox measured = new TextBox();
    TextBox known = new TextBox();
    Label result = new Label();

    public UnitCheckForm()
    {
        Text = "Unit check";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(440, 250);

        AddLabel("Width measured in Eplan (the number as shown):", 15, 15);
        measured.Location = new Point(18, 40);
        measured.Width = 200;
        Controls.Add(measured);

        AddLabel("Width from the data sheet, in millimetres:", 15, 80);
        known.Location = new Point(18, 105);
        known.Width = 200;
        Controls.Add(known);

        Button check = new Button();
        check.Text = "Check";
        check.Location = new Point(18, 145);
        check.Click += new System.EventHandler(OnCheck);
        Controls.Add(check);
        AcceptButton = check;

        result.Location = new Point(18, 185);
        result.Size = new Size(400, 55);
        Controls.Add(result);
        UnitSkin.Apply(this);
    }

    void AddLabel(string text, int x, int y)
    {
        Label l = new Label();
        l.Text = text;
        l.Location = new Point(x, y);
        l.AutoSize = true;
        Controls.Add(l);
    }

    void OnCheck(object sender, EventArgs e)
    {
        double m, k;
        if (!UnitCheckLogic.TryNumber(measured.Text, out m) || !UnitCheckLogic.TryNumber(known.Text, out k))
        {
            result.Text = "Type both widths as plain numbers, for example 482.6 or 19.0.";
            return;
        }
        result.Text = UnitCheckLogic.Judge(m, k);
    }
}

// ---------------------------------------------------------------
// Optional: follow Eplan's dark color scheme.
// Call UnitSkin.Apply(form) once your window is built. When Eplan is set to a light color scheme this does nothing,
// so the window keeps the normal Windows look. When Eplan is dark, the window, its title bar and its controls turn dark.
// You can copy this class into your own scripts. (Example 04 explains the idea step by step.)
// ---------------------------------------------------------------
public static class UnitSkin
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
