// ============================================================
// Example   : 01 - Reusable input dialog
// Purpose   : Ask the user for text, confirm an action, and show a message, using small helpers you can reuse.
// You learn : Windows Forms basics, returning a value from a window, handling Cancel and empty input
// Level     : Beginner
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (run in Eplan 2026.0.3: see README)
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
        // 1. Ask for text. Prompt returns the typed text, or null if the user pressed Cancel.
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

        // 3. Handle empty input. Trim() removes spaces at both ends; Length is how many characters remain.
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
// for example DialogHelper.Prompt(...). Copy this class into your own scripts to reuse it.
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
