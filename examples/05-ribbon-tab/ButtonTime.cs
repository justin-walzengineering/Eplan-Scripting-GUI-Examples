// ============================================================
// Example   : 05 - Ribbon tab (button script: Show time)
// Purpose   : The script that runs when the "Show time" ribbon button is clicked.
// You learn : Two different buttons can run two different scripts
// Level     : Beginner
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (run test pending)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Windows.Forms;
using Eplan.EplApi.Scripting;

public class RibbonTimeButton
{
    [Start]
    public void Run()
    {
        // DateTime.Now is the current date and time. "F" asks for a long, readable format.
        string now = DateTime.Now.ToString("F");
        MessageBox.Show("It is now:\r\n" + now, "Show time", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
