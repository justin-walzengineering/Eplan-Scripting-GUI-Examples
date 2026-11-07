// ============================================================
// Example   : 15 - Hand a value from one script to another (sender)
// Purpose   : Leave a short-lived value for a second script to pick up, and start that second script.
// You learn : AppDomain data, timestamps so old values expire, ExecuteScript from code, ActionCallingContext
// Level     : Advanced
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
//
// Two files work together. Keep them in the same folder:
//   HandOffSender.cs    (this file)  stores a value, then starts the receiver.
//   HandOffReceiver.cs               picks the value up.
// ============================================================

using System;
using System.IO;                                   // Path, File
using System.Windows.Forms;                        // OpenFileDialog, MessageBox
using Eplan.EplApi.ApplicationFramework;           // CommandLineInterpreter, ActionCallingContext
using Eplan.EplApi.Scripting;                      // The [Start] attribute

public class HandOffSender
{
    // Both scripts must use the same key. Think of it as the name on an envelope.
    private const string Key = "EplanExamples.HandOff";

    [Start]
    public void Run()
    {
        // 1. Make up a value to pass on. In a real script this could be a page name, a mode, or a file path.
        string value = "Hello from the sender, made at " + DateTime.Now.ToString("T");

        // 2. Leave it where the other script can find it. AppDomain.CurrentDomain is shared by all scripts
        //    running inside the same Eplan, so it works like a short-term shared note.
        //    We add the time (in "ticks") so the receiver can tell whether the note is fresh.
        AppDomain.CurrentDomain.SetData(Key, value + "|" + DateTime.UtcNow.Ticks);

        // 3. Find the receiver script. We ask the user to select it.
        string receiver = FindReceiver();
        if (receiver == null)
        {
            // Nobody to receive it: remove the note so it does not linger.
            AppDomain.CurrentDomain.SetData(Key, null);
            return;
        }

        // 4. Start the receiver the same way a ribbon button does: the ExecuteScript action.
        ActionCallingContext context = new ActionCallingContext();
        context.AddParameter("ScriptFile", receiver);
        new CommandLineInterpreter().Execute("ExecuteScript", context);
    }

    private static string FindReceiver()
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Title = "Select HandOffReceiver.cs";
            dialog.Filter = "HandOffReceiver.cs|HandOffReceiver.cs";
            dialog.CheckFileExists = true;
            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
        }
    }
}
