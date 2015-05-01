// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Runtime.Json;
using Xunit;
using System.IO;
using System.Text;
using System;
using System.Diagnostics;

namespace Microsoft.Framework.Runtime.Tests
{
    public class JsonContentFacts
    {
        private readonly static string TestContent =
@"{
    ""version"": ""1.0.0-*"",
    ""description"": ""ASP.NET 5 Roslyn compiler implementation."",
    ""compilationOptions"": { ""define"": [ ""TRACE"" ], ""allowUnsafe"": true, ""warningsAsErrors"": true },
    ""dependencies"": {
        ""Microsoft.Framework.Runtime.Common"": { ""version"": ""1.0.0-*"", ""type"": ""build"" },
        ""Microsoft.Framework.Runtime.Compilation.Common"": { ""version"": ""1.0.0-*"", ""type"": ""build"" },
        ""Microsoft.Framework.Runtime.Interfaces"": ""1.0.0-*"",
        ""Microsoft.Framework.Runtime.Roslyn.Common"": { ""version"": ""1.0.0-*"", ""type"": ""build"" },
        ""Microsoft.Framework.Runtime.Roslyn.Interfaces"": ""1.0.0-*"",
        ""Microsoft.Framework.Runtime.Caching"": ""1.0.0-*""
    },
    ""frameworks"": {
        ""dnx451"": {
            ""frameworkAssemblies"": {
                ""System.Collections"": """",
                ""System.IO"": """",
                ""System.Threading.Tasks"": """",
                ""System.Text.Encoding"": """"
            }
        },
        ""dnxcore50"": {
            ""dependencies"": {
                ""System.Collections.Concurrent"": ""4.0.10-beta-*"",
                ""System.Runtime.InteropServices"": ""4.0.20-beta-*"",
                ""System.IO.FileSystem"": ""4.0.0-beta-*""
            }
        }
    },
    ""scripts"": {
        ""postbuild"": [
            ""%project:Directory%/../../build/batchcopy %project:BuildOutputDir%/Debug/dnx451/*.* %project:Directory%/../../artifacts/build/dnx-clr-win-x86/bin"",
            ""%project:Directory%/../../build/batchcopy %project:BuildOutputDir%/Debug/dnx451/*.* %project:Directory%/../../artifacts/build/dnx-mono/bin"",
            ""%project:Directory%/../../build/batchcopy %project:BuildOutputDir%/Debug/dnxcore50/*.* %project:Directory%/../../artifacts/build/dnx-coreclr-win-x86/bin""
        ]
    }
}";
        [Fact]
        public void CreateJsonContentFromStream()
        {
            using (var mem = CreateStreamFromContent(TestContent))
            {
                var content = new JsonContent(mem);
                Assert.Equal(37, content.TotalLines);
            }
        }

        [Fact]
        public void MoveToNextNonEmptyCharInSimpleCase()
        {
            using (var mem = CreateStreamFromContent("{}"))
            {
                var content = new JsonContent(mem);
                Assert.Equal(1, content.TotalLines);

                Assert.True(content.MoveToNextNonEmptyChar());
                Assert.Equal('{', content.CurrentChar);
                Assert.Equal(0, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);

                Assert.True(content.MoveToNextNonEmptyChar());
                Assert.Equal('}', content.CurrentChar);
                Assert.Equal(0, content.CurrentLine);
                Assert.Equal(1, content.CurrentPosition);

                Assert.False(content.MoveToNextNonEmptyChar());
            }
        }

        [Fact]
        public void MoveToNextNonEmptyCharInMultipleLines()
        {
            using (var mem = CreateStreamFromContent(@"{
    ""sample"": ""value"",   
    ""switch"": true
}"))
            {
                var content = new JsonContent(mem);
                Assert.Equal(4, content.TotalLines);

                Assert.True(content.MoveToNextNonEmptyChar());
                Assert.Equal('{', content.CurrentChar);
                Assert.Equal(0, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);

                Assert.True(content.MoveToNextNonEmptyChar());
                Assert.Equal('"', content.CurrentChar);
                Assert.Equal(1, content.CurrentLine);
                Assert.Equal(4, content.CurrentPosition);

                for (int i = 0; i < 9; ++i)
                {
                    Assert.True(content.MoveToNextNonEmptyChar());
                }
                Assert.Equal('"', content.CurrentChar);
                Assert.Equal(1, content.CurrentLine);
                Assert.Equal(14, content.CurrentPosition);

                Assert.True(content.MoveToNextNonEmptyChar());
                Assert.Equal('v', content.CurrentChar);
                Assert.Equal(1, content.CurrentLine);
                Assert.Equal(15, content.CurrentPosition);

                for (int i = 0; i < 6; ++i)
                {
                    Assert.True(content.MoveToNextNonEmptyChar());
                }
                Assert.Equal(',', content.CurrentChar);
                Assert.Equal(1, content.CurrentLine);
                Assert.Equal(21, content.CurrentPosition);

                for (int i = 0; i < 13; ++i)
                {
                    Assert.True(content.MoveToNextNonEmptyChar());
                }
                Assert.Equal('e', content.CurrentChar);
                Assert.Equal(2, content.CurrentLine);
                Assert.Equal(17, content.CurrentPosition);

                Assert.True(content.MoveToNextNonEmptyChar());
                Assert.Equal('}', content.CurrentChar);
                Assert.Equal(3, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);
                Assert.True(content.ValidCursor);

                Assert.False(content.MoveToNextNonEmptyChar());
                Assert.False(content.ValidCursor);
            }
        }

        [Fact]
        public void MoveNextWorksCorrectly()
        {
            var raw =
@"a
bc    
def
 
fg
h


i
jl m n
";
            using (var mem = CreateStreamFromContent(raw))
            {
                var content = new JsonContent(mem);
                Assert.False(content.Started);

                Assert.True(content.MoveNext());
                Assert.True(content.Started);
                Assert.Equal('a', content.CurrentChar);
                Assert.Equal(0, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);

                Assert.True(content.MoveNext());
                Assert.Equal('b', content.CurrentChar);
                Assert.Equal(1, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);

                Assert.True(content.MoveNext());
                Assert.Equal('c', content.CurrentChar);
                Assert.Equal(1, content.CurrentLine);
                Assert.Equal(1, content.CurrentPosition);

                Assert.True(content.MoveNext());
                Assert.Equal(' ', content.CurrentChar);
                Assert.Equal(1, content.CurrentLine);
                Assert.Equal(2, content.CurrentPosition);

                Repeat(4, () => Assert.True(content.MoveNext()));
                Assert.Equal('d', content.CurrentChar);
                Assert.Equal(2, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);

                Repeat(3, () => Assert.True(content.MoveNext()));
                Assert.Equal(' ', content.CurrentChar);
                Assert.Equal(3, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);

                Repeat(4, () => Assert.True(content.MoveNext()));
                Assert.Equal('i', content.CurrentChar);
                Assert.Equal(8, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);

                Repeat(5, () => Assert.True(content.MoveNext()));
                Assert.Equal(' ', content.CurrentChar);
                Assert.Equal(9, content.CurrentLine);
                Assert.Equal(4, content.CurrentPosition);

                Assert.True(content.MoveNext());
                Assert.Equal('n', content.CurrentChar);
                Assert.Equal(9, content.CurrentLine);
                Assert.Equal(5, content.CurrentPosition);

                Assert.False(content.MoveNext());

                // Cursor doesn't move
                Assert.Equal('n', content.CurrentChar);
                Assert.Equal(9, content.CurrentLine);
                Assert.Equal(5, content.CurrentPosition);
            }
        }

        [Fact]
        public void MovePrevWorksCorrectly()
        {
            var raw =
@"a
bc    
def
 
fg
h


i
jl m n
";
            using (var mem = CreateStreamFromContent(raw))
            {
                var content = new JsonContent(mem);
                Assert.False(content.Started);

                Assert.False(content.MovePrev());
                Assert.True(content.MoveNext());
                Assert.False(content.MovePrev());
                Assert.Equal(0, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);

                // Move to the end
                Repeat(20, () => Assert.True(content.MoveNext()));
                Assert.False(content.MoveNext());

                Assert.True(content.MovePrev());
                Assert.Equal(' ', content.CurrentChar);
                Assert.Equal(9, content.CurrentLine);
                Assert.Equal(4, content.CurrentPosition);

                Repeat(5, () => Assert.True(content.MovePrev()));
                Assert.Equal('i', content.CurrentChar);
                Assert.Equal(8, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);

                Assert.True(content.MovePrev());
                Assert.Equal('h', content.CurrentChar);
                Assert.Equal(5, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);

                Repeat(7, () => Assert.True(content.MovePrev()));
                Assert.Equal(' ', content.CurrentChar);
                Assert.Equal(1, content.CurrentLine);
                Assert.Equal(5, content.CurrentPosition);

                Repeat(3, () =>
                {
                    Assert.True(content.MovePrev());
                    Assert.Equal(' ', content.CurrentChar);
                });

                Repeat(3, () => Assert.True(content.MovePrev()));
                Assert.Equal('a', content.CurrentChar);
                Assert.Equal(0, content.CurrentLine);
                Assert.Equal(0, content.CurrentPosition);

                Assert.False(content.MovePrev());
            }
        }

        private MemoryStream CreateStreamFromContent(string content)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        private void Repeat(int count, Action action)
        {
            for (int i = 0; i < count; ++i)
            {
                action();
            }
        }
    }
}