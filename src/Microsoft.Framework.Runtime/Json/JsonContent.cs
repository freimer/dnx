// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Framework.Runtime.Json
{
    /// <summary>
    /// JsonContent represents a json file and its content 
    /// </summary>
    public class JsonContent
    {
        private List<string> _content = new List<string>();

        /// <summary>
        /// Create a JsonContent instance from a stream.
        /// The consumer is responsible of manage the stream's lifecycle
        /// </summary>
        /// <param name="stream">Content source</param>
        public JsonContent(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var reader = new StreamReader(stream);

            string line = reader.ReadLine();
            while (line != null)
            {
                _content.Add(line);
                line = reader.ReadLine();
            }
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
            get { return (_content.Count > CurrentLine) && (_content[CurrentLine].Length > CurrentPosition); }
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
        /// Move the cursor to the previous char
        /// </summary>
        /// <returns>Returns false </returns>
        public bool MovePrev()
        {
            if (CurrentPosition - 1 >= 0)
            {
                CurrentPosition -= 1;
                return true;
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
                    return false;
                }
                else
                {
                    CurrentLine = targetLine;
                    CurrentPosition = _content[CurrentLine].Length - 1;
                    return true;
                }
            }
        }
    }
}