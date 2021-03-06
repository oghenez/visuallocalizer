﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using System.Collections;
using VisualLocalizer.Library;
using VisualLocalizer.Editor.UndoUnits;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Commands.Inline {

    /// <summary>
    /// Used to perform rename operation with a resource reference.
    /// </summary>
    internal sealed class BatchReferenceReplacer : AbstractBatchReferenceProcessor {

        public BatchReferenceReplacer() { }

        /// <summary>
        /// Returns text that replaces current reference
        /// </summary> 
        public override string GetReplaceString(CodeReferenceResultItem item) {
            return item.GetReferenceAfterRename(item.KeyAfterRename);
        }

        /// <summary>
        /// Returns replace span of the reference (what should be replaced) and also updates it to fit the new result item
        /// </summary>
        public override TextSpan GetInlineReplaceSpan(CodeReferenceResultItem item, out int absoluteStartIndex, out int absoluteLength) {
            TextSpan current = item.GetInlineReplaceSpan(true, out absoluteStartIndex, out absoluteLength);

            item.UpdateReplaceSpan();

            return current;
        }

        /// <summary>
        /// Returns new undo unit for the item
        /// </summary>  
        public override AbstractUndoUnit GetUndoUnit(CodeReferenceResultItem item, bool externalChange) {
            return new GridRenameKeyInCodeUndoUnit(item.Key, item.KeyAfterRename);
        }
    }
}
