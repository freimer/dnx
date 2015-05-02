using System;

namespace Microsoft.Framework.Runtime.Json
{
    internal class JsonString : JsonUnit
    {
        private readonly string _value;

        public JsonString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _value = value;
        }

        public override string ToString()
        {
            return _value;
        }

        public static implicit operator string (JsonString instance)
        {
            if (instance == null)
            {
                return null;
            }
            else
            {
                return instance.ToString();
            }
        }
    }
}
