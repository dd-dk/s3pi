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
using System.IO;
using s3pi.Interfaces;

namespace CatalogResource
{
    public class ObjectCatalogResource : CatalogResourceTGIBlockList
    {
        #region Attributes
        uint unknown1;
        MaterialList materialList = null;
        uint unknown2;
        byte unknown3;
        uint unknown4;
        byte unknown5;
        byte unknown6;
        byte[] unknown7 = new byte[4];
        uint objkIndex;
        uint unknown8;
        uint unknown9;
        uint unknown10;
        uint unknown11;
        uint unknown12;
        MTDoorList mtDoorList = null;
        byte unknown13;
        uint diagonalIndex;
        uint hash;
        Boolset roomFlags = new Boolset((uint)0);
        Boolset functionCategoryFlags = new Boolset((uint)0);
        Boolset subCategoryFlags = new Boolset((ulong)0);
        Boolset subRoomFlags = new Boolset((ulong)0);
        Boolset buildCategoryFlags = new Boolset((uint)0);
        uint sinkDDSIndex;
        uint unknown14;
        string materialGrouping1 = "";
        string materialGrouping2 = "";
        uint[] unknown15 = new uint[13];
        uint nullTGIIndex;
        #endregion

        #region Constructors
        public ObjectCatalogResource(int APIversion, Stream s) : base(APIversion, s) { }
        public ObjectCatalogResource(int APIversion, Stream unused, ObjectCatalogResource basis)
            : base(APIversion, basis.list)
        {
            this.unknown1 = basis.unknown1;
            this.materialList = new MaterialList(OnResourceChanged, basis.materialList);
            this.common = new Common(requestedApiVersion, OnResourceChanged, basis.common);
            this.unknown2 = basis.unknown2;
            this.unknown3 = basis.unknown3;
            this.unknown4 = basis.unknown4;
            this.unknown5 = basis.unknown5;
            this.unknown6 = basis.unknown6;
            this.unknown7 = (byte[])basis.unknown7.Clone();
            this.objkIndex = basis.objkIndex;
            this.unknown8 = basis.unknown8;
            this.unknown9 = basis.unknown9;
            this.unknown10 = basis.unknown10;
            this.unknown11 = basis.unknown11;
            this.unknown12 = basis.unknown12;
            this.mtDoorList = new MTDoorList(OnResourceChanged, basis.mtDoorList);
            this.unknown13 = basis.unknown13;
            this.diagonalIndex = basis.diagonalIndex;
            this.hash = basis.hash;
            this.roomFlags = (uint)basis.roomFlags;
            this.functionCategoryFlags = (uint)basis.functionCategoryFlags;
            this.subCategoryFlags = (ulong)basis.subCategoryFlags;
            this.subRoomFlags = (ulong)basis.subRoomFlags;
            this.buildCategoryFlags = (uint)basis.buildCategoryFlags;
            this.sinkDDSIndex = basis.sinkDDSIndex;
            this.unknown14 = basis.unknown14;
            this.materialGrouping1 = basis.materialGrouping1;
            this.materialGrouping2 = basis.materialGrouping2;
            this.unknown15 = (uint[])basis.unknown15.Clone();
            this.nullTGIIndex = basis.nullTGIIndex;

            ApplyBoolsetChangedHandlers();
        }
        public ObjectCatalogResource(int APIversion,
            uint unknown1, IList<Material> materialList, Common common, uint unknown2, byte unknown3, uint unknown4,
            byte unknown5, byte unknown6, byte[] unknown7, uint objkIndex, uint unknown8, uint unknown9, uint unknown10,
            uint unknown11, uint unknown12, IList<MTDoor> mtDoorList, byte unknown13, uint diagonalIndex, uint hash,
            Boolset roomFlags, Boolset functionCategoryFlags, Boolset subCategoryFlags, Boolset subRoomFlags, Boolset buildCategoryFlags,
            uint sinkDDSIndex, uint unknown14, string materialGrouping1, string materialGrouping2, uint[] unknown15, uint nullTGIIndex,
            TGIBlockList ltgib)
            : base(APIversion, ltgib)
        {
            this.unknown1 = unknown1;
            this.materialList = new MaterialList(OnResourceChanged, materialList);
            this.common = new Common(requestedApiVersion, OnResourceChanged, common);
            this.unknown2 = unknown2;
            this.unknown3 = unknown3;
            this.unknown4 = unknown4;
            this.unknown5 = unknown5;
            this.unknown6 = unknown6;
            if (unknown7.Length != this.unknown7.Length) throw new ArgumentLengthException("unknown7", this.unknown7.Length);
            this.unknown7 = (byte[])unknown7.Clone();
            this.objkIndex = objkIndex;
            this.unknown8 = unknown8;
            this.unknown9 = unknown9;
            this.unknown10 = unknown10;
            this.unknown11 = unknown11;
            this.unknown12 = unknown12;
            this.mtDoorList = new MTDoorList(OnResourceChanged, mtDoorList);
            this.unknown13 = unknown13;
            this.diagonalIndex = diagonalIndex;
            this.hash = hash;
            if (roomFlags.Length != this.roomFlags.Length) throw new ArgumentLengthException("roomFlags", this.roomFlags.Length);
            this.roomFlags = (uint)roomFlags;
            if (functionCategoryFlags.Length != this.functionCategoryFlags.Length) throw new ArgumentLengthException("functionCategoryFlags", this.functionCategoryFlags.Length);
            this.functionCategoryFlags = (uint)functionCategoryFlags;
            if (subCategoryFlags.Length != this.subCategoryFlags.Length) throw new ArgumentLengthException("subCategoryFlags", this.subCategoryFlags.Length);
            this.subCategoryFlags = (ulong)subCategoryFlags;
            if (subRoomFlags.Length != this.subRoomFlags.Length) throw new ArgumentLengthException("subRoomFlags", this.subRoomFlags.Length);
            this.subRoomFlags = (ulong)subRoomFlags;
            if (buildCategoryFlags.Length != this.buildCategoryFlags.Length) throw new ArgumentLengthException("buildCategoryFlags", this.buildCategoryFlags.Length);
            this.buildCategoryFlags = (uint)buildCategoryFlags;
            this.sinkDDSIndex = sinkDDSIndex;
            this.unknown14 = unknown14;
            this.materialGrouping1 = materialGrouping1;
            this.materialGrouping2 = materialGrouping2;
            if (unknown15.Length != this.unknown15.Length) throw new ArgumentLengthException("unknown15", this.unknown15.Length);
            this.unknown15 = (uint[])unknown15.Clone();
            this.nullTGIIndex = nullTGIIndex;

            ApplyBoolsetChangedHandlers();
        }

        void ApplyBoolsetChangedHandlers()
        {
            roomFlags.BoolsetChanged += new EventHandler(BoolsetChangedHandler);
            functionCategoryFlags.BoolsetChanged += new EventHandler(BoolsetChangedHandler);
            subCategoryFlags.BoolsetChanged += new EventHandler(BoolsetChangedHandler);
            subRoomFlags.BoolsetChanged += new EventHandler(BoolsetChangedHandler);
            buildCategoryFlags.BoolsetChanged += new EventHandler(BoolsetChangedHandler);
        }

        void BoolsetChangedHandler(object sender, EventArgs e) { this.OnResourceChanged(this, e); }
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
            this.materialList = new MaterialList(OnResourceChanged, s);
            this.common = new Common(requestedApiVersion, OnResourceChanged, s);
            this.unknown2 = r.ReadUInt32();
            this.unknown3 = r.ReadByte();
            this.unknown4 = r.ReadUInt32();
            this.unknown5 = r.ReadByte();
            this.unknown6 = r.ReadByte();
            this.unknown7 = r.ReadBytes(4);
            if (checking) if (this.unknown7.Length != 4)
                    throw new InvalidDataException(String.Format("unknown7: read {0} bytes; expected 4 at 0x{1:X8}.", unknown7.Length, s.Position));
            this.objkIndex = r.ReadUInt32();
            this.unknown8 = r.ReadUInt32();
            this.unknown9 = r.ReadUInt32();
            this.unknown10 = r.ReadUInt32();
            this.unknown11 = r.ReadUInt32();
            this.unknown12 = r.ReadUInt32();
            this.mtDoorList = new MTDoorList(OnResourceChanged, s);
            this.unknown13 = r.ReadByte();
            this.diagonalIndex = r.ReadUInt32();
            this.hash = r.ReadUInt32();
            this.roomFlags = r.ReadUInt32();
            this.functionCategoryFlags = r.ReadUInt32();
            this.subCategoryFlags = r.ReadUInt64();
            this.subRoomFlags = r.ReadUInt64();
            this.buildCategoryFlags = r.ReadUInt32();
            this.sinkDDSIndex = r.ReadUInt32();
            this.unknown14 = r.ReadUInt32();
            //this.materialGrouping1 = System.Text.Encoding.BigEndianUnicode.GetString(r.ReadBytes(r.ReadByte()));
            this.materialGrouping1 = r2.ReadString();
            //this.materialGrouping2 = System.Text.Encoding.BigEndianUnicode.GetString(r.ReadBytes(r.ReadByte()));
            this.materialGrouping2 = r2.ReadString();
            for (int i = 0; i < this.unknown15.Length; i++) unknown15[i] = r.ReadUInt32();
            this.nullTGIIndex = r.ReadUInt32();

            list = new TGIBlockList(OnResourceChanged, s, tgiPosn, tgiSize);

            ApplyBoolsetChangedHandlers();
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
            if (materialList == null) materialList = new MaterialList(OnResourceChanged);
            materialList.UnParse(s);
            common.UnParse(s);
            w.Write(unknown2);
            w.Write(unknown3);
            w.Write(unknown4);
            w.Write(unknown5);
            w.Write(unknown6);
            w.Write(unknown7);
            w.Write(objkIndex);
            w.Write(unknown8);
            w.Write(unknown9);
            w.Write(unknown10);
            w.Write(unknown11);
            w.Write(unknown12);
            if (mtDoorList == null) mtDoorList = new MTDoorList(OnResourceChanged);
            mtDoorList.UnParse(s);
            w.Write(unknown13);
            w.Write(diagonalIndex);
            w.Write(hash);
            w.Write((uint)roomFlags);
            w.Write((uint)functionCategoryFlags);
            w.Write((ulong)subCategoryFlags);
            w.Write((ulong)subRoomFlags);
            w.Write((uint)buildCategoryFlags);
            w.Write(sinkDDSIndex);
            w.Write(unknown14);
            //w.Write((byte)(materialGrouping1.Length * 2));
            //w.Write(System.Text.Encoding.BigEndianUnicode.GetBytes(materialGrouping1));
            Write7BitStr(s, materialGrouping1, System.Text.Encoding.BigEndianUnicode);
            //w.Write((byte)(materialGrouping2.Length * 2));
            //w.Write(System.Text.Encoding.BigEndianUnicode.GetBytes(materialGrouping2));
            Write7BitStr(s, materialGrouping2, System.Text.Encoding.BigEndianUnicode);
            for (int i = 0; i < this.unknown15.Length; i++) w.Write(unknown15[i]);
            w.Write(nullTGIIndex);

            base.UnParse(s, pos);

            w.Flush();

            return s;
        }
        #endregion

        #region Sub-classes
        public class MTDoor : AHandlerElement, IEquatable<MTDoor>
        {
            #region Attributes
            float[] unknown1 = new float[4];
            uint unknown2;
            uint wallMaskIndex;
            #endregion

            #region Constructors
            public MTDoor(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public MTDoor(int APIversion, EventHandler handler, MTDoor basis)
                : base(APIversion, handler) 
            {
                this.unknown1 = (float[])basis.unknown1.Clone();
                this.unknown2 = basis.unknown2;
                this.wallMaskIndex = basis.wallMaskIndex;
            }
            public MTDoor(int APIversion, EventHandler handler, float[] unknown1, uint unknownX, uint wallMaskIndex)
                : base(APIversion, handler) 
            {
                if (unknown1.Length != this.unknown1.Length) throw new ArgumentLengthException("unknown1", this.unknown1.Length);
                this.unknown1 = (float[])unknown1.Clone();
                this.unknown2 = unknownX;
                this.wallMaskIndex = wallMaskIndex;
            }
            #endregion

            #region Data I/O
            private void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.unknown1 = new float[4]; for (int i = 0; i < unknown1.Length; i++) unknown1[i] = r.ReadSingle();
                this.unknown2 = r.ReadUInt32();
                this.wallMaskIndex = r.ReadUInt32();
            }
            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                foreach (float f in unknown1) w.Write(f);
                w.Write(unknown2);
                w.Write(wallMaskIndex);
            }
            #endregion

            #region IEquatable<MTDoorEntry> Members

            public bool Equals(MTDoor other)
            {
                for (int i = 0; i < unknown1.Length; i++) if (unknown1[i] != other.unknown1[i]) return false;
                return (unknown2 == other.unknown2 && wallMaskIndex == other.wallMaskIndex);
            }

            #endregion

            #region AHandlerElement
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            public override AHandlerElement Clone(EventHandler handler) { return new MTDoor(requestedApiVersion, handler, this); }
            #endregion

            #region Content Fields
            public float[] Unknown1
            {
                get { return (float[])unknown1.Clone(); }
                set
                {
                    if (value.Length != unknown1.Length) throw new ArgumentLengthException("unknown1", this.unknown1.Length);
                    if (!ArrayCompare(unknown1, value)) { unknown1 = (float[])value.Clone(); OnElementChanged(); }
                }
            }
            public uint Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnElementChanged(); } } }
            public uint WallMaskIndex { get { return wallMaskIndex; } set { if (wallMaskIndex != value) { wallMaskIndex = value; OnElementChanged(); } } }

            public String Value
            {
                get
                {
                    string s = "";
                    foreach (string f in this.ContentFields)
                    {
                        if (f.Equals("Value")) continue;
                        if (f.Equals("Unknown1")) { for (int i = 0; i < unknown1.Length; i++)s += string.Format("{0}[{1}]: {2}\n", f, "" + i, "" + unknown1[i]); }
                        else s += String.Format("{0}: {1}\n", f, "" + this[f]);
                    }
                    return s;
                }
            }
            #endregion
        }

        public class MTDoorList : AResource.DependentList<MTDoor>
        {
            #region Constructors
            public MTDoorList(EventHandler handler) : base(handler, 256) { }
            public MTDoorList(EventHandler handler, IList<MTDoor> mtDoorList) : base(handler, 256, mtDoorList) { }
            public MTDoorList(EventHandler handler, Stream s) : base(handler, 256, s) { }
            #endregion

            #region Data I/O
            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadByte(); }
            protected override MTDoor CreateElement(Stream s) { return new MTDoor(0, elementHandler, s); }
            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((byte)count); }
            protected override void WriteElement(Stream s, MTDoor element) { element.UnParse(s); }
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("\n--{0}--\n", i) + this[i].Value; return s; } }
            #endregion
        }
        #endregion

        #region Content Fields
        public uint Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, new EventArgs()); } } }
        public MaterialList Materials { get { return materialList; } set { if (materialList != value) { materialList = value == null ? null : new MaterialList(OnResourceChanged, value); } OnResourceChanged(this, new EventArgs()); } }
        public uint Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown4 { get { return unknown4; } set { if (unknown4 != value) { unknown4 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown5 { get { return unknown5; } set { if (unknown5 != value) { unknown5 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown6 { get { return unknown6; } set { if (unknown6 != value) { unknown6 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte[] Unknown7
        {
            get { return (byte[])unknown7.Clone(); } // because "byte" doesn't have a parent or a Changed event
            set
            {
                if (value.Length != this.unknown7.Length) throw new ArgumentLengthException("Unknown7", this.unknown7.Length);
                if (!ArrayCompare(unknown7, value)) { unknown7 = value == null ? null : (byte[])value.Clone(); OnResourceChanged(this, new EventArgs()); }
            }
        }
        public uint OBJKIndex { get { return objkIndex; } set { if (objkIndex != value) { objkIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown8 { get { return unknown8; } set { if (unknown8 != value) { unknown8 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown9 { get { return unknown9; } set { if (unknown9 != value) { unknown9 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown10 { get { return unknown10; } set { if (unknown10 != value) { unknown10 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown11 { get { return unknown11; } set { if (unknown11 != value) { unknown11 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown12 { get { return unknown12; } set { if (unknown12 != value) { unknown12 = value; OnResourceChanged(this, new EventArgs()); } } }
        public MTDoorList MTDoors { get { return mtDoorList; } set { if (mtDoorList != value) { mtDoorList = value == null ? null : new MTDoorList(OnResourceChanged, value); } OnResourceChanged(this, new EventArgs()); } }
        public byte Unknown13 { get { return unknown13; } set { if (unknown13 != value) { unknown13 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint DiagonalIndex { get { return diagonalIndex; } set { if (diagonalIndex != value) { diagonalIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Hash { get { return hash; } set { if (hash != value) { hash = value; OnResourceChanged(this, new EventArgs()); } } }
        public Boolset RoomFlags
        {
            get { return roomFlags; }
            set
            {
                if (value.Length != this.roomFlags.Length) throw new ArgumentLengthException("RoomFlags", this.roomFlags.Length);
                if (roomFlags != value) { roomFlags = value; OnResourceChanged(this, new EventArgs()); }
            }
        }
        public Boolset FunctionCategoryFlags
        {
            get { return functionCategoryFlags; }
            set
            {
                if (value.Length != this.functionCategoryFlags.Length) throw new ArgumentLengthException("FunctionCategoryFlags", this.functionCategoryFlags.Length);
                if (functionCategoryFlags != value) { functionCategoryFlags = value; OnResourceChanged(this, new EventArgs()); }
            }
        }
        public Boolset SubCategoryFlags
        {
            get { return subCategoryFlags; }
            set
            {
                if (value.Length != this.subCategoryFlags.Length) throw new ArgumentLengthException("SubCategoryFlags", this.subCategoryFlags.Length);
                if (subCategoryFlags != value) { subCategoryFlags = value; OnResourceChanged(this, new EventArgs()); }
            }
        }
        public Boolset SubRoomFlags
        {
            get { return subRoomFlags; }
            set
            {
                if (value.Length != this.subRoomFlags.Length) throw new ArgumentLengthException("SubRoomFlags", this.subRoomFlags.Length);
                if (subRoomFlags != value) { subRoomFlags = value; OnResourceChanged(this, new EventArgs()); }
            }
        }
        public Boolset BuildCategoryFlags
        {
            get { return buildCategoryFlags; }
            set
            {
                if (value.Length != this.buildCategoryFlags.Length) throw new ArgumentLengthException("BuildCategoryFlags", this.buildCategoryFlags.Length);
                if (buildCategoryFlags != value) { buildCategoryFlags = value; OnResourceChanged(this, new EventArgs()); }
            }
        }
        public uint SinkDDSIndex { get { return sinkDDSIndex; } set { if (sinkDDSIndex != value) { sinkDDSIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown14 { get { return unknown14; } set { if (unknown14 != value) { unknown14 = value; OnResourceChanged(this, new EventArgs()); } } }
        public string MaterialGrouping1 { get { return materialGrouping1; } set { if (materialGrouping1 != value) { materialGrouping1 = value; OnResourceChanged(this, new EventArgs()); } } }
        public string MaterialGrouping2 { get { return materialGrouping2; } set { if (materialGrouping2 != value) { materialGrouping2 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint[] Unknown15
        {
            get { return (uint[])unknown15.Clone(); }
            set
            {
                if (value.Length != this.unknown15.Length) throw new ArgumentLengthException("Unknown15", this.unknown15.Length);
                if (!ArrayCompare(unknown15, value)) { unknown15 = (uint[])value.Clone(); OnResourceChanged(this, new EventArgs()); }
            }
        }
        public uint NullTGIIndex { get { return nullTGIIndex; } set { if (nullTGIIndex != value) { nullTGIIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        #endregion
    }
}
