// ============================================================
// Example   : 12 - "Please wait" window on its own thread
// Purpose   : Show a moving "please wait" window while Eplan is busy, so the screen never looks frozen.
// You learn : Threads, running a window on its own message loop, ManualResetEvent, closing safely from another thread, try/finally
// Level     : Advanced
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Drawing;                  // Size, Point
using System.Threading;                // Thread, ManualResetEvent
using System.Windows.Forms;            // Form, Label, ProgressBar, Application
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs.
// ---------------------------------------------------------------
public class PleaseWaitDemo
{
    [Start]
    public void Run()
    {
        // Show the window FIRST, then do the slow work.
        PleaseWaitWindow wait = PleaseWaitWindow.Open("Working on it...");
        try
        {
            // Stand-in for slow work that blocks Eplan's own screen (for example updating many objects).
            // The bar in the window keeps moving, because the window lives on a different thread.
            for (int i = 0; i < 8; i++)
            {
                Thread.Sleep(500);
            }
        }
        finally
        {
            // "finally" runs even if the work above fails, so the window can never get stuck on screen.
            wait.Close();
        }

        // Only show messages AFTER closing the window, or they could appear behind it.
        MessageBox.Show("Finished. The window closed by itself.", "Please wait demo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

// ---------------------------------------------------------------
// Part 2: the reusable window. Copy this class into your own scripts.
// ---------------------------------------------------------------
public class PleaseWaitWindow
{
    // Why a thread? Eplan's main thread is busy while your script works, so a normal window on that thread cannot
    // repaint and its bar would freeze. A window on its OWN thread has its own message loop and keeps moving.

    private Thread thread;
    private PleaseWaitForm form;
    private readonly ManualResetEvent shown = new ManualResetEvent(false);   // a flag one thread can wait on while another sets it

    // Starts the window and waits (up to 5 seconds) until it is really on screen.
    public static PleaseWaitWindow Open(string text)
    {
        PleaseWaitWindow window = new PleaseWaitWindow();
        window.thread = new Thread(delegate() { window.RunWindow(text); });
        window.thread.SetApartmentState(ApartmentState.STA);     // Windows Forms needs this
        window.thread.IsBackground = true;                       // never keeps Eplan alive on exit
        window.thread.Start();
        window.shown.WaitOne(5000);
        return window;
    }

    // This method runs on the new thread.
    private void RunWindow(string text)
    {
        try
        {
            form = new PleaseWaitForm(text);
            form.Shown += delegate { shown.Set(); };             // tell the waiting thread "I am visible"
            Application.Run(form);                               // this thread's own message loop; returns when the form closes
        }
        catch (Exception)
        {
        }
        finally
        {
            shown.Set();                                         // never leave the other thread waiting
        }
    }

    // Closes the window from any thread. Safe to call more than once.
    public void Close()
    {
        try
        {
            PleaseWaitForm f = form;
            if (f != null && f.IsHandleCreated)
            {
                // A window may only be changed by its own thread, so we ask it to close itself.
                f.BeginInvoke(new MethodInvoker(delegate { f.Close(); }));
            }
            if (thread != null)
            {
                thread.Join(3000);                               // wait up to 3 seconds for it to finish
            }
        }
        catch (Exception)
        {
        }
    }
}

public class PleaseWaitForm : Form
{
    public PleaseWaitForm(string text)
    {
        Text = "Please wait";
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        ControlBox = false;                                      // no close button: the script closes it
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        ClientSize = new Size(340, 90);

        Label label = new Label();
        label.Text = text + "\r\nEplan may not respond for a moment.";
        label.Location = new Point(14, 12);
        label.Size = new Size(312, 36);

        ProgressBar bar = new ProgressBar();
        bar.Style = ProgressBarStyle.Marquee;                    // a bar that keeps moving, with no known end
        bar.MarqueeAnimationSpeed = 30;
        bar.Location = new Point(14, 56);
        bar.Size = new Size(312, 18);

        Controls.Add(label);
        Controls.Add(bar);
    }
}
