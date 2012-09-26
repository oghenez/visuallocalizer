﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;

namespace VisualLocalizer.Components {
    internal abstract class AbstractCodeLookuper {
        
        public ProjectItem SourceItem { get; set; }
        protected int CurrentLine { get; set; }
        protected int CurrentIndex { get; set; }
        protected int CurrentAbsoluteOffset { get; set; }
        protected int StringStartLine { get; set; }
        protected int StringStartIndex { get; set; }
        protected int StringStartAbsoluteOffset { get; set; }

        protected string text;
        protected char currentChar, previousChar, previousPreviousChar, stringStartChar;

        protected void Move() {
            CurrentIndex++;
            CurrentAbsoluteOffset++;
            if ((currentChar == '\n')) {
                CurrentIndex = 0;
                CurrentLine++;
            }
        }

        protected void PreProcessChar(ref bool insideComment, ref bool insideString, ref bool isVerbatimString, out bool skipLine) {
            skipLine = false;

            if (currentChar == '/' && !insideString) {
                if (previousChar == '/' && !insideComment) {
                    skipLine = true;
                }
                if (previousChar == '*' && previousPreviousChar != '/') {
                    insideComment = false;
                }
            } else if (currentChar == '*' && !insideString) {
                if (previousChar == '/' && !insideComment) {
                    insideComment = true;
                }
            } else if ((currentChar == '\"' || currentChar == '\'') && !insideComment) {
                if (insideString) {
                    if (stringStartChar == currentChar && !isVerbatimString) {
                        if (previousChar != '\\' || (previousChar == '\\' && previousPreviousChar == '\\'))
                            insideString = false;
                    }
                } else {
                    insideString = true;
                    stringStartChar = currentChar;
                    StringStartIndex = CurrentIndex;
                    StringStartLine = CurrentLine;
                    StringStartAbsoluteOffset = CurrentAbsoluteOffset;
                    if (previousChar == '@') {
                        isVerbatimString = true;
                        StringStartIndex--;
                    }
                }
            } else if (!insideComment && isVerbatimString && insideString && previousChar == stringStartChar
                && previousPreviousChar != stringStartChar && previousPreviousChar != '@') {
                insideString = false;
            }
        }
    }
}