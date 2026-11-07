// ============================================================
// Example   : 19 - Renumber helper
// Purpose   : A friendly window in front of Eplan's renumber action: choose what to renumber, set the start and step, and confirm before it runs.
// You learn : Running the renumber action, enabling a button only when a safety box is ticked, enabling options by choice, confirming a change
// Level     : Intermediate
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (DRAFT: not yet run in Eplan)
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

        // One more chance to back out, in plain words, before anything changes.
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
