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
    public class WallFloorPatternCatalogResource : CatalogResourceTGIBlockList
    {
        #region Attributes
        uint unknown1;
        WallFloorPatternMaterialList materialList = null;
        uint unknown2;
        byte unknown3;
        uint unknown4;
        byte unknown5;
        byte unknown6;
        uint unknown7;
        uint unknown8;
        uint vpxy_index1;
        uint unknown9;
        string unknown10 = "";
        byte[] unknown11 = new byte[8];
        #endregion

        #region Constructors
        public WallFloorPatternCatalogResource(int APIversion, Stream s) : base(APIversion, s) { }
        public WallFloorPatternCatalogResource(int APIversion, Stream unused, WallFloorPatternCatalogResource basis)
            : base(APIversion, basis.list)
        {
            this.unknown1 = basis.unknown1;
            this.materialList = new WallFloorPatternMaterialList(OnResourceChanged, basis.materialList);
            this.common = new Common(requestedApiVersion, OnResourceChanged, basis.common);
            this.unknown2 = basis.unknown2;
            this.unknown2 = basis.unknown2;
            this.unknown3 = basis.unknown3;
            this.unknown4 = basis.unknown4;
            this.unknown5 = basis.unknown5;
            this.unknown6 = basis.unknown6;
            this.unknown7 = basis.unknown7;
            this.unknown8 = basis.unknown8;
            this.vpxy_index1 = basis.vpxy_index1;
            this.unknown9 = basis.unknown9;
            this.unknown10 = basis.unknown10;
            this.unknown11 = (byte[])basis.unknown11.Clone();
        }
        public WallFloorPatternCatalogResource(int APIversion, uint unknown1, IList<WallFloorPatternMaterial> materialList, Common common,
            uint unknown2, byte unknown3, uint unknown4, byte unknown5, byte unknown6, uint unknown7, uint unknown8,
            uint index1, uint unknown9, string unknown10, byte[] unknown11,
            TGIBlockList ltgib)
            : base(APIversion, ltgib)
        {
            this.unknown1 = unknown1;
            this.materialList = new WallFloorPatternMaterialList(OnResourceChanged, materialList);
            this.common = new Common(requestedApiVersion, OnResourceChanged, common);
            this.unknown2 = unknown2;
            this.unknown3 = unknown3;
            this.unknown4 = unknown4;
            this.unknown5 = unknown5;
            this.unknown6 = unknown6;
            this.unknown7 = unknown7;
            this.unknown8 = unknown8;
            this.vpxy_index1 = index1;
            this.unknown9 = unknown9;
            this.unknown10 = unknown10;
            if (unknown11.Length != this.unknown11.Length) throw new ArgumentLengthException("unknown11", this.unknown11.Length);
            this.unknown11 = (byte[])unknown11.Clone();
        }
        #endregion

        #region Data I/O
        protected override void Parse(Stream s)
        {
            long tgiPosn, tgiSize;
            BinaryReader r = new BinaryReader(s);
            BinaryReader r2 = new BinaryReader(s, System.Text.Encoding.BigEndianUnicode);

            this.unknown1 = r.ReadUInt32();
            tgiPosn = r.ReadUInt32() + s.Position;
            tgiSize = r.ReadUInt32();
            this.materialList = new WallFloorPatternMaterialList(OnResourceChanged, s);
            this.common = new Common(requestedApiVersion, OnResourceChanged, s);
            this.unknown2 = r.ReadUInt32();
            this.unknown3 = r.ReadByte();
            this.unknown4 = r.ReadUInt32();
            this.unknown5 = r.ReadByte();
            this.unknown6 = r.ReadByte();
            this.unknown7 = r.ReadUInt32();
            this.unknown8 = r.ReadUInt32();
            this.vpxy_index1 = r.ReadUInt32();
            this.unknown9 = r.ReadUInt32();
            this.unknown10 = r2.ReadString();
            this.unknown11 = r.ReadBytes(8);
            if (checking) if (unknown11.Length != 8)
                    throw new InvalidDataException(String.Format("unknown11: read {0} bytes; expected 8 at 0x{1:X8}.", unknown11.Length, s.Position));

            list = new TGIBlockList(OnResourceChanged, s, tgiPosn, tgiSize);
        }

        protected override Stream UnParse()
        {
            long pos;
            MemoryStream s = new MemoryStream();
            BinaryWriter w = new BinaryWriter(s);

            w.Write(unknown1);
            pos = s.Position;
            w.Write((uint)0); // tgiOffset
            w.Write((uint)0); // tgiSize
            if (materialList == null) materialList = new WallFloorPatternMaterialList(OnResourceChanged);
            materialList.UnParse(s);
            common.UnParse(s);
            w.Write(unknown2);
            w.Write(unknown3);
            w.Write(unknown4);
            w.Write(unknown5);
            w.Write(unknown6);
            w.Write(unknown7);
            w.Write(unknown8);
            w.Write(vpxy_index1);
            w.Write(unknown9);
            Write7BitStr(s, unknown10, System.Text.Encoding.BigEndianUnicode);
            w.Write(unknown11);

            base.UnParse(s, pos);

            w.Flush();

            return s;
        }
        #endregion

        #region Sub-classes
        public class WallFloorPatternMaterial : Material,
            IComparable<WallFloorPatternMaterial>, IEqualityComparer<WallFloorPatternMaterial>, IEquatable<WallFloorPatternMaterial>
        {
            #region Attributes
            uint unknown4;
            #endregion

            #region Constructors
            internal WallFloorPatternMaterial(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler, s) { }
            public WallFloorPatternMaterial(int APIversion, EventHandler handler, WallFloorPatternMaterial basis)
                : base(APIversion, handler, basis) { this.unknown4 = basis.unknown4; }
            public WallFloorPatternMaterial(int APIversion, EventHandler handler, byte materialType, uint unknown1, ushort unknown2,
                MaterialBlock mb, IList<TGIBlock> ltgib, uint unknown3, uint unknown4)
                : base(APIversion, handler, materialType, unknown1, unknown2, mb, ltgib, unknown3) { this.unknown4 = unknown4; }
            #endregion

            #region Data I/O
            protected override void Parse(Stream s) { base.Parse(s); unknown4 = (new BinaryReader(s)).ReadUInt32(); }

            public override void UnParse(Stream s) { base.UnParse(s); (new BinaryWriter(s)).Write(unknown4); }
            #endregion

            #region IComparable<WallFloorPatternMaterial> Members

            public int CompareTo(WallFloorPatternMaterial other) { int res = base.CompareTo(other); if (res != 0) return res; return unknown4.CompareTo(other.unknown4); }

            #endregion

            #region IEqualityComparer<WallFloorPatternMaterial> Members

            public bool Equals(WallFloorPatternMaterial x, WallFloorPatternMaterial y) { return x.Equals(y); }

            public int GetHashCode(WallFloorPatternMaterial obj) { return obj.GetHashCode(); }

            public override int GetHashCode() { return base.GetHashCode() ^ unknown4.GetHashCode(); }

            #endregion

            #region IEquatable<WallFloorPatternMaterial> Members

            public bool Equals(WallFloorPatternMaterial other) { return this.CompareTo(other) == 0; }

            #endregion

            #region AApiVersionedFields
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region Content Fields
            public uint Unknown4 { get { return unknown4; } set { if (unknown4 != value) { unknown4 = value; OnElementChanged(); } } }
            #endregion
        }

        public class WallFloorPatternMaterialList : AResource.DependentList<WallFloorPatternMaterial>
        {
            #region Constructors
            internal WallFloorPatternMaterialList(EventHandler handler) : base(handler) { }
            internal WallFloorPatternMaterialList(EventHandler handler, Stream s) : base(handler, s) { }
            public WallFloorPatternMaterialList(EventHandler handler, IList<WallFloorPatternMaterial> lme) : base(handler, lme) { }
            #endregion

            #region Data I/O
            protected override WallFloorPatternMaterial CreateElement(Stream s) { return new WallFloorPatternMaterial(0, elementHandler, s); }
            protected override void WriteElement(Stream s, WallFloorPatternMaterial element) { element.UnParse(s); }
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("\n--{0}--\n", i) + this[i].Value; return s; } }
            #endregion
        }
        #endregion

        #region Content Fields
        public uint Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, new EventArgs()); } } }
        public IList<WallFloorPatternMaterial> Materials { get { return materialList; } set { if (materialList != value) { materialList = value == null ? null : new WallFloorPatternMaterialList(OnResourceChanged, value); } OnResourceChanged(this, new EventArgs()); } }
        public uint Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown4 { get { return unknown4; } set { if (unknown4 != value) { unknown4 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown5 { get { return unknown5; } set { if (unknown5 != value) { unknown5 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown6 { get { return unknown6; } set { if (unknown6 != value) { unknown6 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown7 { get { return unknown7; } set { if (unknown7 != value) { unknown7 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown8 { get { return unknown8; } set { if (unknown8 != value) { unknown8 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint VPXYIndex1 { get { return vpxy_index1; } set { if (vpxy_index1 != value) { vpxy_index1 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown9 { get { return unknown9; } set { if (unknown9 != value) { unknown9 = value; OnResourceChanged(this, new EventArgs()); } } }
        public string Unknown10
        {
            get { return unknown10; }
            set
            {
                if (value.Length > 255) throw new ArgumentLengthException("Unknown11 length must be <= 255 characters");
                if (unknown10 != value) { unknown10 = value; OnResourceChanged(this, new EventArgs()); }
            }
        }
        public byte[] Unknown11
        {
            get { return (byte[])unknown11.Clone(); }
            set
            {
                if (value.Length != this.unknown11.Length) throw new ArgumentLengthException("Unknown12", this.unknown11.Length);
                if (!ArrayCompare(unknown11, value)) { unknown11 = (byte[])value.Clone(); OnResourceChanged(this, new EventArgs()); }
            }
        }

        public override String Value
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
                    if (typeof(IList<WallFloorPatternMaterial>).IsAssignableFrom(tv.Type)) s += h + (tv.Value as WallFloorPatternMaterialList).Value + t;
                    else if (typeof(IList<MaterialBlock>).IsAssignableFrom(tv.Type)) s += h + (tv.Value as MaterialBlockList).Value + t;
                    else if (typeof(IList<TGIBlock>).IsAssignableFrom(tv.Type)) s += h + (tv.Value as TGIBlockList).Value + t;
                    else if (typeof(Common).IsAssignableFrom(tv.Type)) s += h + (tv.Value as Common).Value + t;
                    else if (typeof(TypeCode).IsAssignableFrom(tv.Type)) s += h + (tv.Value as TypeCode).Value + t;
                    else if (typeof(IList<ObjectCatalogResource.MTDoor>).IsAssignableFrom(tv.Type)) s += h + (tv.Value as ObjectCatalogResource.MTDoorList).Value + t;
                    else s += string.Format("{0}: {1}\n", f, "" + tv);
                }
                return s;
            }
        }
        #endregion
    }
}
