// ============================================================
// Example   : 27 - Compile canary
// Purpose   : A small starting point for any script that writes a log line as its very first action, so a missing log tells you the script did not run.
// You learn : Why a script that does not compile shows no message, writing a log file, the usual causes of compile problems, keeping the log outside the project
// Level     : Beginner
// Tested in : Eplan Electric P8 2026.0.3 and 2027.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.IO;                       // File, Path, Directory
using System.Windows.Forms;            // MessageBox
using Eplan.EplApi.Scripting;          // The [Start] attribute

// A class is a named bundle of code. Eplan looks for the method marked [Start] and runs it.
public class CompileCanaryDemo
{
    [Start]
    public void Run()
    {
        // 1. The canary: the very first statement writes a line to a log file.
        //    If you run the script and this file does not appear (or its time did not change),
        //    the script never started. The most common reason is that it did not compile.
        string log = CanaryLog.Start("Compile canary");

        try
        {
            // 2. Your own work goes here. Write a line to the log before each step,
            //    so after a crash you can see how far the script got.
            CanaryLog.Line(log, "step 1: nothing to do in this demo");

            // To see a compile failure on purpose, remove the two slashes at the start of the next line
            // and run the script again. No message appears, and the log is NOT updated:
            // int broken = ;

            MessageBox.Show("The script ran.\r\n\r\nLog file:\r\n" + log, "Compile canary", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            // A script that compiles but fails while running is different: we can catch it and write it down.
            CanaryLog.Line(log, "ERROR: " + ex.Message);
            MessageBox.Show("The script stopped. See the log:\r\n" + log, "Compile canary", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

// A small helper class. Its name is unique to this example (the Canary prefix).
public static class CanaryLog
{
    // Creates the log (or starts it fresh) and writes the first line. Returns the log path.
    public static string Start(string name)
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EplanScriptLogs");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, name.Replace(' ', '_') + ".log");
        File.WriteAllText(path, name + " started " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n");
        return path;
    }

    // Adds one line.
    public static void Line(string path, string text)
    {
        File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss") + "  " + text + "\r\n");
    }
}
