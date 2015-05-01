// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Framework.Runtime.Json
{
    /// <summary>
    /// JsonContent represents a json file and its content 
    /// </summary>
    internal class JsonContent
    {
        private List<string> _content = new List<string>();

        /// <summary>
        /// Create a JsonContent instance from a stream.
        ///
        /// Once created the JsonContent instance won't keep the handle to the 
        /// stream nor dispose or close it.
        /// </summary>
        /// <param name="stream">Content source</param>
        /// <returns>Newly created JsonContent instance</returns>
        public static JsonContent CreateFromStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var content = new List<string>();
            var reader = new StreamReader(stream);

            string line = reader.ReadLine();
            while (line != null)
            {
                content.Add(line);
                line = reader.ReadLine();
            }

            return new JsonContent(content);
        }

        /// <summary>
        /// Create a JsonContent instance from a string.
        /// 
        /// The string will be feed to a memory string and split into lines.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Newly created JsonContent instance</returns>
        public static JsonContent CreateFromString(string input)
        {
            using (var mem = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            {
                return CreateFromStream(mem);
            }
        }

        private JsonContent(List<string> content)
        {
            _content = content;
        }

        public int TotalLines { get { return _content.Count; } }

        public int CurrentLine { get; private set; } = 0;

        public int CurrentPosition { get; private set; } = -1;

        public char CurrentChar
        {
            get { return _content[CurrentLine][CurrentPosition]; }
        }

        public bool ValidCursor
        {
            get
            {
                return CurrentLine < _content.Count &&
                       CurrentLine >= 0 &&
                       CurrentPosition < _content[CurrentLine].Length &&
                       CurrentPosition >= 0;
            }
        }

        public bool IsCurrentNonEmptyChar
        {
            get { return char.IsWhiteSpace(_content[CurrentChar][CurrentPosition]) == false; }
        }

        public bool Started
        {
            get { return CurrentLine != 0 || CurrentPosition != -1; }
        }

        /// <summary>
        /// Move the cursor to the next non empty char
        /// </summary>
        /// <returns>Returns false if the cursor reach the end of the content.</returns>
        public bool MoveToNextNonEmptyChar()
        {
            while (TotalLines > CurrentLine)
            {
                while (_content[CurrentLine].Length > CurrentPosition + 1)
                {
                    char c = _content[CurrentLine][++CurrentPosition];
                    if (!char.IsWhiteSpace(c))
                    {
                        return true;
                    }
                }

                CurrentLine++;
                CurrentPosition = -1;
            }

            return false;
        }

        /// <summary>
        /// Move the cursor to the next char
        /// </summary>
        /// <returns>Returns false if the cursor reach the end of the content.</returns>
        public bool MoveNext()
        {
            if (CurrentPosition + 1 < _content[CurrentLine].Length)
            {
                CurrentPosition += 1;
                return true;
            }
            else
            {
                // find the first non empty line after current line
                var targetLine = CurrentLine;
                while (++targetLine < TotalLines && _content[targetLine].Length == 0)
                {
                }

                if (targetLine >= TotalLines)
                {
                    return false;
                }
                else
                {
                    CurrentLine = targetLine;
                    CurrentPosition = 0;
                    return true;
                }
            }
        }

        /// <summary>
        /// Move the cursor to the previous char.
        /// </summary>
        /// <returns>Returns false if the cursor reach it's inital position at [0, -1]</returns>
        public bool MovePrev()
        {
            if (CurrentPosition - 1 >= 0)
            {
                CurrentPosition -= 1;
                return true;
            }
            else if (CurrentLine == 0 && CurrentPosition == 0)
            {
                /// If cursor at [Line:0, Column:0] current then allow it to 
                /// move into the position ahead of it. Therefore it allows the 
                /// first MoveNext or MoveToNextNonEmptyChar to be functional
                CurrentPosition = -1;
                return true;
            }
            else if (CurrentLine == 0 && CurrentPosition == -1)
            {
                return false;
            }
            else
            {
                var targetLine = CurrentLine;

                // find the first non empty line before current line
                while (--targetLine >= 0 && _content[targetLine].Length == 0)
                {
                }

                if (targetLine < 0)
                {
                    // Even the first line is empty, move it to the initial position.
                    CurrentLine = 0;
                    CurrentPosition = -1;
                    return true;
                }
                else
                {
                    CurrentLine = targetLine;
                    CurrentPosition = _content[CurrentLine].Length - 1;
                    return true;
                }
            }
        }

        /// <summary>
        /// Returns a short status message for debugging
        /// </summary>
        public string GetStatusInfo(string message = null)
        {
            return string.Format(@"{0} at [Line: {1}, Column: {2}, Char: {3}]",
                message ?? "Status", CurrentLine, CurrentPosition, ValidCursor ? CurrentChar.ToString() : "INVALID");
        }
    }
}