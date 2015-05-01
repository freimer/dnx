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

        /// <summary>
        /// Move the cursor to the next non empty char
        /// </summary>
        /// <returns>Returns false if the cursor reach end of the content</returns>
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

        //public char? MoveNext()
        //{

        //    throw new NotImplementedException();
        //}

        //public char? MovePrev()
        //{
        //    throw new NotImplementedException();
        //}
    }
}