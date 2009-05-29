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
using System.Text;

namespace s3pi.Interfaces
{
    /// <summary>
    /// A tuple associating a data type (or class) with a value object (of the given type)
    /// </summary>
    public class TypedValue : IComparable<TypedValue>, IEqualityComparer<TypedValue>, IEquatable<TypedValue>, IConvertible, ICloneable
    {
        /// <summary>
        /// The data type
        /// </summary>
        public readonly Type Type;
        /// <summary>
        /// The value
        /// </summary>
        public readonly object Value;

        string format = "";

        /// <summary>
        /// Create a new TypedValue
        /// </summary>
        /// <param name="t">The data type</param>
        /// <param name="v">The value</param>
        public TypedValue(Type t, object v) : this(t, v, "") { }
        /// <summary>
        /// Create a new TypedValue
        /// </summary>
        /// <param name="t">The data type</param>
        /// <param name="v">The value</param>
        /// <param name="f">The default format</param>
        public TypedValue(Type t, object v, string f) { Type = t; Value = v; format = f; }

        public static implicit operator string(TypedValue tv) { return tv.ToString(tv.format); }

        /// <summary>
        /// Return the Value in default format
        /// </summary>
        /// <returns>String representation of Value in default format</returns>
        public override string ToString() { return ToString(this.format); }
        /// <summary>
        /// Return the Value in given format
        /// </summary>
        /// <param name="format">Format to use for result</param>
        /// <returns>String representation of Value in given format</returns>
        public string ToString(string format)
        {
            if (format == "X")
            {
                if (this.Type == typeof(Int64)) return "0x" + ((Int64)this.Value).ToString("X16");
                if (this.Type == typeof(UInt64)) return "0x" + ((UInt64)this.Value).ToString("X16");
                if (this.Type == typeof(Int32)) return "0x" + ((Int32)this.Value).ToString("X8");
                if (this.Type == typeof(UInt32)) return "0x" + ((UInt32)this.Value).ToString("X8");
                if (this.Type == typeof(Int16)) return "0x" + ((Int16)this.Value).ToString("X4");
                if (this.Type == typeof(UInt16)) return "0x" + ((UInt16)this.Value).ToString("X4");
                if (this.Type == typeof(sbyte)) return "0x" + ((sbyte)this.Value).ToString("X2");
                if (this.Type == typeof(byte)) return "0x" + ((byte)this.Value).ToString("X2");
            }

            if (typeof(String).IsAssignableFrom(this.Type))
            {
                string s = (String)this.Value;
                if (s.IndexOf((char)0) != -1) return s.Length % 2 == 0 ? ToANSIString(s) : ToDisplayString(s.ToCharArray());
                return s.Normalize();
            }

            if (typeof(System.Char[]).IsAssignableFrom(this.Type))
                return ToDisplayString((char[])this.Value);

            if (typeof(Array).IsAssignableFrom(this.Type))
                return FromArray((Array)this.Value);

            return this.Value.ToString();
        }

        static string ToANSIString(string unicode)
        {
            StringBuilder t = new StringBuilder();
            for (int i = 0; i < unicode.Length; i += 2) t.Append((char)((((char)unicode[i]) << 8) + (char)unicode[i + 1]));
            return t.ToString().Normalize();
        }

        static string FromArray(Array ary)
        {
            string s = "";
            int i = 0;
            foreach (object v in ary)
            {
                TypedValue tv = new TypedValue(v.GetType(), v, "X");
                s += String.Format(" [{0:X}:'{1}']", i++, "" + tv);
            }
            return s.TrimStart();
        }

        static readonly string[] LowNames = {
                                                "NUL", "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL",
                                                "BS", "HT", "LF", "VT", "FF", "CR", "SO", "SI",
                                                "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB",
                                                "CAN", "EM", "SUB", "ESC", "FS", "GS", "RS", "US",
                                            };
        static string ToDisplayString(char[] text)
        {
            string ret = "";
            foreach (char c in text)
            {
                if (c < 32)
                    ret += string.Format("<{0}>", LowNames[c]);
                else if (c >= 127)
                    ret += string.Format("<U+{0:X4}>", (int)c);
                else
                    ret += c;
            }
            return ret;
        }

        #region IComparable<TypedValue> Members

        /// <summary>
        /// Compare this TypedValue to another for sort order purposes
        /// </summary>
        /// <param name="other">Target TypedValue</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the objects being compared.  The return value has these meanings:
        /// Value Meaning Less than zero -- This instance is less than obj.
        /// Zero -- This instance is equal to obj.
        /// Greater than zero -- This instance is greater than obj.</returns>
        /// <exception cref="NotImplementedException">Either this object's Type or the target's is not comparable</exception>
        /// <exception cref="ArgumentException">The target is not comparable with this object</exception>
        public int CompareTo(TypedValue other)
        {
            if (!this.Type.IsAssignableFrom(other.Type) || !(this.Type is IComparable) || !(other.Type is IComparable))
                throw new NotImplementedException();
            return ((IComparable)this.Value).CompareTo((IComparable)other.Value);
        }

        #endregion

        #region IEqualityComparer<TypedValue> Members

        public bool Equals(TypedValue x, TypedValue y) { return x.Equals(y); }
        public int GetHashCode(TypedValue obj) { return obj.GetHashCode(); }

        #endregion

        #region IEquatable<TypedValue> Members

        public bool Equals(TypedValue other) { return this.Value.Equals(other.Value); }

        #endregion

        #region IConvertible Members

        public TypeCode GetTypeCode()
        {
            return TypeCode.String;
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            if (typeof(bool).IsAssignableFrom(this.Type)) return (bool)this.Value;
            throw new NotImplementedException();
        }

        public byte ToByte(IFormatProvider provider)
        {
            if (typeof(byte).IsAssignableFrom(this.Type)) return (byte)this.Value;
            throw new NotImplementedException();
        }

        public char ToChar(IFormatProvider provider)
        {
            if (typeof(char).IsAssignableFrom(this.Type)) return (char)this.Value;
            throw new NotImplementedException();
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            if (typeof(DateTime).IsAssignableFrom(this.Type)) return (DateTime)this.Value;
            throw new NotImplementedException();
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            if (typeof(decimal).IsAssignableFrom(this.Type)) return (decimal)this.Value;
            throw new NotImplementedException();
        }

        public double ToDouble(IFormatProvider provider)
        {
            if (typeof(double).IsAssignableFrom(this.Type)) return (double)this.Value;
            throw new NotImplementedException();
        }

        public short ToInt16(IFormatProvider provider)
        {
            if (typeof(short).IsAssignableFrom(this.Type)) return (short)this.Value;
            throw new NotImplementedException();
        }

        public int ToInt32(IFormatProvider provider)
        {
            if (typeof(int).IsAssignableFrom(this.Type)) return (int)this.Value;
            throw new NotImplementedException();
        }

        public long ToInt64(IFormatProvider provider)
        {
            if (typeof(long).IsAssignableFrom(this.Type)) return (long)this.Value;
            throw new NotImplementedException();
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            if (typeof(sbyte).IsAssignableFrom(this.Type)) return (sbyte)this.Value;
            throw new NotImplementedException();
        }

        public float ToSingle(IFormatProvider provider)
        {
            if (typeof(float).IsAssignableFrom(this.Type)) return (float)this.Value;
            throw new NotImplementedException();
        }

        public string ToString(IFormatProvider provider)
        {
            if (typeof(string).IsAssignableFrom(this.Type)) return (string)this.Value;
            throw new NotImplementedException();
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType.IsAssignableFrom(this.Type)) return Convert.ChangeType(this.Value, conversionType, provider);
            throw new NotImplementedException();
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            if (typeof(ushort).IsAssignableFrom(this.Type)) return (ushort)this.Value;
            throw new NotImplementedException();
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            if (typeof(uint).IsAssignableFrom(this.Type)) return (uint)this.Value;
            throw new NotImplementedException();
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            if (typeof(ulong).IsAssignableFrom(this.Type)) return (ulong)this.Value;
            throw new NotImplementedException();
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            if (typeof(ICloneable).IsAssignableFrom(this.Type)) return new TypedValue(this.Type, ((ICloneable)this.Value).Clone(), this.format);
            return this;
        }

        #endregion
    }
}
