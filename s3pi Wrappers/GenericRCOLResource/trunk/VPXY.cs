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

namespace s3pi.GenericRCOLResource
{
    public class VPXY : ARCOLBlock
    {
        static bool checking = s3pi.Settings.Settings.Checking;

        #region Attributes
        uint tag = (uint)FOURCC("VPXY");
        uint version = 4;
        EntryList entryList;
        byte tc02 = 0x02;
        float[] boundingBox = new float[6];
        byte[] unused = new byte[4];
        byte modular;
        uint ftptIndex;
        AResource.TGIBlockList tgiBlockList;
        #endregion

        public VPXY(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler, s) { }
        public VPXY(int APIversion, EventHandler handler, VPXY basis)
            : base(APIversion, handler, null)
        {
            version = basis.version;
            entryList = new EntryList(OnRCOLChanged, basis.entryList);
            boundingBox = (float[])basis.boundingBox;
            unused = (byte[])basis.unused.Clone();
            modular = basis.modular;
            if (modular != 0)
                ftptIndex = basis.ftptIndex;
        }

        #region ARCOLBlock
        public override string Tag { get { return "VPXY"; } }

        public override uint ResourceType { get { return 0x736884F1; } }

        protected override void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);
            tag = r.ReadUInt32();
            if (checking) if (tag != (uint)FOURCC("VPXY"))
                    throw new InvalidDataException(String.Format("Invalid Tag read: '{0}'; expected: 'VPXY'; at 0x{1:X8}", FOURCC(tag), s.Position));
            version = r.ReadUInt32();
            if (checking) if (version != 4)
                    throw new InvalidDataException(String.Format("Invalid Version read: 0x{0:X8}; expected 0x00000004; at 0x{1:X8}", version, s.Position));

            long tgiPosn = r.ReadUInt32() + s.Position;
            long tgiSize = r.ReadUInt32();

            entryList = new EntryList(OnRCOLChanged, s);
            tc02 = r.ReadByte();
            if (checking) if (tc02 != 2)
                    throw new InvalidDataException(String.Format("Invalid TC02 read: 0x{0:X2}; expected 0x02; at 0x{1:X8}", tc02, s.Position));
            for (int i = 0; i < boundingBox.Length; i++) boundingBox[i] = r.ReadSingle();
            unused = r.ReadBytes(4);
            if (checking) if (unused.Length != 4)
                    throw new EndOfStreamException(String.Format("Unused: expected 4 bytes, read {0}.", unused.Length));
            modular = r.ReadByte();
            if (modular != 0)
                ftptIndex = r.ReadUInt32();
            else
                ftptIndex = 0;

            tgiBlockList = new AResource.TGIBlockList(OnRCOLChanged, s, tgiPosn, tgiSize);
        }

        public override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);

            w.Write(tag);
            w.Write(version);

            long pos = ms.Position;
            w.Write((uint)0); // tgiOffset
            w.Write((uint)0); // tgiSize

            if (entryList == null) entryList = new EntryList(OnRCOLChanged);
            entryList.UnParse(ms);

            w.Write(tc02);
            foreach (float f in boundingBox) w.Write(f);
            w.Write(unused);
            w.Write(modular);
            if (modular != 0)
                w.Write(ftptIndex);

            if (tgiBlockList == null) tgiBlockList = new AResource.TGIBlockList(OnRCOLChanged);
            tgiBlockList.UnParse(ms, pos);

            return ms;
        }

        public override AHandlerElement Clone(EventHandler handler) { return new VPXY(requestedApiVersion, handler, this); }
        #endregion

        #region Sub-types
        public class ElementUInt32 : AHandlerElement, IEquatable<ElementUInt32>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            UInt32 data;
            #endregion

            #region Constructors
            public ElementUInt32(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public ElementUInt32(int APIversion, EventHandler handler, UInt32 data) : base(APIversion, handler) { this.data = data; }
            #endregion

            #region Data I/O
            void Parse(Stream s) { data = new BinaryReader(s).ReadUInt32(); }

            internal void UnParse(Stream s) { new BinaryWriter(s).Write(data); }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            /// <summary>
            /// The list of available field names on this API object
            /// </summary>
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override AHandlerElement Clone(EventHandler handler) { return new ElementUInt32(requestedApiVersion, handler, data); }
            #endregion

            #region IEquatable<Entry> Members

            public bool Equals(ElementUInt32 other) { return this.data == other.data; }

            #endregion

            #region Content Fields
            public UInt32 Data { get { return data; } set { if (data != value) { data = value; if (handler != null) handler(this, EventArgs.Empty); } } }

            public string Value { get { return "Data: 0x" + data.ToString("X8"); } }
            #endregion
        }

        public class UintList : AResource.DependentList<ElementUInt32>
        {
            #region Constructors
            public UintList(EventHandler handler) : base(handler, 255) { }
            public UintList(EventHandler handler, Stream s) : base(handler, 255, s) { }
            public UintList(EventHandler handler, IList<ElementUInt32> ltgi) : base(handler, 255, ltgi) { }
            #endregion

            #region Data I/O
            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadByte(); }
            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((byte)count); }

            protected override ElementUInt32 CreateElement(Stream s) { return new ElementUInt32(0, elementHandler, s); }
            protected override void WriteElement(Stream s, ElementUInt32 element) { element.UnParse(s); }
            #endregion
        }

        public class Entry : AHandlerElement, IEquatable<Entry>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            byte entryType;
            // if entryType == 0x00:
            byte entryID;
            UintList tgiIndexes;
            // if entryType == 0x01:
            uint tgiIndex;
            #endregion

            #region Constructors
            public Entry(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public Entry(int APIversion, EventHandler handler, byte entryID, IList<ElementUInt32> ltgi)
                : base(APIversion, handler)
            {
                this.entryType = 0x00;
                this.entryID = entryID;
                this.tgiIndexes = new UintList(handler, ltgi);
            }
            public Entry(int APIversion, EventHandler handler, UInt32 tgi)
                : base(APIversion, handler)
            {
                this.entryType = 0x01;
                this.tgiIndex = tgi;
            }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                entryType = r.ReadByte();
                if (entryType == 0x00)
                {
                    entryID = r.ReadByte();
                    tgiIndexes = new UintList(handler, s);
                }
                else if (entryType == 0x01)
                {
                    tgiIndex = r.ReadUInt32();
                }
                else if (checking)
                    throw new InvalidDataException(String.Format("Unknown EntryType 0x{0:X2} at 0x{1:X8}", entryType, s.Position));
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(entryType);
                if (entryType == 0x00)
                {
                    w.Write(entryID);
                    if (tgiIndexes == null) tgiIndexes = new UintList(handler);
                    tgiIndexes.UnParse(s);
                }
                else if (entryType == 0x01)
                {
                    w.Write(tgiIndex);
                }
                else if (checking)
                    throw new InvalidOperationException(String.Format("Unknown EntryType 0x{0:X2}", entryType));
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            /// <summary>
            /// The list of available field names on this API object
            /// </summary>
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override AHandlerElement Clone(EventHandler handler)
            {
                return this.entryType == 0x00
                    ? new Entry(requestedApiVersion, handler, this.entryID, this.tgiIndexes)
                    : new Entry(requestedApiVersion, handler, this.tgiIndex);
            }
            #endregion

            #region IEquatable<Entry> Members

            public bool Equals(Entry other)
            {
                if (other.entryType == entryType)
                {
                    if (entryType == 0x00)
                    {
                        if (other.entryID == entryID)
                        {
                            if (other.tgiIndexes.Count == tgiIndexes.Count)
                            {
                                for (int i = 0; i < tgiIndexes.Count; i++) if (other.tgiIndexes[i] != tgiIndexes[i]) return false;
                                return true;
                            }
                            return false;
                        }
                    }
                    else if (entryType == 0x01) return other.tgiIndex == tgiIndex;
                }
                return false;
            }

            #endregion

            #region Content Fields
            public byte EntryType { get { return entryType; } set { if (entryType != value) { entryType = value; if (handler != null) handler(this, EventArgs.Empty); } } }
            public byte EntryID
            {
                get { return entryID; }
                set { if (entryType != 0x00) throw new InvalidOperationException(); if (entryID != value) { entryID = value; if (handler != null) handler(this, EventArgs.Empty); } }
            }
            public UintList TGIIndexes
            {
                get { return tgiIndexes; }
                set { if (entryType != 0x00) throw new InvalidOperationException(); if (tgiIndexes != value) { tgiIndexes = new UintList(handler, value); if (handler != null) handler(this, EventArgs.Empty); } }
            }
            public UInt32 TGIIndex
            {
                get { return tgiIndex; }
                set { if (entryType != 0x01) throw new InvalidOperationException(); if (tgiIndex != value) { tgiIndex = value; if (handler != null) handler(this, EventArgs.Empty); } }
            }

            public string Value
            {
                get
                {
                    string s = "";
                    s += "[EntryType: 0x" + entryType.ToString("X2") + "]";
                    if (entryType == 0x00)
                    {
                        s += "  [EntryID: 0x" + entryID.ToString("X2") + "]";
                        s += "\n    TGIIndexes:";
                        for (int i = 0; i < tgiIndexes.Count; i++) s += "  [" + i + "] 0x" + tgiIndexes[i].Data.ToString("X8") + "    ";
                    }
                    else if (entryType == 0x01)
                    {
                        s += "  [TGIIndex: 0x" + tgiIndex.ToString("X8") + "]";
                    }
                    else throw new InvalidOperationException();
                    return s;
                }
            }
            #endregion
        }

        public class EntryList : AResource.DependentList<Entry>
        {
            #region Constructors
            public EntryList(EventHandler handler) : base(handler, 255) { }
            public EntryList(EventHandler handler, Stream s) : base(handler, 255, s) { }
            public EntryList(EventHandler handler, IList<Entry> le) : base(handler, 255, le) { }
            #endregion

            #region Data I/O
            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadByte(); }
            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((byte)count); }

            protected override Entry CreateElement(Stream s) { return new Entry(0, elementHandler, s); }

            protected override void WriteElement(Stream s, Entry element) { element.UnParse(s); }
            #endregion
        }
        #endregion

        #region Content Fields
        public uint Version { get { return version; } set { if (version != value) { version = value; OnRCOLChanged(this, EventArgs.Empty); } } }
        public EntryList Entries { get { return entryList; } set { if (entryList != value) { entryList = new EntryList(handler, value); OnRCOLChanged(this, EventArgs.Empty); } } }
        //public byte TC02 { get { return tc02; } set { if (tc02 != value) { tc02 = value; OnRCOLChanged(this, EventArgs.Empty); } } }
        public float[] BoundingBox
        {
            get { return (float[])boundingBox.Clone(); }
            set
            {
                if (value.Length != this.boundingBox.Length) throw new ArgumentLengthException("BoundingBox", this.boundingBox.Length);
                if (!ArrayCompare(boundingBox, value)) { boundingBox = value == null ? null : (float[])value.Clone(); OnRCOLChanged(this, EventArgs.Empty); }
            }
        }
        public byte[] Unused
        {
            get { return (byte[])unused.Clone(); }
            set
            {
                if (value.Length != this.unused.Length) throw new ArgumentLengthException("Unused", this.unused.Length);
                if (!ArrayCompare(unused, value)) { unused = value == null ? null : (byte[])value.Clone(); OnRCOLChanged(this, EventArgs.Empty); }
            }
        }
        public bool Modular { get { return modular != 0; } set { if (Modular != value) { modular = (byte)(value ? 0x01 : 0x00); OnRCOLChanged(this, EventArgs.Empty); } } }
        public uint FTPTIndex
        {
            get { return ftptIndex; }
            set { if (modular == 0) throw new InvalidOperationException(); if (ftptIndex != value) { ftptIndex = value; OnRCOLChanged(this, EventArgs.Empty); } }
        }
        public AResource.TGIBlockList TGIBlocks
        {
            get { return tgiBlockList; }
            set { if (tgiBlockList != value) { tgiBlockList = new AResource.TGIBlockList(handler, value); OnRCOLChanged(this, EventArgs.Empty); } }
        }

        public string Value
        {
            get
            {
                string s = "";
                s += "Tag: 0x" + tag.ToString("X8");
                s += "\nVersion: 0x" + version.ToString("X8");

                s += "\n--\nEntry List:";
                for (int i = 0; i < entryList.Count; i++)
                    s += "\n[" + i + "]: " + entryList[i].Value;

                s += "\nTC02: 0x" + tc02.ToString("X2");
                s += "\nBoundingBox: " + this["BoundingBox"];
                s += "\nUnused: " + this["Unused"];
                s += "\nModular: " + modular;
                if (Modular)
                    s += "\n" + "FTPTIndex: 0x" + ftptIndex.ToString("X8");

                s += "\n--TGI Blocks:\n" + tgiBlockList.Value + "--";
                return s;
            }
        }
        #endregion
    }
}
