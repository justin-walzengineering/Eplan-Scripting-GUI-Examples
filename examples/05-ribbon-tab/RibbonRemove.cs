// ============================================================
// Example   : 05 - Ribbon tab (remove script)
// Purpose   : Remove the "Demo Tools" tab that RibbonSetup.cs created, so you can undo the example cleanly.
// You learn : Finding a ribbon tab by name and removing it
// Level     : Beginner
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (run in Eplan 2026.0.3: see README)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Windows.Forms;
using Eplan.EplApi.Gui;                            // RibbonBar, RibbonTab
using Eplan.EplApi.Scripting;

public class RibbonRemoveDemo
{
    // Must match the TabName value in RibbonSetup.cs.
    private const string TabName = "Demo Tools";

    [Start]
    public void Run()
    {
        try
        {
            RibbonBar bar = new RibbonBar(true);

            RibbonTab tab = null;
            try
            {
                tab = bar.GetTab(TabName);            // may throw, or return nothing, if the tab does not exist
            }
            catch (Exception)
            {
                tab = null;
            }

            if (tab == null)
            {
                MessageBox.Show("The tab \"" + TabName + "\" was not found. Nothing to remove.",
                    "Remove ribbon tab", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            tab.Remove();                              // removes the tab with its groups and buttons
            MessageBox.Show("The tab \"" + TabName + "\" was removed.\r\n\r\nIf it is still visible, restart Eplan.",
                "Remove ribbon tab", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Remove ribbon tab", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
