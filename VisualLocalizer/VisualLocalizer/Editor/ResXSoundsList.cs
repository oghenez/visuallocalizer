﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.IO;
using System.Windows.Forms;
using VisualLocalizer.Library;
using System.Media;
using VisualLocalizer.Library.Components;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Editor {

    /// <summary>
    /// Represents Sounds tab in ResX editor. Any MemoryStream contained in ResX node is considered to be a sound.
    /// </summary>
    internal sealed class ResXSoundsList : AbstractListView {
        public ResXSoundsList(ResXEditorControl editorControl)
            : base(editorControl) {
        }


        /// <summary>
        /// Returns true if given node's type matches the type of items this control holds
        /// </summary>
        public override bool CanContainItem(ResXDataNode node) {
            if (node == null) throw new ArgumentNullException("node");

            // memory streams are considered to be sounds
            return node.HasValue<MemoryStream>() && (node.FileRef == null || node.FileRef.FileName.ToLower().EndsWith(".wav"));
        }

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public override IKeyValueSource Add(string key, ResXDataNode value) { 
            ListViewKeyItem item = base.Add(key, value) as ListViewKeyItem;
            if (referenceExistingOnAdd) {
                item.FileRefOk = true;
            } else {
                ListViewItem.ListViewSubItem subSize = new ListViewItem.ListViewSubItem();
                subSize.Name = "Size";
                item.SubItems.Insert(2, subSize);

                ListViewItem.ListViewSubItem subLength = new ListViewItem.ListViewSubItem();
                subLength.Name = "Length";
                item.SubItems.Insert(3, subLength);
            }

            UpdateDataOf(item, false);
            
            return item;
        }

        /// <summary>
        /// Reloads displayed data from underlaying ResX node
        /// </summary>
        public override void UpdateDataOf(ListViewKeyItem item, bool reloadImages) {
            base.UpdateDataOf(item, reloadImages);
            
            LargeImageList.Images.Add(item.ImageKey, Editor.play);
            SmallImageList.Images.Add(item.ImageKey, Editor.play);            

            FileInfo info = null;
            if (item.DataNode.FileRef != null && File.Exists(item.DataNode.FileRef.FileName)) {
                info = new FileInfo(item.DataNode.FileRef.FileName);
            }

            if (info != null) {
                item.SubItems["Size"].Text = GetFileSize(info.Length);
                item.SubItems["Length"].Text = GetSoundDigits(SoundInfo.GetSoundLength(info.FullName));
                item.FileRefOk = true;
            } else {
                var stream = item.DataNode.GetValue<MemoryStream>();
                if (stream != null) {
                    item.SubItems["Size"].Text = GetFileSize(stream.Length);
                    item.SubItems["Length"].Text = null;
                    item.FileRefOk = true;
                } else {
                    item.FileRefOk = false;
                }
            }

            item.UpdateErrorSetDisplay();            

            Validate(item);
            NotifyItemsStateChanged();

            if (item.ErrorMessages.Count > 0) {
                item.Status = KEY_STATUS.ERROR;
            } else {
                item.Status = KEY_STATUS.OK;
                item.LastValidKey = item.Key;
            }

            string p = item.ImageKey;
            item.ImageKey = null;
            item.ImageKey = p;
        }

        /// <summary>
        /// Converts given milisecond time to human-readable format of time span
        /// </summary>        
        private string GetSoundDigits(int milis) {
            if (milis < 1000) {
                return string.Format("{0} ms", milis);
            } else {
                int secs = milis / 1000;
                int realSecs = secs % 60;
                int minutes = secs / 60;
                int realMinutes = minutes % 60;
                int realHours = realMinutes / 60;

                return string.Format("{0}:{1}:{2}", realHours < 10 ? "0" + realHours : realHours.ToString(),
                    realMinutes < 10 ? "0" + realMinutes : realMinutes.ToString(), realSecs < 10 ? "0" + realSecs : realSecs.ToString());
            }
        }

        /// <summary>
        /// Create the GUI
        /// </summary>
        protected override void InitializeColumns() {
            base.InitializeColumns();

            ColumnHeader sizeHeader = new ColumnHeader();
            sizeHeader.Text = "File Size";
            sizeHeader.Width = 80;
            sizeHeader.Name = "Size";
            this.Columns.Insert(2, sizeHeader);

            ColumnHeader lengthHeader = new ColumnHeader();
            lengthHeader.Text = "Length";
            lengthHeader.Width = 80;
            lengthHeader.Name = "Length";
            this.Columns.Insert(3, lengthHeader);            
        }

        /// <summary>
        /// Saves given node's content into random file in specified directory and returns the file path
        /// </summary>
        protected override string SaveIntoTmpFile(ResXDataNode node, string name, string directory) {
            MemoryStream ms = node.GetValue<MemoryStream>();
            string filename = name + ".wav";
            string path = Path.Combine(directory, filename);
            
            FileStream fs = null;
            try {
                fs = new FileStream(path, FileMode.Create);

                byte[] buffer = new byte[8192];
                int read=0;
                while ((read = ms.Read(buffer, 0, buffer.Length)) > 0) {
                    fs.Write(buffer, 0, read);
                }

            } finally {
                if (fs != null) fs.Close();
                ms.Close();
            }

            return path;
        }
    }
}
