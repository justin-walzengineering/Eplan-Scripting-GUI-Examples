// ============================================================
// Example   : 04 - Light and dark theme
// Purpose   : Restyle your own window live with a "Dark mode" check box, and match Eplan's own color scheme automatically.
// You learn : Color palettes, restyling controls in a loop, custom-painted check box, dark title bar, reading an Eplan setting
// Level     : Intermediate
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (run in Eplan 2026.0.3: see README)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Diagnostics;              // Process
using System.Drawing;                  // Color, Font, Graphics, Point, Size
using System.Runtime.InteropServices;  // DllImport (to color the title bar)
using System.Windows.Forms;            // Form, controls
using Eplan.EplApi.Base;               // Settings: lets a script read Eplan's own settings
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs.
// ---------------------------------------------------------------
public class LightDarkThemeDemo
{
    [Start]
    public void Run()
    {
        using (ThemeDemoForm form = new ThemeDemoForm())
        {
            // Show the window with Eplan as its owner (see example 03 for placing windows in more detail).
            IntPtr eplan = Process.GetCurrentProcess().MainWindowHandle;
            if (eplan == IntPtr.Zero)
            {
                form.ShowDialog();
            }
            else
            {
                form.StartPosition = FormStartPosition.CenterParent;   // center on the owner
                form.ShowDialog(new ThemeWindowOwner(eplan));
            }
        }
    }
}

// ---------------------------------------------------------------
// Part 2: the demo window.
// ---------------------------------------------------------------
public class ThemeDemoForm : Form
{
    private readonly Label heading = new Label();
    private readonly Label info = new Label();
    private readonly Label status = new Label();
    private readonly TextBox textBox = new TextBox();
    private readonly ComboBox comboBox = new ComboBox();
    private readonly ThemedCheckBox darkCheck = new ThemedCheckBox();
    private readonly ThemedCheckBox otherCheck = new ThemedCheckBox();
    private readonly Button matchButton = new Button();
    private readonly Button closeButton = new Button();

    public ThemeDemoForm()
    {
        Text = "Light and dark theme demo";
        ClientSize = new Size(420, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        heading.Text = "Theme demo";
        heading.Font = new Font(Font, FontStyle.Bold);
        heading.AutoSize = true;
        heading.Location = new Point(16, 14);

        info.Text = "Tick \"Dark mode\" to restyle this whole window live. \"Match Eplan\" copies the color scheme chosen in Eplan's own options.";
        info.Location = new Point(16, 40);
        info.Size = new Size(388, 44);

        textBox.Text = "Type here";
        textBox.Location = new Point(16, 96);
        textBox.Width = 388;

        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Items.AddRange(new object[] { "First choice", "Second choice", "Third choice" });
        comboBox.SelectedIndex = 0;
        comboBox.Location = new Point(16, 128);
        comboBox.Width = 388;

        darkCheck.Text = "Dark mode";
        darkCheck.Location = new Point(16, 168);

        otherCheck.Text = "Another check box";
        otherCheck.Checked = true;
        otherCheck.Location = new Point(16, 196);

        status.Location = new Point(16, 232);
        status.Size = new Size(388, 20);

        matchButton.Text = "Match Eplan";
        matchButton.Location = new Point(16, 262);
        matchButton.Size = new Size(110, 28);

        closeButton.Text = "Close";
        closeButton.DialogResult = DialogResult.OK;
        closeButton.Location = new Point(329, 262);
        closeButton.Size = new Size(75, 28);
        AcceptButton = closeButton;

        Controls.AddRange(new Control[] { heading, info, textBox, comboBox, darkCheck, otherCheck, status, matchButton, closeButton });

        // Events: "when this happens, run that method".
        darkCheck.CheckedChanged += OnDarkCheckChanged;
        matchButton.Click += OnMatchClick;
        HandleCreated += delegate { ThemeHelper.Apply(this, darkCheck.Checked); };   // the title bar needs the window to exist

        // Start in the same scheme as Eplan.
        ApplyEplanScheme();
    }

    // The user ticked or unticked "Dark mode": restyle everything straight away.
    private void OnDarkCheckChanged(object sender, EventArgs e)
    {
        ThemeHelper.Apply(this, darkCheck.Checked);
    }

    private void OnMatchClick(object sender, EventArgs e)
    {
        ApplyEplanScheme();
    }

    private void ApplyEplanScheme()
    {
        bool dark = ThemeHelper.IsEplanDark();
        status.Text = "Eplan is using: " + (dark ? "dark" : "light") + " (" + ThemeHelper.LastReason + ")";
        darkCheck.Checked = dark;                       // this also fires CheckedChanged, which applies the theme
        ThemeHelper.Apply(this, dark);                  // and apply once more in case the box did not change
    }
}

// ---------------------------------------------------------------
// Part 3: the reusable theme code. Copy these classes into your own scripts.
// ---------------------------------------------------------------

// A palette is just a named set of colors. We keep one for dark and one for light.
public class ThemePalette
{
    public Color Back;       // window background
    public Color Text;       // normal text
    public Color Muted;      // less important text
    public Color Field;      // background of text boxes and buttons
    public Color Border;     // outlines
    public Color Accent;     // check marks and highlights
    public Color Hover;      // button when the mouse is over it

    public static readonly ThemePalette Dark = new ThemePalette
    {
        Back = Color.FromArgb(32, 32, 32),
        Text = Color.FromArgb(240, 240, 240),
        Muted = Color.FromArgb(165, 165, 165),
        Field = Color.FromArgb(45, 45, 45),
        Border = Color.FromArgb(90, 90, 90),
        Accent = Color.FromArgb(0, 120, 212),
        Hover = Color.FromArgb(62, 62, 62)
    };

    public static readonly ThemePalette Light = new ThemePalette
    {
        Back = Color.FromArgb(243, 243, 243),
        Text = Color.FromArgb(26, 26, 26),
        Muted = Color.FromArgb(100, 100, 100),
        Field = Color.FromArgb(255, 255, 255),
        Border = Color.FromArgb(160, 160, 160),
        Accent = Color.FromArgb(0, 120, 212),
        Hover = Color.FromArgb(229, 241, 251)
    };
}

public static class ThemeHelper
{
    // Windows functions for the title bar. (See example 03 for what [DllImport] means.)
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    // Plain words about how the last "is Eplan dark?" answer was reached. Shown in the demo window.
    public static string LastReason = "not checked yet";

    // --- Which scheme is Eplan using? -----------------------------------------------------
    // Eplan stores the choice from its Options dialog in the setting USER.MF.GuiColorScheme:
    //   0 = automatic (follow Windows), 1 = dark, 2 = light.
    // Reading the setting is far more reliable than guessing from screen pixels.
    // Note: if the user changes the scheme in Eplan, Eplan may need a restart before it shows it.
    public static bool IsEplanDark()
    {
        try
        {
            Settings settings = new Settings();                        // Eplan's settings object
            int scheme = settings.GetNumericSetting("USER.MF.GuiColorScheme", 0);   // 0 = first (and only) value

            if (scheme == 1) { LastReason = "Eplan setting: dark"; return true; }
            if (scheme == 2) { LastReason = "Eplan setting: light"; return false; }

            // 0 = automatic, so Eplan follows Windows. Check the Windows setting.
            bool windowsDark = WindowsAppsUseDarkTheme();
            LastReason = "Eplan setting: automatic, so Windows decides";
            return windowsDark;
        }
        catch (Exception)
        {
            // The setting could not be read (for example, if the script is run outside Eplan).
            // Eplan's own default is dark, so we use that. Cosmetic code must never stop the script.
            LastReason = "setting not available, using Eplan's default";
            return true;
        }
    }

    // Windows keeps its "apps" theme in the registry: 0 = dark, 1 = light.
    private static bool WindowsAppsUseDarkTheme()
    {
        object value = Microsoft.Win32.Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", 1);
        return (value is int) && ((int)value == 0);
    }

    // --- Restyling a window -------------------------------------------------------------------
    // Applies the dark or light palette to a form and everything on it.
    public static void Apply(Form form, bool dark)
    {
        try
        {
            ThemePalette palette = dark ? ThemePalette.Dark : ThemePalette.Light;

            form.SuspendLayout();                       // avoid flicker while we change many things
            ApplyToControl(form, palette);
            form.ResumeLayout();

            SetTitleBar(form, dark, palette);
            form.Invalidate(true);                      // ask Windows to redraw everything
        }
        catch (Exception)
        {
            // Styling is cosmetic: if it fails, the window simply keeps its normal look.
        }
    }

    // Walks through a control and all the controls inside it (a technique called recursion).
    private static void ApplyToControl(Control control, ThemePalette p)
    {
        control.ForeColor = p.Text;

        if (control is Form)
        {
            control.BackColor = p.Back;
        }
        else if (control is ThemedCheckBox)
        {
            ThemedCheckBox box = (ThemedCheckBox)control;
            box.SetColors(p);
        }
        else if (control is TextBox)
        {
            TextBox textBox = (TextBox)control;
            textBox.BackColor = p.Field;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is ComboBox)
        {
            ComboBox combo = (ComboBox)control;
            combo.BackColor = p.Field;
            combo.FlatStyle = FlatStyle.Flat;
        }
        else if (control is Button)
        {
            Button button = (Button)control;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = p.Field;
            button.FlatAppearance.BorderColor = p.Border;
            button.FlatAppearance.MouseOverBackColor = p.Hover;
            button.UseVisualStyleBackColor = false;
        }
        else if (control is Label)
        {
            control.BackColor = Color.Transparent;      // let the window color show through
        }

        // Do the same for everything inside this control.
        foreach (Control child in control.Controls)
        {
            ApplyToControl(child, p);
        }
    }

    // --- The title bar -----------------------------------------------------------------------------
    // The title bar belongs to Windows, not to your form, so we ask Windows to color it.
    // 20 (or 19 on older Windows 10) = dark title bar. 35 and 36 = caption and text color (Windows 11 only).
    // Each call may fail on an older Windows. That is fine; we just ignore it.
    private static void SetTitleBar(Form form, bool dark, ThemePalette p)
    {
        if (!form.IsHandleCreated)
        {
            return;                                     // the window does not exist yet; HandleCreated will call us again
        }

        try
        {
            int useDark = dark ? 1 : 0;
            if (DwmSetWindowAttribute(form.Handle, 20, ref useDark, 4) != 0)
            {
                DwmSetWindowAttribute(form.Handle, 19, ref useDark, 4);
            }

            int caption = ToBgr(p.Back);
            int text = ToBgr(p.Text);
            DwmSetWindowAttribute(form.Handle, 35, ref caption, 4);
            DwmSetWindowAttribute(form.Handle, 36, ref text, 4);
        }
        catch (Exception)
        {
        }
    }

    // Windows wants colors as blue-green-red, not red-green-blue.
    private static int ToBgr(Color c)
    {
        return c.R | (c.G << 8) | (c.B << 16);
    }
}

// The standard check box can be hard to see in dark mode, so we draw our own.
// "OnPaint" is the method Windows Forms calls whenever a control needs to be drawn.
public class ThemedCheckBox : CheckBox
{
    private Color boxColor = Color.White;
    private Color borderColor = Color.Gray;
    private Color accentColor = Color.FromArgb(0, 120, 212);
    private Color backgroundColor = SystemColors.Control;

    public ThemedCheckBox()
    {
        AutoSize = false;
        Size = new Size(200, 24);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public void SetColors(ThemePalette p)
    {
        boxColor = p.Field;
        borderColor = p.Border;
        accentColor = p.Accent;
        backgroundColor = p.Back;
        ForeColor = p.Text;
        Invalidate();                                   // redraw with the new colors
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(backgroundColor);

        // The box: 16 x 16 pixels, vertically centered.
        Rectangle box = new Rectangle(0, (Height - 16) / 2, 16, 16);
        using (SolidBrush fill = new SolidBrush(Checked ? accentColor : boxColor))
        using (Pen outline = new Pen(Checked ? accentColor : borderColor))
        {
            g.FillRectangle(fill, box);
            g.DrawRectangle(outline, box.X, box.Y, box.Width - 1, box.Height - 1);
        }

        // The tick mark, drawn as two short lines.
        if (Checked)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (Pen tick = new Pen(Color.White, 2f))
            {
                g.DrawLines(tick, new Point[]
                {
                    new Point(box.X + 3, box.Y + 8),
                    new Point(box.X + 7, box.Y + 12),
                    new Point(box.X + 13, box.Y + 4)
                });
            }
        }

        // The text to the right of the box.
        Rectangle textArea = new Rectangle(box.Right + 8, 0, Width - box.Right - 8, Height);
        TextRenderer.DrawText(g, Text, Font, textArea, ForeColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }
}

// ShowDialog needs an "owner" in the form of IWin32Window. This tiny class wraps a window handle.
public class ThemeWindowOwner : IWin32Window
{
    private readonly IntPtr handle;

    public ThemeWindowOwner(IntPtr handle)
    {
        this.handle = handle;
    }

    public IntPtr Handle
    {
        get { return handle; }
    }
}
