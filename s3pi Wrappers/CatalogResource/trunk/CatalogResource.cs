﻿/***************************************************************************
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
using System.IO;
using s3pi.Interfaces;

namespace CatalogResource
{
    /// <summary>
    /// A resource wrapper that understands Catalog Entry resources
    /// </summary>
    public abstract class CatalogResource : AResource
    {
        protected const Int32 recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        protected static bool checking = s3pi.Settings.Settings.Checking;

        #region Attributes
        protected Common common = null;
        #endregion

        #region Constructors
        protected CatalogResource(int APIversion, Stream s)
            : base(APIversion, s)
        {
            common = new Common(requestedApiVersion, OnResourceChanged);
            if (stream == null) { stream = UnParse(); dirty = true; }
            stream.Position = 0;
            Parse(stream);
        }
        #endregion

        #region Data I/O
        protected abstract void Parse(Stream s);

        protected abstract Stream UnParse();
        #endregion

        #region IResource Members
        /// <summary>
        /// The resource content as a Stream
        /// </summary>
        public override Stream Stream
        {
            get
            {
                if (dirty)
                {
                    stream = UnParse();
                    stream.Position = 0;
                    dirty = false;
                }
                return stream;
            }
        }
        #endregion

        #region Sub-classes
        public class Common : AHandlerElement
        {
            #region Attributes
            uint unknown1;
            ulong nameGUID;
            ulong descGUID;
            string name = "";
            string desc = "";
            float price;
            float unknown2;
            byte[] unknown3 = new byte[4];
            BuildBuyProductStatus buildBuyProductStatusFlags;
            ulong pngInstance;
            #endregion

            #region Constructors
            internal Common(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }

            public Common(int APIversion, EventHandler handler) : base(APIversion, handler) { }

            public Common(int APIversion, EventHandler handler, uint unknown1, ulong nameGUID, ulong descGUID, string name, string desc, float price, float unknown2,
                byte[] unknown3, byte unknown4, ulong pngInstance)
                : base(APIversion, handler)
            {
                this.unknown1 = unknown1;
                this.nameGUID = nameGUID;
                this.descGUID = descGUID;
                this.name = name;
                this.desc = desc;
                this.price = price;
                this.unknown2 = unknown2;
                if (unknown3.Length != this.unknown3.Length) throw new ArgumentLengthException("unknown3", this.unknown3.Length);
                this.unknown3 = (byte[])unknown3.Clone();
                this.buildBuyProductStatusFlags = (BuildBuyProductStatus)unknown4;
                this.pngInstance = pngInstance;
            }

            public Common(int APIversion, EventHandler handler, Common basis)
                : base(APIversion, handler)
            {
                this.unknown1 = basis.unknown1;
                this.nameGUID = basis.nameGUID;
                this.descGUID = basis.descGUID;
                this.name = basis.name;
                this.desc = basis.desc;
                this.price = basis.price;
                this.unknown2 = basis.unknown2;
                this.unknown3 = (byte[])basis.unknown3.Clone();
                this.buildBuyProductStatusFlags = basis.buildBuyProductStatusFlags;
                this.pngInstance = basis.pngInstance;
            }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                BinaryReader r2 = new BinaryReader(s, System.Text.Encoding.BigEndianUnicode);

                unknown1 = r.ReadUInt32();
                nameGUID = r.ReadUInt64();
                descGUID = r.ReadUInt64();
                name = r2.ReadString();
                //name = System.Text.Encoding.BigEndianUnicode.GetString(r.ReadBytes(r.ReadByte()));
                desc = r2.ReadString();
                //desc = System.Text.Encoding.BigEndianUnicode.GetString(r.ReadBytes(r.ReadByte()));
                price = r.ReadSingle();
                unknown2 = r.ReadSingle();
                unknown3 = r.ReadBytes(4);
                if (checking) if (unknown3.Length != 4)
                        throw new InvalidDataException(String.Format("unknown3: read {0} bytes; expected 4; at 0x{1:X8}", unknown3.Length, s.Position));
                buildBuyProductStatusFlags = (BuildBuyProductStatus)r.ReadByte();
                pngInstance = r.ReadUInt64();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(unknown1);
                w.Write(nameGUID);
                w.Write(descGUID);
                Write7BitStr(s, name, System.Text.Encoding.BigEndianUnicode);
                //w.Write((byte)(name.Length * 2));
                //w.Write(System.Text.Encoding.BigEndianUnicode.GetBytes(name));
                Write7BitStr(s, desc, System.Text.Encoding.BigEndianUnicode);
                //w.Write((byte)(desc.Length * 2));
                //w.Write(System.Text.Encoding.BigEndianUnicode.GetBytes(desc));
                w.Write(price);
                w.Write(unknown2);
                w.Write(unknown3);
                w.Write((byte)buildBuyProductStatusFlags);
                w.Write(pngInstance);
            }
            #endregion

            #region AHandlerElement
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override AHandlerElement Clone(EventHandler handler) { return new Common(requestedApiVersion, handler, this); }
            #endregion

            #region Sub-classes
            [Flags]
            public enum BuildBuyProductStatus : byte
            {
                ShowInCatalog = 0x01,
                ProductForTesting = 0x02,
                ProductInDevelopment = 0x04,
                ShippingProduct = 0x08,

                DebugProduct = 0x10,
                ProductionProduct = 0x20,
                ObjProductMadeUsingNewEntryScheme = 0x40,
                //
            }
            #endregion

            #region Content Fields
            public uint Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }

            public ulong NameGUID { get { return nameGUID; } set { if (nameGUID != value) { nameGUID = value; OnElementChanged(); } } }

            public ulong DescGUID { get { return descGUID; } set { if (descGUID != value) { descGUID = value; OnElementChanged(); } } }

            public string Name { get { return name; } set { if (name != value) { name = value; OnElementChanged(); } } }

            public string Desc { get { return desc; } set { if (desc != value) { desc = value; OnElementChanged(); } } }

            public float Price { get { return price; } set { if (price != value) { price = value; OnElementChanged(); } } }

            public float Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnElementChanged(); } } }

            public byte[] Unknown3
            {
                get { return (byte[])unknown3.Clone(); }
                set
                {
                    if (value.Length != this.unknown3.Length) throw new ArgumentLengthException("Unknown3", this.unknown3.Length);
                    bool same = true;
                    for (int i = 0; same && i < value.Length; i++) same = unknown3[i] == value[i];
                    if (!same)
                    {
                        unknown3 = (byte[])value.Clone();
                        OnElementChanged();
                    }
                }
            }

            public BuildBuyProductStatus BuildBuyProductStatusFlags { get { return buildBuyProductStatusFlags; } set { if (buildBuyProductStatusFlags != value) { buildBuyProductStatusFlags = value; OnElementChanged(); } } }

            public ulong PngInstance { get { return pngInstance; } set { if (pngInstance != value) { pngInstance = value; OnElementChanged(); } } }

            public String Value
            {
                get
                {
                    string s = "";
                    foreach (string f in this.ContentFields)
                    {
                        if (f.Equals("Value")) continue;
                        s += String.Format("{0}: {1}\n", f, "" + this[f]);
                    }
                    return s;
                }
            }
            #endregion
        }

        #region TypeCode
        public abstract class TypeCode : AHandlerElement,
            IComparable<TypeCode>, IEqualityComparer<TypeCode>, IEquatable<TypeCode>
        {
            #region Attributes
            protected byte[] prefix = new byte[0];
            #endregion

            #region Constructors
            protected TypeCode(int APIversion, EventHandler handler, byte[] pfx) : base(APIversion, handler) { prefix = pfx == null ? null : (byte[])pfx.Clone(); }
            protected TypeCode(int APIversion, EventHandler handler, Stream s, byte[] pfx) : this(APIversion, handler, pfx) { Parse(s); }

            public static TypeCode CreateTypeCode(int APIversion, EventHandler handler, Stream s, byte[] prefix)
            {
                switch (prefix[1])
                {
                    case 0x01: return new TypeCode01(APIversion, handler, s, prefix);
                    case 0x02: return new TypeCode02(APIversion, handler, s, prefix);
                    case 0x03: return new TypeCode03(APIversion, handler, s, prefix);
                    case 0x04: return new TypeCode04(APIversion, handler, s, prefix);
                    case 0x05: return new TypeCode05(APIversion, handler, s, prefix);
                    case 0x06: return new TypeCode06(APIversion, handler, s, prefix);
                    case 0x07: return new TypeCode07(APIversion, handler, s, prefix);
                }
                throw new InvalidDataException(String.Format("Unknown TypeCode 0x{0:X2} at 0x{1:X8}", prefix[1], s.Position));
            }
            #endregion

            #region Data I/O
            protected abstract void Parse(Stream s);
            internal virtual void UnParse(Stream s)
            {
                if (prefix == null) return;
                BinaryWriter w = new BinaryWriter(s);
                foreach (byte b in prefix) w.Write(b);
            }
            #endregion

            #region IComparable<TypeCode> Members

            public abstract int CompareTo(TypeCode other);

            #endregion

            #region IEqualityComparer<TypeCode> Members

            public virtual bool Equals(TypeCode x, TypeCode y) { if (x.GetType() != y.GetType()) return false; return x.CompareTo(y) == 0; }

            public virtual int GetHashCode(TypeCode obj) { return ((Object)obj).GetHashCode(); }

            public override int GetHashCode() { return GetHashCode(this); }

            #endregion

            #region IEquatable<TypeCode> Members

            public bool Equals(TypeCode other) { return Equals(this, other); }

            #endregion

            #region AApiVersionedFields
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            #endregion

            #region ContentFields
            public byte ControlCode { get { return prefix[0]; } set { if (prefix[0] != value) { prefix[0] = value; handler(this, new EventArgs()); } } }

            public String Value
            {
                get
                {
                    TypeCode01 tc1 = this as TypeCode01;
                    string s = "";
                    foreach (string f in this.ContentFields)
                    {
                        if (f.Equals("Value")) continue;
                        if (f.Equals("ControlCode") && prefix == null) continue;
                        if (tc1 != null && f.Equals("SubType") && tc1.HasUnknown1) continue;
                        s += String.Format("{0}: {1}\n", f, "" + this[f]);
                    }
                    return s;
                }
            }
            #endregion
        }

        public class TypeCode01 : TypeCode
        {
            byte subType;
            string unknown1 = "";
            byte unknown2;

            internal TypeCode01(int APIversion, EventHandler handler, Stream s, byte[] prefix) : base(APIversion, handler, s, prefix) { }

            public TypeCode01(int APIversion, EventHandler handler, byte[] prefix, string unknown1)
                : base(APIversion, handler, prefix)
            {
                if (unknown1.Length > 0x3f)
                    throw new ArgumentException(String.Format("String length must not exceed 0x3f: 0x{0:X}", unknown1.Length));
                subType = (byte)(0x80 | unknown1.Length);
                this.unknown1 = unknown1;
                this.unknown2 = 0;
            }

            public TypeCode01(int APIversion, EventHandler handler, byte[] prefix, byte unknown2)
                : base(APIversion, handler, prefix)
            {
                subType = 0x40;
                this.unknown1 = "";
                this.unknown2 = unknown2;
            }

            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                subType = r.ReadByte();
                if (checking) if (/*(subType & 0xC0) == 0 || */(subType & 0xC0) == 0xC0)
                        throw new InvalidDataException(String.Format("Unexpected subType read: 0x{0:X2} at 0x{1:X8}", subType & 0xC0, s.Position));
                unknown1 = (subType & 0x80) == 0 ? "" : new String(r.ReadChars(subType & 0x3f));
                unknown2 = (subType & 0x40) == 0 ? (byte)0 : r.ReadByte();
            }

            internal override void UnParse(Stream s)
            {
                base.UnParse(s);
                BinaryWriter w = new BinaryWriter(s);
                w.Write(subType);
                if ((subType & 0x80) != 0) w.Write(unknown1.ToCharArray());
                if ((subType & 0x40) != 0) w.Write(unknown2);
            }

            public override int CompareTo(TypeCode other)
            {
                TypeCode01 tc = other as TypeCode01;
                if (tc == null) return -1;
                if (HasUnknown1 && tc.HasUnknown1) return unknown1.CompareTo(tc.unknown1);
                if (HasUnknown2 && tc.HasUnknown2)
                {
                    int res = subType.CompareTo(tc.subType); if (res != 0) return res;
                    return unknown2.CompareTo(tc.unknown2);
                }
                return subType.CompareTo(tc.subType);
            }

            public override int GetHashCode(TypeCode obj) { return HasUnknown1 ? unknown1.GetHashCode() : HasUnknown2 ? unknown2.GetHashCode() : -1; }

            public override AHandlerElement Clone(EventHandler handler)
            {
                if (HasUnknown1) return new TypeCode01(requestedApiVersion, handler, prefix, unknown1);
                if (HasUnknown2) return new TypeCode01(requestedApiVersion, handler, prefix, unknown2);
                throw new InvalidOperationException();
            }

            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public bool HasUnknown1
            {
                get { return (subType & 0x80) != 0; }
                set
                {
                    if (HasUnknown1 == value) return;
                    if (HasUnknown2) throw new InvalidOperationException("This TypeCode01 has Unknown2 - remove first");
                    subType = (byte)(value ? 0x80 : 0x00);
                    unknown1 = "";
                    OnElementChanged();
                }
            }
            public bool HasUnknown2
            {
                get { return (subType & 0x40) != 0; }
                set
                {
                    if (HasUnknown2 == value) return;
                    if (HasUnknown1) throw new InvalidOperationException("This TypeCode01 has Unknown1 - remove first");
                    subType = (byte)((value ? 0x40 : 0x00) | SubType);
                    unknown2 = 0x00;
                    OnElementChanged();
                }
            }

            public string Unknown1
            {
                get { return unknown1; }
                set
                {
                    if (!HasUnknown1) throw new InvalidOperationException("This TypeCode01 has no Unknown1");
                    if (value.Length > 0x3f)
                        throw new ArgumentException(String.Format("String length (0x{0:X}) must not exceed 0x3F.", value.Length));
                    if (unknown1 != value) { unknown1 = value; OnElementChanged(); }
                }
            }
            public byte Unknown2
            {
                get { return unknown2; }
                set
                {
                    if (!HasUnknown2) throw new InvalidOperationException("This TypeCode01 has no Unknown2");
                    if (unknown2 != value) { unknown2 = value; OnElementChanged(); }
                }
            }
            public byte SubType
            {
                get
                {
                    if (HasUnknown1) throw new InvalidOperationException("This TypeCode01 has Unknown1 - cannot get SubType");
                    return (byte)(subType & 0x3F);
                }
                set
                {
                    if (subType == value) return;
                    if (HasUnknown1) throw new InvalidOperationException("This TypeCode01 has Unknown1 - cannot set SubType");
                    if ((value & 0xC0) != 0) throw new ArgumentOutOfRangeException("Maximum value for SubType is 0x3F.");
                    subType &= 0xC0;
                    subType |= value;
                    OnElementChanged();
                }
            }
        }

        public class TypeCode02 : TypeCode
        {
            byte red;
            byte green;
            byte blue;
            byte alpha;

            internal TypeCode02(int APIversion, EventHandler handler, Stream s, byte[] prefix) : base(APIversion, handler, s, prefix) { }

            public TypeCode02(int APIversion, EventHandler handler, byte[] prefix, byte r, byte g, byte b, byte a) : base(APIversion, handler, prefix) { red = r; green = g; blue = b; alpha = a; }

            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                red = r.ReadByte();
                green = r.ReadByte();
                blue = r.ReadByte();
                alpha = r.ReadByte();
            }

            internal override void UnParse(Stream s)
            {
                base.UnParse(s);
                BinaryWriter w = new BinaryWriter(s);
                w.Write(red);
                w.Write(green);
                w.Write(blue);
                w.Write(alpha);
            }

            public override int CompareTo(TypeCode other)
            {
                TypeCode02 tc = other as TypeCode02;
                if (tc == null) return -1;
                return GetHashCode(this).CompareTo(GetHashCode(tc));
            }

            public override int GetHashCode(TypeCode obj)
            {
                TypeCode02 tc = obj as TypeCode02;
                if (tc == null) base.GetHashCode(obj);
                return (((tc.red << 8) + tc.green << 8) + tc.blue << 8) + tc.alpha;
            }

            public override AHandlerElement Clone(EventHandler handler) { return new TypeCode02(requestedApiVersion, handler, prefix, red, green, blue, alpha); }

            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public byte Red { get { return red; } set { if (red != value) { red = value; OnElementChanged(); } } }
            public byte Green { get { return green; } set { if (green != value) { green = value; OnElementChanged(); } } }
            public byte Blue { get { return blue; } set { if (blue != value) { blue = value; OnElementChanged(); } } }
            public byte Alpha { get { return alpha; } set { if (alpha != value) { alpha = value; OnElementChanged(); } } }
        }

        public class TypeCode03 : TypeCode
        {
            byte tgiIndex;

            internal TypeCode03(int APIversion, EventHandler handler, Stream s, byte[] prefix) : base(APIversion, handler, s, prefix) { }
            public TypeCode03(int APIversion, EventHandler handler, byte[] prefix, byte tgiIndex) : base(APIversion, handler, prefix) { this.tgiIndex = tgiIndex; }

            protected override void Parse(Stream s) { tgiIndex = (new BinaryReader(s)).ReadByte(); }

            internal override void UnParse(Stream s)
            {
                base.UnParse(s);
                BinaryWriter w = new BinaryWriter(s);
                w.Write(tgiIndex);
            }

            public override int CompareTo(TypeCode other)
            {
                TypeCode03 tc = other as TypeCode03;
                if (tc == null) return -1;
                return tgiIndex.CompareTo(tc.tgiIndex);
            }

            public override int GetHashCode(TypeCode obj)
            {
                TypeCode03 tc = obj as TypeCode03;
                if (tc == null) base.GetHashCode(obj);
                return tgiIndex.GetHashCode();
            }

            public override AHandlerElement Clone(EventHandler handler) { return new TypeCode03(requestedApiVersion, handler, prefix, tgiIndex); }

            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public byte TGIIndex { get { return tgiIndex; } set { if (tgiIndex != value) { tgiIndex = value; OnElementChanged(); } } }
        }

        public class TypeCode04 : TypeCode
        {
            float unknown1;

            internal TypeCode04(int APIversion, EventHandler handler, Stream s, byte[] prefix) : base(APIversion, handler, s, prefix) { }
            public TypeCode04(int APIversion, EventHandler handler, byte[] prefix, float unknown1) : base(APIversion, handler, prefix) { this.unknown1 = unknown1; }

            protected override void Parse(Stream s) { unknown1 = (new BinaryReader(s)).ReadSingle(); }

            internal override void UnParse(Stream s)
            {
                base.UnParse(s);
                BinaryWriter w = new BinaryWriter(s);
                w.Write(unknown1);
            }

            public override int CompareTo(TypeCode other)
            {
                TypeCode04 tc = other as TypeCode04;
                if (tc == null) return -1;
                return unknown1.CompareTo(tc.unknown1);
            }

            public override int GetHashCode(TypeCode obj)
            {
                TypeCode04 tc = obj as TypeCode04;
                if (tc == null) base.GetHashCode(obj);
                return unknown1.GetHashCode();
            }

            public override AHandlerElement Clone(EventHandler handler) { return new TypeCode04(requestedApiVersion, handler, prefix, unknown1); }

            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public float Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }
        }

        public class TypeCode05 : TypeCode
        {
            float unknown1;
            float unknown2;

            internal TypeCode05(int APIversion, EventHandler handler, Stream s, byte[] prefix) : base(APIversion, handler, s, prefix) { }
            public TypeCode05(int APIversion, EventHandler handler, byte[] prefix, float unknown1, float unknown2) : base(APIversion, handler, prefix) { this.unknown1 = unknown1; this.unknown2 = unknown2; }

            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                unknown1 = r.ReadSingle();
                unknown2 = r.ReadSingle();
            }

            internal override void UnParse(Stream s)
            {
                base.UnParse(s);
                BinaryWriter w = new BinaryWriter(s);
                w.Write(unknown1);
                w.Write(unknown2);
            }

            public override int CompareTo(TypeCode other)
            {
                TypeCode05 tc = other as TypeCode05;
                if (tc == null) return -1;
                return GetHashCode(this).CompareTo(GetHashCode(tc));
            }

            public override int GetHashCode(TypeCode obj)
            {
                TypeCode05 tc = obj as TypeCode05;
                if (tc == null) base.GetHashCode(obj);
                return tc.unknown1.GetHashCode() ^ tc.unknown2.GetHashCode();
            }

            public override AHandlerElement Clone(EventHandler handler) { return new TypeCode05(requestedApiVersion, handler, prefix, unknown1, unknown2); }

            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public float Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }
            public float Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnElementChanged(); } } }
        }

        public class TypeCode06 : TypeCode
        {
            float unknown1;
            float unknown2;
            float unknown3;

            internal TypeCode06(int APIversion, EventHandler handler, Stream s, byte[] prefix) : base(APIversion, handler, s, prefix) { }
            public TypeCode06(int APIversion, EventHandler handler, byte[] prefix, float unknown1, float unknown2, float unknown3)
                : base(APIversion, handler, prefix)
            {
                this.unknown1 = unknown1;
                this.unknown2 = unknown2;
                this.unknown3 = unknown3;
            }

            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                unknown1 = r.ReadSingle();
                unknown2 = r.ReadSingle();
                unknown3 = r.ReadSingle();
            }

            internal override void UnParse(Stream s)
            {
                base.UnParse(s);
                BinaryWriter w = new BinaryWriter(s);
                w.Write(unknown1);
                w.Write(unknown2);
                w.Write(unknown3);
            }

            public override int CompareTo(TypeCode other)
            {
                TypeCode06 tc = other as TypeCode06;
                if (tc == null) return -1;
                return GetHashCode(this).CompareTo(GetHashCode(tc));
            }

            public override int GetHashCode(TypeCode obj)
            {
                TypeCode06 tc = obj as TypeCode06;
                if (tc == null) base.GetHashCode(obj);
                return tc.unknown1.GetHashCode() ^ tc.unknown2.GetHashCode() ^ tc.unknown3.GetHashCode();
            }

            public override AHandlerElement Clone(EventHandler handler) { return new TypeCode06(requestedApiVersion, handler, prefix, unknown1, unknown2, unknown3); }

            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public float Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }
            public float Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnElementChanged(); } } }
            public float Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnElementChanged(); } } }
        }

        public class TypeCode07 : TypeCode
        {
            byte unknown1;

            internal TypeCode07(int APIversion, EventHandler handler, Stream s, byte[] prefix) : base(APIversion, handler, s, prefix) { }
            public TypeCode07(int APIversion, EventHandler handler, byte[] prefix, byte unknown1) : base(APIversion, handler, prefix) { this.unknown1 = unknown1; }

            protected override void Parse(Stream s) { unknown1 = (new BinaryReader(s)).ReadByte(); }

            internal override void UnParse(Stream s)
            {
                base.UnParse(s);
                (new BinaryWriter(s)).Write(unknown1);
            }

            public override int CompareTo(TypeCode other)
            {
                TypeCode07 tc = other as TypeCode07;
                if (tc == null) return -1;
                return unknown1.CompareTo(tc.unknown1);
            }

            public override int GetHashCode(TypeCode obj)
            {
                TypeCode07 tc = obj as TypeCode07;
                if (tc == null) base.GetHashCode(obj);
                return unknown1.GetHashCode();
            }

            public override AHandlerElement Clone(EventHandler handler) { return new TypeCode07(requestedApiVersion, handler, prefix, unknown1); }

            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public byte Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }
        }

        public class TypeCode2F : TypeCode
        {
            byte unknown1;
            uint unknown2;

            internal TypeCode2F(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler, s, null) { }
            public TypeCode2F(int APIversion, EventHandler handler, byte unknown1, uint unknown2) : base(APIversion, handler, null) { this.unknown1 = unknown1; this.unknown2 = unknown2; }

            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                unknown1 = r.ReadByte();
                unknown2 = r.ReadUInt32();
            }

            internal override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write((byte)0x2F);
                w.Write(unknown1);
                w.Write(unknown2);
            }

            public override int CompareTo(TypeCode other)
            {
                TypeCode2F tc = other as TypeCode2F;
                if (tc == null) return -1;
                return GetHashCode(this).CompareTo(GetHashCode(tc));
            }

            public override int GetHashCode(TypeCode obj)
            {
                TypeCode2F tc = obj as TypeCode2F;
                if (tc == null) base.GetHashCode(obj);
                return unknown1.GetHashCode() ^ unknown2.GetHashCode();
            }

            public override AHandlerElement Clone(EventHandler handler) { return new TypeCode2F(requestedApiVersion, handler, unknown1, unknown2); }

            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public byte Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }
            public uint Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnElementChanged(); } } }
        }

        public class TypeCode40 : TypeCode
        {
            int length;
            internal TypeCode40(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler, s, null) { }
            private TypeCode40(int APIversion, EventHandler handler, int length) : base(APIversion, handler, null) { this.length = length; }

            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                length = 1;
                while (r.PeekChar() == 0x40) { length++; r.ReadChar(); }
            }

            internal override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                for (int i = 0; i < length; i++) w.Write((byte)0x40);
            }

            public override int CompareTo(TypeCode other)
            {
                TypeCode40 tc = other as TypeCode40;
                if (tc == null) return -1;
                return length.CompareTo(tc.length);
            }

            public override int GetHashCode(TypeCode obj)
            {
                TypeCode40 tc = obj as TypeCode40;
                if (tc == null) base.GetHashCode(obj);
                return length.GetHashCode();
            }

            public override AHandlerElement Clone(EventHandler handler) { return new TypeCode40(requestedApiVersion, handler, length); }

            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public int Length { get { return length; } set { if (length != value) { length = value; OnElementChanged(); } } }
        }

        public class TypeCodeList : AResource.DependentList<TypeCode>
        {
            #region Constructors
            public TypeCodeList(EventHandler handler) : base(handler) { }
            public TypeCodeList(EventHandler handler, IList<TypeCode> ltc) : base(handler, ltc) { }
            public TypeCodeList(EventHandler handler, Stream s) : base(handler, s) { }
            #endregion

            #region Data I/O
            protected override TypeCode CreateElement(Stream s) { throw new NotImplementedException(); }
            protected override TypeCode CreateElement(Stream s, out bool inc)
            {
                BinaryReader r = new BinaryReader(s);
                byte controlCode = r.ReadByte();
                switch (controlCode)
                {
                    case 0x40: inc = false; return new TypeCode40(0, handler, s);
                    case 0x2F: inc = true; return new TypeCode2F(0, handler, s);
                    default: inc = true; return TypeCode.CreateTypeCode(0, handler, s, new byte[] { controlCode, r.ReadByte() });
                }
            }

            protected override void WriteCount(Stream s, uint count) { foreach (TypeCode tc in this) if (tc is TypeCode40) count--; (new BinaryWriter(s)).Write(count); }
            protected override void WriteElement(Stream s, TypeCode element) { element.UnParse(s); }
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("\n--{0}: {1}--\n", i, this[i].GetType().Name) + this[i].Value; return s; } }
            #endregion
        }
        #endregion

        public class MaterialBlock : AHandlerElement,
            IComparable<MaterialBlock>, IEqualityComparer<MaterialBlock>, IEquatable<MaterialBlock>
        {
            #region Attributes
            byte xmlindex;
            TypeCode01 unknown1 = null;
            TypeCode01 unknown2 = null;
            TypeCodeList tcList = null;
            MaterialBlockList mbList = null;
            #endregion

            #region Constructors
            internal MaterialBlock(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }

            public MaterialBlock(int APIversion, EventHandler handler, MaterialBlock basis)
                : base(APIversion, handler)
            {
                this.handler = basis.handler;
                this.xmlindex = basis.xmlindex;
                this.unknown1 = (TypeCode01)basis.unknown1.Clone(handler);
                this.unknown2 = (TypeCode01)basis.unknown2.Clone(handler);
                tcList = new TypeCodeList(handler, basis.tcList);
                mbList = new MaterialBlockList(handler, basis.mbList);
            }

            public MaterialBlock(int APIversion, EventHandler handler, byte xmlindex, TypeCode01 unknown1, TypeCode01 unknown2,
                IList<TypeCode> ltc, IList<MaterialBlock> lmb)
                : base(APIversion, handler)
            {
                this.handler = handler;
                this.xmlindex = xmlindex;
                this.unknown1 = (TypeCode01)unknown1.Clone(handler);
                this.unknown2 = (TypeCode01)unknown2.Clone(handler);
                tcList = new TypeCodeList(handler, ltc);
                mbList = new MaterialBlockList(handler, lmb);
            }
            #endregion

            #region Data I/O
            protected void Parse(Stream s)
            {
                this.xmlindex = (new BinaryReader(s)).ReadByte();
                this.unknown1 = new TypeCode01(requestedApiVersion, handler, s, null);
                this.unknown2 = new TypeCode01(requestedApiVersion, handler, s, null);
                this.tcList = new TypeCodeList(handler, s);
                this.mbList = new MaterialBlockList(handler, s);
            }

            public void UnParse(Stream s)
            {
                (new BinaryWriter(s)).Write(xmlindex);
                unknown1.UnParse(s);
                unknown2.UnParse(s);
                tcList.UnParse(s);
                mbList.UnParse(s);
            }
            #endregion

            #region IComparable<MaterialBlock> Members

            public int CompareTo(MaterialBlock other)
            {
                int res = xmlindex.CompareTo(other.xmlindex); if (res != 0) return res;
                res = unknown1.CompareTo(other.unknown1); if (res != 0) return res;
                res = unknown2.CompareTo(other.unknown2); if (res != 0) return res;
                res = tcList.Count.CompareTo(other.tcList.Count); if (res != 0) return res;
                for (int i = 0; i < tcList.Count; i++) { res = tcList[i].CompareTo(other.tcList[i]); if (res != 0) return res; }
                res = mbList.Count.CompareTo(other.mbList.Count); if (res != 0) return res;
                for (int i = 0; i < mbList.Count; i++) { res = mbList[i].CompareTo(other.mbList[i]); if (res != 0) return res; }
                return 0;
            }

            #endregion

            #region IEqualityComparer<MaterialBlock> Members

            public bool Equals(MaterialBlock x, MaterialBlock y) { return x.Equals(y); }

            public int GetHashCode(MaterialBlock obj) { return obj.GetHashCode(); }

            public override int GetHashCode()
            {
                int hc = xmlindex.GetHashCode() ^ unknown1.GetHashCode() ^ unknown2.GetHashCode();
                foreach (TypeCode tc in tcList) hc ^= tc.GetHashCode();
                foreach (MaterialBlock mb in mbList) hc ^= mb.GetHashCode();
                return hc;
            }

            #endregion

            #region IEquatable<MaterialBlock> Members

            public bool Equals(MaterialBlock other) { return this.CompareTo(other) == 0; }

            #endregion

            #region ICloneable Members

            public object Clone() { return new MaterialBlock(requestedApiVersion, handler, this); }

            #endregion

            #region AHandlerElement
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            public override AHandlerElement Clone(EventHandler handler) { return new MaterialBlock(requestedApiVersion, handler, this); }
            #endregion

            #region Content Fields
            public byte XMLIndex { get { return xmlindex; } set { if (xmlindex != value) { xmlindex = value; OnElementChanged(); } } }
            public TypeCode01 Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }
            public TypeCode01 Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnElementChanged(); } } }
            public TypeCodeList TypeCodes { get { return tcList; } set { if (tcList != (value as TypeCodeList)) { tcList = new TypeCodeList(handler, value); OnElementChanged(); } } }
            public MaterialBlockList MaterialBlocks { get { return mbList; } set { if (mbList != (value as MaterialBlockList)) { mbList = new MaterialBlockList(handler, value); OnElementChanged(); } } }

            public String Value
            {
                get
                {
                    string s = "";
                    foreach (string f in this.ContentFields)
                    {
                        if (f.Equals("Value")) continue;
                        TypedValue tv = this[f];
                        string h = String.Format("\n---------\n---------\n{0}: {1}\n---------\n", tv.Type.Name, f);
                        string t = "---------\n";
                        if (typeof(TypeCode01).IsAssignableFrom(tv.Type)) s += h + (tv.Value as TypeCode01).Value + t;
                        else if (typeof(TypeCodeList).IsAssignableFrom(tv.Type)) s += h + (tv.Value as TypeCodeList).Value + t;
                        else if (typeof(MaterialBlockList).IsAssignableFrom(tv.Type)) s += h + (tv.Value as MaterialBlockList).Value + t;
                        else s += string.Format("{0}: {1}\n", f, "" + tv);
                    }
                    return s;
                }
            }
            #endregion
        }

        public class MaterialBlockList : AResource.DependentList<MaterialBlock>
        {
            #region Constructors
            public MaterialBlockList(EventHandler handler) : base(handler) { }
            public MaterialBlockList(EventHandler handler, IList<MaterialBlock> lmb) : base(handler, lmb) { }
            internal MaterialBlockList(EventHandler handler, Stream s) : base(handler, s) { }
            #endregion

            #region Data I/O
            protected override MaterialBlock CreateElement(Stream s) { return new MaterialBlock(0, elementHandler, s); }
            protected override void WriteElement(Stream s, MaterialBlock element) { element.UnParse(s); }
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("\n--{0}--\n", i) + this[i].Value; return s; } }
            #endregion
        }

        public class Material : AHandlerElement,
            IComparable<Material>, IEqualityComparer<Material>, IEquatable<Material>
        {
            #region Attributes
            byte materialType;
            uint unknown1;
            ushort unknown2;
            MaterialBlock mb = null;
            TGIBlockList list = null;
            uint unknown3;
            #endregion

            #region Constructors
            internal Material(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public Material(int APIversion, EventHandler handler, Material basis)
                : base(APIversion, handler)
            {
                this.materialType = basis.materialType;
                this.unknown1 = basis.unknown1;
                this.unknown2 = basis.unknown2;
                this.mb = (MaterialBlock)basis.mb.Clone(handler);
                this.list = new TGIBlockList(handler, basis.list);
                this.unknown3 = basis.unknown3;
            }
            public Material(int APIversion, EventHandler handler, byte materialType, uint unknown1, ushort unknown2,
                MaterialBlock mb, IList<TGIBlock> ltgib, uint unknown3)
                : base(APIversion, handler)
            {
                this.materialType = materialType;
                this.unknown1 = unknown1;
                this.unknown2 = unknown2;
                this.mb = (MaterialBlock)mb.Clone(handler);
                this.list = new TGIBlockList(handler, ltgib);
                this.unknown3 = unknown3;
            }
            #endregion

            #region Data I/O
            protected virtual void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);

                materialType = r.ReadByte();
                if (materialType != 1) unknown1 = r.ReadUInt32();
                long oset = r.ReadUInt32() + s.Position;
                unknown2 = r.ReadUInt16();
                long tgiPosn = r.ReadUInt32() + s.Position;
                long tgiSize = r.ReadUInt32();

                mb = new MaterialBlock(requestedApiVersion, handler, s);

                list = new TGIBlockList(handler, s, tgiPosn, tgiSize);

                if (checking) if (oset != s.Position)
                        throw new InvalidDataException(String.Format("Position of final DWORD read: 0x{0:X8}, actual: 0x{1:X8}",
                            oset, s.Position));

                unknown3 = r.ReadUInt32();
            }

            public virtual void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                long pOset, ptgiO, pos;

                w.Write(materialType);
                if (materialType != 1) w.Write(unknown1);

                pOset = s.Position;
                w.Write((uint)0); // oset

                w.Write(unknown2);

                ptgiO = s.Position;
                w.Write((uint)0); // tgiOffset
                w.Write((uint)0); // tgiSize

                mb.UnParse(s);

                list.UnParse(s, ptgiO);

                pos = s.Position;
                s.Position = pOset;
                w.Write((uint)(pos - pOset - sizeof(uint)));

                s.Position = pos;
                w.Write(unknown3);
            }
            #endregion

            #region IComparable<Material> Members

            public int CompareTo(Material other)
            {
                int res = materialType.CompareTo(other.materialType); if (res != 0) return res;
                res = unknown2.CompareTo(other.unknown2); if (res != 0) return res;
                res = mb.CompareTo(other.mb); if (res != 0) return res;
                return unknown3.CompareTo(other.unknown3);
            }

            #endregion

            #region IEqualityComparer<Material> Members

            public bool Equals(Material x, Material y) { return x.Equals(y); }

            public int GetHashCode(Material obj) { return obj.GetHashCode(); }

            public override int GetHashCode() { return materialType.GetHashCode() ^ unknown2.GetHashCode() ^ mb.GetHashCode() ^ unknown3.GetHashCode(); }

            #endregion

            #region IEquatable<Material> Members

            public bool Equals(Material other) { return this.CompareTo(other) == 0; }

            #endregion

            #region ICloneable Members

            public object Clone() { return new Material(requestedApiVersion, handler, this); }

            #endregion

            #region AHandlerElement
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            public override AHandlerElement Clone(EventHandler handler) { return new Material(requestedApiVersion, handler, this); }
            #endregion

            #region Content Fields
            public byte MaterialType { get { return materialType; } set { if (materialType != value) { materialType = value; OnElementChanged(); } } }
            public uint Unknown1 { get { return unknown1; } set { if (materialType == 1) throw new InvalidOperationException(); if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }
            public ushort Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnElementChanged(); } } }
            public MaterialBlock MaterialBlock { get { return mb; } set { if (mb != value) { mb = value; OnElementChanged(); } } }
            public TGIBlockList TGIBlocks
            {
                get { return list; }
                set
                {
                    if (list != (value as TGIBlockList))
                    {
                        list = new TGIBlockList(handler, value);
                        OnElementChanged();
                    }
                }
            }
            public uint Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnElementChanged(); } } }

            public String Value
            {
                get
                {
                    string s = "";
                    foreach (string f in this.ContentFields)
                    {
                        if (f.Equals("Value")) continue;
                        TypedValue tv = this[f];
                        string h = String.Format("\n---------\n---------\n{0}: {1}\n---------\n", tv.Type.Name, f);
                        string t = "---------\n";
                        if (typeof(MaterialBlock).IsAssignableFrom(tv.Type)) s += h + (tv.Value as MaterialBlock).Value + t;
                        else if (typeof(TGIBlockList).IsAssignableFrom(tv.Type)) s += h + (tv.Value as TGIBlockList).Value + t;
                        else s += string.Format("{0}: {1}\n", f, "" + tv);
                    }
                    return s;
                }
            }
            #endregion
        }

        public class MaterialList : AResource.DependentList<Material>
        {
            #region Constructors
            internal MaterialList(EventHandler handler) : base(handler) { }
            internal MaterialList(EventHandler handler, Stream s) : base(handler, s) { }
            public MaterialList(EventHandler handler, IList<Material> lme) : base(handler, lme) { }
            #endregion

            #region Data I/O
            protected override Material CreateElement(Stream s) { return new Material(0, elementHandler, s); }
            protected override void WriteElement(Stream s, Material element) { element.UnParse(s); }
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("\n--{0}--\n", i) + this[i].Value; return s; } }
            #endregion
        }
        #endregion

        #region Content Fields
        public Common CommonBlock { get { return common; } set { if (common != value) { common = new Common(requestedApiVersion, OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        
        public virtual String Value
        {
            get
            {
                string s = "";
                foreach (string f in this.ContentFields)
                {
                    if (f.Equals("Value") || f.Equals("Stream") || f.Equals("AsBytes")) continue;
                    TypedValue tv = this[f];
                    string h = String.Format("\n---------\n---------\n{0}: {1}\n---------\n", tv.Type.Name, f);
                    string t = "---------\n";
                    if (typeof(MaterialList).IsAssignableFrom(tv.Type)) s += h + (tv.Value as MaterialList).Value + t;
                    else if (typeof(MaterialBlockList).IsAssignableFrom(tv.Type)) s += h + (tv.Value as MaterialBlockList).Value + t;
                    else if (typeof(TGIBlockList).IsAssignableFrom(tv.Type)) s += h + (tv.Value as TGIBlockList).Value + t;
                    else if (typeof(Common).IsAssignableFrom(tv.Type)) s += h + (tv.Value as Common).Value + t;
                    else if (typeof(TypeCode).IsAssignableFrom(tv.Type)) s += h + (tv.Value as TypeCode).Value + t;
                    else if (typeof(ObjectCatalogResource.MTDoorList).IsAssignableFrom(tv.Type)) s += h + (tv.Value as ObjectCatalogResource.MTDoorList).Value + t;
                    else s += string.Format("{0}: {1}\n", f, "" + tv);
                }
                return s;
            }
        }
        #endregion
    }

    /// <summary>
    /// A CatalogResource wrapper that contains a TGIBlockList
    /// </summary>
    public abstract class CatalogResourceTGIBlockList : CatalogResource
    {
        #region Attributes
        protected TGIBlockList list = null;
        #endregion

        #region Constructors
        public CatalogResourceTGIBlockList(int APIversion, Stream s) : base(APIversion, s) { }
        public CatalogResourceTGIBlockList(int APIversion, IList<TGIBlock> tgibl) : base(APIversion, null) { this.list = new TGIBlockList(OnResourceChanged, tgibl); }
        #endregion

        #region Data I/O
        protected void UnParse(Stream s, long pos)
        {
            if (list == null) list = new TGIBlockList(OnResourceChanged);
            list.UnParse(s, pos);
        }
        #endregion

        #region Content Fields
        public TGIBlockList TGIBlocks { get { return list; } set { if (list != value) { list = new TGIBlockList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        #endregion
    }

    /// <summary>
    /// ResourceHandler for CatalogResource wrapper
    /// </summary>
    public class CatalogResourceHandler : AResourceHandler
    {
        public CatalogResourceHandler()
        {
            this.Add(typeof(FenceCatalogResource), new List<string>(new string[] { "0x0418FE2A" }));
            this.Add(typeof(FireplaceCatalogResource), new List<string>(new string[] { "0x04F3CC01" }));
            this.Add(typeof(ObjectCatalogResource), new List<string>(new string[] { "0x319E4F1D" }));
            this.Add(typeof(ProxyProductCatalogResource), new List<string>(new string[] { "0x04AC5D93" }));
            this.Add(typeof(RailingCatalogResource), new List<string>(new string[] { "0x04C58103" }));
            this.Add(typeof(RoofStyleCatalogResource), new List<string>(new string[] { "0x91EDBD3E" }));
            this.Add(typeof(RoofPatternCatalogResource), new List<string>(new string[] { "0xF1EDBD86" }));
            this.Add(typeof(StairsCatalogResource), new List<string>(new string[] { "0x049CA4CD" }));
            this.Add(typeof(TerrainPaintBrushCatalogResource), new List<string>(new string[] { "0x04ED4BB2" }));
            this.Add(typeof(TerrainGeometryWaterBrushCatalogResource), new List<string>(new string[] { "0x04B30669", "0x060B390C" }));
            this.Add(typeof(WallFloorPatternCatalogResource), new List<string>(new string[] { "0x515CA4CD" }));
            this.Add(typeof(WallCatalogResource), new List<string>(new string[] { "0x9151E6BC" }));

        }
    }
}
