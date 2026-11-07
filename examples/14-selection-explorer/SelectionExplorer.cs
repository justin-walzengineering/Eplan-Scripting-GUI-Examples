// ============================================================
// Example   : 14 - Selection explorer
// Purpose   : Write the properties of everything you have selected in Eplan to a text file, to learn what an object holds and to find property numbers.
// You learn : Reading the selection, reflection on the data model, building a text report, SaveFileDialog, opening the result
// Level     : Intermediate
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026 (pages selected in the page navigator))
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
//
// Read-only: this example never changes your project.
// ============================================================

using System;
using System.Collections.Generic;      // List<T>
using System.Diagnostics;              // Process
using System.IO;                       // File
using System.Reflection;               // Assembly, PropertyInfo, MethodInfo, BindingFlags
using System.Text;                     // StringBuilder
using System.Windows.Forms;            // MessageBox, SaveFileDialog
using Eplan.EplApi.Scripting;          // The [Start] attribute

public class SelectionExplorerDemo
{
    // A report of more than this many objects would be too long to be useful.
    private const int MaxObjects = 100;

    [Start]
    public void Run()
    {
        StringBuilder report = new StringBuilder();
        int count = 0;

        try
        {
            Cursor.Current = Cursors.WaitCursor;
            object project = SxDm.CurrentProject();
            report.AppendLine("Selection explorer   " + DateTime.Now.ToString("g"));
            report.AppendLine("Project: " + Convert.ToString(SxDm.Get(project, "ProjectName")));
            report.AppendLine();

            List<object> selection = GetSelection();
            if (selection.Count == 0)
            {
                Cursor.Current = Cursors.Default;
                MessageBox.Show("Nothing is selected.\r\n\r\nSelect one or more objects (for example a page in the page navigator, or a device in the drawing) and run the script again. It works best from a ribbon button or the Quick Access toolbar, so the selection is kept.",
                    "Selection explorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (object obj in selection)
            {
                if (count >= MaxObjects)
                {
                    report.AppendLine("... only the first " + MaxObjects + " objects are shown.");
                    break;
                }
                count++;
                AppendObject(report, obj, count);
            }
        }
        catch (Exception ex)
        {
            Cursor.Current = Cursors.Default;
            MessageBox.Show(ex.Message, "Selection explorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }

        using (SaveFileDialog dialog = new SaveFileDialog())
        {
            dialog.Filter = "Text file (*.txt)|*.txt";
            dialog.FileName = "selection-properties.txt";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            File.WriteAllText(dialog.FileName, report.ToString(), Encoding.UTF8);

            // Open the text file with whatever program Windows has set for it.
            ProcessStartInfo start = new ProcessStartInfo(dialog.FileName);
            start.UseShellExecute = true;
            Process.Start(start);
        }
    }

    // The objects selected in Eplan. The selection is "locked" by default (it comes back empty), so we unlock it first.
    private static List<object> GetSelection()
    {
        Type selectionType = SxDm.FindType("Eplan.EplApi.HEServices.SelectionSet", "Eplan.EplApi.HEServicesu");
        object selection = Activator.CreateInstance(selectionType);
        PropertyInfo unlock = selectionType.GetProperty("LockSelectionByDefault");
        if (unlock != null && unlock.CanWrite)
        {
            unlock.SetValue(selection, false, null);
        }
        return SxDm.GetList(selection, "Selection");
    }

    // One block of the report: a heading, then one line per property that has a value.
    private static void AppendObject(StringBuilder report, object obj, int number)
    {
        string name = "";
        try { name = Convert.ToString(SxDm.Get(obj, "Name")); } catch (Exception) { }
        report.AppendLine("==== " + number + ". " + obj.GetType().Name + (name.Length > 0 ? "   " + name : ""));

        try
        {
            foreach (string[] row in SxDm.AllProperties(obj))
            {
                if (row[2].Length == 0)
                {
                    continue;                              // skip properties without a value, to keep the report short
                }
                report.AppendLine(row[0].PadRight(8) + row[1].PadRight(44) + row[2].Replace("\r", " ").Replace("\n", " "));
            }
        }
        catch (Exception ex)
        {
            report.AppendLine("   (properties could not be read: " + ex.Message + ")");
        }
        report.AppendLine();
    }
}

// ---------------------------------------------------------------
// Reaching Eplan's data model WITHOUT a compile-time reference.
// Eplan's script compiler does not reference the data model, so a script cannot write "Project" or "Page" as a
// type name. The data model is still loaded inside Eplan, so we look it up by text and use "reflection": asking
// .NET to find a member by its name while the script runs. Every call here is wrapped by the caller in try/catch.
// ---------------------------------------------------------------
public static class SxDm
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
