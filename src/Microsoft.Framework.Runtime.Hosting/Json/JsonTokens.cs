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
        public int Column
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Line
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    internal class JsonUnit : JsonPosition
    {
        public int Column
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Line
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    internal class JsonPrimitive : JsonUnit
    {

    }

    internal class JsonArray : JsonUnit
    {

    }

    internal class JsonDictonary : JsonUnit
    {

    }
}
