// ============================================================
// Example   : 15 - Hand a value from one script to another (receiver)
// Purpose   : Pick up a short-lived value that another script left behind, and ignore it if it is old.
// You learn : Reading AppDomain data, checking a timestamp, clearing the value after use
// Level     : Advanced
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Windows.Forms;
using Eplan.EplApi.Scripting;

public class HandOffReceiver
{
    // Must match the key in HandOffSender.cs.
    private const string Key = "EplanExamples.HandOff";

    // How old a note may be before we ignore it, in seconds.
    private const int MaxAgeSeconds = 60;

    [Start]
    public void Run()
    {
        string value = TakeValue();
        if (value == null)
        {
            MessageBox.Show("No fresh value was waiting.\r\n\r\nRun HandOffSender.cs, which starts this script for you.",
                "Hand-off receiver", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        MessageBox.Show("Received:\r\n" + value, "Hand-off receiver", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Takes the value, but only if it is fresh. Returns null if there is none, or if it is too old.
    private static string TakeValue()
    {
        object stored = AppDomain.CurrentDomain.GetData(Key);
        AppDomain.CurrentDomain.SetData(Key, null);        // always clear it: a note is for one reader, once

        string text = stored as string;
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        // The sender wrote "value|ticks". Split at the LAST "|" in case the value itself contains one.
        int bar = text.LastIndexOf('|');
        if (bar < 0)
        {
            return null;
        }

        long ticks;
        if (!long.TryParse(text.Substring(bar + 1), out ticks))
        {
            return null;
        }

        TimeSpan age = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);
        if (age.TotalSeconds < 0 || age.TotalSeconds > MaxAgeSeconds)
        {
            return null;                                     // too old (or from the future): ignore it
        }

        return text.Substring(0, bar);
    }
}
