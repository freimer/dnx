// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Runtime.Json
{
    internal interface JsonPosition
    {
        int Line { get; }
        int Column { get; }
    }

    internal class JsonToken : JsonPosition
    {
        public JsonToken()
        {
            Line = 0;
            Column = 0;
        }

        public JsonToken(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public int Line { get; private set; }

        public int Column { get; private set; }
    }

    internal class JsonUnit : JsonPosition
    {
        public JsonUnit()
        {
            Line = 0;
            Column = 0;
        }

        public JsonUnit(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public int Line { get; private set; }

        public int Column { get; private set; }
    }

    internal class JsonPrimitive : JsonUnit
    {

    }

    internal class JsonDictonary : JsonUnit
    {

    }
}
