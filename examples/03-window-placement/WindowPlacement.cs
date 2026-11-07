// ============================================================
// Example   : 03 - Place a window well
// Purpose   : Open your own dialog centered on the Eplan main window, owned by it, without an extra taskbar button, and always on a visible screen.
// You learn : Window owners, StartPosition, reading another window's position, multiple monitors, a first look at P/Invoke
// Level     : Intermediate
// Tested in : Compile-checked against Eplan Electric P8 2025.0.3 and 2026.0.3 (run test pending)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Diagnostics;              // Process: information about the running Eplan program
using System.Drawing;                  // Point, Size, Rectangle
using System.Runtime.InteropServices;  // DllImport: calling Windows functions directly
using System.Windows.Forms;            // Form, Label, Button, Screen
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs. It shows a small demo window.
// ---------------------------------------------------------------
public class WindowPlacementDemo
{
    [Start]
    public void Run()
    {
        // Build a small window. It has not been shown yet.
        using (Form form = new Form())
        using (Label label = new Label())
        using (Button closeButton = new Button())
        {
            form.Text = "Window placement demo";
            form.ClientSize = new Size(380, 150);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MinimizeBox = false;
            form.MaximizeBox = false;

            label.Location = new Point(12, 12);
            label.Size = new Size(356, 90);
            label.Text = EplanWindowHelper.Describe();       // tells us where Eplan is and which screen it is on

            closeButton.Text = "Close";
            closeButton.DialogResult = DialogResult.OK;
            closeButton.Location = new Point(293, 112);
            form.AcceptButton = closeButton;

            form.Controls.Add(label);
            form.Controls.Add(closeButton);

            // This one line does the work. It is explained in part 2 below.
            EplanWindowHelper.ShowDialogCentered(form);
        }
    }
}

// ---------------------------------------------------------------
// Part 2: a helper you can copy into your own scripts.
// "static" means you use it directly, without creating an object first.
// ---------------------------------------------------------------
public static class EplanWindowHelper
{
    // --- Windows functions we borrow -----------------------------------------------------
    // The .NET toolbox cannot tell us where ANOTHER program's window is, but Windows itself can.
    // [DllImport] lets our code call a function inside a Windows file (user32.dll).
    // This is called "P/Invoke". You only need to copy these lines, not understand them fully.
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);          // "Is this window minimized?"

    // Windows describes a rectangle with four numbers: left, top, right, bottom.
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    // --- Finding the Eplan window --------------------------------------------------------
    // The script runs INSIDE Eplan, so "the current process" is Eplan.
    // A "handle" is just a number Windows uses to identify a window. Zero means "none".
    public static IntPtr GetEplanHandle()
    {
        try
        {
            return Process.GetCurrentProcess().MainWindowHandle;
        }
        catch (Exception)
        {
            return IntPtr.Zero;                  // never let a layout detail stop the script
        }
    }

    // Tries to read the position and size of the Eplan window.
    // Returns false if it is not available, or minimized (then its position is not meaningful).
    public static bool TryGetEplanBounds(IntPtr handle, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        if (handle == IntPtr.Zero || IsIconic(handle))
        {
            return false;
        }

        RECT rect;
        if (!GetWindowRect(handle, out rect))
        {
            return false;
        }

        // Rectangle wants a left, top, width and height, so we convert.
        bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        return true;
    }

    // --- The placement maths ---------------------------------------------------------------
    // Works out where the top-left corner of a window of size "formSize" should go.
    public static Point GetCenteredLocation(Size formSize, IntPtr ownerHandle)
    {
        Rectangle owner;
        Rectangle workArea;

        if (TryGetEplanBounds(ownerHandle, out owner))
        {
            // The "working area" of a screen is the part not covered by the taskbar.
            // We use the screen that Eplan is on, so this also works with several monitors.
            workArea = Screen.FromHandle(ownerHandle).WorkingArea;
        }
        else
        {
            // Eplan's window is not available or is minimized: use the main screen instead.
            workArea = Screen.PrimaryScreen.WorkingArea;
            owner = workArea;
        }

        // Centre: the middle of Eplan, minus half the size of our window.
        int x = owner.Left + (owner.Width - formSize.Width) / 2;
        int y = owner.Top + (owner.Height - formSize.Height) / 2;

        // Keep the whole window on screen. If Eplan is half off a monitor, our window must not be.
        x = Math.Max(workArea.Left, Math.Min(x, workArea.Right - formSize.Width));
        y = Math.Max(workArea.Top, Math.Min(y, workArea.Bottom - formSize.Height));

        return new Point(x, y);
    }

    // --- Showing the window ----------------------------------------------------------------
    // Shows "form" as a dialog (the user must close it before returning to Eplan) and returns the result.
    public static DialogResult ShowDialogCentered(Form form)
    {
        IntPtr handle = GetEplanHandle();

        form.ShowInTaskbar = false;                        // no second button in the taskbar
        form.StartPosition = FormStartPosition.Manual;     // "we will set the position ourselves"

        // We set the position in the Load event, which happens right before the window appears.
        // At that moment the window has its final size (Windows may scale it for high-DPI screens),
        // so the centring is accurate. "delegate { ... }" is a small piece of code that runs on the event.
        form.Load += delegate
        {
            form.Location = GetCenteredLocation(form.Size, handle);
        };

        if (handle == IntPtr.Zero)
        {
            return form.ShowDialog();                      // no owner found: still show it
        }

        // Giving the window an OWNER keeps it in front of Eplan and makes it behave as part of Eplan.
        return form.ShowDialog(new WindowOwner(handle));
    }

    // Describes the Eplan window in plain words. Used by the demo window.
    public static string Describe()
    {
        IntPtr handle = GetEplanHandle();
        Rectangle bounds;
        if (!TryGetEplanBounds(handle, out bounds))
        {
            return "The Eplan window could not be measured (it may be minimized).\r\nThis window is centered on the main screen instead.";
        }

        Screen screen = Screen.FromHandle(handle);
        return string.Format(
            "Eplan window: {0} x {1} at ({2}, {3}).\r\nScreen: {4}, usable area {5} x {6}.\r\n\r\nThis window is centered on Eplan and stays on that screen. Try moving Eplan to another monitor and run the script again.",
            bounds.Width, bounds.Height, bounds.Left, bounds.Top,
            screen.DeviceName, screen.WorkingArea.Width, screen.WorkingArea.Height);
    }
}

// ShowDialog needs an "owner" in the form of IWin32Window. This tiny class wraps a handle so we can supply one.
public class WindowOwner : IWin32Window
{
    private readonly IntPtr handle;

    public WindowOwner(IntPtr handle)
    {
        this.handle = handle;
    }

    public IntPtr Handle
    {
        get { return handle; }
    }
}
