// ============================================================
// Example   : 06 - Remember window size and position
// Purpose   : Reopen a window where the user left it, with the same size, and fall back safely if that spot is no longer on a screen.
// You learn : Saving small settings in the Windows registry, window events, multiple monitors, defensive code
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 and 2027.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Drawing;                  // Rectangle, Point, Size
using System.Windows.Forms;            // Form, Screen, controls
using Microsoft.Win32;                 // Registry: where Windows keeps small per-user settings
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs. It opens a resizable demo window.
// ---------------------------------------------------------------
public class RememberWindowDemo
{
    [Start]
    public void Run()
    {
        using (Form form = new Form())
        using (TextBox box = new TextBox())
        using (Button resetButton = new Button())
        using (Button closeButton = new Button())
        {
            form.Text = "Remember window demo";
            form.ClientSize = new Size(460, 260);
            form.MinimumSize = new Size(360, 200);           // never smaller than this
            form.FormBorderStyle = FormBorderStyle.Sizable;  // the user can resize it
            form.StartPosition = FormStartPosition.CenterScreen;
            form.ShowInTaskbar = false;

            box.Multiline = true;
            box.ReadOnly = true;
            box.Location = new Point(12, 12);
            box.Size = new Size(436, 200);
            box.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;  // grows with the window
            box.Text = "Move this window and change its size, then close it and run the script again." +
                       "\r\n\r\nIt opens where you left it." +
                       "\r\n\r\nTry it with the window maximized, and with Eplan on a second monitor.";

            resetButton.Text = "Forget saved position";
            resetButton.Size = new Size(150, 28);
            resetButton.Location = new Point(12, 220);
            resetButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            resetButton.Click += delegate
            {
                WindowMemory.Forget("RememberWindowDemo");
                MessageBox.Show("Done. The next run opens in the center of the screen.", "Remember window demo");
            };

            closeButton.Text = "Close";
            closeButton.DialogResult = DialogResult.OK;
            closeButton.Size = new Size(90, 28);
            closeButton.Location = new Point(358, 220);
            closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            form.AcceptButton = closeButton;

            form.Controls.Add(box);
            form.Controls.Add(resetButton);
            form.Controls.Add(closeButton);

            // One line does the remembering. The key names this window so several windows do not mix up.
            WindowMemory.Attach(form, "RememberWindowDemo");

            // Start with the Close button focused, so the text box does not open with all its text highlighted.
            form.ActiveControl = closeButton;

            RememberSkin.Apply(form);

            form.ShowDialog();
        }
    }
}

// ---------------------------------------------------------------
// Part 2: a helper class you can copy into your own scripts.
// ---------------------------------------------------------------
public static class WindowMemory
{
    // The Windows registry is a small database of settings. This is our own folder in it,
    // inside the current user's area, so no administrator rights are needed.
    private const string Root = @"Software\EplanScriptExamples\Windows";

    // Names of windows whose memory was cleared with Forget while the window was open.
    // Without this, closing the window would save its position again straight away.
    private static string forgotten = "";

    // Call this once, after you have set the form up. It restores the saved bounds now
    // and saves the bounds again when the window closes.
    public static void Attach(Form form, string key)
    {
        forgotten = forgotten.Replace(";" + key + ";", "");   // a new window starts remembering again

        try
        {
            Rectangle saved;
            if (TryLoad(key, out saved) && IsUsable(saved, form.MinimumSize))
            {
                form.StartPosition = FormStartPosition.Manual;    // "I will place it myself"
                form.Bounds = saved;
            }
        }
        catch (Exception)
        {
            // Remembering is a convenience. If it fails, the window just opens normally.
        }

        // FormClosed (not FormClosing): a close that the user cancels should not save anything.
        form.FormClosed += delegate
        {
            try
            {
                Save(form, key);
            }
            catch (Exception)
            {
            }
        };
    }

    // Deletes the saved values for one window.
    public static void Forget(string key)
    {
        if (!forgotten.Contains(";" + key + ";"))
        {
            forgotten += ";" + key + ";";                    // do not save again when the window closes
        }
        try
        {
            Registry.CurrentUser.DeleteSubKey(Root + "\\" + key, false);
        }
        catch (Exception)
        {
        }
    }

    private static void Save(Form form, string key)
    {
        if (forgotten.Contains(";" + key + ";"))
        {
            return;                                          // the user asked us to forget this window
        }

        // A maximized or minimized window reports the whole screen as its size.
        // RestoreBounds is the size it will go back to, which is what we want to remember.
        Rectangle bounds = form.WindowState == FormWindowState.Normal ? form.Bounds : form.RestoreBounds;

        using (RegistryKey sub = Registry.CurrentUser.CreateSubKey(Root + "\\" + key))
        {
            sub.SetValue("Left", bounds.Left, RegistryValueKind.DWord);
            sub.SetValue("Top", bounds.Top, RegistryValueKind.DWord);
            sub.SetValue("Width", bounds.Width, RegistryValueKind.DWord);
            sub.SetValue("Height", bounds.Height, RegistryValueKind.DWord);
        }
    }

    private static bool TryLoad(string key, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        using (RegistryKey sub = Registry.CurrentUser.OpenSubKey(Root + "\\" + key))
        {
            if (sub == null)
            {
                return false;                  // nothing saved yet
            }
            // Negative numbers are valid (a monitor to the left of the main one). Convert.ToInt32 keeps them.
            int left = Convert.ToInt32(sub.GetValue("Left", 0));
            int top = Convert.ToInt32(sub.GetValue("Top", 0));
            int width = Convert.ToInt32(sub.GetValue("Width", 0));
            int height = Convert.ToInt32(sub.GetValue("Height", 0));
            bounds = new Rectangle(left, top, width, height);
            return true;
        }
    }

    // Is the saved rectangle sensible? It must not be tiny, and the user must be able to grab its title bar
    // on a screen that still exists. (A monitor may have been unplugged since last time.)
    public static bool IsUsable(Rectangle saved, Size minimum)
    {
        if (saved.Width < Math.Max(100, minimum.Width) || saved.Height < Math.Max(100, minimum.Height))
        {
            return false;
        }

        // A point near the top-left corner, just inside the title bar.
        Point grabPoint = new Point(saved.Left + 60, saved.Top + 10);
        foreach (Screen screen in Screen.AllScreens)
        {
            if (screen.WorkingArea.Contains(grabPoint))
            {
                return true;
            }
        }
        return false;
    }
}

// ---------------------------------------------------------------
// Optional: follow Eplan's dark color scheme.
// Call RememberSkin.Apply(form) once your window is built. When Eplan is set to a light color scheme this does nothing,
// so the window keeps the normal Windows look. When Eplan is dark, the window, its title bar and its controls turn dark.
// You can copy this class into your own scripts. (Example 04 explains the idea step by step.)
// ---------------------------------------------------------------
public static class RememberSkin
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
