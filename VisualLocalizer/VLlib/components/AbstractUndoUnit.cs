﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.OLE.Interop;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Library {
    public abstract class AbstractUndoUnit : IOleParentUndoUnit {

        protected bool isUndo;
        protected static int globalid = 1;
        protected int id;
        
        public AbstractUndoUnit() {
            id = globalid;
            globalid++;
            isUndo = true;
            PrependUnits = new List<IOleUndoUnit>();
            AppendUnits = new List<IOleUndoUnit>();
        }

        public abstract void Undo();
        public abstract void Redo();
        public abstract string GetUndoDescription();
        public abstract string GetRedoDescription();

        public void Do(IOleUndoManager pUndoManager) {
            List<IOleUndoUnit> prepunits = handleUnits(PrependUnits, pUndoManager);
            
            if (isUndo) {
                Undo();
            } else {
                Redo();
            }

            List<IOleUndoUnit> appunits = handleUnits(AppendUnits, pUndoManager);

            this.AppendUnits.Clear();
            this.PrependUnits.Clear();

            this.AppendUnits.AddRange(appunits);
            this.PrependUnits.AddRange(prepunits);

            pUndoManager.Add(this);

            isUndo = !isUndo;
        }

        private List<IOleUndoUnit> handleUnits(List<IOleUndoUnit> units, IOleUndoManager pUndoManager) {
            foreach (IOleUndoUnit unit in units)
                unit.Do(pUndoManager);
            
            if (isUndo) {
                return pUndoManager.RemoveTopFromRedoStack(units.Count);
            } else {
                return pUndoManager.RemoveTopFromUndoStack(units.Count);
            }
        }

        public void GetDescription(out string pBstr) {
            if (isUndo)
                pBstr = GetUndoDescription();
            else
                pBstr = GetRedoDescription();
        }

        public void GetUnitType(out Guid pClsid, out int plID) {
            pClsid = GetType().GUID;
            plID = id;
        }

        public void OnNextAdd() {            
        }

        public List<IOleUndoUnit> PrependUnits {
            get;
            private set;
        }

        public List<IOleUndoUnit> AppendUnits {
            get;
            private set;
        }


        private IOleParentUndoUnit openUnit;
        private bool closed;

        public void Add(IOleUndoUnit pUU) {
            if (openUnit == null)
                AppendUnits.Add(pUU);
            else
                openUnit.Add(pUU);
        }

        public int Close(IOleParentUndoUnit pPUU, int fCommit) {
            if (fCommit == 1) {
                openUnit = null;
                Add(pPUU);
                closed = true;
            }

            return VSConstants.S_OK;
        }

        public int FindUnit(IOleUndoUnit pUU) {
            bool found = false;
            foreach (IOleUndoUnit unit in AppendUnits)
                if (unit == pUU) found = true;

            return found ? VSConstants.S_OK : VSConstants.S_FALSE;
        }

        public void GetParentState(out uint pdwState) {
            if (closed)
                pdwState = (uint)UASFLAGS.UAS_NOPARENTENABLE;
            else 
                pdwState = (uint)(UASFLAGS.UAS_MASK);
        }

        public void Open(IOleParentUndoUnit pPUU) {
            openUnit = pPUU;
        }
    }
}