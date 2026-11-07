// ============================================================
// Example   : 16 - Modern folder picker
// Purpose   : Show the Explorer-style "choose a folder" window (address bar, Quick access, search) instead of the old tree-style one.
// You learn : COM interop, interfaces, HRESULT return values, Cancel detection, a reusable helper
// Level     : Advanced
// Tested in : Eplan Electric P8 2026.0.3 (Run in Eplan 2026)
// Author    : Justin Walz
// License   : MIT (see LICENSE in the repo root)
// ============================================================

using System;
using System.Runtime.InteropServices;  // COM interop: ComImport, Guid, DllImport, Marshal
using System.Windows.Forms;            // MessageBox
using Eplan.EplApi.Scripting;          // The [Start] attribute

// ---------------------------------------------------------------
// Part 1: the script Eplan runs.
// ---------------------------------------------------------------
public class ModernFolderPickerDemo
{
    [Start]
    public void Run()
    {
        // One line asks the user for a folder. It returns the path, or null if they pressed Cancel.
        string folder = ModernFolderPicker.Pick("Choose a folder for the demo", null);

        if (folder == null)
        {
            MessageBox.Show("No folder was chosen.", "Modern folder picker", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        MessageBox.Show("You chose:\r\n" + folder, "Modern folder picker", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

// ---------------------------------------------------------------
// Part 2: the reusable helper.
// .NET Framework's own FolderBrowserDialog is the old tree-style window. Windows also has the modern one built in
// (the same window Explorer uses), but it is only reachable through COM, a Windows technology for calling
// components. The declarations below describe that component to C#. You only need to copy them, not understand
// every line. The ORDER of the methods inside each interface matters: it must match Windows exactly.
// ---------------------------------------------------------------
public static class ModernFolderPicker
{
    // Asks the user for a folder. title = text at the top (optional). startFolder = where to begin (optional).
    // Returns the chosen folder, or null if the user cancelled or something went wrong.
    public static string Pick(string title, string startFolder)
    {
        try
        {
            IFileOpenDialogFolders dialog = (IFileOpenDialogFolders)new FileOpenDialogComObject();

            // Options: "pick folders" and "only real folders on disk" (not things like Control Panel).
            uint options;
            dialog.GetOptions(out options);
            dialog.SetOptions(options | FosPickFolders | FosForceFileSystem);

            if (!string.IsNullOrEmpty(title))
            {
                dialog.SetTitle(title);
            }

            if (!string.IsNullOrEmpty(startFolder) && System.IO.Directory.Exists(startFolder))
            {
                IShellItemInfo startItem;
                Guid shellItemGuid = typeof(IShellItemInfo).GUID;
                if (SHCreateItemFromParsingName(startFolder, IntPtr.Zero, ref shellItemGuid, out startItem) == 0)
                {
                    dialog.SetFolder(startItem);
                }
            }

            // Show returns a number called an HRESULT. 0 means OK. A special value means the user cancelled.
            int result = dialog.Show(IntPtr.Zero);
            if (result == ErrorCancelledHResult || result != 0)
            {
                return null;
            }

            IShellItemInfo chosen;
            dialog.GetResult(out chosen);
            string path;
            chosen.GetDisplayName(SigdnFileSysPath, out path);
            return path;
        }
        catch (Exception)
        {
            return null;                       // a picker is a convenience: fail quietly
        }
    }

    // --- Constants from the Windows documentation --------------------------------------------
    private const uint FosPickFolders = 0x00000020;
    private const uint FosForceFileSystem = 0x00000040;
    private const uint SigdnFileSysPath = 0x80058000;
    private const int ErrorCancelledHResult = unchecked((int)0x800704C7);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string path, IntPtr bindContext, ref Guid riid, out IShellItemInfo item);

    // The dialog object itself.
    [ComImport, Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
    private class FileOpenDialogComObject
    {
    }

    // A file or folder as Windows sees it. We only use GetDisplayName.
    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemInfo
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItemInfo ppsi);
        void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItemInfo psi, uint hint, out int piOrder);
    }

    // The dialog's methods, in the exact order Windows defines them.
    [ComImport, Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialogFolders
    {
        [PreserveSig] int Show(IntPtr parent);
        void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(uint fos);
        void GetOptions(out uint pfos);
        void SetDefaultFolder(IShellItemInfo psi);
        void SetFolder(IShellItemInfo psi);
        void GetFolder(out IShellItemInfo ppsi);
        void GetCurrentSelection(out IShellItemInfo ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItemInfo ppsi);
        void AddPlace(IShellItemInfo psi, int alignment);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);
    }
}
