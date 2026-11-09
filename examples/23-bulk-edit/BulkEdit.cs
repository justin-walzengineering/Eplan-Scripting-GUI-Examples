// ============================================================
// Example   : 23 - Bulk edit pages by filter (preview, then confirm)
// Purpose   : Change one property on many pages at once: choose the pages by name, see exactly what would change, tick the ones you want, then apply.
// You learn : Reflection on the data model, filtering, a preview grid with tick boxes, find-and-replace, locking, checking a change by reading it back
// Level     : Advanced
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
//
// THE APPLY STEP CHANGES YOUR PROJECT. Try it on a copy first, and on one page before many.
// Showing the preview changes nothing.
// ============================================================

using System;
using System.Collections.Generic;      // List<T>
using System.Diagnostics;              // Process
using System.Drawing;                  // Point, Size, Color
using System.IO;                       // File, Path
using System.Reflection;               // Assembly, PropertyInfo, MethodInfo, BindingFlags
using System.Text;                     // StringBuilder
using System.Windows.Forms;            // Form, DataGridView, controls
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs.
// ---------------------------------------------------------------
public class BulkEditDemo
{
    [Start]
    public void Run()
    {
        object project;
        List<object> pages;
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            project = BeDm.CurrentProject();
            pages = BeDm.GetList(project, "Pages");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Bulk edit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }

        using (BulkEditForm form = new BulkEditForm(pages))
        {
            form.ShowDialog();
        }
    }
}

// One page in the preview: the page object, its name, and the old and new value of the property.
public class BulkRow
{
    public object Page;
    public string Name;
    public string Old;
    public string New;
}

// ---------------------------------------------------------------
// Part 2: the window.
// ---------------------------------------------------------------
public class BulkEditForm : Form
{
    // The property we edit. 11011 is the page description in Eplan. You could add a drop-down of other properties.
    private const int PropertyId = 11011;

    private readonly List<object> pages;
    private readonly List<BulkRow> rows = new List<BulkRow>();
    private readonly TextBox nameFilterBox = new TextBox();
    private readonly ComboBox modeBox = new ComboBox();
    private readonly TextBox findBox = new TextBox();
    private readonly TextBox replaceBox = new TextBox();
    private readonly Label findLabel = new Label();
    private readonly Label replaceLabel = new Label();
    private readonly DataGridView grid = new DataGridView();
    private readonly Label summary = new Label();
    private readonly CheckBox safeCheck = new CheckBox();
    private readonly Button applyButton = new Button();

    public BulkEditForm(List<object> pages)
    {
        this.pages = pages;

        Text = "Bulk edit page descriptions";
        ClientSize = new Size(820, 560);
        MinimumSize = new Size(640, 420);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        AddLabel("Pages whose name contains", 12, 18);
        nameFilterBox.Location = new Point(170, 14);
        nameFilterBox.Width = 200;
        nameFilterBox.TextChanged += delegate { Rebuild(); };
        Label filterHint = new Label();
        filterHint.Text = "(leave empty for all pages)";
        filterHint.Location = new Point(378, 18);
        filterHint.AutoSize = true;
        filterHint.ForeColor = SystemColors.GrayText;

        AddLabel("Change", 12, 52);
        modeBox.Location = new Point(170, 48);
        modeBox.Width = 200;
        modeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        modeBox.Items.AddRange(new object[] { "Replace text in the description", "Set the description to" });
        modeBox.SelectedIndex = 0;
        modeBox.SelectedIndexChanged += delegate { UpdateModeLabels(); Rebuild(); };

        findLabel.Location = new Point(12, 86);
        findLabel.AutoSize = true;
        findBox.Location = new Point(170, 82);
        findBox.Width = 280;
        findBox.TextChanged += delegate { Rebuild(); };

        replaceLabel.Location = new Point(470, 86);
        replaceLabel.AutoSize = true;
        replaceBox.Location = new Point(540, 82);
        replaceBox.Width = 260;
        replaceBox.TextChanged += delegate { Rebuild(); };

        grid.Location = new Point(12, 118);
        grid.Size = new Size(796, 340);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.CellContentClick += delegate { UpdateSummary(); };
        grid.CurrentCellDirtyStateChanged += delegate
        {
            if (grid.IsCurrentCellDirty) { grid.CommitEdit(DataGridViewDataErrorContexts.Commit); }   // updates the tick count immediately
            UpdateSummary();
        };
        grid.DataError += delegate(object s, DataGridViewDataErrorEventArgs e) { e.ThrowException = false; };

        summary.Location = new Point(12, 466);
        summary.Size = new Size(796, 20);
        summary.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Label warning = new Label();
        warning.Text = "Applying changes your project. Try it on a copy, and on one page first.";
        warning.ForeColor = Color.Firebrick;
        warning.Font = new Font(Font, FontStyle.Bold);
        warning.Location = new Point(12, 490);
        warning.Size = new Size(796, 20);
        warning.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        safeCheck.Text = "I am working on a copy, or I have a backup";
        safeCheck.Location = new Point(12, 518);
        safeCheck.AutoSize = true;
        safeCheck.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        safeCheck.CheckedChanged += delegate { UpdateSummary(); };

        applyButton.Text = "Apply to ticked pages...";
        applyButton.Location = new Point(560, 516);
        applyButton.Size = new Size(160, 28);
        applyButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        applyButton.Click += OnApply;

        Button close = new Button();
        close.Text = "Close";
        close.DialogResult = DialogResult.Cancel;
        close.Location = new Point(730, 516);
        close.Size = new Size(78, 28);
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        CancelButton = close;

        Controls.AddRange(new Control[] { nameFilterBox, filterHint, modeBox, findLabel, findBox, replaceLabel, replaceBox, grid, summary, warning, safeCheck, applyButton, close });

        // Columns first, then rows.
        DataGridViewCheckBoxColumn tick = new DataGridViewCheckBoxColumn();
        tick.HeaderText = "Apply";
        tick.Width = 50;
        tick.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        grid.Columns.Add(tick);
        grid.Columns.Add("page", "Page");
        grid.Columns.Add("old", "Description now");
        grid.Columns.Add("new", "Description after");
        for (int i = 1; i < grid.Columns.Count; i++) { grid.Columns[i].ReadOnly = true; }

        UpdateModeLabels();
        Rebuild();
    }

    private void AddLabel(string text, int x, int y)
    {
        Label label = new Label();
        label.Text = text;
        label.Location = new Point(x, y);
        label.AutoSize = true;
        Controls.Add(label);
    }

    private void UpdateModeLabels()
    {
        bool replaceMode = modeBox.SelectedIndex == 0;
        findLabel.Text = replaceMode ? "Find" : "New text";
        replaceLabel.Text = "Replace with";
        replaceLabel.Visible = replaceMode;
        replaceBox.Visible = replaceMode;
    }

    // Works out the preview: which pages match, what each description is now and what it would become. Changes nothing.
    private void Rebuild()
    {
        rows.Clear();
        string nameFilter = (nameFilterBox.Text ?? "").Trim().ToLowerInvariant();
        bool replaceMode = modeBox.SelectedIndex == 0;
        string find = findBox.Text ?? "";
        string replace = replaceBox.Text ?? "";

        foreach (object page in pages)
        {
            string name = "";
            string old = "";
            try { name = Convert.ToString(BeDm.Get(page, "Name")); } catch (Exception) { }
            if (nameFilter.Length > 0 && !name.ToLowerInvariant().Contains(nameFilter))
            {
                continue;
            }
            try { old = BeDm.Text(BeDm.Get(BeDm.Get(page, "Properties"), "PAGE_NOMINATIOMN")); } catch (Exception) { }

            string changed;
            if (find.Length == 0)
            {
                changed = old;                                         // nothing typed yet: no change
            }
            else if (replaceMode)
            {
                changed = old.Replace(find, replace);
            }
            else
            {
                changed = find;                                        // "set to": the same new text for every page
            }

            BulkRow row = new BulkRow();
            row.Page = page;
            row.Name = name;
            row.Old = old;
            row.New = changed;
            rows.Add(row);
        }

        grid.Rows.Clear();
        foreach (BulkRow row in rows)
        {
            bool willChange = row.New != row.Old;
            int index = grid.Rows.Add(willChange, row.Name, row.Old, row.New);
            if (willChange)
            {
                grid.Rows[index].DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 210);     // highlight what would change
            }
        }
        UpdateSummary();
    }

    private int TickedCount()
    {
        int count = 0;
        for (int i = 0; i < grid.Rows.Count && i < rows.Count; i++)
        {
            bool ticked = grid.Rows[i].Cells[0].Value is bool && (bool)grid.Rows[i].Cells[0].Value;
            if (ticked && rows[i].New != rows[i].Old)
            {
                count++;
            }
        }
        return count;
    }

    private void UpdateSummary()
    {
        int ticked = TickedCount();
        summary.Text = rows.Count + " pages match.  " + ticked + " ticked page(s) would change.";
        applyButton.Enabled = safeCheck.Checked && ticked > 0;
    }

    private void OnApply(object sender, EventArgs e)
    {
        int ticked = TickedCount();
        if (MessageBox.Show("Change the description of " + ticked + " page(s)?\r\n\r\nThis changes your project.", "Bulk edit",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        int changed = 0;
        List<string> problems = new List<string>();
        IDisposable lockStep = null;
        try
        {
            Cursor.Current = Cursors.WaitCursor;

            // A "locking step" tells Eplan that a script is about to change data, so the changes are grouped and saved together.
            Type lockType = BeDm.FindType("Eplan.EplApi.DataModel.LockingStep", "Eplan.EplApi.DataModelu");
            if (lockType != null)
            {
                lockStep = Activator.CreateInstance(lockType) as IDisposable;
            }

            for (int i = 0; i < grid.Rows.Count && i < rows.Count; i++)
            {
                bool tickedRow = grid.Rows[i].Cells[0].Value is bool && (bool)grid.Rows[i].Cells[0].Value;
                if (!tickedRow || rows[i].New == rows[i].Old)
                {
                    continue;
                }
                try
                {
                    WriteDescription(rows[i].Page, rows[i].New);

                    // Read it back, to be sure Eplan really took the change.
                    string after = BeDm.Text(BeDm.Get(BeDm.Get(rows[i].Page, "Properties"), "PAGE_NOMINATIOMN"));
                    if (after.IndexOf(rows[i].New, StringComparison.Ordinal) >= 0 || rows[i].New.Length == 0)
                    {
                        changed++;
                    }
                    else
                    {
                        problems.Add(rows[i].Name + ": reads back as \"" + after + "\"");
                    }
                }
                catch (Exception ex)
                {
                    problems.Add(rows[i].Name + ": " + ex.Message);
                }
            }
        }
        finally
        {
            if (lockStep != null)
            {
                lockStep.Dispose();
            }
            Cursor.Current = Cursors.Default;
        }

        string text = changed + " page(s) changed.";
        if (problems.Count > 0)
        {
            text += "\r\n\r\nNot changed (" + problems.Count + "):\r\n" + string.Join("\r\n", problems.ToArray(), 0, Math.Min(problems.Count, 12));
        }
        MessageBox.Show(text, "Bulk edit", MessageBoxButtons.OK, problems.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        Rebuild();                                                     // show the new current values
    }

    // Writes the page description through the data model.
    private static void WriteDescription(object page, string newText)
    {
        // Ask Eplan to lock the page for writing.
        try { BeDm.Call(page, "SmartLock"); } catch (Exception) { }

        // Build the property id (a number) as an AnyPropertyId object, then look the value up with it.
        Type idType = BeDm.FindType("Eplan.EplApi.DataModel.AnyPropertyId", "Eplan.EplApi.DataModelu");
        object id = Activator.CreateInstance(idType);
        idType.GetMethod("SetPropertyId").Invoke(id, new object[] { PropertyId });

        object properties = BeDm.Get(page, "Properties");
        MethodInfo getter = null;
        foreach (MethodInfo m in properties.GetType().GetMethods())
        {
            ParameterInfo[] p = m.GetParameters();
            if (m.Name == "get_Property" && p.Length == 1 && p[0].ParameterType == idType)
            {
                getter = m;
                break;
            }
        }
        if (getter == null)
        {
            throw new InvalidOperationException("The property list has no lookup by property id.");
        }
        object value = getter.Invoke(properties, new object[] { id });

        // PropertyValue.Set(string) puts the new text into the value.
        MethodInfo set = value.GetType().GetMethod("Set", new Type[] { typeof(string) });
        if (set == null)
        {
            throw new InvalidOperationException("The property value cannot be set from text.");
        }
        set.Invoke(value, new object[] { newText });
    }
}

// ---------------------------------------------------------------
// Reaching Eplan's data model WITHOUT a compile-time reference.
// Eplan's script compiler does not reference the data model, so a script cannot write "Project" or "Page" as a
// type name. The data model is still loaded inside Eplan, so we look it up by name and use "reflection": asking
// .NET to find a member by its name while the script runs. Every call here is wrapped by the caller in try/catch.
// ---------------------------------------------------------------
public static class BeDm
{
    // Finds a type from its full name. Looks in the assemblies already loaded, then loads the DLL if needed.
    public static Type FindType(string fullName, string assemblyName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type found = assembly.GetType(fullName, false);
            if (found != null)
            {
                return found;
            }
        }
        try
        {
            Type loadedType = Assembly.Load(assemblyName).GetType(fullName, false);
            if (loadedType != null)
            {
                return loadedType;
            }
        }
        catch (Exception)
        {
        }
        try
        {
            string dll = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), assemblyName + ".dll");
            if (File.Exists(dll))
            {
                return Assembly.LoadFrom(dll).GetType(fullName, false);
            }
        }
        catch (Exception)
        {
        }
        return null;
    }

    // The project that is open in Eplan right now. Throws a friendly error if there is none.
    public static object CurrentProject()
    {
        Type selectionType = FindType("Eplan.EplApi.HEServices.SelectionSet", "Eplan.EplApi.HEServicesu");
        if (selectionType == null)
        {
            throw new InvalidOperationException("Eplan's selection service was not found.");
        }
        object selection = Activator.CreateInstance(selectionType);
        // Without this, Eplan tries to lock the project and fails with "Failed to generate an object of the LockingStep class".
        PropertyInfo unlock = selectionType.GetProperty("LockSelectionByDefault");
        if (unlock != null && unlock.CanWrite)
        {
            unlock.SetValue(selection, false, null);
        }
        object project = selectionType.GetMethod("GetCurrentProject").Invoke(selection, new object[] { false });
        if (project == null)
        {
            throw new InvalidOperationException("No project is open.");
        }
        return project;
    }

    // Reads a public property by name. We walk up through the base classes one level at a time, and we go through the list of properties ourselves, because asking for one by name can fail with "ambiguous match" when a class declares the same name more than once (Eplan does this for its page properties).
    public static object Get(object obj, string name)
    {
        for (Type t = obj.GetType(); t != null; t = t.BaseType)
        {
            foreach (PropertyInfo property in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (property.Name == name && property.CanRead && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(obj, null);
                }
            }
        }
        throw new MissingMemberException(obj.GetType().Name, name);
    }

    // An array property, as a list of objects. Returns an empty list when the property is empty.
    public static List<object> GetList(object obj, string name)
    {
        List<object> result = new List<object>();
        Array array = Get(obj, name) as Array;
        if (array != null)
        {
            foreach (object item in array)
            {
                result.Add(item);
            }
        }
        return result;
    }

    // Calls a method that takes no parameters, for example SmartLock().
    public static void Call(object obj, string name)
    {
        MethodInfo method = obj.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if (method == null)
        {
            throw new MissingMethodException(obj.GetType().Name, name);
        }
        method.Invoke(obj, null);
    }

    // The text of a property value. Reading an empty property can throw, so we return "" if it does.
    public static string Text(object propertyValue)
    {
        try
        {
            return propertyValue == null ? "" : PickLanguage(propertyValue.ToString());
        }
        catch (Exception)
        {
            return "";
        }
    }

    // Multi-language texts look like "en_US@Hello;de_DE@Hallo;". Pick English, else the "any language" text, else the first.
    public static string PickLanguage(string s)
    {
        if (s == null || s.IndexOf('@') < 0)
        {
            return s ?? "";
        }
        string english = null;
        string anyLanguage = null;
        string first = null;
        foreach (string part in s.Split(';'))
        {
            int at = part.IndexOf('@');
            if (at < 0)
            {
                continue;
            }
            string language = part.Substring(0, at);
            string text = part.Substring(at + 1);
            if (text.Length == 0)
            {
                continue;
            }
            if (language == "en_US" && english == null) { english = text; }
            if (language == "??_??" && anyLanguage == null) { anyLanguage = text; }
            if (first == null) { first = text; }
        }
        return english ?? anyLanguage ?? first ?? "";
    }

    // Every property of an object as rows of { id, name, value }.
    public static List<string[]> AllProperties(object storableObject)
    {
        List<string[]> rows = new List<string[]>();
        object propertyList = Get(storableObject, "Properties");
        List<object> ids = GetList(propertyList, "ExistingIds");
        List<object> values = GetList(propertyList, "ExistingValues");
        for (int i = 0; i < ids.Count && i < values.Count; i++)
        {
            string id = "";
            string name = "";
            try { id = Convert.ToString(Get(ids[i], "AsInt")); } catch (Exception) { }
            try { name = Convert.ToString(Get(Get(ids[i], "Definition"), "Name")); } catch (Exception) { }
            rows.Add(new string[] { id, name, Text(values[i]) });
        }
        return rows;
    }
}
