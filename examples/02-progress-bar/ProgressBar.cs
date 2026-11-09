// ============================================================
// Example   : 02 - Progress bar around a long action
// Purpose   : Show Eplan's own progress window while a script works through a list, and let the user cancel it.
// You learn : The Progress object, splitting a job into parts, steps, cancelling, try/catch/finally
// Level     : Beginner
// Tested in : Compile-checked against Eplan Electric P8 2026.0.3 (run in Eplan 2026.0.3: see README)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Threading;               // Thread.Sleep, used here to pretend that work takes time
using System.Windows.Forms;           // MessageBox
using Eplan.EplApi.Base;              // Progress: Eplan's own progress window
using Eplan.EplApi.Scripting;         // The [Start] attribute

public class ProgressBarDemo
{
    // Eplan runs this method when the script is started.
    [Start]
    public void Run()
    {
        // Create the progress window object. "SimpleProgress" is the basic style.
        // (There is also "EnhancedProgress".)
        Progress progress = new Progress("SimpleProgress");

        // These variables remember what happened, so we can tell the user AFTER the progress window has closed.
        bool completed = false;
        string errorText = null;

        // try / catch / finally:
        //   try     - the work we want to do
        //   catch   - what to do if something goes wrong (instead of crashing)
        //   finally - cleanup that must ALWAYS happen, even after an error or Cancel
        try
        {
            progress.SetTitle("Progress bar demo");
            progress.SetAllowCancel(true);       // show a Cancel button
            progress.ShowImmediately();          // show the window now, not after a short delay

            // The whole bar represents 100%. We split it into two "parts" that add up to 100%:
            //   Part 1 = 30% of the bar, 5 items
            //   Part 2 = 70% of the bar, 15 items
            // "&&" means "and": only start part 2 if part 1 finished without being cancelled.
            completed = RunPart(progress, 30.0, "Preparing", 5, "Preparing item")
                     && RunPart(progress, 70.0, "Processing", 15, "Processing item");
        }
        catch (Exception ex)
        {
            errorText = ex.Message;              // keep the message for later
        }
        finally
        {
            // Eplan's documentation is clear about this one: always end the progress, or its window can lock Eplan.
            // EndPart(true) means "end and close the window".
            progress.EndPart(true);
            progress.Dispose();
        }

        // Now the progress window is gone, so a message box will not hide behind it.
        if (errorText != null)
        {
            MessageBox.Show(errorText, "Progress bar demo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else if (completed)
        {
            MessageBox.Show("All items were processed.", "Progress bar demo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("Cancelled. Nothing more was processed.", "Progress bar demo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // A helper method that runs one "part" of the bar.
    //   percent   - how much of the whole bar this part uses (all parts together should be 100)
    //   partName  - the label shown for this part
    //   itemCount - how many items to work through
    //   text      - the message shown above the bar, followed by "3 of 15" and so on
    // It returns true if every item was done, or false if the user pressed Cancel.
    private bool RunPart(Progress progress, double percent, string partName, int itemCount, string text)
    {
        progress.BeginPart(percent, partName);

        // Tell Eplan how many steps are in this part. Call this AFTER BeginPart, not before.
        progress.SetNeededSteps(itemCount);

        bool finishedAll = true;

        // A "for" loop repeats a block of code. Here "i" counts from 1 up to itemCount.
        for (int i = 1; i <= itemCount; i++)
        {
            // Check for Cancel before each item, so the user does not wait long after clicking it.
            if (progress.Canceled())
            {
                finishedAll = false;
                break;                           // "break" leaves the loop
            }

            progress.SetActionText(text + " " + i + " of " + itemCount);

            // ----- Your real work goes here. -----
            // We just wait a moment so you can watch the bar move.
            Thread.Sleep(150);

            progress.Step(1);                    // one more step is done: the bar moves forward
        }

        progress.EndPart(false);                 // end this part but keep the window open for the next one
        return finishedAll;
    }
}
