﻿/***************************************************************************
 *  Copyright (C) 2010 by Peter L Jones                                    *
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

namespace CASPartResource
{
    /// <summary>
    /// A resource wrapper that understands Sim Outfit resources
    /// </summary>
    public class SimOutfitResource : AResource
    {
        const int recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

        static bool checking = s3pi.Settings.Settings.Checking;

        #region Attributes
        uint version = 16;
        XMLEntryList xmlEntries;
        int unknown1;
        int unknown2;
        float unknown3;
        float unknown4;
        float unknown5;
        uint unknown6;
        UnknownFlags unknown7;
        UnknownFlags unknown8;
        UnknownFlags unknown9;
        UnknownFlags unknown10;
        byte skinToneIndex;
        float unknown11;
        UInt32 colour1;
        UInt32 colour2;
        UInt32 colour3;
        CASEntryList caspEntries;
        byte zero;
        FaceEntryList faceEntries;
        CountedTGIBlockList tgiBlocks;

        #endregion

        public SimOutfitResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); OnResourceChanged(this, new EventArgs()); } stream.Position = 0; Parse(stream); }

        #region Data I/O
        void Parse(Stream s)
        {
            int tgiPosn;

            BinaryReader r = new BinaryReader(s);

            version = r.ReadUInt32();
            tgiPosn = r.ReadInt32() + 8;
            xmlEntries = new XMLEntryList(OnResourceChanged, s);
            unknown1 = r.ReadInt32();
            unknown2 = r.ReadInt32();
            unknown3 = r.ReadSingle();
            unknown4 = r.ReadSingle();
            unknown5 = r.ReadSingle();
            unknown6 = r.ReadUInt32();
            unknown7 = (UnknownFlags)r.ReadUInt32();
            unknown8 = (UnknownFlags)r.ReadUInt32();
            unknown9 = (UnknownFlags)r.ReadUInt32();
            unknown10 = (UnknownFlags)r.ReadUInt32();
            skinToneIndex = r.ReadByte();
            unknown11 = r.ReadSingle();
            colour1 = r.ReadUInt32();
            colour2 = r.ReadUInt32();
            colour3 = r.ReadUInt32();
            caspEntries = new CASEntryList(OnResourceChanged, s);

            zero = r.ReadByte();
            if (checking) if (zero != 0)
                    throw new InvalidDataException(String.Format("Expected zero, read: 0x{0:X2}, at: 0x{1:X8}",
                        zero, s.Position));

            faceEntries = new FaceEntryList(OnResourceChanged, s);

            if (checking) if (tgiPosn != s.Position)
                    throw new InvalidDataException(String.Format("Position of TGIBlock read: 0x{0:X8}, actual: 0x{1:X8}",
                        tgiPosn, s.Position));

            byte count = r.ReadByte();
            tgiBlocks = new CountedTGIBlockList(OnResourceChanged, "IGT", count, s);
        }

        protected override Stream UnParse()
        {
            long posn, tgiPosn, end;
            MemoryStream s = new MemoryStream();
            BinaryWriter w = new BinaryWriter(s);

            w.Write(version);
            posn = s.Position;
            w.Write((int)0); //offset

            if (xmlEntries == null) xmlEntries = new XMLEntryList(OnResourceChanged); xmlEntries.UnParse(s);

            w.Write(unknown1);
            w.Write(unknown2);
            w.Write(unknown3);
            w.Write(unknown4);
            w.Write(unknown5);
            w.Write(unknown6);
            w.Write((uint)unknown7);
            w.Write((uint)unknown8);
            w.Write((uint)unknown9);
            w.Write((uint)unknown10);
            w.Write(skinToneIndex);
            w.Write(unknown11);

            w.Write(colour1);
            w.Write(colour2);
            w.Write(colour3);

            if (caspEntries == null) caspEntries = new CASEntryList(OnResourceChanged); caspEntries.UnParse(s);

            w.Write(zero);

            if (faceEntries == null) faceEntries = new FaceEntryList(OnResourceChanged); faceEntries.UnParse(s);

            tgiPosn = s.Position;
            if (tgiBlocks == null) tgiBlocks = new CountedTGIBlockList(OnResourceChanged, "IGT");
            w.Write((byte)tgiBlocks.Count);
            tgiBlocks.UnParse(s);

            end = s.Position;

            s.Position = posn;
            w.Write((int)(tgiPosn - posn - sizeof(int)));
            s.Position = end;

            s.Flush();
            return s;
        }
        #endregion

        #region Sub-types
        public class XMLEntry : AHandlerElement, IEquatable<XMLEntry>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            byte unknown1;
            string xml;
            #endregion

            #region Constructors
            public XMLEntry(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public XMLEntry(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public XMLEntry(int APIversion, EventHandler handler, XMLEntry basis) : this(APIversion, handler, basis.unknown1, basis.xml) { }
            public XMLEntry(int APIversion, EventHandler handler, byte unknown1, string xml) : base(APIversion, handler) { this.unknown1 = unknown1; this.xml = xml; }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                unknown1 = r.ReadByte();
                xml = System.Text.Encoding.Unicode.GetString(r.ReadBytes(r.ReadInt32() * 2));
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(unknown1);
                w.Write(xml.Length);
                w.Write(System.Text.Encoding.Unicode.GetBytes(xml));
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            public override AHandlerElement Clone(EventHandler handler) { return new XMLEntry(requestedApiVersion, handler, this); }
            #endregion

            #region IEquatable<XMLEntry> Members

            public bool Equals(XMLEntry other)
            {
                return
                    this.unknown1 == other.unknown1
                    && this.xml == other.xml
                    ;
            }

            #endregion

            #region Content Fields
            [ElementPriority(1)]
            public byte Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }
            //[ElementPriority(2)]
            //public string Xml { get { return xml; } set { if (xml != value) { xml = value; OnElementChanged(); } } }

            [ElementPriority(99)]
            public TextReader XmlFile
            {
                get { return new StringReader(xml); }
                set
                {
                    string temp = value.ReadToEnd();
                    if (xml != temp) { xml = temp; OnElementChanged(); }
                }
            }

            public string Value
            {
                get
                {
                    string s = "";
                    s += "Unknown1: " + this["Unknown1"];
                    s += "\nXml: " + (xml.Length > 160 ? xml.Substring(0, 157) + "..." : xml);
                    return s;
                }
            }
            #endregion
        }
        public class XMLEntryList : AResource.DependentList<XMLEntry>
        {
            #region Constructors
            public XMLEntryList(EventHandler handler) : base(handler) { }
            public XMLEntryList(EventHandler handler, Stream s) : base(handler, s) { }
            public XMLEntryList(EventHandler handler, IEnumerable<XMLEntry> le) : base(handler, le) { }
            #endregion

            #region Data I/O
            protected override XMLEntry CreateElement(Stream s) { return new XMLEntry(0, elementHandler, s); }
            protected override void WriteElement(Stream s, XMLEntry element) { element.UnParse(s); }
            #endregion

            public override void Add() { this.Add(new XMLEntry(0, null)); }
        }

        public class IndexPair : AHandlerElement, IEquatable<IndexPair>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            byte txtc1index;
            byte txtc2index;
            #endregion

            #region Constructors
            public IndexPair(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public IndexPair(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public IndexPair(int APIversion, EventHandler handler, IndexPair basis) : this(APIversion, handler, basis.txtc1index) { }
            public IndexPair(int APIversion, EventHandler handler, byte index) : base(APIversion, handler) { this.txtc1index = index; }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                txtc1index = r.ReadByte();
                txtc2index = r.ReadByte();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(txtc1index);
                w.Write(txtc2index);
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            public override AHandlerElement Clone(EventHandler handler) { return new IndexPair(requestedApiVersion, handler, this); }
            #endregion

            #region IEquatable<XMLEntry> Members

            public bool Equals(IndexPair other) { return this.txtc1index == other.txtc1index; }

            #endregion

            #region Content Fields
            [ElementPriority(1)]
            public byte TXTC1Index { get { return txtc1index; } set { if (txtc1index != value) { txtc1index = value; OnElementChanged(); } } }
            public byte TXTC2Index { get { return txtc2index; } set { if (txtc2index != value) { txtc2index = value; OnElementChanged(); } } }

            public string Value { get { return string.Format("{0}: {1}; {2}: {3}", "TXTC1Index", this["TXTC1Index"], "TXTC2Index", this["TXTC2Index"]); } }
            #endregion
        }
        public class IndexPairList : AResource.DependentList<IndexPair>
        {
            #region Constructors
            public IndexPairList(EventHandler handler) : base(handler) { }
            public IndexPairList(EventHandler handler, Stream s) : base(handler, s) { }
            public IndexPairList(EventHandler handler, IEnumerable<IndexPair> le) : base(handler, le) { }
            #endregion

            #region Data I/O
            protected override int ReadCount(Stream s) { return new BinaryReader(s).ReadByte(); }
            protected override void WriteCount(Stream s, int count) { new BinaryWriter(s).Write((byte)count); }
            protected override IndexPair CreateElement(Stream s) { return new IndexPair(0, elementHandler, s); }
            protected override void WriteElement(Stream s, IndexPair element) { element.UnParse(s); }
            #endregion

            public override void Add() { this.Add(new IndexPair(0, null)); }
        }

        public class CASEntry  : AHandlerElement, IEquatable<CASEntry>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            byte casPartIndex;
            ClothingType clothing;
            IndexPairList txtcIndexes;
            #endregion

            #region Constructors
            public CASEntry(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public CASEntry(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public CASEntry(int APIversion, EventHandler handler, CASEntry basis) : this(APIversion, handler, basis.casPartIndex, basis.clothing, basis.txtcIndexes) { }
            public CASEntry(int APIversion, EventHandler handler, byte casPartIndex, ClothingType clothing, IList<IndexPair> ibe)
                : base(APIversion, handler) { this.casPartIndex = casPartIndex; this.clothing = clothing; this.txtcIndexes = new IndexPairList(handler, ibe); }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                casPartIndex = r.ReadByte();
                clothing = (ClothingType)r.ReadUInt32();
                txtcIndexes = new IndexPairList(handler, s);
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(casPartIndex);
                w.Write((uint)clothing);
                txtcIndexes.UnParse(s);
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            public override AHandlerElement Clone(EventHandler handler) { return new CASEntry(requestedApiVersion, handler, this); }
            #endregion

            #region IEquatable<InnerEntry> Members

            public bool Equals(CASEntry other)
            {
                return
                    this.casPartIndex == other.casPartIndex
                    && this.clothing == other.clothing
                    && this.txtcIndexes.Equals(other.txtcIndexes)
                    ;
            }

            #endregion

            #region Content Fields
            [ElementPriority(1)]
            public byte CASPartIndex { get { return casPartIndex; } set { if (casPartIndex != value) { casPartIndex = value; OnElementChanged(); } } }
            [ElementPriority(2)]
            public ClothingType Clothing { get { return clothing; } set { if (clothing != value) { clothing = value; OnElementChanged(); } } }
            [ElementPriority(3)]
            public IndexPairList TXTCIndexes { get { return txtcIndexes; } set { if (!txtcIndexes.Equals(value)) { txtcIndexes = new IndexPairList(handler, value); OnElementChanged(); } } }

            public string Value
            {
                get
                {
                    string s = "";
                    foreach (string field in ContentFields)
                        if (field.Equals("Value")) continue;
                        else if (field.Equals("TXTCIndexes"))
                        {
                            s += "\n--\nTXTCIndexes";
                            string fmt = "\n" + "  [{0:X" + txtcIndexes.Count.ToString("X").Length + "}]: {1}";
                            for (int i = 0; i < txtcIndexes.Count; i++)
                                s += String.Format(fmt, i, txtcIndexes[i]["Value"]);
                            s += "\n----";
                        }
                        else
                            s += string.Format("{0}: {1}; ", field, this[field]);
                    return s.Trim();
                }
            }
            #endregion
        }
        public class CASEntryList : AResource.DependentList<CASEntry>
        {
            #region Constructors
            public CASEntryList(EventHandler handler) : base(handler, Byte.MaxValue) { }
            public CASEntryList(EventHandler handler, Stream s) : base(handler, s, Byte.MaxValue) { }
            public CASEntryList(EventHandler handler, IEnumerable<CASEntry> le) : base(handler, le, Byte.MaxValue) { }
            #endregion

            #region Data I/O
            protected override int ReadCount(Stream s) { return new BinaryReader(s).ReadByte(); }
            protected override void WriteCount(Stream s, int count) { new BinaryWriter(s).Write((byte)count); }
            protected override CASEntry CreateElement(Stream s) { return new CASEntry(0, elementHandler, s); }
            protected override void WriteElement(Stream s, CASEntry element) { element.UnParse(s); }
            #endregion

            public override void Add() { this.Add(new CASEntry(0, null)); }
        }

        public class FaceEntry : AHandlerElement, IEquatable<FaceEntry>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            byte faceIndex;
            float unknown1;
            #endregion

            #region Constructors
            public FaceEntry(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public FaceEntry(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public FaceEntry(int APIversion, EventHandler handler, FaceEntry basis)
                : this(APIversion, handler, basis.faceIndex, basis.unknown1) { }
            public FaceEntry(int APIversion, EventHandler handler, byte faceIndex, float unknown1)
                : base(APIversion, handler)
            {
                this.faceIndex = faceIndex;
                this.unknown1 = unknown1;
            }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                faceIndex = r.ReadByte();
                unknown1 = r.ReadSingle();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(faceIndex);
                w.Write(unknown1);
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override AHandlerElement Clone(EventHandler handler) { return new FaceEntry(requestedApiVersion, handler, this); }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<Entry> Members

            public bool Equals(FaceEntry other)
            {
                return
                    this.faceIndex == other.faceIndex
                    && this.unknown1 == other.unknown1
                    ;
            }

            #endregion

            #region Content Fields
            [ElementPriority(1)]
            public byte FaceIndex { get { return faceIndex; } set { if (faceIndex != value) { faceIndex = value; OnElementChanged(); } } }
            [ElementPriority(2)]
            public float Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }

            public string Value { get { return string.Format("FaceIndex: {0}; Unknown1: {1}", this["FaceIndex"], unknown1); } }
            #endregion
        }
        public class FaceEntryList : AResource.DependentList<FaceEntry>
        {
            #region Constructors
            public FaceEntryList(EventHandler handler) : base(handler, Byte.MaxValue) { }
            public FaceEntryList(EventHandler handler, Stream s) : base(handler, s, Byte.MaxValue) { }
            public FaceEntryList(EventHandler handler, IEnumerable<FaceEntry> le) : base(handler, le, Byte.MaxValue) { }
            #endregion

            #region Data I/O
            protected override int ReadCount(Stream s) { return new BinaryReader(s).ReadByte(); }
            protected override void WriteCount(Stream s, int count) { new BinaryWriter(s).Write((Byte)count); }
            protected override FaceEntry CreateElement(Stream s) { return new FaceEntry(0, elementHandler, s); }
            protected override void WriteElement(Stream s, FaceEntry element) { element.UnParse(s); }
            #endregion

            public override void Add() { this.Add(new FaceEntry(0, null)); }
        }
        #endregion

        #region Content Fields
        [ElementPriority(1)]
        public uint Version { get { return version; } set { if (version != value) { version = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(2)]
        public XMLEntryList XmlEntries { get { return xmlEntries; } set { if (xmlEntries.Equals(value)) { xmlEntries = new XMLEntryList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(3)]
        public int Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(4)]
        public int Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(5)]
        public float Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(6)]
        public float Unknown4 { get { return unknown4; } set { if (unknown4 != value) { unknown4 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(7)]
        public float Unknown5 { get { return unknown5; } set { if (unknown5 != value) { unknown5 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(8)]
        public uint Unknown6 { get { return unknown6; } set { if (unknown6 != value) { unknown6 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(9)]
        public UnknownFlags Unknown7 { get { return unknown7; } set { if (unknown7 != value) { unknown7 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(10)]
        public UnknownFlags Unknown8 { get { return unknown8; } set { if (unknown8 != value) { unknown8 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(11)]
        public UnknownFlags Unknown9 { get { return unknown9; } set { if (unknown9 != value) { unknown9 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(12)]
        public UnknownFlags Unknown10 { get { return unknown10; } set { if (unknown10 != value) { unknown10 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(13), TGIBlockListContentField("TGIBlocks")]
        public byte SkinToneIndex { get { return skinToneIndex; } set { if (skinToneIndex != value) { skinToneIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(14)]
        public float Unknown11 { get { return unknown11; } set { if (unknown11 != value) { unknown11 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(15)]
        public UInt32 Colour1 { get { return colour1; } set { if (!colour1.Equals(value)) { colour1 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(16)]
        public UInt32 Colour2 { get { return colour2; } set { if (!colour2.Equals(value)) { colour2 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(17)]
        public UInt32 Colour3 { get { return colour3; } set { if (!colour3.Equals(value)) { colour3 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(18)]
        public CASEntryList CASPEntries { get { return caspEntries; } set { if (caspEntries.Equals(value)) { caspEntries = new CASEntryList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(19)]
        public FaceEntryList FACEEntries { get { return faceEntries; } set { if (faceEntries.Equals(value)) { faceEntries = new FaceEntryList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(20)]
        public CountedTGIBlockList TGIBlocks { get { return tgiBlocks; } set { if (!tgiBlocks.Equals(value)) { tgiBlocks = new CountedTGIBlockList(OnResourceChanged, "IGT", value); OnResourceChanged(this, new EventArgs()); } } }

        public string Value
        {
            get
            {
                string s = "";

                foreach (string field in ContentFields)
                    if (field.Equals("Value") || field.Equals("AsBytes") || field.Equals("Stream")) continue;
                    else if (field.EndsWith("Index"))
                    {
                        s += String.Format("\n{0}: {1} ({2})", field, this[field], tgiBlocks[Convert.ToInt32(this[field].Value)]);
                    }
                    else if (field.Equals("XmlEntries"))
                    {
                        s += "\n--\nXmlEntries:";
                        for (int i = 0; i < xmlEntries.Count; i++)
                            s += String.Format("\n--{0}--\n{1}", i, xmlEntries[i].Value);
                        s += "\n----";
                    }
                    else if (field.Equals("CASPEntries"))
                    {
                        s += "\n--\nCASPEntries:";
                        for (int i = 0; i < caspEntries.Count; i++)
                            s += String.Format("\n--{0}--\n{1}", i, caspEntries[i].Value);
                        s += "\n----";
                    }
                    else if (field.Equals("FACEEntries"))
                    {
                        s += "\n--\nFACEEntries:";
                        string fmt = "\n" + "  [{0:X" + faceEntries.Count.ToString("X").Length + "}]: {1}";
                        for (int i = 0; i < faceEntries.Count; i++)
                            s += String.Format(fmt, i, faceEntries[i].Value);
                        s += "\n----";
                    }
                    else if (field.Equals("TGIBlocks"))
                    {
                        s += "\n--\nTGIBlocks:";
                        string fmt = "\n  [{0:X" + tgiBlocks.Count.ToString("X").Length + "}]: {1}";
                        for (int i = 0; i < tgiBlocks.Count; i++)
                            s += String.Format(fmt, i, tgiBlocks[i].Value);
                        s += "\n----";
                    }
                    else
                    {
                        s += String.Format("\n{0}: {1}", field, this[field]);
                    }

                return s;
            }
        }
        #endregion
    }

    /// <summary>
    /// ResourceHandler for SimOutfitResource wrapper
    /// </summary>
    public class SimOutfitResourceHandler : AResourceHandler
    {
        public SimOutfitResourceHandler()
        {
            this.Add(typeof(SimOutfitResource), new List<string>(new string[] { "0x025ED6F4", }));
        }
    }
}