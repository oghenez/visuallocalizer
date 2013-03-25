﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Represents undo for "Merge" operation - basically just many Add operations, added in the AppendUnits
    /// </summary>
    [Guid("446BCC4F-A768-4c84-AA53-0FDBED51BD5E")]
    internal sealed class MergeUndoUnit : AbstractUndoUnit {

        private string SourceFile { get; set; }

        public MergeUndoUnit(string file, Stack<IOleUndoUnit> partialUnits) {
            if (file == null) throw new ArgumentNullException("file");
            if (partialUnits == null) throw new ArgumentNullException("partialUnits");

            this.SourceFile = file;
            this.AppendUnits.AddRange(partialUnits);
        }

        public override void Undo() {            
        }

        public override void Redo() {            
        }

        public override string GetUndoDescription() {
            return string.Format("Merge with \"{0}\" ({1} items)", SourceFile, AppendUnits.Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
