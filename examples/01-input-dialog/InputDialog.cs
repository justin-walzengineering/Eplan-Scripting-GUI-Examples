// ============================================================
// Example   : 01 - Reusable input dialog
// Purpose   : Ask the user for text, confirm an action, and show a message using small helpers you can reuse.
// You learn : Windows Forms basics, returning a value from a window, handling Cancel and empty input
// Level     : Beginner
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 and 2027.0.3 (also run in Eplan 2026.0.3; see README)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

// "using" lines tell C# which toolboxes this file uses.
using System;
using System.Drawing;                 // Size, Point: sizes and positions of things on a window
using System.Windows.Forms;           // Form, Label, TextBox, Button, MessageBox: building blocks for windows
using Eplan.EplApi.Scripting;         // The [Start] attribute that tells Eplan which method to run

// A "class" groups related code together. Eplan looks inside it for the method marked [Start].
public class InputDialogDemo
{
    // [Start] means: "when the user runs this script in Eplan, begin here."
    // A "method" is a named list of steps. "void" means it doesn't hand back a result.
    [Start]
    public void Run()
    {
        // 1. Ask for text. Prompt returns the text the user typed, or null if they pressed Cancel.
        string reportName = DialogHelper.Prompt(
            "New report",                       // window title
            "What should the report be called?", // question shown above the text box
            "My report");                        // starting text in the box

        // 2. Handle Cancel. "null" means "nothing". The user backed out, so we stop quietly.
        if (reportName == null)
        {
            DialogHelper.Info("Cancelled", "No report was created.");
            return;                              // "return" leaves the method early
        }

        // 3. Handle empty input. Trim() removes spaces at the beginning and end; Length is the number of characters left.
        if (reportName.Trim().Length == 0)
        {
            DialogHelper.Info("Missing name", "Please enter a name and try again.");
            return;
        }

        // 4. Ask a yes/no question. Confirm returns true for Yes and false for No.
        bool ok = DialogHelper.Confirm("Please confirm", "Create a report named '" + reportName.Trim() + "'?");

        // 5. An "if" runs its block only when the condition is true.
        if (ok)
        {
            // This is where a real script would do its work (read or write project data, and so on).
            DialogHelper.Info("Done", "Pretend we created '" + reportName.Trim() + "'.");
        }
    }
}

// A "static class" is a toolbox of helpers. You call them without creating an object first,
// for example DialogHelper.Prompt(...). You can copy this class into your own scripts and reuse it.
public static class DialogHelper
{
    // Shows a window with a question and a text box.
    // Returns what the user typed, or null if they pressed Cancel or closed the window.
    public static string Prompt(string title, string message, string defaultText)
    {
        // "using" here also means "clean up this window automatically when we are done with it".
        using (Form form = new Form())
        using (Label label = new Label())
        using (TextBox textBox = new TextBox())
        using (Button okButton = new Button())
        using (Button cancelButton = new Button())
        {
            // --- Describe each control: what it says and where it sits on the window. ---
            form.Text = title;

            label.Text = message;
            label.AutoSize = false;              // we set the size ourselves, so the layout below is predictable
            label.Location = new Point(12, 12);
            // Measure how tall the text needs to be when wrapped to 360 pixels wide.
            label.Size = label.GetPreferredSize(new Size(360, 0));

            textBox.Text = defaultText ?? "";    // "??" means: use "" if defaultText is null
            textBox.Location = new Point(12, label.Bottom + 8);
            textBox.Width = 360;

            okButton.Text = "OK";
            okButton.DialogResult = DialogResult.OK;         // clicking it closes the window with "OK"
            okButton.Location = new Point(216, textBox.Bottom + 16);

            cancelButton.Text = "Cancel";
            cancelButton.DialogResult = DialogResult.Cancel; // clicking it closes the window with "Cancel"
            cancelButton.Location = new Point(297, textBox.Bottom + 16);

            // --- Configure the window itself. ---
            form.ClientSize = new Size(384, okButton.Bottom + 12);
            form.FormBorderStyle = FormBorderStyle.FixedDialog; // not resizable, looks like a dialog
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.ShowInTaskbar = false;
            form.TopMost = true;                 // keep it in front of the Eplan window
            form.AcceptButton = okButton;        // pressing Enter clicks OK
            form.CancelButton = cancelButton;    // pressing Esc clicks Cancel

            // Put the controls on the window.
            form.Controls.Add(label);
            form.Controls.Add(textBox);
            form.Controls.Add(okButton);
            form.Controls.Add(cancelButton);

            // Show the window and wait. The code stops here until the user closes it.
            form.Shown += delegate { textBox.SelectAll(); textBox.Focus(); };
            InputSkin.Apply(form);
            DialogResult result = form.ShowDialog();

            if (result == DialogResult.OK)
            {
                return textBox.Text;
            }
            return null;                          // Cancel (or the X button)
        }
    }

    // Asks a yes/no question. Returns true if the user clicked Yes.
    public static bool Confirm(string title, string message)
    {
        DialogResult result = MessageBox.Show(
            message, title,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);     // "No" is the safe default
        return result == DialogResult.Yes;
    }

    // Shows a simple message with an OK button.
    public static void Info(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

// ---------------------------------------------------------------
// Optional: follow Eplan's dark color scheme.
// Call InputSkin.Apply(form) once your window is built. When Eplan is set to a light color scheme this does nothing,
// so the window keeps the normal Windows look. When Eplan is dark, the window, its title bar and its controls turn dark.
// You can copy this class into your own scripts. (Example 04 explains the idea step by step.)
// ---------------------------------------------------------------
public static class InputSkin
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
