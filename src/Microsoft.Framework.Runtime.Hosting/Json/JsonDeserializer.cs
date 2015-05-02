// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Framework.Runtime.Json
{
    internal class JsonDeserializer
    {
        // maximum number of entries a Json deserialized dictionary is allowed to have
        private const int _maxJsonDeserializerMembers = Int32.MaxValue;
        private const int _maxDeserializeDepth = 100;
        private const int _maxInputLength = 2097152;

        private JsonContent _input;

        public object Deserialize(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _input = JsonContent.CreateFromStream(stream);

            return Deserialize();
        }

        public JsonObject DeserializeAsJsonObject(Stream stream)
        {
            return (JsonObject)Deserialize(stream);
        }

        public object Deserialize(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (input.Length > _maxInputLength)
            {
                throw new ArgumentException(JsonDeserializerResource.JSON_MaxJsonLengthExceeded, nameof(input));
            }

            _input = JsonContent.CreateFromString(input);

            return Deserialize();
        }

        private object Deserialize()
        {
            object result = DeserializeInternal(0);

            // There are still unprocessed char. The parsing is not finished. Error happened.
            if (_input.MoveToNextNonEmptyChar() == true)
            {
                throw CreateExceptionFromContent(JsonDeserializerResource.JSON_IllegalPrimitive);
            }

            return result;
        }

        private object DeserializeInternal(int depth)
        {
            if (++depth > _maxDeserializeDepth)
            {
                throw CreateExceptionFromContent(JsonDeserializerResource.JSON_DepthLimitExceeded);
            }

            if (!_input.MoveToNextNonEmptyChar())
            {
                return null;
            }

            var nextChar = _input.CurrentChar;
            _input.MovePrev();

            if (IsNextElementObject(nextChar))
            {
                return DeserializeDictionary(depth);
            }

            if (IsNextElementArray(nextChar))
            {
                return DeserializeList(depth);
            }

            if (IsNextElementString(nextChar))
            {
                return DeserializeString();
            }

            return DeserializePrimitiveObject();
        }

        private FileFormatException CreateExceptionFromContent(string message)
        {
            return FileFormatException.Create(
                _input.GetStatusInfo(message),
                _input.CurrentLine,
                _input.CurrentColumn);
        }

        private IList<object> DeserializeList(int depth)
        {
            var list = new List<object>();

            if (!_input.MoveNext())
            {
                throw CreateExceptionFromContent("Parsing reach the end of the content before it can finish");
            }

            if (!_input.ValidCursor)
            {
                throw CreateExceptionFromContent("Invalid cursor");
            }

            if (_input.CurrentChar != '[')
            {
                throw CreateExceptionFromContent(JsonDeserializerResource.JSON_InvalidArrayStart);
            }

            bool expectMore = false;
            while (_input.MoveToNextNonEmptyChar() && _input.CurrentChar != ']')
            {
                _input.MovePrev();

                object o = DeserializeInternal(depth);
                list.Add(o);

                expectMore = false;

                // we might be done here.
                _input.MoveToNextNonEmptyChar();
                if (_input.CurrentChar == ']')
                {
                    break;
                }

                expectMore = true;
                if (_input.CurrentChar != ',')
                {
                    throw CreateExceptionFromContent(JsonDeserializerResource.JSON_InvalidArrayExpectComma);
                }
            }

            if (expectMore)
            {
                throw CreateExceptionFromContent(JsonDeserializerResource.JSON_InvalidArrayExtraComma);
            }

            if (_input.CurrentChar != ']')
            {
                throw CreateExceptionFromContent(JsonDeserializerResource.JSON_InvalidArrayEnd);
            }

            return list;
        }

        private JsonObject DeserializeDictionary(int depth)
        {
            IDictionary<string, object> dictionary = null;

            if (!_input.MoveNext())
            {
                throw CreateExceptionFromContent("Parsing reach the end of the content before it can finish");
            }

            if (!_input.ValidCursor)
            {
                throw CreateExceptionFromContent("Invalid cursor");
            }

            if (_input.CurrentChar != '{')
            {
                throw CreateExceptionFromContent(JsonDeserializerResource.JSON_ExpectedOpenBrace);
            }

            // Loop through each JSON entry in the input object
            while (_input.MoveToNextNonEmptyChar())
            {
                char c = _input.CurrentChar;

                _input.MovePrev();

                if (c == ':')
                {
                    throw CreateExceptionFromContent(JsonDeserializerResource.JSON_InvalidMemberName);
                }

                string memberName = null;
                if (c != '}')
                {
                    // Find the member name
                    memberName = DeserializeMemberName();
                    _input.MoveToNextNonEmptyChar();
                    if (_input.CurrentChar != ':')
                    {
                        throw CreateExceptionFromContent(JsonDeserializerResource.JSON_InvalidObject);
                    }
                }

                if (dictionary == null)
                {
                    dictionary = new Dictionary<string, object>();

                    // If the object contains nothing (i.e. {}), we're done
                    if (memberName == null)
                    {
                        // Move the cursor to the '}' character.
                        _input.MoveToNextNonEmptyChar();
                        break;
                    }
                }

                ThrowIfMaxJsonDeserializerMembersExceeded(dictionary.Count);

                // Deserialize the property value.  Here, we don't know its type
                object propVal = DeserializeInternal(depth);
                dictionary[memberName] = propVal;
                _input.MoveToNextNonEmptyChar();
                if (_input.CurrentChar == '}')
                {
                    break;
                }

                if (_input.CurrentChar != ',')
                {
                    throw CreateExceptionFromContent(JsonDeserializerResource.JSON_InvalidObject);
                }
            }

            if (_input.CurrentChar != '}')
            {
                throw CreateExceptionFromContent(JsonDeserializerResource.JSON_InvalidObject);
            }

            return new JsonObject(dictionary);
        }

        // Deserialize a member name.
        // e.g. { MemberName: ... }
        // e.g. { 'MemberName': ... }
        // e.g. { "MemberName": ... }
        private string DeserializeMemberName()
        {
            // It could be double quoted, single quoted, or not quoted at all
            if (!_input.MoveToNextNonEmptyChar())
            {
                return null;
            }

            var c = _input.CurrentChar;
            _input.MovePrev();

            // If it's quoted, treat it as a string
            if (IsNextElementString(c))
            {
                return DeserializeString().ToString();
            }

            // Non-quoted token
            return DeserializePrimitiveToken();
        }

        private object DeserializePrimitiveObject()
        {
            string input = DeserializePrimitiveToken();
            if (input.Equals("null"))
            {
                return null;
            }

            if (input.Equals("true"))
            {
                return true;
            }

            if (input.Equals("false"))
            {
                return false;
            }

            // Is it a floating point value
            bool hasDecimalPoint = input.IndexOf('.') >= 0;
            // DevDiv 56892: don't try to parse to Int32/64/Decimal if it has an exponent sign
            bool hasExponent = input.LastIndexOf("e", StringComparison.OrdinalIgnoreCase) >= 0;
            // [Last]IndexOf(char, StringComparison) overload doesn't exist, so search for "e" as a string not a char
            // Use 'Last'IndexOf since if there is an exponent it would be more quickly found starting from the end of the string
            // since 'e' is always toward the end of the number. e.g. 1.238907598768972987E82

            if (!hasExponent)
            {
                // when no exponent, could be Int32, Int64, Decimal, and may fall back to Double
                // otherwise it must be Double

                if (!hasDecimalPoint)
                {
                    // No decimal or exponent. All Int32 and Int64s fall into this category, so try them first
                    // First try int
                    int n;
                    if (Int32.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                    {
                        // NumberStyles.Integer: AllowLeadingWhite, AllowTrailingWhite, AllowLeadingSign
                        return n;
                    }

                    // Then try a long
                    long l;
                    if (Int64.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out l))
                    {
                        // NumberStyles.Integer: AllowLeadingWhite, AllowTrailingWhite, AllowLeadingSign
                        return l;
                    }
                }

                // No exponent, may or may not have a decimal (if it doesn't it couldn't be parsed into Int32/64)
                decimal dec;
                if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out dec))
                {
                    // NumberStyles.Number: AllowLeadingWhite, AllowTrailingWhite, AllowLeadingSign,
                    //                      AllowTrailingSign, AllowDecimalPoint, AllowThousands
                    return dec;
                }
            }

            // either we have an exponent or the number couldn't be parsed into any previous type. 
            Double d;
            if (Double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            {
                // NumberStyles.Float: AllowLeadingWhite, AllowTrailingWhite, AllowLeadingSign, AllowDecimalPoint, AllowExponent
                return d;
            }

            // must be an illegal primitive
            throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, JsonDeserializerResource.JSON_IllegalPrimitive, input));
        }

        private string DeserializePrimitiveToken()
        {
            var sb = new StringBuilder();

            while (_input.MoveNext())
            {
                var c = _input.CurrentChar;
                if (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' || c == '+')
                {
                    sb.Append(c);
                }
                else
                {
                    _input.MovePrev();
                    break;
                }
            }

            return sb.ToString();
        }

        private JsonString DeserializeString()
        {
            var sb = new StringBuilder();
            var escapedChar = false;

            _input.MoveNext();

            // First determine which quote is used by the string.
            var quoteChar = CheckQuoteChar(_input.CurrentChar);
            while (_input.MoveNext())
            {
                if (_input.CurrentChar == '\\')
                {
                    if (escapedChar)
                    {
                        sb.Append('\\');
                        escapedChar = false;
                    }
                    else
                    {
                        escapedChar = true;
                    }

                    continue;
                }

                if (escapedChar)
                {
                    AppendCharToBuilder(_input.CurrentChar, sb);
                    escapedChar = false;
                }
                else
                {
                    if (_input.CurrentChar == quoteChar)
                    {
                        return new JsonString(Utf16StringValidator.ValidateString(sb.ToString()));
                    }

                    sb.Append(_input.CurrentChar);
                }
            }

            throw CreateExceptionFromContent(JsonDeserializerResource.JSON_UnterminatedString);
        }

        private void AppendCharToBuilder(char? c, StringBuilder sb)
        {
            if (c == '"' || c == '\'' || c == '/')
            {
                sb.Append(c.Value);
            }
            else if (c == 'b')
            {
                sb.Append('\b');
            }
            else if (c == 'f')
            {
                sb.Append('\f');
            }
            else if (c == 'n')
            {
                sb.Append('\n');
            }
            else if (c == 'r')
            {
                sb.Append('\r');
            }
            else if (c == 't')
            {
                sb.Append('\t');
            }
            else if (c == 'u')
            {
                var c4 = new char[4];
                for (int i = 0; i < 4; ++i)
                {
                    _input.MoveNext();
                    c4[i] = _input.CurrentChar;
                }

                sb.Append((char)int.Parse(new string(c4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            }
            else
            {
                throw CreateExceptionFromContent(JsonDeserializerResource.JSON_BadEscape);
            }
        }

        private char CheckQuoteChar(char c)
        {
            var quoteChar = '"';
            if (c == '\'')
            {
                quoteChar = c;
            }
            else if (c != '"')
            {
                // Fail if the string is not quoted.
                throw CreateExceptionFromContent(JsonDeserializerResource.JSON_StringNotQuoted);
            }

            return quoteChar;
        }

        // MSRC 12038: limit the maximum number of entries that can be added to a Json deserialized dictionary,
        // as a large number of entries potentially can result in too many hash collisions that may cause DoS
        private void ThrowIfMaxJsonDeserializerMembersExceeded(int count)
        {
            if (count >= _maxJsonDeserializerMembers)
            {
                throw new InvalidOperationException(string.Format(JsonDeserializerResource.JSON_MaxJsonDeserializerMembers, _maxJsonDeserializerMembers));
            }
        }

        private static bool IsNextElementArray(char? c)
        {
            return c == '[';
        }

        private static bool IsNextElementObject(char? c)
        {
            return c == '{';
        }

        private static bool IsNextElementString(char? c)
        {
            return c == '"' || c == '\'';
        }
    }
}
