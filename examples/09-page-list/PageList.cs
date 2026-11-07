// ============================================================
// Example   : 09 - Page list with search, sort and CSV export
// Purpose   : Show every page of the open project in a table you can search, sort and save as a CSV file.
// You learn : Reaching Eplan's data model by reflection, ListView with sortable columns, filtering as you type, writing a CSV file
// Level     : Advanced
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
//
// Read-only: this example never changes your project.
// ============================================================

using System;
using System.Collections;              // IComparer
using System.Collections.Generic;      // List<T>
using System.Diagnostics;              // Process
using System.Drawing;                  // Point, Size
using System.IO;                       // File, Path
using System.Reflection;               // Assembly, PropertyInfo, MethodInfo, BindingFlags
using System.Text;                     // StringBuilder
using System.Windows.Forms;            // Form, ListView, TextBox, SaveFileDialog
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs.
// ---------------------------------------------------------------
public class PageListDemo
{
    [Start]
    public void Run()
    {
        List<string[]> pages;
        string projectName;
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            object project = PgDm.CurrentProject();
            projectName = Convert.ToString(PgDm.Get(project, "ProjectName"));
            pages = ReadPages(project);
        }
        catch (Exception ex)
        {
            Exception real = ex;
            while (real is TargetInvocationException && real.InnerException != null) { real = real.InnerException; }   // show the real reason, not the reflection wrapper
            MessageBox.Show(real.Message, "Page list", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }

        using (PageListForm form = new PageListForm(projectName, pages))
        {
            form.ShowDialog();
        }
    }

    // Turns each page into a row of three texts: name, description, page type.
    private static List<string[]> ReadPages(object project)
    {
        List<string[]> rows = new List<string[]>();
        foreach (object page in PgDm.GetList(project, "Pages"))
        {
            string name = "";
            string type = "";
            string description = "";
            try { name = Convert.ToString(PgDm.Get(page, "Name")); } catch (Exception) { }
            try { type = Convert.ToString(PgDm.Get(page, "PageTypeName")); } catch (Exception) { }
            try
            {
                // PAGE_NOMINATIOMN is the page description (Eplan's own spelling of the property name).
                description = PgDm.Text(PgDm.Get(PgDm.Get(page, "Properties"), "PAGE_NOMINATIOMN"));
            }
            catch (Exception) { }
            rows.Add(new string[] { name, description, type });
        }
        return rows;
    }
}

// ---------------------------------------------------------------
// Part 2: the window.
// ---------------------------------------------------------------
public class PageListForm : Form
{
    private readonly List<string[]> all;
    private readonly List<string[]> shown = new List<string[]>();
    private readonly TextBox searchBox = new TextBox();
    private readonly ListView list = new ListView();
    private readonly Label countLabel = new Label();
    private int sortColumn = 0;
    private bool sortAscending = true;

    public PageListForm(string projectName, List<string[]> pages)
    {
        all = pages;

        Text = "Page list: " + projectName;
        ClientSize = new Size(760, 480);
        MinimumSize = new Size(480, 320);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        Label searchLabel = new Label();
        searchLabel.Text = "Search";
        searchLabel.Location = new Point(12, 16);
        searchLabel.AutoSize = true;

        searchBox.Location = new Point(64, 12);
        searchBox.Width = 540;
        searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        searchBox.TextChanged += delegate { Refill(); };

        countLabel.Location = new Point(612, 16);
        countLabel.Size = new Size(136, 18);
        countLabel.TextAlign = ContentAlignment.MiddleRight;
        countLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        list.View = View.Details;
        list.FullRowSelect = true;
        list.HideSelection = false;
        list.Columns.Add("Page", 220);
        list.Columns.Add("Description", 380);
        list.Columns.Add("Type", 130);
        list.Location = new Point(12, 44);
        list.Size = new Size(736, 386);
        list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        list.ColumnClick += OnColumnClick;                       // click a header to sort

        Button export = new Button();
        export.Text = "Save as CSV...";
        export.Location = new Point(12, 440);
        export.Size = new Size(110, 28);
        export.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        export.Click += OnExport;

        Button close = new Button();
        close.Text = "Close";
        close.DialogResult = DialogResult.Cancel;
        close.Location = new Point(673, 440);
        close.Size = new Size(75, 28);
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        CancelButton = close;

        Controls.AddRange(new Control[] { searchLabel, searchBox, countLabel, list, export, close });
        Shown += delegate { searchBox.Focus(); };
        Refill();
    }

    // Shows the pages that contain every word typed in the search box, in any column.
    private void Refill()
    {
        string[] words = (searchBox.Text ?? "").Trim().ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        shown.Clear();
        foreach (string[] row in all)
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
            }
        }
        SortShown();
        Redraw();
    }

    private void SortShown()
    {
        int column = sortColumn;
        bool ascending = sortAscending;
        shown.Sort(delegate(string[] a, string[] b)
        {
            int result = string.Compare(a[column], b[column], StringComparison.CurrentCultureIgnoreCase);
            return ascending ? result : -result;
        });
    }

    private void Redraw()
    {
        list.BeginUpdate();
        list.Items.Clear();
        foreach (string[] row in shown)
        {
            list.Items.Add(new ListViewItem(row));
        }
        list.EndUpdate();
        countLabel.Text = shown.Count + " of " + all.Count + " pages";
    }

    private void OnColumnClick(object sender, ColumnClickEventArgs e)
    {
        sortAscending = (e.Column == sortColumn) ? !sortAscending : true;   // click again to reverse
        sortColumn = e.Column;
        SortShown();
        Redraw();
    }

    // Saves the pages that are currently shown. Semicolon-separated, with quotes where needed, so Excel opens it directly.
    private void OnExport(object sender, EventArgs e)
    {
        using (SaveFileDialog dialog = new SaveFileDialog())
        {
            dialog.Filter = "CSV file (*.csv)|*.csv";
            dialog.FileName = "pages.csv";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Page;Description;Type");
            foreach (string[] row in shown)
            {
                csv.AppendLine(Quote(row[0]) + ";" + Quote(row[1]) + ";" + Quote(row[2]));
            }
            File.WriteAllText(dialog.FileName, csv.ToString(), Encoding.UTF8);
            MessageBox.Show(shown.Count + " pages saved to:\r\n" + dialog.FileName, "Page list", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // A cell that contains a semicolon, a quote or a line break must be wrapped in quotes, with inner quotes doubled.
    private static string Quote(string text)
    {
        if (text.IndexOfAny(new char[] { ';', '"', '\r', '\n' }) >= 0)
        {
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        return text;
    }
}

// ---------------------------------------------------------------
// Reaching Eplan's data model WITHOUT a compile-time reference.
// Eplan's script compiler does not reference the data model, so a script cannot write "Project" or "Page" as a
// type name. The data model is still loaded inside Eplan, so we look it up by text and use "reflection": asking
// .NET to find a member by its name while the script runs. Every call here is wrapped by the caller in try/catch.
// ---------------------------------------------------------------
public static class PgDm
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
