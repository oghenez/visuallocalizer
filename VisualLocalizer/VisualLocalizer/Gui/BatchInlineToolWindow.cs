﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.Shell.Interop;
using VisualLocalizer.Commands;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.OLE.Interop;
using System.IO;

namespace VisualLocalizer.Gui {

    /// <summary>
    /// Represents "Batch inline" toolwindow
    /// </summary>
    [Guid("E7755751-5A96-451b-9DFF-DCA1422CCA0A")]
    internal sealed class BatchInlineToolWindow : AbstractCodeToolWindow<BatchInlineToolPanel> {

        /// <summary>
        /// Creates new instance
        /// </summary>
        public BatchInlineToolWindow() {
            this.Caption = "Batch Inline - Visual Localizer"; // window title

            // set the toolbar
            this.ToolBar = new CommandID(typeof(VisualLocalizer.Guids.VLBatchInlineToolbarCommandSet).GUID, PackageCommandIDs.BatchInlineToolbarID);
            this.ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            OleMenuCommandService menuService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            if (menuService == null) throw new InvalidOperationException("Cannot obtain OleMenuCommandService.");

            // add "Run" button
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchInlineToolbarCommandSet).GUID, PackageCommandIDs.BatchInlineToolbarRunID,
                new EventHandler(RunClick), null, menuService);

            // add "Remove unchecked" button
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchInlineToolbarCommandSet).GUID, PackageCommandIDs.BatchInlineToolbarRemoveUncheckedID,
                new EventHandler(RemoveUnchecked), null, menuService);

            // add "Restore unchecked" button
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchInlineToolbarCommandSet).GUID, PackageCommandIDs.BatchInlineToolbarPutBackUncheckedID,
                new EventHandler(RestoreUnchecked), null, menuService);
        }

        /// <summary>
        /// When window is closed
        /// </summary>        
        protected override void OnWindowHidden(object sender, EventArgs e) {
            VLDocumentViewsManager.ReleaseLocks(); // unlock all previously locked documents
            MenuManager.OperationInProgress = false; // enable other operations to run
        }

        /// <summary>
        /// "Remove unchecked" button clicked
        /// </summary>        
        private void RemoveUnchecked(object sender, EventArgs e) {
            try {
                panel.RemoveUncheckedRows(true);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// "Restore unchecked" button clicked
        /// </summary>        
        private void RestoreUnchecked(object sender, EventArgs e) {
            try {
                panel.RestoreRemovedRows();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// "Run" button clicked
        /// </summary>        
        private void RunClick(object sender, EventArgs e) {
            int checkedRows = panel.CheckedRowsCount;
            int rowCount = panel.Rows.Count;
            int rowErrors = 0;

            try {
                VLDocumentViewsManager.ReleaseLocks(); // unlock locked documents
                MenuManager.OperationInProgress = false; // permit other operations
                BatchInliner inliner = new BatchInliner(); 

                inliner.Inline(panel.GetData(), false, ref rowErrors); // run inliner
               
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
            } finally {
                if (this.Frame != null)
                    ((IVsWindowFrame)this.Frame).CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave); // close the toolwindow

                VLOutputWindow.VisualLocalizerPane.Activate();
                VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command completed - selected {0} rows of {1}, {2} rows processed successfully", checkedRows, rowCount, checkedRows - rowErrors);
            }
        }

        /// <summary>
        /// Set content of toolwindow
        /// </summary>        
        public void SetData(List<CodeReferenceResultItem> list) {
            if (list == null) throw new ArgumentNullException("list");
            panel.SetData(list);
        }
    }
}
