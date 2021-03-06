﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using System.Resources;
using VisualLocalizer.Components;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Represents undo unit for changing comment of a string resource
    /// </summary>
    [Guid("E00A8D51-409A-453b-83AB-B63B5B79AC76")]
    internal sealed class GridChangeCommentUndoUnit : AbstractUndoUnit {

        public string Key { get; private set; }
        public string OldComment { get; private set; }
        public string NewComment { get; private set; }        
        public ResXStringGridRow SourceRow { get; private set; }
        public AbstractResXEditorGrid Grid { get; private set; }

        public GridChangeCommentUndoUnit(ResXStringGridRow sourceRow, AbstractResXEditorGrid grid, string key, string oldComment, string newComment) {
            if (sourceRow == null) throw new ArgumentNullException("sourceRow");
            if (grid == null) throw new ArgumentNullException("grid");
            
            this.SourceRow = sourceRow;
            this.Grid = grid;
            this.Key = key;
            this.OldComment = oldComment;
            this.NewComment = newComment;
        }

        public override void Undo() {
            ChangeComment(NewComment, OldComment);   
        }

        public override void Redo() {
            ChangeComment(OldComment, NewComment);
        }

        private void ChangeComment(string from, string to) {
            if (Grid.EditorControl.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");

            try {
                SourceRow.DataSourceItem.Comment = to;
                SourceRow.Cells[Grid.CommentColumnName].Tag = from;
                SourceRow.Cells[Grid.CommentColumnName].Value = to;
                Grid.ValidateRow(SourceRow);
                Grid.NotifyDataChanged();
                Grid.SetContainingTabPageSelected();

                VLOutputWindow.VisualLocalizerPane.WriteLine("Edited comment of \"{0}\"", Key);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        public override string GetUndoDescription() {
            return string.Format("Comment of \"{0}\" changed", Key);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
