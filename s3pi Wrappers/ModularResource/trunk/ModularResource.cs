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
        ushort unknown3;
        byte[] unknown4 = new byte[4];
        uint unknown5;
        uint unknown6;
        uint unknown7;
        uint unknown8;
        uint unknown9;
        TGIBlockList tgiBlocks;
        #endregion

        public ModularResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); dirty = true; } stream.Position = 0; Parse(stream); }

        #region Data I/O
        void Parse(Stream s)
        {
            long tgiPosn, tgiSize;
            BinaryReader r = new BinaryReader(s);

            unknown1 = r.ReadUInt16();
            tgiPosn = r.ReadUInt32() + s.Position;
            tgiSize = r.ReadUInt32();
            unknown2 = r.ReadUInt16();
            unknown3 = r.ReadUInt16();
            unknown4 = r.ReadBytes(4);
            if (checking) if (unknown4.Length != 4)
                    throw new InvalidDataException(String.Format("Expected four bytes; read {0}; position 0x{1:X8}", unknown4.Length, s.Position));
            unknown5 = r.ReadUInt32();
            unknown6 = r.ReadUInt32();
            unknown7 = r.ReadUInt32();
            unknown8 = r.ReadUInt32();
            unknown9 = r.ReadUInt32();
            tgiBlocks = new TGIBlockList(OnResourceChanged, s, tgiPosn, tgiSize);
        }

        Stream UnParse()
        {
            long pos;
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);

            w.Write(unknown1);
            pos = ms.Position;
            w.Write((uint)0);//tgiOffset
            w.Write((uint)0);//tgiSize
            w.Write(unknown2);
            w.Write(unknown3);
            w.Write(unknown4);
            w.Write(unknown5);
            w.Write(unknown6);
            w.Write(unknown7);
            w.Write(unknown8);
            w.Write(unknown9);
            if (tgiBlocks == null) tgiBlocks = new TGIBlockList(OnResourceChanged);
            tgiBlocks.UnParse(ms, pos);

            return ms;
        }
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

        #region Content Fields
        public ushort Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public ushort Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public ushort Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public byte[] Unknown4 { get { return (byte[])unknown4.Clone(); } set { if (value.Length != unknown4.Length)throw new ArgumentLengthException("Unknown4", unknown4.Length); if (!ArrayCompare(unknown4, value)) { unknown4 = (byte[])value.Clone(); OnResourceChanged(this, EventArgs.Empty); } } }
        public uint Unknown5 { get { return unknown5; } set { if (unknown5 != value) { unknown5 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public uint Unknown6 { get { return unknown6; } set { if (unknown6 != value) { unknown6 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public uint Unknown7 { get { return unknown7; } set { if (unknown7 != value) { unknown7 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public uint Unknown8 { get { return unknown8; } set { if (unknown8 != value) { unknown8 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public uint Unknown9 { get { return unknown9; } set { if (unknown9 != value) { unknown9 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public TGIBlockList TGIBlocks { get { return tgiBlocks; } set { if (tgiBlocks != value) { tgiBlocks = new TGIBlockList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }

        public String Value
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
                    if (typeof(TGIBlockList).IsAssignableFrom(tv.Type)) s += h + (tv.Value as TGIBlockList).Value + t;
                    else s += string.Format("{0}: {1}\n", f, "" + tv);
                }
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
