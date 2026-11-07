// ============================================================
// Example   : 13 - Dark-mode-safe controls
// Purpose   : Replacements for the standard controls that stay white or hard to see in dark mode: a date field, a drop-down list, a check box and a progress bar.
// You learn : Custom painting with OnPaint, ToolStripDropDown, owner-drawn lists, SetWindowTheme, passing colors to controls
// Level     : Intermediate
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (DRAFT: not yet run in Eplan)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Drawing;                  // Color, Graphics, Pen, Brush, Rectangle, Point
using System.Drawing.Drawing2D;        // SmoothingMode
using System.Runtime.InteropServices;  // DllImport
using System.Windows.Forms;            // Controls, ToolStripDropDown, MonthCalendar, TextRenderer
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs. A demo window with a "Dark mode" switch.
// ---------------------------------------------------------------
public class DarkSafeControlsDemo
{
    [Start]
    public void Run()
    {
        using (SafeDemoForm form = new SafeDemoForm())
        {
            form.ShowDialog();
        }
    }
}

// A palette is a set of colors. The controls below do not choose colors themselves, they are given them.
public class SafePalette
{
    public Color Back;
    public Color Text;
    public Color Field;
    public Color Border;
    public Color Accent;

    public static readonly SafePalette Dark = new SafePalette
    {
        Back = Color.FromArgb(32, 32, 32), Text = Color.FromArgb(240, 240, 240), Field = Color.FromArgb(45, 45, 45),
        Border = Color.FromArgb(100, 100, 100), Accent = Color.FromArgb(0, 120, 212)
    };

    public static readonly SafePalette Light = new SafePalette
    {
        Back = Color.FromArgb(243, 243, 243), Text = Color.FromArgb(26, 26, 26), Field = Color.White,
        Border = Color.FromArgb(160, 160, 160), Accent = Color.FromArgb(0, 120, 212)
    };
}

public class SafeDemoForm : Form
{
    private readonly SafeDateField dateField = new SafeDateField();
    private readonly SafeComboBox combo = new SafeComboBox();
    private readonly SafeCheckBox darkCheck = new SafeCheckBox();
    private readonly SafeCheckBox otherCheck = new SafeCheckBox();
    private readonly ProgressBar bar = new ProgressBar();
    private readonly Label dateLabel = new Label();
    private readonly Label comboLabel = new Label();

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

    public SafeDemoForm()
    {
        Text = "Dark-safe controls";
        ClientSize = new Size(400, 270);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        dateLabel.Text = "Date field";
        dateLabel.Location = new Point(16, 20);
        dateLabel.AutoSize = true;
        dateField.Location = new Point(140, 16);
        dateField.Size = new Size(160, 26);

        comboLabel.Text = "Drop-down list";
        comboLabel.Location = new Point(16, 60);
        comboLabel.AutoSize = true;
        combo.Location = new Point(140, 56);
        combo.Size = new Size(160, 26);
        combo.Items.AddRange(new object[] { "First choice", "Second choice", "Third choice" });
        combo.SelectedIndex = 0;

        otherCheck.Text = "A check box";
        otherCheck.Checked = true;
        otherCheck.Location = new Point(16, 100);

        bar.Location = new Point(16, 140);
        bar.Size = new Size(368, 18);
        bar.Value = 60;
        HandleCreated += delegate { SetWindowTheme(bar.Handle, "", ""); ApplyPalette(true); };   // lets the bar take our colors

        darkCheck.Text = "Dark mode";
        darkCheck.Checked = true;
        darkCheck.Location = new Point(16, 190);
        darkCheck.CheckedChanged += delegate { ApplyPalette(darkCheck.Checked); };

        Button close = new Button();
        close.Text = "Close";
        close.DialogResult = DialogResult.OK;
        close.Location = new Point(309, 226);
        close.Size = new Size(75, 28);
        AcceptButton = close;

        Controls.AddRange(new Control[] { dateLabel, dateField, comboLabel, combo, otherCheck, bar, darkCheck, close });
        ApplyPalette(true);
    }

    private void ApplyPalette(bool dark)
    {
        SafePalette p = dark ? SafePalette.Dark : SafePalette.Light;
        BackColor = p.Back;
        ForeColor = p.Text;
        dateLabel.ForeColor = p.Text;
        comboLabel.ForeColor = p.Text;
        dateField.SetColors(p);
        combo.SetColors(p);
        otherCheck.SetColors(p);
        darkCheck.SetColors(p);
        bar.BackColor = p.Field;          // works because SetWindowTheme switched off the system look for this bar
        bar.ForeColor = p.Accent;
        Invalidate(true);
    }
}

// ---------------------------------------------------------------
// Part 2: the reusable controls. Copy the ones you need.
// ---------------------------------------------------------------

// A check box we draw ourselves, because the standard one is almost invisible on a dark background.
public class SafeCheckBox : CheckBox
{
    private SafePalette palette = SafePalette.Dark;

    public SafeCheckBox()
    {
        AutoSize = false;
        Size = new Size(200, 24);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public void SetColors(SafePalette p)
    {
        palette = p;
        ForeColor = p.Text;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(palette.Back);
        Rectangle box = new Rectangle(0, (Height - 16) / 2, 16, 16);
        using (SolidBrush fill = new SolidBrush(Checked ? palette.Accent : palette.Field))
        using (Pen outline = new Pen(Checked ? palette.Accent : palette.Border))
        {
            g.FillRectangle(fill, box);
            g.DrawRectangle(outline, box.X, box.Y, box.Width - 1, box.Height - 1);
        }
        if (Checked)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen tick = new Pen(Color.White, 2f))
            {
                g.DrawLines(tick, new Point[] { new Point(box.X + 3, box.Y + 8), new Point(box.X + 7, box.Y + 12), new Point(box.X + 13, box.Y + 4) });
            }
        }
        TextRenderer.DrawText(g, Text, Font, new Rectangle(box.Right + 8, 0, Width - box.Right - 8, Height), palette.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }
}

// A date field. The standard date picker cannot be colored, so this one draws itself and opens a calendar when clicked.
public class SafeDateField : Control
{
    private SafePalette palette = SafePalette.Dark;
    private DateTime value = DateTime.Today;

    // System.EventHandler is written in full because Eplan also has a type called EventHandler.
    public event System.EventHandler ValueChanged;

    public SafeDateField()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Cursor = Cursors.Hand;
    }

    public DateTime Value
    {
        get { return value; }
        set
        {
            this.value = value.Date;
            Invalidate();
            if (ValueChanged != null)
            {
                ValueChanged(this, EventArgs.Empty);
            }
        }
    }

    public void SetColors(SafePalette p)
    {
        palette = p;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(palette.Field);
        using (Pen border = new Pen(palette.Border))
        {
            g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }
        TextRenderer.DrawText(g, value.ToShortDateString(), Font, new Rectangle(8, 0, Width - 34, Height), palette.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

        // A tiny calendar symbol on the right: a box, a header bar and two "rings".
        Rectangle cal = new Rectangle(Width - 24, (Height - 14) / 2, 14, 14);
        using (Pen pen = new Pen(palette.Text))
        using (SolidBrush brush = new SolidBrush(palette.Text))
        {
            g.DrawRectangle(pen, cal.X, cal.Y + 2, cal.Width - 1, cal.Height - 3);
            g.FillRectangle(brush, cal.X, cal.Y + 2, cal.Width, 4);
            g.DrawLine(pen, cal.X + 3, cal.Y, cal.X + 3, cal.Y + 3);
            g.DrawLine(pen, cal.X + 10, cal.Y, cal.X + 10, cal.Y + 3);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);

        // A drop-down panel that holds a MonthCalendar. AutoClose makes it vanish when the user clicks elsewhere.
        MonthCalendar calendar = new MonthCalendar();
        calendar.SetDate(value);
        calendar.MaxSelectionCount = 1;
        calendar.BackColor = palette.Field;
        calendar.ForeColor = palette.Text;
        calendar.TitleBackColor = palette.Accent;
        calendar.TitleForeColor = Color.White;
        calendar.TrailingForeColor = palette.Border;

        ToolStripDropDown dropDown = new ToolStripDropDown();
        dropDown.Items.Add(new ToolStripControlHost(calendar));
        dropDown.Padding = Padding.Empty;
        calendar.DateSelected += delegate
        {
            Value = calendar.SelectionStart;
            dropDown.Close();
        };
        dropDown.Show(this, 0, Height);
    }
}

// A drop-down list we draw ourselves, so the arrow and the list follow the colors.
public class SafeComboBox : ComboBox
{
    private SafePalette palette = SafePalette.Dark;

    public SafeComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        DrawMode = DrawMode.OwnerDrawFixed;                    // we draw each list item ourselves
        FlatStyle = FlatStyle.Flat;
        ItemHeight = 20;
    }

    public void SetColors(SafePalette p)
    {
        palette = p;
        BackColor = p.Field;
        ForeColor = p.Text;
        Invalidate();
    }

    // Called by Windows Forms for each item in the list, and for the "selected item" shown when the list is closed.
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        bool highlighted = (e.State & DrawItemState.Selected) == DrawItemState.Selected && (e.State & DrawItemState.ComboBoxEdit) == 0;
        using (SolidBrush back = new SolidBrush(highlighted ? palette.Accent : palette.Field))
        {
            e.Graphics.FillRectangle(back, e.Bounds);
        }
        if (e.Index >= 0)
        {
            TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), Font, new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height),
                highlighted ? Color.White : palette.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }
}
