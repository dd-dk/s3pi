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
    public class FenceCatalogResource : CatalogResourceTGIBlockList
    {
        #region Attributes
        MaterialList materialList = null;
        uint unknown2;
        byte unknown3;
        uint unknown4;
        byte unknown5;
        uint unknown6;
        byte unknown7;
        uint unknown8;
        uint unknown9;
        uint unknown10;
        uint unknown11;
        byte unknown12;
        #endregion

        #region Constructors
        public FenceCatalogResource(int APIversion, Stream s) : base(APIversion, s) { }
        public FenceCatalogResource(int APIversion, Stream unused, FenceCatalogResource basis)
            : base(APIversion, basis.version, basis.list)
        {
            this.materialList = (basis.version >= 0x00000007) ? new MaterialList(OnResourceChanged, basis.materialList) : null;
            this.common = new Common(requestedApiVersion, OnResourceChanged, basis.common);
            this.unknown2 = basis.unknown2;
            this.unknown3 = basis.unknown3;
            this.unknown4 = basis.unknown4;
            this.unknown5 = basis.unknown5;
            this.unknown6 = basis.unknown6;
            this.unknown7 = basis.unknown7;
            this.unknown8 = basis.unknown8;
            this.unknown9 = basis.unknown9;
            this.unknown10 = basis.unknown10;
            this.unknown11 = basis.unknown11;
            this.unknown12 = basis.unknown12;
        }
        public FenceCatalogResource(int APIversion, uint version, Common common, uint unknown2, byte unknown3, uint unknown4,
            byte unknown5, uint unknown6, byte unknown7, uint unknown8, uint unknown9, uint unknown10, uint unknown11, byte unknown12,
            TGIBlockList ltgib)
            : this(APIversion, version, null, common, unknown2, unknown3, unknown4, unknown5, unknown6, unknown7, unknown8, unknown9, unknown10, unknown11, unknown12, ltgib)
        {
            if (checking) if (version >= 0x00000007)
                    throw new InvalidOperationException(String.Format("Constructor requires MaterialList for version {0}", version));
        }
        public FenceCatalogResource(int APIversion, uint version, MaterialList materialList, Common common, uint unknown2, byte unknown3, uint unknown4,
            byte unknown5, uint unknown6, byte unknown7, uint unknown8, uint unknown9, uint unknown10, uint unknown11, byte unknown12,
            TGIBlockList ltgib)
            : base(APIversion, version, ltgib)
        {
            this.materialList = materialList != null ? new MaterialList(OnResourceChanged, materialList) : null;
            this.common = new Common(requestedApiVersion, OnResourceChanged, common);
            this.unknown2 = unknown2;
            this.unknown3 = unknown3;
            this.unknown4 = unknown4;
            this.unknown5 = unknown5;
            this.unknown6 = unknown6;
            this.unknown7 = unknown7;
            this.unknown8 = unknown8;
            this.unknown9 = unknown9;
            this.unknown10 = unknown10;
            this.unknown11 = unknown11;
            this.unknown12 = unknown12;
        }
        #endregion

        #region Data I/O
        protected override void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);
            base.Parse(s);
            this.materialList = (this.version >= 0x00000007) ? new MaterialList(OnResourceChanged, s) : null;
            this.common = new Common(requestedApiVersion, OnResourceChanged, s);
            this.unknown2 = r.ReadUInt32();
            this.unknown3 = r.ReadByte();
            this.unknown4 = r.ReadUInt32();
            this.unknown5 = r.ReadByte();
            this.unknown6 = r.ReadUInt32();
            this.unknown7 = r.ReadByte();
            this.unknown8 = r.ReadUInt32();
            this.unknown9 = r.ReadUInt32();
            this.unknown10 = r.ReadUInt32();
            this.unknown11 = r.ReadUInt32();
            this.unknown12 = r.ReadByte();

            list = new TGIBlockList(OnResourceChanged, s, tgiPosn, tgiSize);
        }

        protected override Stream UnParse()
        {
            Stream s = base.UnParse();
            BinaryWriter w = new BinaryWriter(s);
            if (version >= 0x00000007)
            {
                if (materialList == null) materialList = new MaterialList(OnResourceChanged);
                materialList.UnParse(s);
            }
            if (common == null) common = new Common(requestedApiVersion, OnResourceChanged);
            common.UnParse(s);
            w.Write(unknown2);
            w.Write(unknown3);
            w.Write(unknown4);
            w.Write(unknown5);
            w.Write(unknown6);
            w.Write(unknown7);
            w.Write(unknown8);
            w.Write(unknown9);
            w.Write(unknown10);
            w.Write(unknown11);
            w.Write(unknown12);

            base.UnParse(s);

            w.Flush();

            return s;
        }
        #endregion

        #region AApiVersionedFields
        /// <summary>
        /// The list of available field names on this API object
        /// </summary>
        public override List<string> ContentFields
        {
            get {
                List<string> res = base.ContentFields;
                if (this.version < 0x00000007) res.Remove("Materials");
                return res;
            }
        }
        #endregion

        #region Content Fields
        public MaterialList Materials
        {
            get { if (version < 0x00000007) throw new InvalidOperationException(); return materialList; }
            set { if (version < 0x00000007) throw new InvalidOperationException(); if (materialList != value) { materialList = value == null ? null : new MaterialList(OnResourceChanged, value); } OnResourceChanged(this, new EventArgs()); }
        }
        public uint Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown4 { get { return unknown4; } set { if (unknown4 != value) { unknown4 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown5 { get { return unknown5; } set { if (unknown5 != value) { unknown5 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown6 { get { return unknown6; } set { if (unknown6 != value) { unknown6 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown7 { get { return unknown7; } set { if (unknown7 != value) { unknown7 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown8 { get { return unknown8; } set { if (unknown8 != value) { unknown8 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown9 { get { return unknown9; } set { if (unknown9 != value) { unknown9 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown10 { get { return unknown10; } set { if (unknown10 != value) { unknown10 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown11 { get { return unknown11; } set { if (unknown11 != value) { unknown11 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown12 { get { return unknown12; } set { if (unknown12 != value) { unknown12 = value; OnResourceChanged(this, new EventArgs()); } } }
        #endregion
    }
}
