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
using System.Text;

namespace CASPartResource
{
    /// <summary>
    /// A resource wrapper that understands CAS Part resources
    /// </summary>
    public class CASPartResource : AResource
    {
        const int recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

        static bool checking = s3pi.Settings.Settings.Checking;

        #region Attributes
        uint version = 18;
        PresetList presets;
        string unknown1 = "";
        float sortPriority;
        byte unknown2;
        ClothingType clothing;
        DataTypeFlags dataType;
        AgeGenderFlags ageGender;
        ClothingCategoryFlags clothingCategory;
        byte casPart1Index;
        byte casPart2Index;
        byte blendInfoFatIndex;
        byte blendInfoFitIndex;
        byte blendInfoThinIndex;
        byte blendInfoSpecialIndex;
        uint unknown3;
        ByteEntryList vpxyIndexes;
        LODInfoEntryList lodInfo;
        ByteEntryList diffuse1Indexes;
        ByteEntryList specular1Indexes;
        ByteEntryList diffuse2Indexes;
        ByteEntryList specular2Indexes;
        ByteEntryList bondIndexes;
        string unknown4 = "";
        CountedTGIBlockList tgiBlocks;

        #endregion

        public CASPartResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); OnResourceChanged(this, new EventArgs()); } stream.Position = 0; Parse(stream); }

        #region Data I/O
        void Parse(Stream s)
        {
            int tgiPosn;

            BinaryReader r = new BinaryReader(s);

            version = r.ReadUInt32();
            tgiPosn = r.ReadInt32() + 8;
            presets = new PresetList(OnResourceChanged, s);
            unknown1 = BigEndianUnicodeString.Read(s);
            sortPriority = r.ReadSingle();
            unknown2 = r.ReadByte();
            clothing = (ClothingType)r.ReadUInt32();
            dataType = (DataTypeFlags)r.ReadUInt32();
            ageGender = new AgeGenderFlags(0, OnResourceChanged, s);
            clothingCategory = (ClothingCategoryFlags)r.ReadUInt32();
            casPart1Index = r.ReadByte();
            casPart2Index = r.ReadByte();
            blendInfoFatIndex = r.ReadByte();
            blendInfoFitIndex = r.ReadByte();
            blendInfoThinIndex = r.ReadByte();
            blendInfoSpecialIndex = r.ReadByte();
            unknown3 = r.ReadUInt32();
            vpxyIndexes = new ByteEntryList(OnResourceChanged, s);
            lodInfo = new LODInfoEntryList(OnResourceChanged, s);
            diffuse1Indexes = new ByteEntryList(OnResourceChanged, s);
            specular1Indexes = new ByteEntryList(OnResourceChanged, s);
            diffuse2Indexes = new ByteEntryList(OnResourceChanged, s);
            specular2Indexes = new ByteEntryList(OnResourceChanged, s);
            bondIndexes = new ByteEntryList(OnResourceChanged, s);
            unknown4 = BigEndianUnicodeString.Read(s);

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

            if (presets == null) presets = new PresetList(OnResourceChanged);
            presets.UnParse(s);

            BigEndianUnicodeString.Write(s, unknown1);
            w.Write(sortPriority);
            w.Write(unknown2);
            w.Write((uint)clothing);
            w.Write((uint)dataType);
            if (ageGender == null) ageGender = new AgeGenderFlags(0, OnResourceChanged);
            ageGender.UnParse(s);
            w.Write((uint)clothingCategory);
            w.Write(casPart1Index);
            w.Write(casPart2Index);
            w.Write(blendInfoFatIndex);
            w.Write(blendInfoFitIndex);
            w.Write(blendInfoThinIndex);
            w.Write(blendInfoSpecialIndex);
            w.Write(unknown3);

            if (vpxyIndexes == null) vpxyIndexes = new ByteEntryList(OnResourceChanged); vpxyIndexes.UnParse(s);
            if (lodInfo == null) lodInfo = new LODInfoEntryList(OnResourceChanged); lodInfo.UnParse(s);
            if (diffuse1Indexes == null) diffuse1Indexes = new ByteEntryList(OnResourceChanged); diffuse1Indexes.UnParse(s);
            if (specular1Indexes == null) specular1Indexes = new ByteEntryList(OnResourceChanged); specular1Indexes.UnParse(s);
            if (diffuse2Indexes == null) diffuse2Indexes = new ByteEntryList(OnResourceChanged); diffuse2Indexes.UnParse(s);
            if (specular2Indexes == null) specular2Indexes = new ByteEntryList(OnResourceChanged); specular2Indexes.UnParse(s);
            if (bondIndexes == null) bondIndexes = new ByteEntryList(OnResourceChanged); bondIndexes.UnParse(s);
            BigEndianUnicodeString.Write(s, unknown4);

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
        public class Preset : AHandlerElement, IEquatable<Preset>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            string xml;
            uint unknown1;
            #endregion

            #region Constructors
            public Preset(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public Preset(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public Preset(int APIversion, EventHandler handler, Preset basis) : this(APIversion, handler, basis.xml, basis.unknown1) { }
            public Preset(int APIversion, EventHandler handler, string xml, uint unknown1) : base(APIversion, handler) { this.xml = xml; this.unknown1 = unknown1; }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                xml = System.Text.Encoding.Unicode.GetString(r.ReadBytes(r.ReadInt32() * 2));
                unknown1 = r.ReadUInt32();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(xml.Length);
                w.Write(System.Text.Encoding.Unicode.GetBytes(xml));
                w.Write(unknown1);
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            public override AHandlerElement Clone(EventHandler handler) { return new Preset(requestedApiVersion, handler, this); }
            #endregion

            #region IEquatable<Preset> Members

            public bool Equals(Preset other)
            {
                return
                    this.xml.Equals(other.xml)
                    && this.unknown1.Equals(other.unknown1)
                    ;
            }
            public override bool Equals(object obj)
            {
                return obj as Preset != null ? this.Equals(obj as Preset) : false;
            }
            public override int GetHashCode()
            {
                return
                    this.xml.GetHashCode()
                    ^ this.unknown1.GetHashCode()
                    ;
            }

            #endregion

            #region Content Fields
            //[ElementPriority(1)]
            //public string Xml { get { return xml; } set { if (xml != value) { xml = value; OnElementChanged(); } } }
            [ElementPriority(2)]
            public uint Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }

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
                    return "Xml: " + (xml.Length > 160 ? xml.Substring(0, 157) + "..." : xml) +
                        "\nUnknown1: " + this["Unknown1"];
                }
            }
            #endregion
        }
        public class PresetList : DependentList<Preset>
        {
            #region Constructors
            public PresetList(EventHandler handler) : base(handler) { }
            public PresetList(EventHandler handler, Stream s) : base(handler, s) { }
            public PresetList(EventHandler handler, IEnumerable<Preset> le) : base(handler, le) { }
            #endregion

            #region Data I/O
            protected override Preset CreateElement(Stream s) { return new Preset(0, elementHandler, s); }
            protected override void WriteElement(Stream s, Preset element) { element.UnParse(s); }
            #endregion

            public override void Add() { this.Add(new Preset(0, null)); }
        }

        public class ByteEntryList : SimpleList<byte>
        {
            #region Constructors
            public ByteEntryList(EventHandler handler) : base(handler, ReadByte, WriteByte, byte.MaxValue, ReadListCount, WriteListCount) { }
            public ByteEntryList(EventHandler handler, Stream s) : base(handler, s, ReadByte, WriteByte, byte.MaxValue, ReadListCount, WriteListCount) { }
            public ByteEntryList(EventHandler handler, IEnumerable<byte> le) : base(handler, le, ReadByte, WriteByte, byte.MaxValue, ReadListCount, WriteListCount) { }
            #endregion

            #region Data I/O
            static int ReadListCount(Stream s) { return new BinaryReader(s).ReadByte(); }
            static void WriteListCount(Stream s, int count) { new BinaryWriter(s).Write((byte)count); }
            static byte ReadByte(Stream s) { return new BinaryReader(s).ReadByte(); }
            static void WriteByte(Stream s, byte value) { new BinaryWriter(s).Write(value); }
            #endregion
        }

        public class LODInfoEntry : AHandlerElement, IEquatable<LODInfoEntry>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            byte level;
            DataTypeFlags destTexture;
            LODAssetList lodAssets;
            #endregion

            #region Constructors
            public LODInfoEntry(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public LODInfoEntry(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public LODInfoEntry(int APIversion, EventHandler handler, LODInfoEntry basis)
                : this(APIversion, handler, basis.level, basis.destTexture, basis.lodAssets) { }
            public LODInfoEntry(int APIversion, EventHandler handler, byte level, DataTypeFlags destTexture, IEnumerable<LODAsset> lodAssets)
                : base(APIversion, handler)
            {
                this.level = level;
                this.destTexture = destTexture;
                this.lodAssets = new LODAssetList(handler, lodAssets);
            }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                level = r.ReadByte();
                destTexture = (DataTypeFlags)r.ReadUInt32();
                lodAssets = new LODAssetList(handler, s);
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(level);
                w.Write((uint)destTexture);
                if (lodAssets == null) lodAssets = new LODAssetList(handler);
                lodAssets.UnParse(s);
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override AHandlerElement Clone(EventHandler handler) { return new LODInfoEntry(requestedApiVersion, handler, this); }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<LODInfoEntry> Members

            public bool Equals(LODInfoEntry other)
            {
                return
                    this.level == other.level
                    && this.destTexture == other.destTexture
                    && this.lodAssets.Equals(other.lodAssets)
                    ;
            }

            public override bool Equals(object obj)
            {
                return obj as LODInfoEntry != null ? this.Equals(obj as LODInfoEntry) : false;
            }

            public override int GetHashCode()
            {
                return
                    this.level.GetHashCode()
                    ^ this.destTexture.GetHashCode()
                    ^ this.lodAssets.GetHashCode()
                    ;
            }

            #endregion

            #region Content Fields
            [ElementPriority(1)]
            public byte Level { get { return level; } set { if (level != value) { level = value; OnElementChanged(); } } }
            [ElementPriority(2)]
            public DataTypeFlags DestTexture { get { return destTexture; } set { if (destTexture != value) { destTexture = value; OnElementChanged(); } } }
            [ElementPriority(3)]
            public LODAssetList LODAssets { get { return lodAssets; } set { if (!lodAssets.Equals(value)) { lodAssets = new LODAssetList(handler, value); OnElementChanged(); } } }

            public string Value
            {
                get
                {
                    return ValueBuilder;
                }
            }
            #endregion
        }
        public class LODInfoEntryList : DependentList<LODInfoEntry>
        {
            #region Constructors
            public LODInfoEntryList(EventHandler handler) : base(handler, Byte.MaxValue) { }
            public LODInfoEntryList(EventHandler handler, Stream s) : base(handler, s, Byte.MaxValue) { }
            public LODInfoEntryList(EventHandler handler, IEnumerable<LODInfoEntry> le) : base(handler, le, Byte.MaxValue) { }
            #endregion

            #region Data I/O
            protected override int ReadCount(Stream s) { return new BinaryReader(s).ReadByte(); }
            protected override void WriteCount(Stream s, int count) { new BinaryWriter(s).Write((byte)count); }
            protected override LODInfoEntry CreateElement(Stream s) { return new LODInfoEntry(0, elementHandler, s); }
            protected override void WriteElement(Stream s, LODInfoEntry element) { element.UnParse(s); }
            #endregion

            public override void Add() { this.Add(new LODInfoEntry(0, null)); }
        }

        public class LODAsset : AHandlerElement, IEquatable<LODAsset>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            CASGeomFlags sorting;
            CASGeomFlags specLevel;
            CASGeomFlags castShadow;
            #endregion

            #region Constructors
            public LODAsset(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public LODAsset(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public LODAsset(int APIversion, EventHandler handler, LODAsset basis) : this(APIversion, handler, basis.sorting, basis.specLevel, basis.castShadow) { }
            public LODAsset(int APIversion, EventHandler handler, CASGeomFlags sorting, CASGeomFlags specLevel, CASGeomFlags castShadow)
                : base(APIversion, handler) { this.sorting = sorting; this.specLevel = specLevel; this.castShadow = castShadow; }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                sorting = (CASGeomFlags)r.ReadUInt32();
                specLevel = (CASGeomFlags)r.ReadUInt32();
                castShadow = (CASGeomFlags)r.ReadUInt32();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write((uint)sorting);
                w.Write((uint)specLevel);
                w.Write((uint)castShadow);
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            public override AHandlerElement Clone(EventHandler handler) { return new LODAsset(requestedApiVersion, handler, this); }
            #endregion

            #region IEquatable<LODAsset> Members

            public bool Equals(LODAsset other)
            {
                return
                    this.sorting == other.sorting
                    && this.specLevel == other.specLevel
                    && this.castShadow == other.castShadow
                    ;
            }

            public override bool Equals(object obj)
            {
                return obj as LODAsset != null ? this.Equals(obj as LODAsset) : false;
            }

            public override int GetHashCode()
            {
                return
                    this.sorting.GetHashCode()
                    ^ this.specLevel.GetHashCode()
                    ^ this.castShadow.GetHashCode()
                    ;
            }

            #endregion

            #region Content Fields
            [ElementPriority(1)]
            public CASGeomFlags Sorting { get { return sorting; } set { if (sorting != value) { sorting = value; OnElementChanged(); } } }
            [ElementPriority(2)]
            public CASGeomFlags SpecLevel { get { return specLevel; } set { if (specLevel != value) { specLevel = value; OnElementChanged(); } } }
            [ElementPriority(3)]
            public CASGeomFlags CastShadow { get { return castShadow; } set { if (castShadow != value) { castShadow = value; OnElementChanged(); } } }

            public string Value { get { return ValueBuilder; } }
            /*public string Value
            {
                get
                {
                    System.Text.StringBuilder sb = new StringBuilder();
                    foreach (string field in ContentFields)
                        if (!field.Equals("Value"))
                            sb.Append(string.Format("{0}: {1}; ", field, this[field]));
                    return sb.ToString().TrimEnd(';', ' ');
                }
            }/**/
            #endregion
        }
        public class LODAssetList : DependentList<LODAsset>
        {
            #region Constructors
            public LODAssetList(EventHandler handler) : base(handler, Byte.MaxValue) { }
            public LODAssetList(EventHandler handler, Stream s) : base(handler, s, Byte.MaxValue) { }
            public LODAssetList(EventHandler handler, IEnumerable<LODAsset> le) : base(handler, le, Byte.MaxValue) { }
            #endregion

            #region Data I/O
            protected override int ReadCount(Stream s) { return new BinaryReader(s).ReadByte(); }
            protected override void WriteCount(Stream s, int count) { new BinaryWriter(s).Write((byte)count); }
            protected override LODAsset CreateElement(Stream s) { return new LODAsset(0, elementHandler, s); }
            protected override void WriteElement(Stream s, LODAsset element) { element.UnParse(s); }
            #endregion

            public override void Add() { this.Add(new LODAsset(0, null)); }
        }
        #endregion

        #region Content Fields
        [ElementPriority(1)]
        public uint Version { get { return version; } set { if (version != value) { version = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(2)]
        public PresetList Presets { get { return presets; } set { if (!presets.Equals(value)) { presets = new PresetList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(3)]
        public string Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(4)]
        public float SortPriority { get { return sortPriority; } set { if (sortPriority != value) { sortPriority = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(5)]
        public byte Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(6)]
        public ClothingType Clothing { get { return clothing; } set { if (clothing != value) { clothing = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(7)]
        public DataTypeFlags DataType { get { return dataType; } set { if (dataType != value) { dataType = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(8)]
        public AgeGenderFlags AgeGender { get { return ageGender; } set { if (!ageGender.Equals(value)) { ageGender = new AgeGenderFlags(0, OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(9)]
        public ClothingCategoryFlags ClothingCategory { get { return clothingCategory; } set { if (clothingCategory != value) { clothingCategory = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(10), TGIBlockListContentField("TGIBlocks")]
        public byte CasPart1Index { get { return casPart1Index; } set { if (casPart1Index != value) { casPart1Index = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(11), TGIBlockListContentField("TGIBlocks")]
        public byte CasPart2Index { get { return casPart2Index; } set { if (casPart2Index != value) { casPart2Index = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(12), TGIBlockListContentField("TGIBlocks")]
        public byte BlendInfoFatIndex { get { return blendInfoFatIndex; } set { if (blendInfoFatIndex != value) { blendInfoFatIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(13), TGIBlockListContentField("TGIBlocks")]
        public byte BlendInfoFitIndex { get { return blendInfoFitIndex; } set { if (blendInfoFitIndex != value) { blendInfoFitIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(14), TGIBlockListContentField("TGIBlocks")]
        public byte BlendInfoThinIndex { get { return blendInfoThinIndex; } set { if (blendInfoThinIndex != value) { blendInfoThinIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(15), TGIBlockListContentField("TGIBlocks")]
        public byte BlendInfoSpecialIndex { get { return blendInfoSpecialIndex; } set { if (blendInfoSpecialIndex != value) { blendInfoSpecialIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(16)]
        public uint Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(17), DataGridExpandable]
        public ByteEntryList VPXYIndexes { get { return vpxyIndexes; } set { if (!vpxyIndexes.Equals(value)) { vpxyIndexes = new ByteEntryList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(18)]
        public LODInfoEntryList LODInfo { get { return lodInfo; } set { if (!lodInfo.Equals(value)) { lodInfo = new LODInfoEntryList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(19), DataGridExpandable]
        public ByteEntryList Diffuse1Indexes { get { return diffuse1Indexes; } set { if (!diffuse1Indexes.Equals(value)) { diffuse1Indexes = new ByteEntryList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(20), DataGridExpandable]
        public ByteEntryList Specular1Indexes { get { return specular1Indexes; } set { if (!specular1Indexes.Equals(value)) { specular1Indexes = new ByteEntryList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(21), DataGridExpandable]
        public ByteEntryList Diffuse2Indexes { get { return diffuse2Indexes; } set { if (!diffuse2Indexes.Equals(value)) { diffuse2Indexes = new ByteEntryList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(22), DataGridExpandable]
        public ByteEntryList Specular2Indexes { get { return specular2Indexes; } set { if (!specular2Indexes.Equals(value)) { specular2Indexes = new ByteEntryList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(23), DataGridExpandable]
        public ByteEntryList BONDIndexes { get { return bondIndexes; } set { if (!bondIndexes.Equals(value)) { bondIndexes = new ByteEntryList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(24)]
        public string Unknown4 { get { return unknown4; } set { if (unknown4 != value) { unknown4 = value; OnResourceChanged(this, new EventArgs()); } } }

        [ElementPriority(25)]
        public CountedTGIBlockList TGIBlocks { get { return tgiBlocks; } set { if (!tgiBlocks.Equals(value)) { tgiBlocks = new CountedTGIBlockList(OnResourceChanged, "IGT", value); OnResourceChanged(this, new EventArgs()); } } }

        public string Value { get { return ValueBuilder; } }
        #endregion
    }

    /// <summary>
    /// ResourceHandler for CASPartResource wrapper
    /// </summary>
    public class CASPartResourceHandler : AResourceHandler
    {
        public CASPartResourceHandler()
        {
            this.Add(typeof(CASPartResource), new List<string>(new string[] { "0x034AEECB", }));
        }
    }
}
