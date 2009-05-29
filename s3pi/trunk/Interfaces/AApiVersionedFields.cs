/***************************************************************************
 *  Copyright (C) 2009 by Peter L Jones                                    *
 *  peter@users.sf.net                                                     *
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
        public TypedValue this[string index]
        {
            get
            {
                MethodInfo m = this.GetType().GetMethod("get_" + index, new Type[0]);
                if (m == null)
                    throw new ArgumentOutOfRangeException("index", "Unexpected value received in index: " + index);

                try
                {
                    return new TypedValue(m.ReturnType, m.Invoke(this, null), "X");
                }
                catch (Exception ex)
                {
                    return new TypedValue(typeof(Exception), ex);
                }
            }
            set
            {
                MethodInfo m = this.GetType().GetMethod("set_" + index, new Type[] { value.Type });
                if (m == null)
                    throw new ArgumentOutOfRangeException("index", "Unexpected value received in index: " + index);

                m.Invoke(this, new object[] { value.Value });
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
            foreach (MethodInfo m in t.GetMethods()) banlist.Add(m.Name);
        }
#if UNDEF
        static Int32 getRecommendedApiVersion(Type t)
        {
            FieldInfo fi = t.GetField("recommendedApiVersion", BindingFlags.Static | BindingFlags.NonPublic);
            if (fi == null || fi.FieldType != typeof(Int32))
                throw new Exception("recommendedApiVersion not found on Type " + t.FullName);

            return (Int32)fi.GetValue(null);
        }

        static bool checkVersion(MethodInfo m, Int32 v)
        {
            return true;
#if UNDEF
            foreach (Attribute attr in Attribute.GetCustomAttributes(m))
            {
                if (attr.GetType() == typeof(MinimumVersionAttribute))
                    if (v < (attr as MinimumVersionAttribute).Version)
                        return false;
                if (attr.GetType() == typeof(MaximumVersionAttribute))
                    if (v > (attr as MaximumVersionAttribute).Version)
                        return false;
            }
#endif
        }
#endif

        /// <summary>
        /// Versioning is not currently implemented
        /// Return the list of fields for a given API Class
        /// </summary>
        /// <param name="APIversion">Set to 0 (== "best")</param>
        /// <param name="t">The class type for which to get the fields</param>
        /// <returns>List of field names for the given API version</returns>
        public static List<string> GetContentFields(Int32 APIversion, Type t)
        {
            List<string> fields = null;

            //Int32 recommendedApiVersion = getRecommendedApiVersion(t);
            fields = new List<string>();
            MethodInfo[] am = t.GetMethods();
            foreach (MethodInfo m in am)
            {
                if (!m.IsPublic || banlist.Contains(m.Name)) continue;
                if (!m.Name.StartsWith("get_") || m.GetParameters().Length > 0) continue;
                //if (!checkVersion(m, APIversion == 0 ? recommendedApiVersion : APIversion)) continue;

                fields.Add(m.Name.Replace("get_", ""));
            }

            return fields;
        }

        public static Dictionary<string, Type> GetContentFieldTypes(Int32 APIversion, Type t)
        {
            Dictionary<string, Type> types = null;

            //Int32 recommendedApiVersion = getRecommendedApiVersion(t);
            types = new Dictionary<string, Type>();
            MethodInfo[] am = t.GetMethods();
            foreach (MethodInfo m in am)
            {
                if (!m.IsPublic || banlist.Contains(m.Name)) continue;
                if (!m.Name.StartsWith("get_") || m.GetParameters().Length > 0) continue;
                //if (!checkVersion(m, APIversion == 0 ? recommendedApiVersion : APIversion)) continue;

                types.Add(m.Name.Substring(4), m.ReturnType);
            }

            return types;
        }

#if UNDEF
        protected static List<string> getMethods(Int32 APIversion, Type t)
        {
            List<string> methods = null;

            //Int32 recommendedApiVersion = getRecommendedApiVersion(t);
            methods = new List<string>();
            MethodInfo[] am = t.GetMethods();
            foreach (MethodInfo m in am)
            {
                if (!m.IsPublic || banlist.Contains(m.Name)) continue;
                if ((m.Name.StartsWith("get_") && m.GetParameters().Length == 0)) continue;
                //if (!checkVersion(m, APIversion == 0 ? recommendedApiVersion : APIversion)) continue;

                methods.Add(m.Name);
            }

            return methods;
        }

        public List<string> Methods { get; }
        
        public TypedValue Invoke(string method, params TypedValue[] parms)
        {
            Type[] at = new Type[parms.Length];
            object[] ao = new object[parms.Length];
            for (int i = 0; i < parms.Length; i++) { at[i] = parms[i].Type; ao[i] = parms[i].Value; }

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
        public static void Write7BitStr(System.IO.Stream s, string value)
        {
            System.IO.BinaryWriter w = new System.IO.BinaryWriter(s);
            for (int i = value.Length; true; ) { w.Write((byte)(i & 0x7F)); i = i >> 7; if (i == 0) break; }
            w.Write(value.ToCharArray());
        }

        /// <summary>
        /// Convert a string (up to 8 characters) to a UInt64
        /// </summary>
        /// <param name="s">String to convert</param>
        /// <returns>UInt64 packed representation of <paramref name="s"/></returns>
        public static UInt64 FOURCC(string s)
        {
            if (s.Length > 8) throw new ArgumentException(String.Format("String length {0} invalid; maximum is 8.", s.Length), "s");
            UInt64 i = 0;
            for (int j = s.Length; j >= 0; j--) i += (((uint)s[j]) << (8 * j));
            return i;
        }

        /// <summary>
        /// Convert a string (up to 8 characters) to a UInt64
        /// </summary>
        /// <param name="s">String to convert</param>
        /// <returns>UInt64 packed representation of <paramref name="s"/></returns>
        public static string FOURCC(UInt64 i)
        {
            string s = "";
            for (int j = 7; j >= 0; j--) { char c = (char)((i >> (j * 8)) & 0xff); if (s.Length > 0 || c != 0) s += c; }
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
}
