﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using System.IO;
using VisualLocalizer.Library.Gui;

namespace VisualLocalizer.Library.Components {

    /// <summary>
    /// Flags for the "open file" dialog
    /// </summary>
    public static class OPENFILENAME {
        /// <summary>
        /// Multiple files can be selected
        /// </summary>
        public static uint OFN_ALLOWMULTISELECT = 0x00000200;

        /// <summary>
        /// Prompt user to create non-existing file
        /// </summary>
        public static uint OFN_CREATEPROMPT = 0x00002000;

        /// <summary>
        /// Do not add selected files to the list of recently used
        /// </summary>
        public static uint OFN_DONTADDTORECENT = 0x02000000;

        /// <summary>
        /// Check existence of the files
        /// </summary>
        public static uint OFN_FILEMUSTEXIST = 0x00001000;

        /// <summary>
        /// Show hidden files
        /// </summary>
        public static uint OFN_FORCESHOWHIDDEN = 0x10000000;

        /// <summary>
        /// Display dialog when overwriting files
        /// </summary>
        public static uint OFN_OVERWRITEPROMPT = 0x00000002;

        /// <summary>
        /// Check existence of a directory
        /// </summary>
        public static uint OFN_PATHMUSTEXIST = 0x00000800;

        /// <summary>
        /// Readonly files should be included in the search
        /// </summary>
        public static uint OFN_READONLY=0x00000001;

        /// <summary>
        /// Shared files (network disks) should be included in the search
        /// </summary>
        public static uint OFN_SHAREAWARE = 0x00004000;

        /// <summary>
        /// Show help icon
        /// </summary>
        public static uint OFN_SHOWHELP = 0x00000010;

        /// <summary>
        /// True if resizing the dialog should be allowed
        /// </summary>
        public static uint OFN_ENABLESIZING = 0x00800000;

        /// <summary>
        /// Only current directory should be included in the search
        /// </summary>
        public static uint OFN_NOCHANGEDIR = 0x00000008;

        /// <summary>
        /// Files shared over network should not be included in the search
        /// </summary>
        public static uint OFN_NONETWORKBUTTON = 0x00020000;
    }

    /// <summary>
    /// Provides methods for displaying GUI dialogs in VS environment.
    /// </summary>
    public static class MessageBox {

        /// <summary>
        /// Initializes the services
        /// </summary>
        static MessageBox() {
            UIShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
        }

        /// <summary>
        /// Instance of the IVsUIShell service
        /// </summary>
        public static IVsUIShell UIShell {
            get;
            private set;
        }

        /// <summary>
        /// Displays info dialog with the OK button and specified message
        /// </summary>
        public static DialogResult Show(string message) {
            return Show(message, null, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO);
        }

        /// <summary>
        /// Displays info dialog with the OK button, specified message and title
        /// </summary>
        public static DialogResult Show(string message,string title) {
            return Show(message, title, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO);
        }

        /// <summary>
        /// Displays error dialog with the OK button and specified message
        /// </summary>
        public static DialogResult ShowError(string message) {            
            return Show(message, null, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_CRITICAL);
        }

        /// <summary>
        /// Displays error dialog with detailed information about the exception
        /// </summary>        
        public static DialogResult ShowException(Exception ex) {
            return ShowException(ex, null);
        }

        /// <summary>
        /// Displays error dialog with detailed information about the exception
        /// </summary>   
        public static DialogResult ShowException(Exception ex, Dictionary<string,string> specialInfo) {
            if (ex == null) throw new ArgumentNullException("ex");

            IntPtr hwnd;
            int hr = UIShell.GetDialogOwnerHwnd(out hwnd);
            Marshal.ThrowExceptionForHR(hr);
            
            ErrorDialog d = new ErrorDialog(ex, specialInfo);
            return d.ShowDialog(NativeWindow.FromHandle(hwnd));
        }

        /// <summary>
        /// Displays message box with provided values
        /// </summary>
        public static DialogResult Show(string message,string title,OLEMSGBUTTON buttons,OLEMSGDEFBUTTON defaultButton,OLEMSGICON icon) {
            if (UIShell == null) throw new InvalidOperationException("MessageBox is not sufficiently initialized.");

            int result;
            Guid g = Guid.Empty;
            int hr = UIShell.ShowMessageBox(0, ref g, title, message, null, 0, buttons, defaultButton, icon, 1, out result);
            Marshal.ThrowExceptionForHR(hr);

            return ConvertFromIntToDialogResult(result);
        }

        /// <summary>
        /// Converts Visual Studio dialog result to the Windows Forms dialog result
        /// </summary>
        private static DialogResult ConvertFromIntToDialogResult(int code) {
            switch (code) {
                case 1:
                    return DialogResult.OK;
                case 2:
                    return DialogResult.Cancel;
                case 3:
                    return DialogResult.Abort;
                case 4:
                    return DialogResult.Retry;
                case 5:
                    return DialogResult.Ignore;
                case 6:
                    return DialogResult.Yes;
                default:
                    return DialogResult.No;
            }
        }

        /// <summary>
        /// Displays Visul Studio dialog for selecting files
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="initialDirectory">Initial directory</param>
        /// <param name="filter">Filter - same as FileDialog filter, but separated with \0</param>
        /// <param name="filterIndex">Selected filter</param>
        /// <param name="flags">Mask of OFN flags</param>
        /// <returns>Selected file paths separated with \0</returns>    
        public static string[] SelectFilesViaDlg(string title,string initialDirectory, string filter,uint filterIndex,uint flags) {
            uint buffersize = 255;
            
            VSOPENFILENAMEW o = new VSOPENFILENAMEW();
            o.dwFlags = flags;
            o.pwzInitialDir = initialDirectory;
            o.pwzFilter = filter;
            o.pwzDlgTitle = title;
            o.nFilterIndex = filterIndex;
            o.nMaxFileName = buffersize;
            o.lStructSize = (uint)Marshal.SizeOf(typeof(VSOPENFILENAMEW));            
            o.pwzFileName = Marshal.StringToBSTR(new string('\0', (int)buffersize));
            
            IntPtr dialogOwner;
            int hr = UIShell.GetDialogOwnerHwnd(out dialogOwner);
            Marshal.ThrowExceptionForHR(hr);

            o.hwndOwner = dialogOwner;                        

            VSOPENFILENAMEW[] arr=new VSOPENFILENAMEW[1] {o};
            hr = UIShell.GetOpenFileNameViaDlg(arr);
            if (hr == VSConstants.OLE_E_PROMPTSAVECANCELLED) return null;
            Marshal.ThrowExceptionForHR(hr);

            string returnedData=Marshal.PtrToStringBSTR(arr[0].pwzFileName);
            string[] tokens = returnedData.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) throw new Exception("Unexpected OpenFileDialog result.");

            if (tokens.Length == 1) {
                return new string[] { tokens[0] };
            } else {
                string directory = tokens[0];
                string[] ret = new string[tokens.Length - 1];

                for (int i = 1; i < tokens.Length; i++) {
                    ret[i - 1] = Path.Combine(directory, tokens[i]);
                }

                return ret;
            }
        }
    }
}
