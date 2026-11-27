// ============================================================
// Example   : 29 - Showcase ribbon (remove script)
// Purpose   : Remove the "Script Examples" tab that ShowcaseRibbonSetup.cs created.
// You learn : Finding a ribbon tab by name and removing it
// Level     : Beginner
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 and 2027.0.3 (run in Eplan 2027: see README)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Windows.Forms;
using Eplan.EplApi.Gui;                            // RibbonBar, RibbonTab
using Eplan.EplApi.Scripting;

public class ShowcaseRibbonRemove
{
    // Must match the TabName value in ShowcaseRibbonSetup.cs.
    private const string TabName = "Script Examples";

    [Start]
    public void Run()
    {
        try
        {
            RibbonBar bar = new RibbonBar(true);

            RibbonTab tab = null;
            try
            {
                tab = bar.GetTab(TabName);
            }
            catch (Exception)
            {
                tab = null;
            }

            if (tab == null)
            {
                MessageBox.Show("The tab \"" + TabName + "\" was not found. Nothing to remove.",
                    "Remove showcase ribbon", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            tab.Remove();
            MessageBox.Show("The tab \"" + TabName + "\" was removed.\r\n\r\nIf it is still visible, restart Eplan.",
                "Remove showcase ribbon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Remove showcase ribbon", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
