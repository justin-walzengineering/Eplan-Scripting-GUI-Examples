// ============================================================
// Example   : 22 - Results window
// Purpose   : Collect the results of a long job and show them once, in a window the user can scroll, copy and save, instead of many message boxes.
// You learn : A small result log class, a reusable report window, clipboard, SaveFileDialog, following Eplan's color scheme
// Level     : Intermediate
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (DRAFT: not yet run in Eplan)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Collections.Generic;      // List<string>
using System.Diagnostics;              // Process
using System.Drawing;                  // Color, Font, Point, Size
using System.IO;                       // File
using System.Runtime.InteropServices;  // DllImport
using System.Text;                     // StringBuilder
using System.Windows.Forms;            // Form, TextBox, Clipboard, SaveFileDialog
using Eplan.EplApi.Base;               // Settings
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs. It pretends to check some items and reports the results at the end.
// ---------------------------------------------------------------
public class ResultsWindowDemo
{
    [Start]
    public void Run()
    {
        // 1. Make a log. During the job, add one line per finding. Nothing is shown yet.
        ResultLog log = new ResultLog();
        for (int i = 1; i <= 12; i++)
        {
            string item = "Item " + i.ToString("00");
            if (i % 5 == 0)
            {
                log.Error(item + ": a required value is missing");
            }
            else if (i % 3 == 0)
            {
                log.Warning(item + ": the value looks unusual");
            }
            else
            {
                log.Ok(item + ": checked");
            }
        }

        // 2. At the end, show everything in one window.
        ResultsReport.Show("Results window demo", log);
    }
}

// ---------------------------------------------------------------
// Part 2: the log. It only collects lines and counts them.
// ---------------------------------------------------------------
public class ResultLog
{
    public readonly List<string> Lines = new List<string>();
    public int OkCount;
    public int WarningCount;
    public int ErrorCount;

    public void Ok(string text) { Lines.Add("OK       " + text); OkCount++; }
    public void Warning(string text) { Lines.Add("WARNING  " + text); WarningCount++; }
    public void Error(string text) { Lines.Add("ERROR    " + text); ErrorCount++; }

    // A one-line summary for the top of the window.
    public string Summary()
    {
        return Lines.Count + " items:  " + OkCount + " OK,  " + WarningCount + " warnings,  " + ErrorCount + " errors";
    }
}

// ---------------------------------------------------------------
// Part 3: the reusable report window.
// ---------------------------------------------------------------
public class ResultsReport : Form
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private readonly ResultLog log;
    private readonly string title;
    private readonly bool dark;

    public static void Show(string title, ResultLog log)
    {
        using (ResultsReport window = new ResultsReport(title, log))
        {
            IntPtr eplan = IntPtr.Zero;
            try { eplan = Process.GetCurrentProcess().MainWindowHandle; } catch (Exception) { }
            if (eplan == IntPtr.Zero)
            {
                window.ShowDialog();
            }
            else
            {
                window.StartPosition = FormStartPosition.CenterParent;
                window.ShowDialog(new ResultsOwner(eplan));
            }
        }
    }

    private ResultsReport(string title, ResultLog log)
    {
        this.log = log;
        this.title = title;
        dark = IsEplanDark();

        Color back = dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
        Color fore = dark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(26, 26, 26);
        Color field = dark ? Color.FromArgb(45, 45, 45) : Color.White;
        Color border = dark ? Color.FromArgb(90, 90, 90) : Color.FromArgb(160, 160, 160);
        Color head = dark ? Color.FromArgb(98, 169, 235) : Color.FromArgb(15, 108, 189);

        Text = title;
        ClientSize = new Size(620, 400);
        MinimumSize = new Size(420, 260);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = back;
        ForeColor = fore;

        Label summary = new Label();
        summary.Text = log.Summary();
        summary.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        summary.ForeColor = log.ErrorCount > 0 ? Color.FromArgb(235, 90, 90) : head;
        summary.Location = new Point(14, 12);
        summary.Size = new Size(592, 26);
        summary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        TextBox box = new TextBox();
        box.Multiline = true;
        box.ReadOnly = true;
        box.ScrollBars = ScrollBars.Both;
        box.WordWrap = false;
        box.Font = new Font("Consolas", 9.5f);          // fixed-width, so columns line up
        box.BackColor = field;
        box.ForeColor = fore;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Location = new Point(14, 44);
        box.Size = new Size(592, 304);
        box.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        box.Text = string.Join(Environment.NewLine, log.Lines.ToArray());
        box.Select(0, 0);                                // no selection highlight when the window opens

        Button copy = MakeButton("Copy", 14, field, border, fore);
        copy.Click += delegate
        {
            Clipboard.SetText(BuildText());
            copy.Text = "Copied";
        };
        Button save = MakeButton("Save...", 100, field, border, fore);
        save.Click += OnSave;
        Button close = MakeButton("Close", 530, field, border, fore);
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        close.DialogResult = DialogResult.OK;
        AcceptButton = close;
        CancelButton = close;

        Controls.AddRange(new Control[] { summary, box, copy, save, close });
        HandleCreated += delegate { ColorTitleBar(back, fore); };
    }

    private static Button MakeButton(string text, int x, Color back, Color border, Color fore)
    {
        Button button = new Button();
        button.Text = text;
        button.Location = new Point(x, 360);
        button.Size = new Size(76, 28);
        button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = border;
        button.BackColor = back;
        button.ForeColor = fore;
        button.UseVisualStyleBackColor = false;
        return button;
    }

    // The text that Copy and Save use: a heading, the summary and every line.
    private string BuildText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(title + "   " + DateTime.Now.ToString("g"));
        sb.AppendLine(log.Summary());
        sb.AppendLine();
        foreach (string line in log.Lines)
        {
            sb.AppendLine(line);
        }
        return sb.ToString();
    }

    private void OnSave(object sender, EventArgs e)
    {
        using (SaveFileDialog dialog = new SaveFileDialog())
        {
            dialog.Filter = "Text file (*.txt)|*.txt";
            dialog.FileName = "results.txt";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dialog.FileName, BuildText());
            }
        }
    }

    private void ColorTitleBar(Color back, Color fore)
    {
        try
        {
            int useDark = dark ? 1 : 0;
            if (DwmSetWindowAttribute(Handle, 20, ref useDark, 4) != 0) { DwmSetWindowAttribute(Handle, 19, ref useDark, 4); }
            int caption = back.R | (back.G << 8) | (back.B << 16);
            int text = fore.R | (fore.G << 8) | (fore.B << 16);
            DwmSetWindowAttribute(Handle, 35, ref caption, 4);
            DwmSetWindowAttribute(Handle, 36, ref text, 4);
        }
        catch (Exception)
        {
        }
    }

    // As in example 04: 0 = automatic (follow Windows), 1 = dark, 2 = light.
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
            return true;
        }
    }
}

public class ResultsOwner : IWin32Window
{
    private readonly IntPtr handle;

    public ResultsOwner(IntPtr handle)
    {
        this.handle = handle;
    }

    public IntPtr Handle
    {
        get { return handle; }
    }
}
