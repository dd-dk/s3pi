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

namespace ModularResource
{
    public class ModularResource : AResource
    {
        const int recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

        static bool checking = s3pi.Settings.Settings.Checking;

        #region Attributes
        ushort unknown1;
        ushort unknown2;
        TGIIndexList tgiIndexes;
        TGIBlockList tgiBlocks;
        #endregion

        public ModularResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); OnResourceChanged(this, new EventArgs()); } stream.Position = 0; Parse(stream); }

        #region Data I/O
        void Parse(Stream s)
        {
            long tgiPosn, tgiSize;
            BinaryReader r = new BinaryReader(s);

            unknown1 = r.ReadUInt16();
            tgiPosn = r.ReadUInt32() + s.Position;
            tgiSize = r.ReadUInt32();
            unknown2 = r.ReadUInt16();
            tgiIndexes = new TGIIndexList(OnResourceChanged, s);
            tgiBlocks = new TGIBlockList(OnResourceChanged, s, tgiPosn, tgiSize);
        }

        protected override Stream UnParse()
        {
            long pos;
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);

            w.Write(unknown1);
            pos = ms.Position;
            w.Write((uint)0);//tgiOffset
            w.Write((uint)0);//tgiSize
            w.Write(unknown2);
            if (tgiIndexes == null) tgiIndexes = new TGIIndexList(OnResourceChanged);
            tgiIndexes.UnParse(ms);
            if (tgiBlocks == null) tgiBlocks = new TGIBlockList(OnResourceChanged);
            tgiBlocks.UnParse(ms, pos);

            return ms;
        }
        #endregion

        #region Sub-classes
        public class TGIIndex : AHandlerElement, IEquatable<TGIIndex>
        {
            uint element;
            public TGIIndex(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public TGIIndex(int APIversion, EventHandler handler, TGIIndex basis) : this(APIversion, handler, basis.element) { }
            public TGIIndex(int APIversion, EventHandler handler, uint value) : base(APIversion, handler) { element = value; }

            #region AHandlerElement Members
            public override AHandlerElement Clone(EventHandler handler) { return new TGIIndex(requestedApiVersion, handler, this); }

            public override int RecommendedApiVersion { get { return 1; } }

            public override List<string> ContentFields { get { return AApiVersionedFields.GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<TGIIndex> Members

            public bool Equals(TGIIndex other) { return (element).Equals(other.element); }

            #endregion

            public uint Element { get { return element; } set { if (element != value) { element = value; OnElementChanged(); } } }
            public string Value { get { return "0x" + ((uint)element).ToString("X8"); } }
        }

        public class TGIIndexList : AResource.DependentList<TGIIndex>
        {
            #region Constructors
            public TGIIndexList(EventHandler handler) : base(handler, ushort.MaxValue) { }
            public TGIIndexList(EventHandler handler, IList<TGIIndex> ltgii) : base(handler, ushort.MaxValue, ltgii) { }
            internal TGIIndexList(EventHandler handler, Stream s) : base(handler, ushort.MaxValue, s) { }
            #endregion

            #region Data I/O
            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadUInt16(); }
            protected override TGIIndex CreateElement(Stream s) { return new TGIIndex(0, elementHandler, (new BinaryReader(s)).ReadUInt32()); }

            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((UInt16)count); }
            protected override void WriteElement(Stream s, TGIIndex element) { (new BinaryWriter(s)).Write(element.Element); }
            #endregion

            public override void Add() { this.Add(new TGIIndex(0, elementHandler)); }

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("0x{0:X2}: 0x{1}\n", i, this[i].Element.ToString("X8")); return s; } }
            #endregion
        }
        #endregion

        #region Content Fields
        public ushort Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public ushort Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public TGIIndexList TGIIndexes { get { return tgiIndexes; } set { if (tgiIndexes != value) { tgiIndexes = new TGIIndexList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        public TGIBlockList TGIBlocks { get { return tgiBlocks; } set { if (tgiBlocks != value) { tgiBlocks = new TGIBlockList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }

        public String Value
        {
            get
            {
                string h = "\n---------\n---------\n{0}: {1}\n---------\n";
                string t = "---------\n";
                string s = "";
                s += "Unknown1: 0x" + unknown1.ToString("X4");
                s += "\nUnknown2: 0x" + unknown2.ToString("X4");
                s += String.Format(h, "TGIIndexList", "TGIIndexes") + tgiIndexes.Value + t;
                s += String.Format(h, "TGIBlockList", "TGIBlocks") + tgiBlocks.Value + t;
                return s;
            }
        }
        #endregion
    }

    /// <summary>
    /// ResourceHandler for TxtcResource wrapper
    /// </summary>
    public class ModularResourceHandler : AResourceHandler
    {
        public ModularResourceHandler()
        {
            this.Add(typeof(ModularResource), new List<string>(new string[] { "0xCF9A4ACE", }));
        }
    }
}
