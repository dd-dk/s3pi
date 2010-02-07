/***************************************************************************
 *  Copyright (C) 2009 by Peter L Jones                                    *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 3 Package Interface (s3pi)               *
 *                                                                         *
 *  s3pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s3pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s3pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace s3pi.Interfaces
{
    /// <summary>
    /// API Objects should all descend from this Abstract class.
    /// It will provide versioning support -- when implemented.
    /// It provides ContentFields support
    /// </summary>
    public abstract class AApiVersionedFields : IApiVersion, IContentFields
    {
        #region IApiVersion Members
        /// <summary>
        /// The version of the API in use
        /// </summary>
        public int RequestedApiVersion { get { return requestedApiVersion; } }
        /// <summary>
        /// The best supported version of the API available
        /// </summary>
        public abstract int RecommendedApiVersion { get; }

        #endregion

        #region IContentFields Members
        /// <summary>
        /// The list of available field names on this API object
        /// </summary>
        public abstract List<string> ContentFields { get; } // This should be implemented with a call to GetContentFields(requestedApiVersion, this.GetType())
        /// <summary>
        /// A typed value on this object
        /// </summary>
        /// <param name="index">The name of the field (i.e. one of the values from ContentFields)</param>
        /// <returns>The typed value of the named field</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown index name is requested</exception>
        public virtual TypedValue this[string index]
        {
            get
            {
                string[] fields = index.Split('.');
                object result = this;
                Type t = this.GetType();
                foreach (string f in fields)
                {
                    PropertyInfo p = t.GetProperty(f);
                    if (p == null)
                        throw new ArgumentOutOfRangeException("index", "Unexpected value received in index: " + index);
                    t = p.PropertyType;
                    result = p.GetValue(result, null);
                }
                return new TypedValue(t, result, "X");
            }
            set
            {
                string[] fields = index.Split('.');
                object result = this;
                Type t = this.GetType();
                PropertyInfo p = null;
                for (int i = 0; i < fields.Length; i++)
                {
                    p = t.GetProperty(fields[i]);
                    if (p == null)
                        throw new ArgumentOutOfRangeException("index", "Unexpected value received in index: " + index);
                    if (i < fields.Length - 1)
                    {
                        t = p.PropertyType;
                        result = p.GetValue(result, null);
                    }
                }
                p.SetValue(result, value.Value, null);
            }
        }

        #endregion

        /// <summary>
        /// Versioning is not currently implemented
        /// Set this to the version of the API requested on object creation
        /// </summary>
        protected int requestedApiVersion = 0;

        static List<string> banlist;
        static AApiVersionedFields()
        {
            Type t = typeof(AApiVersionedFields);
            banlist = new List<string>();
            foreach (PropertyInfo m in t.GetProperties()) banlist.Add(m.Name);
        }

        static Int32 Version(Type attribute, Type type, string field)
        {
            foreach (VersionAttribute attr in type.GetProperty(field).GetCustomAttributes(attribute, true)) return attr.Version;
            return 0;
        }
        static Int32 MinimumVersion(Type type, string field) { return Version(typeof(MinimumVersionAttribute), type, field); }
        static Int32 MaximumVersion(Type type, string field) { return Version(typeof(MaximumVersionAttribute), type, field); }
        //protected Int32 MinimumVersion(string field) { return AApiVersionedFields.MinimumVersion(this.GetType(), field); }
        //protected Int32 MaximumVersion(string field) { return AApiVersionedFields.MaximumVersion(this.GetType(), field); }
        static Int32 getRecommendedApiVersion(Type t)
        {
            FieldInfo fi = t.GetField("recommendedApiVersion", BindingFlags.Static | BindingFlags.NonPublic);
            if (fi == null || fi.FieldType != typeof(Int32))
                return 0;
            return (Int32)fi.GetValue(null);
        }
        static bool checkVersion(Type type, string field, int requestedApiVersion)
        {
            if (requestedApiVersion == 0) return true;
            int min = MinimumVersion(type, field);
            if (min != 0 && requestedApiVersion < min) return false;
            int max = MaximumVersion(type, field);
            if (max != 0 && requestedApiVersion > max) return false;
            return true;
        }

        /// <summary>
        /// Versioning is not currently implemented
        /// Return the list of fields for a given API Class
        /// </summary>
        /// <param name="APIversion">Set to 0 (== "best")</param>
        /// <param name="t">The class type for which to get the fields</param>
        /// <returns>List of field names for the given API version</returns>
        public static List<string> GetContentFields(Int32 APIversion, Type t)
        {
            List<string> fields = new List<string>();

            Int32 recommendedApiVersion = getRecommendedApiVersion(t);//Could be zero if no "recommendedApiVersion" const field
            PropertyInfo[] ap = t.GetProperties();
            foreach (PropertyInfo m in ap)
            {
                if (banlist.Contains(m.Name)) continue;
                if (!checkVersion(t, m.Name, APIversion == 0 ? recommendedApiVersion : APIversion)) continue;

                fields.Add(m.Name);
            }
            fields.Sort();

            return fields;
        }

        /// <summary>
        /// Gets a lookup table from fieldname to type.
        /// </summary>
        /// <param name="APIversion">Version of API to use</param>
        /// <param name="t">API data type to query</param>
        /// <returns></returns>
        public static Dictionary<string, Type> GetContentFieldTypes(Int32 APIversion, Type t)
        {
            Dictionary<string, Type> types = new Dictionary<string, Type>();

            Int32 recommendedApiVersion = getRecommendedApiVersion(t);//Could be zero if no "recommendedApiVersion" const field
            PropertyInfo[] ap = t.GetProperties();
            foreach (PropertyInfo m in ap)
            {
                if (banlist.Contains(m.Name)) continue;
                if (!checkVersion(t, m.Name, APIversion == 0 ? recommendedApiVersion : APIversion)) continue;

                types.Add(m.Name, m.PropertyType);
            }

            return types;
        }

#if UNDEF
        protected static List<string> getMethods(Int32 APIversion, Type t)
        {
            List<string> methods = null;

            Int32 recommendedApiVersion = getRecommendedApiVersion(t);//Could be zero if no "recommendedApiVersion" const field
            methods = new List<string>();
            MethodInfo[] am = t.GetMethods();
            foreach (MethodInfo m in am)
            {
                if (!m.IsPublic || banlist.Contains(m.Name)) continue;
                if ((m.Name.StartsWith("get_") && m.GetParameters().Length == 0)) continue;
                if (!checkVersion(t, m.Name, APIversion == 0 ? recommendedApiVersion : APIversion)) continue;

                methods.Add(m.Name);
            }

            return methods;
        }

        public List<string> Methods { get; }
        
        public TypedValue Invoke(string method, params TypedValue[] parms)
        {
            Type[] at = new Type[parms.Length];
            object[] ao = new object[parms.Length];
            for (int i = 0; i < parms.Length; i++) { at[i] = parms[i].Type; ao[i] = parms[i].Value; }//Array.Copy, please...

            MethodInfo m = this.GetType().GetMethod(method, at);
            if (m == null)
                throw new ArgumentOutOfRangeException("Unexpected method received: " + method + "(...)");

            return new TypedValue(m.ReturnType, m.Invoke(this, ao), "X");
        }
#endif

        /// <summary>
        /// A class enabling sorting API objects by a ContentFields name
        /// </summary>
        /// <typeparam name="T">API object type</typeparam>
        public class Comparer<T> : IComparer<T>
            where T : IContentFields
        {
            string field;
            /// <summary>
            /// Sort API Objects by <paramref name="field"/>
            /// </summary>
            /// <param name="field">ContentField name to sort by</param>
            public Comparer(string field) { this.field = field; }

            #region IComparer<T> Members

            /// <summary>
            /// Compares two objects of type T and returns a value indicating whether one is less than,
            /// equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first IContentFields object to compare.</param>
            /// <param name="y">The second IContentFields object to compare.</param>
            /// <returns>Value Condition Less than zero -- x is less than y.
            /// Zero -- x equals y.
            /// Greater than zero -- x is greater than y.</returns>
            public int Compare(T x, T y) { return x[field].CompareTo(y[field]); }

            #endregion

        }

        // Random helper functions that should live somewhere...

        /// <summary>
        /// Write a 7BITSTR value to a stream
        /// </summary>
        /// <param name="s">Stream to write to</param>
        /// <param name="value">String to write, prefixed by length, seven bits at a time</param>
        /// <param name="enc">encoding to use on the <see cref="System.IO.BinaryWriter(System.IO.Stream, System.Text.Encoding)"/></param>
        public static void Write7BitStr(System.IO.Stream s, string value, System.Text.Encoding enc)
        {
            byte[] bytes = enc.GetBytes(value);
            System.IO.BinaryWriter w = new System.IO.BinaryWriter(s, enc);
            for (int i = bytes.Length; true; ) { w.Write((byte)((i & 0x7F) | (i > 0x7F ? 0x80 : 0))); i = i >> 7; if (i == 0) break; }
            w.Write(bytes);
        }
        public static void Write7BitStr(System.IO.Stream s, string value) { Write7BitStr(s, value, System.Text.Encoding.Default); }

        /// <summary>
        /// Convert a string (up to 8 characters) to a UInt64
        /// </summary>
        /// <param name="s">String to convert</param>
        /// <returns>UInt64 packed representation of <paramref name="s"/></returns>
        public static UInt64 FOURCC(string s)
        {
            if (s.Length > 8) throw new ArgumentLengthException("String", 8);
            UInt64 i = 0;
            for (int j = s.Length - 1; j >= 0; j--) i += ((uint)s[j]) << (j * 8);
            return i;
        }

        /// <summary>
        /// Convert a UInt64 to a string (up to 8 characters, high-order zeros omitted)
        /// </summary>
        /// <param name="i">Bytes to convert</param>
        /// <returns>String representation of <paramref name="i"/></returns>
        public static string FOURCC(UInt64 i)
        {
            string s = "";
            for (int j = 7; j >= 0; j--) { char c = (char)((i >> (j * 8)) & 0xff); if (s.Length > 0 || c != 0) s = c + s; }
            return s;
        }

        /// <summary>
        /// Return a space-separated string containing valid enumeration names for the given type
        /// </summary>
        /// <param name="t">Enum type</param>
        /// <returns>Valid enum names</returns>
        public static string FlagNames(Type t) { string p = ""; foreach (string q in Enum.GetNames(t)) p += " " + q; return p.Trim(); }

        /// <summary>
        /// Check that the two arrays are equal (same type, same value content)
        /// </summary>
        /// <param name="x">First array</param>
        /// <param name="y">Second array</param>
        /// <returns>True if type and content of <paramref name="x"/>  equals type and content of <paramref name="y"/></returns>
        public static bool ArrayCompare(System.Collections.IList x, System.Collections.IList y)
        {
            if (x.GetType() != y.GetType()) throw new ArgumentException();
            if (x.Count != y.Count) return false;
            for (int i = 0; i < x.Count; i++) if (x[i] != y[i]) return false;
            return true;
        }
    }

    /// <summary>
    /// This class is a useful extension to AApiVersionedFields where a change handler is required
    /// </summary>
    public abstract class AHandlerElement : AApiVersionedFields
    {
        /// <summary>
        /// Element change event handler
        /// </summary>
        protected EventHandler handler;
        /// <summary>
        /// Indicates if this list element has been changed by OnElementChanged()
        /// </summary>
        protected bool dirty = false;
        /// <summary>
        /// Initialize a new instance
        /// </summary>
        /// <param name="APIversion">Requested API version</param>
        /// <param name="handler">Element change event handler</param>
        public AHandlerElement(int APIversion, EventHandler handler) { requestedApiVersion = APIversion; this.handler = handler; }
        /// <summary>
        /// Get a copy of this element but with a new change event handler
        /// </summary>
        /// <param name="handler">Element change event handler</param>
        /// <returns>Return a copy of this element but with a new change event handler</returns>
        public abstract AHandlerElement Clone(EventHandler handler);
        /// <summary>
        /// Raise a change event
        /// </summary>
        protected virtual void OnElementChanged()
        {
            dirty = true;
            //Console.WriteLine(this.GetType().Name + " dirtied.");
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
