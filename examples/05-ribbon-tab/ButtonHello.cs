// ============================================================
// Example   : 05 - Ribbon tab (button script: Say hello)
// Purpose   : The script that runs when the "Say hello" ribbon button is clicked.
// You learn : A ribbon button is just a way to run a script
// Level     : Beginner
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (run in Eplan 2026.0.3: see README)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Windows.Forms;
using Eplan.EplApi.Scripting;

public class RibbonHelloButton
{
    [Start]
    public void Run()
    {
        // Any script can sit behind a ribbon button. Replace this with your own work.
        MessageBox.Show("Hello from a ribbon button!", "Say hello", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
