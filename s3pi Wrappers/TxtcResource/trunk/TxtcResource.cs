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

namespace TxtcResource
{
    /// <summary>
    /// A resource wrapper that understands Texture Compositor resources
    /// </summary>
    public class TxtcResource : AResourceX
    {
        const int recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

        static bool checking = s3pi.Settings.Settings.Checking;

        #region Attributes
        uint version;
        SuperBlockList superBlocks;
        uint unknown1;
        uint unknown2;
        byte unknown3;
        byte unknown4;
        EntryBlockList entries;
        CountedTGIBlockList tgiBlocks;
        #endregion

        public TxtcResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); dirty = true; } stream.Position = 0; Parse(stream); }

        #region Data I/O
        void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);

            version = r.ReadUInt32();
            long tgiPos = r.ReadUInt32() + s.Position;
            if (version >= 7)
                superBlocks = new SuperBlockList(OnResourceChanged, s);
            unknown1 = r.ReadUInt32();
            unknown2 = r.ReadUInt32();
            unknown3 = r.ReadByte();
            uint count = r.ReadUInt32();
            if (version >= 8)
                unknown4 = r.ReadByte();
            entries = new EntryBlockList(OnResourceChanged, count, s);
            if (checking) if (tgiPos != s.Position)
                    throw new InvalidDataException(string.Format("TGI Block found at 0x{0:X8}; expected position 0x{1:X8}", s.Position, tgiPos));
            tgiBlocks = new CountedTGIBlockList(OnResourceChanged, 255, "IGT", r.ReadByte(), s);
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);

            w.Write(version);
            long osetPos = ms.Position;
            w.Write((uint)0);
            if (version >= 7)
            {
                if (superBlocks == null) superBlocks = new SuperBlockList(OnResourceChanged);
                superBlocks.UnParse(ms);
            }
            w.Write(unknown1);
            w.Write(unknown2);
            w.Write(unknown3);
            if (entries == null) entries = new EntryBlockList(OnResourceChanged);
            w.Write(entries.Count);
            if (version >= 8)
                w.Write(unknown4);
            entries.UnParse(ms);
            if (tgiBlocks == null) tgiBlocks = new CountedTGIBlockList(OnResourceChanged, 255, "IGT");
            long tgiPosn = ms.Position;
            w.Write((byte)tgiBlocks.Count);
            tgiBlocks.UnParse(ms);

            ms.Position = osetPos;
            w.Write((uint)(tgiPosn - osetPos - sizeof(uint)));
            ms.Position = ms.Length;

            return ms;
        }
        #endregion

        #region Sub-classes
        public class SuperBlock : AHandlerElement, IEquatable<SuperBlock>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            byte id;
            uint unknown1;
            byte[] unknown2 = new byte[10];
            byte unknown3;
            EntryBlockList entries;
            uint unknown4;
            #endregion

            #region Constructors
            public SuperBlock(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public SuperBlock(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public SuperBlock(int APIversion, EventHandler handler, SuperBlock basis)
                : this(APIversion, handler,
                basis.id, basis.unknown1, basis.unknown2, basis.unknown3, basis.unknown4)
            {
                this.entries = new EntryBlockList(handler, basis.entries);
            }
            public SuperBlock(int APIversion, EventHandler handler, byte id, uint unknown1, byte[] unknown2, byte unknown3, uint unknown4)
                : base(APIversion, handler)
            {
                this.id = id;
                this.unknown1 = unknown1;
                this.unknown2 = (byte[])unknown2.Clone();
                this.unknown3 = unknown3;
                this.entries = new EntryBlockList(handler);
                this.unknown4 = unknown4;
            }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                id = r.ReadByte();
                long chain = r.ReadUInt32() + s.Position;
                unknown1 = r.ReadUInt32();
                long offset = r.ReadUInt32() + s.Position;
                unknown2 = r.ReadBytes(10);
                if (checking) if (unknown2.Length != 10)
                        throw new EndOfStreamException(String.Format("Expected 10 bytes, read {0} at 0x{1:X8}", unknown2.Length, s.Position));
                uint count = r.ReadUInt32();
                unknown3 = r.ReadByte();
                entries = new EntryBlockList(handler, count, s);
                if (checking) if (offset != s.Position)
                        throw new InvalidDataException(String.Format("Expected position of final DWORD 0x{0:X8}, actual position 0x{1:X8}", offset, s.Position));
                unknown4 = r.ReadUInt32();
                if (checking) if (chain != s.Position)
                        throw new InvalidDataException(String.Format("Expected position of next SuperBlock 0x{0:X8}, actual position 0x{1:X8}", chain, s.Position));
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(id);
                long chainPos = s.Position;
                w.Write((uint)0);//chain
                w.Write(unknown1);
                long osetPos = s.Position;
                w.Write((uint)0);//offset
                w.Write(unknown2);
                if (entries == null) entries = new EntryBlockList(handler);
                w.Write(entries.Count);
                w.Write(unknown3);
                entries.UnParse(s);
                long dwPos = s.Position;
                w.Write(unknown4);
                long nsbPos = s.Position;


                s.Position = chainPos;
                w.Write((uint)(nsbPos - chainPos - sizeof(uint)));
                s.Position = osetPos;
                w.Write((uint)(dwPos - osetPos - sizeof(uint)));
                s.Position = nsbPos;
            }
            #endregion

            #region AHandlerElement Members
            public override AHandlerElement Clone(EventHandler handler) { return new SuperBlock(requestedApiVersion, handler, this); }

            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            public override List<string> ContentFields { get { return AApiVersionedFields.GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<SuperBlock> Members

            public bool Equals(SuperBlock other)
            {
                return id == other.id
                    && unknown1 == other.unknown1
                    && ArrayCompare(unknown2, other.unknown2)
                    && unknown3 == other.unknown3
                    && entries == other.entries
                    && unknown4 == other.unknown4;
            }

            #endregion

            #region Content Fields
            public byte ID { get { return id; } set { if (id != value) { id = value; OnElementChanged(); } } }
            public uint Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }
            public byte[] Unknown2 { get { return (byte[])unknown2.Clone(); } set { if (!ArrayCompare(unknown2, value)) { unknown2 = (byte[])value.Clone(); OnElementChanged(); } } }
            public byte Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnElementChanged(); } } }
            public EntryBlockList Entries { get { return entries; } set { if (entries != value) { entries = new EntryBlockList(handler, value); OnElementChanged(); } } }
            public uint Unknown4 { get { return unknown4; } set { if (unknown4 != value) { unknown4 = value; OnElementChanged(); } } }

            public string Value
            {
                get
                {
                    string s = "";
                    s += "ID: 0x" + id.ToString("X2");
                    s += "\nUnknown1: 0x" + unknown1.ToString("X8");
                    s += "\nUnknown2: " + this["Unknown2"];
                    s += "\nUnknown3: 0x" + unknown3.ToString("X2");
                    s += "\nEntries:";
                    for (int i = 0; i < entries.Count; i++)
                        for (int j = 0; j < entries[i].Entries.Count; j++)
                            s += "\n[" + i + "][" + j + "] " + entries[i].Entries[j].Value;
                    s += "\nUnknown4: 0x" + unknown4.ToString("X8");
                    return s;
                }
            }
            #endregion
        }

        public class SuperBlockList : AResource.DependentList<SuperBlock>
        {
            public SuperBlockList(EventHandler handler) : base(handler, 255) { }
            public SuperBlockList(EventHandler handler, Stream s) : base(handler, 255, s) { }
            public SuperBlockList(EventHandler handler, IList<SuperBlock> lsb) : base(handler, 255, lsb) { }

            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadByte(); }
            protected override SuperBlock CreateElement(Stream s) { return new SuperBlock(0, elementHandler, s); }

            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((byte)count); }
            protected override void WriteElement(Stream s, SuperBlock element) { element.UnParse(s); }

            public override void Add() { this.Add(new SuperBlock(0, elementHandler)); }
        }

        public abstract class Entry : AHandlerElement, IEquatable<Entry>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            protected uint property;
            protected byte unknown;
            protected byte dataType;
            #endregion

            #region Constructors
            public Entry(int APIversion, EventHandler handler, uint property, byte unknown, byte dataType)
                : base(APIversion, handler) { this.property = property; this.unknown = unknown; this.dataType = dataType; }

            public static Entry CreateEntry(int APIversion, EventHandler handler, Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                uint property = r.ReadUInt32();
                if (property == 0)
                    return new EntryNull(APIversion, handler);

                if (checking) if (!Enum.IsDefined(typeof(Properties), property))
                        throw new InvalidDataException(String.Format("Unexpected property ID 0x{0:X8} at 0x{1:X8}", property, s.Position));
                byte unknown = r.ReadByte();
                byte dataType = r.ReadByte();
                switch (dataType)
                {
                    //0x00, 0x01, 0x05, 0x0C
                    case 0x00:
                    case 0x01:
                    case 0x05:
                    case 0x0C:
                        return new EntryByte(APIversion, handler, property, unknown, dataType, r.ReadByte());
                    //0x02, 0x06
                    case 0x02:
                    case 0x06:
                        return new EntryUInt16(APIversion, handler, property, unknown, dataType, r.ReadUInt16());
                    //0x03, 0x07
                    case 0x03:
                    case 0x07:
                        return new EntryUInt32(APIversion, handler, property, unknown, dataType, r.ReadUInt32());
                    //0x04, 0x08
                    case 0x04:
                    case 0x08:
                        return new EntryUInt64(APIversion, handler, property, unknown, dataType, r.ReadUInt64());
                    //0x09
                    case 0x09:
                        return new EntrySingle(APIversion, handler, property, unknown, dataType, r.ReadSingle());
                    case 0x0A:
                    case 0x0B:
                        return new EntrySingleArray(APIversion, handler, property, unknown, dataType, r);
                    //0x0D
                    case 0x0D:
                        return new EntryString(APIversion, handler, property, unknown, dataType, new String(r.ReadChars(r.ReadUInt16())));
                    default:
                        if (checking)
                            throw new InvalidDataException(String.Format("Unsupported data type 0x{0:X2} at 0x{1:X8}", dataType, s.Position));
                        break;
                }
                return null;
            }
            #endregion

            #region Data I/O
            internal virtual void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(property);
                if (property == 0)
                    return;
                w.Write(unknown);
                w.Write(dataType);
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            public override List<string> ContentFields { get { return AApiVersionedFields.GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<Entry> Members

            public bool Equals(Entry other)
            {
                MemoryStream thisMS = new MemoryStream();
                UnParse(thisMS);
                MemoryStream otherMS = new MemoryStream();
                other.UnParse(otherMS);
                return ArrayCompare(thisMS.ToArray(), otherMS.ToArray());
            }

            #endregion

            #region Content Fields
            public uint Property { get { return property; } set { if (property != value) { property = value; OnElementChanged(); } } }
            public byte Unknown { get { return unknown; } set { if (unknown != value) { unknown = value; OnElementChanged(); } } }
            public byte DataType { get { return dataType; } set { if (dataType != value) { dataType = value; OnElementChanged(); } } }

            public virtual string Value
            {
                get
                {
                    string s = "";
                    s += "Property: 0x" + property.ToString("X8") + (Enum.IsDefined(typeof(Properties), property) ? " (" + ((Properties)property) + ")" : "(undefined)");
                    s += "; Unknown: 0x" + unknown.ToString("X2");
                    s += "; DataType: 0x" + dataType.ToString("X2");
                    return s;
                }
            }
            #endregion
        }

        public class EntryNull : Entry
        {
            public EntryNull(int APIversion, EventHandler handler, EntryNull basis)
                : base(APIversion, handler, 0, 0, 0) { throw new NotImplementedException(); }
            public EntryNull(int APIversion, EventHandler handler)
                : base(APIversion, handler, 0, 0, 0) { }
            internal override void UnParse(Stream s) { throw new NotImplementedException(); }
            public override AHandlerElement Clone(EventHandler handler) { throw new NotImplementedException(); }
            public override string Value { get { throw new NotImplementedException(); } }
        }
        public class EntryByte : Entry
        {
            byte data;
            public EntryByte(int APIversion, EventHandler handler, EntryByte basis)
                : this(APIversion, handler, basis.property, basis.unknown, basis.dataType, basis.data) { }
            public EntryByte(int APIversion, EventHandler handler, uint property, byte unknown, byte dataType, byte data)
                : base(APIversion, handler, property, unknown, dataType) { this.data = data; }
            internal override void UnParse(Stream s) { base.UnParse(s); new BinaryWriter(s).Write(data); }
            public override AHandlerElement Clone(EventHandler handler) { return new EntryByte(requestedApiVersion, handler, this); }
            public byte Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            public override string Value { get { return base.Value + "; Data: 0x" + data.ToString("X2"); } }
        }
        public class EntryUInt16 : Entry
        {
            UInt16 data;
            public EntryUInt16(int APIversion, EventHandler handler, EntryUInt16 basis)
                : this(APIversion, handler, basis.property, basis.unknown, basis.dataType, basis.data) { }
            public EntryUInt16(int APIversion, EventHandler handler, uint property, byte unknown, byte dataType, UInt16 data)
                : base(APIversion, handler, property, unknown, dataType) { this.data = data; }
            internal override void UnParse(Stream s) { base.UnParse(s); new BinaryWriter(s).Write(data); }
            public override AHandlerElement Clone(EventHandler handler) { return new EntryUInt16(requestedApiVersion, handler, this); }
            public UInt16 Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            public override string Value { get { return base.Value + "; Data: 0x" + data.ToString("X4"); } }
        }
        public class EntryUInt32 : Entry
        {
            UInt32 data;
            public EntryUInt32(int APIversion, EventHandler handler, EntryUInt32 basis)
                : this(APIversion, handler, basis.property, basis.unknown, basis.dataType, basis.data) { }
            public EntryUInt32(int APIversion, EventHandler handler, uint property, byte unknown, byte dataType, UInt32 data)
                : base(APIversion, handler, property, unknown, dataType) { this.data = data; }
            internal override void UnParse(Stream s) { base.UnParse(s); new BinaryWriter(s).Write(data); }
            public override AHandlerElement Clone(EventHandler handler) { return new EntryUInt32(requestedApiVersion, handler, this); }
            public UInt32 Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            public override string Value { get { return base.Value + "; Data: 0x" + data.ToString("X8"); } }
        }
        public class EntryUInt64 : Entry
        {
            UInt64 data;
            public EntryUInt64(int APIversion, EventHandler handler, EntryUInt64 basis)
                : this(APIversion, handler, basis.property, basis.unknown, basis.dataType, basis.data) { }
            public EntryUInt64(int APIversion, EventHandler handler, uint property, byte unknown, byte dataType, UInt64 data)
                : base(APIversion, handler, property, unknown, dataType) { this.data = data; }
            internal override void UnParse(Stream s) { base.UnParse(s); new BinaryWriter(s).Write(data); }
            public override AHandlerElement Clone(EventHandler handler) { return new EntryUInt64(requestedApiVersion, handler, this); }
            public UInt64 Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            public override string Value { get { return base.Value + "; Data: 0x" + data.ToString("X16"); } }
        }
        public class EntrySingle : Entry
        {
            Single data;
            public EntrySingle(int APIversion, EventHandler handler, EntrySingle basis)
                : this(APIversion, handler, basis.property, basis.unknown, basis.dataType, basis.data) { }
            public EntrySingle(int APIversion, EventHandler handler, uint property, byte unknown, byte dataType, Single data)
                : base(APIversion, handler, property, unknown, dataType) { this.data = data; }
            internal override void UnParse(Stream s) { base.UnParse(s); new BinaryWriter(s).Write(data); }
            public override AHandlerElement Clone(EventHandler handler) { return new EntrySingle(requestedApiVersion, handler, this); }
            public Single Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            public override string Value { get { return base.Value + "; Data: " + data.ToString(); } }
        }
        public class EntrySingleArray : Entry
        {
            Single[] data = new Single[4];
            public EntrySingleArray(int APIversion, EventHandler handler, uint property, byte unknown, byte dataType, BinaryReader r)
                : base(APIversion, handler, property, unknown, dataType) { for (int i = 0; i < data.Length; i++) data[i] = r.ReadSingle(); }
            public EntrySingleArray(int APIversion, EventHandler handler, EntrySingleArray basis)
                : this(APIversion, handler, basis.property, basis.unknown, basis.dataType, basis.data) { }
            public EntrySingleArray(int APIversion, EventHandler handler, uint property, byte unknown, byte dataType, Single[] data)
                : base(APIversion, handler, property, unknown, dataType) { if (data.Length != this.data.Length) throw new ArgumentLengthException(); this.data = (Single[])data.Clone(); }
            internal override void UnParse(Stream s) { base.UnParse(s); BinaryWriter w = new BinaryWriter(s); for (int i = 0; i < data.Length; i++) w.Write(data[i]); }
            public override AHandlerElement Clone(EventHandler handler) { return new EntrySingleArray(requestedApiVersion, handler, this); }
            public Single[] Data { get { return (Single[])data.Clone(); } set { if (value.Length != this.data.Length) throw new ArgumentLengthException(); if (!ArrayCompare(data, value)) { data = (Single[])value.Clone(); OnElementChanged(); } } }
            public override string Value { get { return base.Value + "; Data: " + (new TypedValue(data.GetType(), data, "X")); } }
        }
        public class EntryString : Entry
        {
            String data;
            public EntryString(int APIversion, EventHandler handler, EntryString basis)
                : this(APIversion, handler, basis.property, basis.unknown, basis.dataType, basis.data) { }
            public EntryString(int APIversion, EventHandler handler, uint property, byte unknown, byte dataType, String data)
                : base(APIversion, handler, property, unknown, dataType) { this.data = data; }
            internal override void UnParse(Stream s) { base.UnParse(s); BinaryWriter w = new BinaryWriter(s); w.Write((UInt16)data.Length); w.Write(data.ToCharArray()); }
            public override AHandlerElement Clone(EventHandler handler) { return new EntryString(requestedApiVersion, handler, this); }
            public String Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            public override string Value { get { return base.Value + "; Data: \"" + data + "\""; } }
        }

        public class EntryList : AResource.DependentList<Entry>
        {
            public EntryList(EventHandler handler) : base(handler) { }
            public EntryList(EventHandler handler, IList<Entry> le) : base(handler, le) { }

            protected override uint ReadCount(Stream s) { throw new InvalidOperationException(); }
            protected override Entry CreateElement(Stream s) { throw new InvalidOperationException(); }

            protected override void WriteCount(Stream s, uint count) { } // List owner must do this, if required
            protected override void WriteElement(Stream s, Entry element) { element.UnParse(s); }

            public override void Add() { throw new NotImplementedException(); }

            protected override Type GetElementType(params object[] fields)
            {
                if (fields.Length == 1 && typeof(Entry).IsAssignableFrom(fields[0].GetType())) return fields[0].GetType();

                uint property = (uint)fields[0];
                if (property == 0) return typeof(EntryNull);

                if (checking) if (!Enum.IsDefined(typeof(Properties), property))
                        throw new InvalidDataException(String.Format("Unexpected property ID 0x{0:X8}", property));

                switch ((byte)fields[2])
                {
                    //0x00, 0x01, 0x05, 0x0C
                    case 0x00:
                    case 0x01:
                    case 0x05:
                    case 0x0C:
                        return typeof(EntryByte);
                    //0x02, 0x06
                    case 0x02:
                    case 0x06:
                        return typeof(EntryUInt16);
                    //0x03, 0x07
                    case 0x03:
                    case 0x07:
                        return typeof(EntryUInt32);
                    //0x04, 0x08
                    case 0x04:
                    case 0x08:
                        return typeof(EntryUInt64);
                    //0x09
                    case 0x09:
                        return typeof(EntrySingle);
                    case 0x0A:
                    case 0x0B:
                        return typeof(EntrySingleArray);
                    //0x0D
                    case 0x0D:
                        return typeof(EntryString);
                }
                throw new InvalidDataException(String.Format("Unsupported data type 0x{0:X2}", (byte)fields[2]));
            }
        }

        public class EntryBlock : AHandlerElement, IEquatable<EntryBlock>
        {
            const int recommendedApiVersion = 1;

            EntryList theList;

            public EntryBlock(int APIversion, EventHandler handler) : base(APIversion, handler) { theList = new EntryList(handler); }
            public EntryBlock(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public EntryBlock(int APIversion, EventHandler handler, EntryBlock basis) : base(APIversion, handler) { theList = new EntryList(handler, basis.theList); }

            void Parse(Stream s)
            {
                theList = new EntryList(handler);
                for (Entry e = Entry.CreateEntry(requestedApiVersion, handler, s); e.Property != 0; e = Entry.CreateEntry(requestedApiVersion, handler, s)) theList.Add(e);
            }

            internal void UnParse(Stream s)
            {
                theList.UnParse(s);
                (new BinaryWriter(s)).Write((uint)0);
            }

            #region AHandlerElement Members
            public override AHandlerElement Clone(EventHandler handler) { return new EntryBlock(requestedApiVersion, handler, this); }

            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            public override List<string> ContentFields { get { return AApiVersionedFields.GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<EntryBlock> Members

            public bool Equals(EntryBlock other) { return theList.Equals(other.theList); }

            #endregion

            #region Content Fields
            public EntryList Entries { get { return theList; } set { if (theList != value) { theList = new EntryList(handler, value); OnElementChanged(); } } }
            #endregion
        }

        public class EntryBlockList : AResource.DependentList<EntryBlock>
        {
            uint blockCount;
            public EntryBlockList(EventHandler handler) : base(handler) { }
            public EntryBlockList(EventHandler handler, IList<EntryBlock> leb) : base(handler, leb) { }
            public EntryBlockList(EventHandler handler, uint blockCount, Stream s) : base(null) { elementHandler = handler; this.blockCount = blockCount; Parse(s); this.handler = handler; }

            protected override uint ReadCount(Stream s) { return blockCount; }
            protected override EntryBlock CreateElement(Stream s) { return new EntryBlock(0, elementHandler, s); }

            protected override void WriteCount(Stream s, uint count) { } // List owner must do this
            protected override void WriteElement(Stream s, EntryBlock element) { element.UnParse(s); }

            public override void Add() { this.Add(new EntryBlock(0, handler)); }
        }

        enum Properties : uint
        {
            DestinationBlend = 0x048F7567,
            SkipShaderModel = 0x06A775CE,
            MaskSource = 0x10DA0B6A,
            Width = 0x182E64EB,
            MaskSelect = 0x1F091259,
            MinShaderModel = 0x2EDF5F53,
            SkipDetailLevel = 0x331178DF,
            MaskBias = 0x3A3260E6,
            MaskKey = 0x49DE3B16,
            Rotation = 0x49F996DB,
            Height = 0x4C47D5C0,
            DefaultColour = 0x64399EC5,
            ID = 0x687720A6,
            Description = 0x6B7119C1,
            ImageSource = 0x8A7006DB,
            RenderTarget = 0xA2C91332,
            SourceRectangle = 0xA3AAFC98,
            MinDetailLevel = 0xAE5FE82A,
            Colour = 0xB01748DA,
            ColourWrite = 0xB07B3B93,
            HSVShift = 0xB67C2EF8,
            ChannelSelect = 0xD0E69002,
            UIVisible = 0xD92A4C8B,
            DefaultFabric = 0xDCFF6D7B,
            SourceBlend = 0xE055EE36,
            DestinationRectangle = 0xE1D6D01F,
            EnableFiltering = 0xE27FE962,
            ImageKey = 0xF6CC8471,
            EnableBlending = 0xFBF310C7,
        }
        #endregion

        #region Content Fields
        public uint Version { get { return version; } set { if (version != value) { version = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public SuperBlockList SuperBlocks { get { return superBlocks; } set { if (superBlocks != value) { superBlocks = new SuperBlockList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        public uint Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public uint Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public byte Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public byte Unknown4 { get { return unknown4; } set { if (unknown4 != value) { unknown4 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public EntryBlockList Entries { get { return entries; } set { if (entries != value) { entries = new EntryBlockList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        public CountedTGIBlockList TGIBlocks { get { return tgiBlocks; } set { if (tgiBlocks != value) { tgiBlocks = new CountedTGIBlockList(OnResourceChanged, "IGT", value); OnResourceChanged(this, EventArgs.Empty); } } }

        public string Value
        {
            get
            {
                string s = "";
                s += "Version: 0x" + version.ToString("X8");
                if (version >= 7)
                    for (int i = 0; i < superBlocks.Count; i++)
                    {
                        s += "\n--SuperBlock " + i + "--";
                        s += superBlocks[i].Value;
                        s += "\n----";
                    }
                s += "\nUnknown1: 0x" + unknown1.ToString("X8");
                s += "\nUnknown2: 0x" + unknown2.ToString("X8");
                s += "\nUnknown3: 0x" + unknown3.ToString("X2");
                if (version >= 8)
                    s += "\nUnknown3: 0x" + unknown3.ToString("X2");
                for (int i = 0; i < entries.Count; i++)
                    for (int j = 0; j < entries[i].Entries.Count; j++)
                        s += "\nEntries[" + i + "][" + j + "] " + entries[i].Entries[j].Value;
                s += "\nTGI Blocks:\n";
                s += tgiBlocks.Value;
                return s;
            }
        }
        #endregion
    }

    /// <summary>
    /// ResourceHandler for TxtcResource wrapper
    /// </summary>
    public class TxtcResourceHandler : AResourceHandler
    {
        public TxtcResourceHandler()
        {
            this.Add(typeof(TxtcResource), new List<string>(new string[] { "0x033A1435", }));
        }
    }
}
