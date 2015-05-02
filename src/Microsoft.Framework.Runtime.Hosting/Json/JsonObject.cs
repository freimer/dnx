using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.Runtime.Json
{
    internal class JsonObject
    {
        private readonly IDictionary<string, object> _data;

        public JsonObject(IDictionary<string, object> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _data = data;
        }

        public ICollection<string> Keys
        {
            get { return _data.Keys; }
        }

        public object Value(string key)
        {
            object result;
            if (!_data.TryGetValue(key, out result))
            {
                result = null;
            }

            return result;
        }

        public T ValueAs<T>(string key)
        {
            object value = Value(key);
            if (value == null)
            {
                return default(T);
            }
            else
            {
                try
                {
                    return (T)value;
                }
                catch (InvalidCastException)
                {
                    return default(T);
                }
            }
        }

        public T ValueAs<T>(string key, Func<object, T> cast)
        {
            return cast(Value(key));
        }

        public JsonObject ValueAsJsonObject(string key)
        {
            return ValueAs<JsonObject>(key, value =>
            {
                return value as JsonObject;
            });
        }

        public JsonString ValueAsString(string key)
        {
            return ValueAs(key, value => value as JsonString);
        }

        public bool ValueAsBoolean(string key, bool defaultValue = false)
        {
            return ValueAs(key, value =>
            {
                if (value != null && value is bool)
                {
                    return (bool)value;
                }

                return defaultValue;
            });
        }

        public bool? ValueAsNullableBoolean(string key)
        {
            return ValueAs<bool?>(key, value =>
            {
                if (value != null && value is bool)
                {
                    return (bool?)value;
                }
                else
                {
                    return null;
                }
            });
        }

        public object[] ValueAsArray(string key)
        {
            return ValueAs(key, value => (value as IList<object>)?.ToArray());
        }

        //public T[] ValueAsArray<T>(string key)
        //{
        //    return ValueAs(key, value =>
        //    {
        //        var list = value as IList<object>;
        //        if (list == null)
        //        {
        //            return null;
        //        }
        //        else
        //        {
        //            // bad...
        //            var result = new List<T>();
        //            foreach (var each in list)
        //            {
        //                result.Add((T)each);
        //            }

        //            return result.ToArray();
        //        }
        //    });
        //}

        public string[] ValueAsStringArray(string key)
        {
            return ValueAs(key, value =>
            {
                var list = value as IList<object>;
                if (list == null)
                {
                    return null;
                }

                if (list.Count == 0)
                {
                    return new string[] { };
                }

                if (list.First() is JsonString)
                {
                    return list.Select(each => (each as JsonString).ToString()).ToArray();
                }
                else
                {
                    return null;
                }
            });
        }
    }
}
