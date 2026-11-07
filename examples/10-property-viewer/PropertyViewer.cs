// ============================================================
// Example   : 10 - Property viewer
// Purpose   : Look at every property of the project or of whatever you have selected, in a table you can search, to learn which properties exist and what they hold.
// You learn : Reflection on the data model, reading the current selection, the "unlock the selection" step, searching a table, copying results
// Level     : Advanced
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (DRAFT: not yet run in Eplan)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
//
// Read-only: this example never changes your project.
// ============================================================

using System;
using System.Collections.Generic;      // List<T>
using System.Diagnostics;              // Process
using System.Drawing;                  // Point, Size
using System.IO;                       // File, Path
using System.Reflection;               // Assembly, PropertyInfo, MethodInfo, BindingFlags
using System.Text;                     // StringBuilder
using System.Windows.Forms;            // Form, ListView, ComboBox, Clipboard
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs.
// ---------------------------------------------------------------
public class PropertyViewerDemo
{
    [Start]
    public void Run()
    {
        List<string> labels = new List<string>();
        List<object> targets = new List<object>();

        try
        {
            Cursor.Current = Cursors.WaitCursor;

            // The first choice is always the project itself.
            object project = PvDm.CurrentProject();
            labels.Add("Project: " + Convert.ToString(PvDm.Get(project, "ProjectName")));
            targets.Add(project);

            // Then everything that is selected right now (select objects in Eplan BEFORE you run the script).
            foreach (object selected in GetSelection())
            {
                labels.Add(DescribeObject(selected));
                targets.Add(selected);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Property viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }

        using (PropertyViewerForm form = new PropertyViewerForm(labels, targets))
        {
            form.ShowDialog();
        }
    }

    // The objects selected in Eplan. By default Eplan "locks" the selection for a script and it comes back empty,
    // so we switch that off first. Run the script from a ribbon button or the Quick Access toolbar so the page keeps focus.
    private static List<object> GetSelection()
    {
        List<object> result = new List<object>();
        try
        {
            Type selectionType = PvDm.FindType("Eplan.EplApi.HEServices.SelectionSet", "Eplan.EplApi.HEServicesu");
            object selection = Activator.CreateInstance(selectionType);

            PropertyInfo unlock = selectionType.GetProperty("LockSelectionByDefault");
            if (unlock != null && unlock.CanWrite)
            {
                unlock.SetValue(selection, false, null);
            }
            result = PvDm.GetList(selection, "Selection");
        }
        catch (Exception)
        {
            // No selection available: the viewer still shows the project.
        }
        if (result.Count > 50)
        {
            result.RemoveRange(50, result.Count - 50);           // a very long drop-down is no use to anyone
        }
        return result;
    }

    // A short label: the kind of object and its name, if it has one.
    private static string DescribeObject(object obj)
    {
        string kind = obj.GetType().Name;
        string name = "";
        try { name = Convert.ToString(PvDm.Get(obj, "Name")); } catch (Exception) { }
        return name.Length > 0 ? kind + ": " + name : kind;
    }
}

// ---------------------------------------------------------------
// Part 2: the window.
// ---------------------------------------------------------------
public class PropertyViewerForm : Form
{
    private readonly List<object> targets;
    private readonly ComboBox targetBox = new ComboBox();
    private readonly TextBox searchBox = new TextBox();
    private readonly ListView list = new ListView();
    private readonly Label countLabel = new Label();
    private List<string[]> rows = new List<string[]>();
    private readonly List<string[]> shown = new List<string[]>();

    public PropertyViewerForm(List<string> labels, List<object> targets)
    {
        this.targets = targets;

        Text = "Property viewer (read-only)";
        ClientSize = new Size(760, 500);
        MinimumSize = new Size(520, 340);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        Label showLabel = new Label();
        showLabel.Text = "Show";
        showLabel.Location = new Point(12, 16);
        showLabel.AutoSize = true;

        targetBox.Location = new Point(56, 12);
        targetBox.Width = 692;
        targetBox.DropDownStyle = ComboBoxStyle.DropDownList;
        targetBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        foreach (string label in labels) { targetBox.Items.Add(label); }
        targetBox.SelectedIndexChanged += delegate { LoadTarget(); };

        Label searchLabel = new Label();
        searchLabel.Text = "Search";
        searchLabel.Location = new Point(12, 48);
        searchLabel.AutoSize = true;

        searchBox.Location = new Point(56, 44);
        searchBox.Width = 500;
        searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        searchBox.TextChanged += delegate { Refill(); };

        countLabel.Location = new Point(564, 48);
        countLabel.Size = new Size(184, 18);
        countLabel.TextAlign = ContentAlignment.MiddleRight;
        countLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        list.View = View.Details;
        list.FullRowSelect = true;
        list.HideSelection = false;
        list.Columns.Add("ID", 70);
        list.Columns.Add("Property", 280);
        list.Columns.Add("Value", 360);
        list.Location = new Point(12, 76);
        list.Size = new Size(736, 372);
        list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Button copy = new Button();
        copy.Text = "Copy shown rows";
        copy.Location = new Point(12, 460);
        copy.Size = new Size(120, 28);
        copy.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        copy.Click += delegate
        {
            StringBuilder sb = new StringBuilder();
            foreach (string[] row in shown) { sb.AppendLine(row[0] + "\t" + row[1] + "\t" + row[2]); }
            if (sb.Length > 0) { Clipboard.SetText(sb.ToString()); }
        };

        Button close = new Button();
        close.Text = "Close";
        close.DialogResult = DialogResult.Cancel;
        close.Location = new Point(673, 460);
        close.Size = new Size(75, 28);
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        CancelButton = close;

        Controls.AddRange(new Control[] { showLabel, targetBox, searchLabel, searchBox, countLabel, list, copy, close });
        targetBox.SelectedIndex = 0;
    }

    // Reads all properties of the chosen object.
    private void LoadTarget()
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            rows = PvDm.AllProperties(targets[targetBox.SelectedIndex]);
        }
        catch (Exception ex)
        {
            rows = new List<string[]>();
            MessageBox.Show("I could not read the properties:\r\n\r\n" + ex.Message, "Property viewer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
        Refill();
    }

    private void Refill()
    {
        string[] words = (searchBox.Text ?? "").Trim().ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        shown.Clear();
        list.BeginUpdate();
        list.Items.Clear();
        foreach (string[] row in rows)
        {
            string haystack = (row[0] + " " + row[1] + " " + row[2]).ToLowerInvariant();
            bool matches = true;
            foreach (string word in words)
            {
                if (!haystack.Contains(word)) { matches = false; break; }
            }
            if (matches)
            {
                shown.Add(row);
                list.Items.Add(new ListViewItem(row));
            }
        }
        list.EndUpdate();
        countLabel.Text = shown.Count + " of " + rows.Count + " properties";
    }
}

// ---------------------------------------------------------------
// Reaching Eplan's data model WITHOUT a compile-time reference.
// Eplan's script compiler does not reference the data model, so a script cannot write "Project" or "Page" as a
// type name. The data model is still loaded inside Eplan, so we look it up by text and use "reflection": asking
// .NET to find a member by its name while the script runs. Every call here is wrapped by the caller in try/catch.
// ---------------------------------------------------------------
public static class PvDm
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
