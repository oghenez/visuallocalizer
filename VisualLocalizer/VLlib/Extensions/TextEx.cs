﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using System.Globalization;
using EnvDTE;
using EnvDTE80;
using System.Web;

namespace VisualLocalizer.Library.Extensions {

    /// <summary>
    /// Available programming languages
    /// </summary>
    public enum LANGUAGE { 
        /// <summary>
        /// C#
        /// </summary>
        CSHARP,
 
        /// <summary>
        /// Visual Basic .NET
        /// </summary>
        VB 
    }

    /// <summary>
    /// Container for extension methods working with text objects. 
    /// </summary>
    public static class TextEx {        

        private static CodeDomProvider csharp = Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#");
        private static CodeDomProvider vb = Microsoft.VisualBasic.VBCodeProvider.CreateProvider("VisualBasic");

        /// <summary>
        /// Radix of an escape sequence
        /// </summary>
        private enum ESCAPE_SEQUENCE_RADIX { HEX, OCT };

        /// <summary>
        /// List of character categories which can appear in an identifier
        /// </summary>
        private static UnicodeCategory[] validIdentifierCategories = {
                                                     UnicodeCategory.TitlecaseLetter,
                                                     UnicodeCategory.UppercaseLetter,
                                                     UnicodeCategory.LowercaseLetter,
                                                     UnicodeCategory.ModifierLetter,
                                                     UnicodeCategory.OtherLetter,
                                                     UnicodeCategory.LetterNumber,
                                                     UnicodeCategory.NonSpacingMark,
                                                     UnicodeCategory.SpacingCombiningMark,
                                                     UnicodeCategory.DecimalDigitNumber,
                                                     UnicodeCategory.ConnectorPunctuation,
                                                     UnicodeCategory.Format
                                                    };      

        /// <summary>
        /// Returns true if given text is valid identifier in specified language.
        /// </summary>        
        public static bool IsValidIdentifier(this string text, LANGUAGE lang) {
            if (string.IsNullOrEmpty(text)) return false;

            bool ok = true; 
            if (lang == LANGUAGE.CSHARP) {
                ok = csharp.IsValidIdentifier(text);
            }
            if (lang == LANGUAGE.VB) {
                ok = vb.IsValidIdentifier(text);
            }
            return ok;
        }

        /// <summary>
        /// Removes all whitespace characters from given text and returns result.
        /// </summary>        
        public static string RemoveWhitespace(this string text) {
            if (text == null) return null;

            StringBuilder b = new StringBuilder();
            foreach (char c in text)
                if (!char.IsWhiteSpace(c)) b.Append(c);

            return b.ToString();
        }

        /// <summary>
        /// Returns true if given character can be part of identifier (that is, belongs to valid unicode category)
        /// </summary>        
        public static bool CanBePartOfIdentifier(this char p) {
            UnicodeCategory charCat = char.GetUnicodeCategory(p);
            foreach (UnicodeCategory c in validIdentifierCategories)
                if (c == charCat) return true;
            return false;
        }

        /// <summary>
        /// Returns true if given char can be part of string literal (unescaped)
        /// </summary>        
        public static bool IsPrintable(this char c) {
            return !char.IsControl(c) && c != '\\' && c != '\"';
        }

        /// <summary>
        /// Returns text modified that way, so it can be displayed as atrribute's value in ASP .NET element
        /// </summary>        
        public static string ConvertAspNetUnescapeSequences(this string text) {
            if (text == null) throw new ArgumentNullException("text");

            return HttpUtility.HtmlEncode(text);
        }

        /// <summary>
        /// Removes escape sequences from atrribute's value in ASP .NET element
        /// </summary>        
        public static string ConvertAspNetEscapeSequences(this string text) {
            if (text == null) throw new ArgumentNullException("text");
            return HttpUtility.HtmlDecode(text);
        }

        /// <summary>
        /// Returns text modified that way, so it can be displayed as string literal in C# code
        /// </summary>        
        public static string ConvertCSharpUnescapeSequences(this string text) {
            if (text == null) throw new ArgumentNullException("text");

            StringBuilder b = new StringBuilder();

            foreach (char c in text) {
                if (c.IsPrintable()) {
                    b.Append(c);
                } else {
                    // unescape well-known characters
                    switch (c) {
                        case '\0': b.Append("\\0"); break;
                        case '\a': b.Append("\\a"); break;
                        case '\b': b.Append("\\b"); break;
                        case '\f': b.Append("\\f"); break;
                        case '\n': b.Append("\\n"); break;
                        case '\r': b.Append("\\r"); break;
                        case '\t': b.Append("\\t"); break;
                        case '\'': b.Append("'"); break;
                        case '\"': b.Append("\\\""); break;
                        case '\\': b.Append("\\\\"); break;
                        default:
                            b.Append(c.Escape()); // hexadecimal unescape
                            break;
                    }
                }
            }

            return b.ToString();
        }

        /// <summary>
        /// Removes escape sequences from C# string literal
        /// </summary>  
        public static string ConvertCSharpEscapeSequences(this string text,bool isVerbatim) {
            if (text == null) throw new ArgumentNullException("text");

            string resultText;
            if (isVerbatim) {
                resultText = text.Replace("\"\"", "\"");                
            } else {
                StringBuilder result = new StringBuilder();

                for (int i=0;i<text.Length;i++) {
                    char c = text[i];
                    
                    if (c == '\\') { // escape sequence start 
                        i++;
                        char next = text[i];
                    
                        switch (next) {
                            case '0': result.Append('\0'); break;
                            case '"': result.Append('"'); break;
                            case '\\': result.Append('\\'); break;
                            case 'r': result.Append('\r'); break;
                            case 'f': result.Append('\f'); break; 
                            case 't': result.Append('\t'); break;
                            case 'b': result.Append('\b'); break;
                            case 'n': result.Append('\n'); break;
                            case 'a': result.Append('\a'); break;
                            case 'x': result.Append(ReadEscapeSeq(text, i + 1, 4, ESCAPE_SEQUENCE_RADIX.HEX)); i += 4; break;
                            default:
                                if (next >= '0' && next <= '8') {
                                    result.Append(ReadEscapeSeq(text, i + 1, 3, ESCAPE_SEQUENCE_RADIX.OCT));
                                    i += 3;
                                } else {
                                    result.Append(next); 
                                }
                                break;
                        }
                    } else {
                        result.Append(c);  
                    }                                                  
                }
                
                resultText = result.ToString();
            }
            return resultText;
        }

        /// <summary>
        /// Reads the hexadecimal escape sequence in given text, starting at given index
        /// </summary>        
        private static char ReadEscapeSeq(string text, int startIndex, int charCount, ESCAPE_SEQUENCE_RADIX radix) {
            int end = startIndex + charCount;
            if (end > text.Length) throw new Exception("Invalid string escape sequence.");

            int sum = 0;
            bool initialized = false;
            for (int i = startIndex; i < end; i++) {
                if ((radix == ESCAPE_SEQUENCE_RADIX.HEX && text[i].IsHexDec()) || (radix == ESCAPE_SEQUENCE_RADIX.OCT && text[i].IsOct())) {
                    int r = radix == ESCAPE_SEQUENCE_RADIX.HEX ? 16 : 8;
                    sum = sum * r + ToDecimal(text[i]);
                    initialized = true;
                } else {
                    break;
                }
            }
            if (!initialized) throw new Exception("Invalid string escape sequence.");

            return (char)sum;
        }

        /// <summary>
        /// Returns text modified that way, so it can be displayed as string literal in VB code
        /// </summary>  
        public static string ConvertVBUnescapeSequences(this string text) {
            if (text == null) return null;
            return text.Replace("\"", "\"\""); 
        }

        /// <summary>
        /// Removes escape sequences from VB string literal
        /// </summary>  
        public static string ConvertVBEscapeSequences(this string text) {
            if (text == null) return null;
            return text.Replace("\"\"", "\"");
        }

        /// <summary>
        /// Returns numeric value for hexadecimal character (1 for '1', 11 for 'b' ... )
        /// </summary>        
        private static int ToDecimal(this char hexDec) {
            int x = hexDec - '0';
            if (x < 10) {
                return x;
            } else {
                if (char.IsLower(hexDec)) {
                    return (hexDec - 'a') + 10;
                } else if (char.IsUpper(hexDec)) {
                    return (hexDec - 'A') + 10;
                } else throw new ArgumentException("Invalid hexdec character " + hexDec);
            }
        }

        /// <summary>
        /// Returns true if given character is a hexadecimal character
        /// </summary>
        private static bool IsHexDec(this char c) {
            if (c >= '0' && c <= '9') {
                return true;
            } else {
                if (char.IsLower(c)) {
                    return c >= 'a' && c <= 'f';
                } else if (char.IsUpper(c)) {
                    return c >= 'A' && c <= 'F';
                } else return false;
            }
        }

        /// <summary>
        /// Returns true if given character can be used as octal number
        /// </summary>        
        private static bool IsOct(this char c) {
            return (c - '0') >= 0 && (c - '0') < 8;
        }

        /// <summary>
        /// Returns character in escaped hexadecimal format: \x1234
        /// </summary>        
        private static string Escape(this char c) {
            return string.Format("\\x{0:x4}", (int)c);
        }

        /// <summary>
        /// Replaces all invalid characters (those which cannot be part of identifiers) with underscores and returns result.
        /// </summary>        
        public static string CreateIdentifier(this string original, LANGUAGE lang) {
            if (original == null) return string.Empty;
            
            StringBuilder b = new StringBuilder();

            foreach (char c in original) {
                if (c.CanBePartOfIdentifier()) {
                    b.Append(c);
                } else {
                    b.Append('_');
                }
            }

            string ident = b.ToString();
            int counter = 0;
            while (!ident.IsValidIdentifier(lang)) {
                ident = "_" + ident;
                counter++;
                if (counter == 5) break;
            }

            return ident;
        }

        /// <summary>
        /// Returns true if given text ends with any of specified endings.
        /// </summary>        
        public static bool EndsWithAny(this string text, string[] extensions) {
            if (extensions == null) throw new ArgumentNullException("extensions");
            if (text == null) throw new ArgumentNullException("text");

            foreach (string ext in extensions)
                if (text.EndsWith(ext)) return true;
            return false;
        }

        /// <summary>
        /// Parses given text separated by newlines and tabs into lines and columns. Respects quoted content.
        /// </summary>        
        public static List<List<string>> ParseTabbedText(this string text) {
            if (text == null) throw new ArgumentNullException("text");

            List<List<string>> result = new List<List<string>>();
            List<string> row = new List<string>();
            int i=0;
            bool lineChanged=false;

            while (i < text.Length) {
                string field = ReadField(text, ref i, out lineChanged);
                if (lineChanged) {
                    result.Add(row);
                    row = new List<string>();
                }
                if (field != null) row.Add(field);
            }
            result.Add(row);

            return result;
        }

        /// <summary>
        /// Reads and returns single field from the tab-separated text. 
        /// </summary>
        /// <param name="text">Text to read from</param>
        /// <param name="i">Position where to start</param>
        /// <param name="lineChanged">True, if newline character appeared before the field</param>
        /// <returns>Escaped text of the field</returns>
        private static string ReadField(string text, ref int i, out bool lineChanged) {
            lineChanged = false;            
            int continuosQuoteCount = 0;
            StringBuilder b = null;

            if (text[i] == '\r' || text[i] == '\n') {
                lineChanged = true;
                i++;
            }
            if (lineChanged && text[i] == '\n') i++;

            bool dataEscaped = false;
            if (i < text.Length && text[i] == '"') {
                dataEscaped = true;
                i++;
            }

            while (i < text.Length) {
                if (dataEscaped && continuosQuoteCount % 2 == 1 && text[i] != '"') {                    
                    if (text[i] == '\t') i++;
                    break;
                }

                if (text[i] == '\t' && !dataEscaped) {
                    i++;
                    break;
                }
                if (text[i] == '\r' && !dataEscaped) {                    
                    break;
                }
                if (text[i] == '"' && dataEscaped) {
                    continuosQuoteCount++;
                    if (continuosQuoteCount % 2 == 0) {
                        if (b == null) b = new StringBuilder();
                        b.Append('"');
                    }
                } else {
                    if (b == null) b = new StringBuilder();
                    continuosQuoteCount = 0;
                    b.Append(text[i]);
                }

                i++;
            }


            return b == null ? null : b.ToString();
        }
    }
}
