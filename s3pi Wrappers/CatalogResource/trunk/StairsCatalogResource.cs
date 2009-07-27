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
    public class StairsCatalogResource : CatalogResourceTGIBlockList
    {
        #region Attributes
        uint unknown1;
        uint unknown2;
        byte unknown3;
        uint unknown4;
        byte unknown5;
        uint unknown6;
        byte unknown7;
        uint vpxy_index1;
        uint vpxy_index2;
        uint vpxy_index3;
        uint catalogRailing;
        uint catalogWall;
        uint catalogWallFloorPattern;
        uint catalogFence;
        #endregion

        #region Constructors
        public StairsCatalogResource(int APIversion, Stream s) : base(APIversion, s) { }
        public StairsCatalogResource(int APIversion, Stream unused, StairsCatalogResource basis)
            : base(APIversion, basis.list)
        {
            this.unknown1 = basis.unknown1;
            this.common = new Common(requestedApiVersion, OnResourceChanged, basis.common);
            this.unknown2 = basis.unknown2;
            this.unknown3 = basis.unknown3;
            this.unknown4 = basis.unknown4;
            this.unknown5 = basis.unknown5;
            this.unknown6 = basis.unknown6;
            this.unknown7 = basis.unknown7;
            this.vpxy_index1 = basis.vpxy_index1;
            this.vpxy_index2 = basis.vpxy_index2;
            this.vpxy_index3 = basis.vpxy_index3;
            this.catalogRailing = basis.catalogRailing;
            this.catalogWall = basis.catalogWall;
            this.catalogWallFloorPattern = basis.catalogWallFloorPattern;
            this.catalogFence = basis.catalogFence;
        }
        public StairsCatalogResource(int APIversion, uint unknown1, Common common,
            uint unknown2, byte unknown3, uint unknown4, byte unknown5, uint unknown6, byte unknown7, uint index1, uint index2, uint index3,
            uint catalogRailing, uint catalogWall, uint catalogWallFloorPattern, uint catalogFence,
            TGIBlockList ltgib)
            : base(APIversion, ltgib)
        {
            this.unknown1 = unknown1;
            this.common = new Common(requestedApiVersion, OnResourceChanged, common);
            this.unknown2 = unknown2;
            this.unknown3 = unknown3;
            this.unknown4 = unknown4;
            this.unknown5 = unknown5;
            this.unknown6 = unknown6;
            this.unknown7 = unknown7;
            this.vpxy_index1 = index1;
            this.vpxy_index2 = index2;
            this.vpxy_index3 = index3;
            this.catalogRailing = catalogRailing;
            this.catalogWall = catalogWall;
            this.catalogWallFloorPattern = catalogWallFloorPattern;
            this.catalogFence = catalogFence;
        }
        #endregion

        #region Data I/O
        protected override void Parse(Stream s)
        {
            long tgiPosn, tgiSize;
            BinaryReader r = new BinaryReader(s);

            this.unknown1 = r.ReadUInt32();
            tgiPosn = r.ReadUInt32() + s.Position;
            tgiSize = r.ReadUInt32();
            this.common = new Common(requestedApiVersion, OnResourceChanged, s);
            this.unknown2 = r.ReadUInt32();
            this.unknown3 = r.ReadByte();
            this.unknown4 = r.ReadUInt32();
            this.unknown5 = r.ReadByte();
            this.unknown6 = r.ReadUInt32();
            this.unknown7 = r.ReadByte();
            this.vpxy_index1 = r.ReadUInt32();
            this.vpxy_index2 = r.ReadUInt32();
            this.vpxy_index3 = r.ReadUInt32();
            this.catalogRailing = r.ReadUInt32();
            this.catalogWall = r.ReadUInt32();
            this.catalogWallFloorPattern = r.ReadUInt32();
            this.catalogFence = r.ReadUInt32();

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
            if (common == null) common = new Common(requestedApiVersion, OnResourceChanged);
            common.UnParse(s);

            w.Write(unknown2);
            w.Write(unknown3);
            w.Write(unknown4);
            w.Write(unknown5);
            w.Write(unknown6);
            w.Write(unknown7);
            w.Write(vpxy_index1);
            w.Write(vpxy_index2);
            w.Write(vpxy_index3);
            w.Write(catalogRailing);
            w.Write(catalogWall);
            w.Write(catalogWallFloorPattern);
            w.Write(catalogFence);

            base.UnParse(s, pos);

            w.Flush();

            return s;
        }
        #endregion

        #region Content Fields
        public uint Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown4 { get { return unknown4; } set { if (unknown4 != value) { unknown4 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown5 { get { return unknown5; } set { if (unknown5 != value) { unknown5 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown6 { get { return unknown6; } set { if (unknown6 != value) { unknown6 = value; OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown7 { get { return unknown7; } set { if (unknown7 != value) { unknown7 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint VPXYIndex1 { get { return vpxy_index1; } set { if (vpxy_index1 != value) { vpxy_index1 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint VPXYIndex2 { get { return vpxy_index2; } set { if (vpxy_index2 != value) { vpxy_index2 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint VPXYIndex3 { get { return vpxy_index3; } set { if (vpxy_index3 != value) { vpxy_index3 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint CatalogRailingIndex { get { return catalogRailing; } set { if (catalogRailing != value) { catalogRailing = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint CatalogWallIndex { get { return catalogWall; } set { if (catalogWall != value) { catalogWall = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint CatalogWallFloorPatternIndex { get { return catalogWallFloorPattern; } set { if (catalogWallFloorPattern != value) { catalogWallFloorPattern = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint CatalogFenceIndex { get { return catalogFence; } set { if (catalogFence != value) { catalogFence = value; OnResourceChanged(this, new EventArgs()); } } }
        #endregion
    }
}
