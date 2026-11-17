// ============================================================
// Example   : 28 - Unit check
// Purpose   : Compare a width you measured in Eplan with a known dimension from a data sheet, and say whether it matches in millimetres or in inches.
// You learn : A small form with number fields, parsing numbers safely, comparing with a tolerance, millimetres versus inches, separating the logic from the window
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 (compile-checked only, DRAFT: not yet run in Eplan)
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

        AddLabel("Width you measured in Eplan (the number as shown):", 15, 15);
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
