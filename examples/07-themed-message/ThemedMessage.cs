// ============================================================
// Example   : 07 - Themed message and confirm window
// Purpose   : Show messages and Yes/No questions in a window that follows Eplan's light or dark color scheme, instead of a standard message box.
// You learn : Why MessageBox ignores dark mode, building your own small dialog, returning true/false, reading Eplan's color scheme
// Level     : Beginner
// Tested in : Eplan Electric P8 2026.0.3 (run in Eplan with the dark color scheme)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Diagnostics;              // Process
using System.Drawing;                  // Color, Font, Size, Point
using System.Runtime.InteropServices;  // DllImport (dark title bar)
using System.Windows.Forms;            // Form, Label, Button
using Eplan.EplApi.Base;               // Settings: read Eplan's own color scheme
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs. It shows how the helper is used.
// ---------------------------------------------------------------
public class ThemedMessageDemo
{
    [Start]
    public void Run()
    {
        // A message with an OK button: window title, a short heading, then the details.
        NoticeWindow.Show("Themed message", "Everything is ready", "This window follows Eplan's color scheme.\r\nA standard message box would stay light, even in dark mode.");

        // A question with Yes and No. Ask returns true for Yes and false for No.
        bool yes = NoticeWindow.Ask("Themed question", "Continue?", "Choose Yes or No. Pressing Esc counts as No.");

        NoticeWindow.Show("Your answer", yes ? "You chose Yes" : "You chose No", "Use the same two lines of code anywhere in your own scripts.");
    }
}

// ---------------------------------------------------------------
// Part 2: the reusable window. Copy this class into your own scripts.
// ---------------------------------------------------------------
public class NoticeWindow : Form
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private readonly bool dark;

    // The simple way to use it: NoticeWindow.Show(...) and NoticeWindow.Ask(...).
    public static void Show(string title, string heading, string text)
    {
        using (NoticeWindow window = new NoticeWindow(title, heading, text, false))
        {
            window.Display();
        }
    }

    public static bool Ask(string title, string heading, string text)
    {
        using (NoticeWindow window = new NoticeWindow(title, heading, text, true))
        {
            return window.Display() == DialogResult.Yes;
        }
    }

    private NoticeWindow(string title, string heading, string text, bool question)
    {
        dark = IsEplanDark();

        // Colors for each scheme. Back = window background, Text = normal text, Field = button background.
        Color back = dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
        Color fore = dark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(26, 26, 26);
        Color head = dark ? Color.FromArgb(98, 169, 235) : Color.FromArgb(15, 108, 189);
        Color field = dark ? Color.FromArgb(45, 45, 45) : Color.White;
        Color border = dark ? Color.FromArgb(90, 90, 90) : Color.FromArgb(160, 160, 160);

        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = back;
        ForeColor = fore;
        ClientSize = new Size(420, 160);

        Label headingLabel = new Label();
        headingLabel.Text = heading;
        headingLabel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        headingLabel.ForeColor = head;
        headingLabel.AutoSize = false;
        headingLabel.Location = new Point(18, 16);
        headingLabel.Size = new Size(384, 28);

        Label textLabel = new Label();
        textLabel.Text = text;
        textLabel.AutoSize = false;
        textLabel.Location = new Point(18, 52);
        textLabel.Size = new Size(384, 56);

        Controls.Add(headingLabel);
        Controls.Add(textLabel);

        if (question)
        {
            Button yesButton = MakeButton("Yes", DialogResult.Yes, new Point(228, 118), field, border, fore);
            Button noButton = MakeButton("No", DialogResult.No, new Point(314, 118), field, border, fore);
            AcceptButton = yesButton;
            CancelButton = noButton;              // Esc = No
            Controls.Add(yesButton);
            Controls.Add(noButton);
        }
        else
        {
            Button okButton = MakeButton("OK", DialogResult.OK, new Point(314, 118), field, border, fore);
            AcceptButton = okButton;
            CancelButton = okButton;
            Controls.Add(okButton);
        }

        // The title bar is controlled by Windows, so we ask Windows to color it. Failures are ignored.
        HandleCreated += delegate { ColorTitleBar(back, fore); };
    }

    private static Button MakeButton(string text, DialogResult result, Point location, Color back, Color border, Color fore)
    {
        Button button = new Button();
        button.Text = text;
        button.DialogResult = result;
        button.Location = location;
        button.Size = new Size(80, 28);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = border;
        button.BackColor = back;
        button.ForeColor = fore;
        button.UseVisualStyleBackColor = false;
        return button;
    }

    // Shows the window in front of Eplan.
    private DialogResult Display()
    {
        IntPtr eplan = IntPtr.Zero;
        try
        {
            eplan = Process.GetCurrentProcess().MainWindowHandle;
        }
        catch (Exception)
        {
        }

        if (eplan == IntPtr.Zero)
        {
            StartPosition = FormStartPosition.CenterScreen;
            return ShowDialog();
        }
        return ShowDialog(new NoticeOwner(eplan));
    }

    private void ColorTitleBar(Color back, Color fore)
    {
        try
        {
            int useDark = dark ? 1 : 0;
            if (DwmSetWindowAttribute(Handle, 20, ref useDark, 4) != 0)
            {
                DwmSetWindowAttribute(Handle, 19, ref useDark, 4);
            }
            int caption = back.R | (back.G << 8) | (back.B << 16);    // Windows wants blue-green-red order
            int text = fore.R | (fore.G << 8) | (fore.B << 16);
            DwmSetWindowAttribute(Handle, 35, ref caption, 4);
            DwmSetWindowAttribute(Handle, 36, ref text, 4);
        }
        catch (Exception)
        {
        }
    }

    // Same idea as example 04: Eplan keeps its color scheme in USER.MF.GuiColorScheme.
    // 0 = automatic (follow Windows), 1 = dark, 2 = light.
    private static bool IsEplanDark()
    {
        try
        {
            Settings settings = new Settings();
            int scheme = settings.GetNumericSetting("USER.MF.GuiColorScheme", 0);
            if (scheme == 1) { return true; }
            if (scheme == 2) { return false; }
            object value = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
            return (value is int) && ((int)value == 0);
        }
        catch (Exception)
        {
            return true;                       // Eplan's default is dark
        }
    }
}

// ShowDialog needs an owner in the form of IWin32Window. This wraps a window handle.
public class NoticeOwner : IWin32Window
{
    private readonly IntPtr handle;

    public NoticeOwner(IntPtr handle)
    {
        this.handle = handle;
    }

    public IntPtr Handle
    {
        get { return handle; }
    }
}
