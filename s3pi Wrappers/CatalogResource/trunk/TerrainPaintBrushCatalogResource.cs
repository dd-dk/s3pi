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
    class TerrainPaintBrushCatalogResource : TerrainGeometryWaterBrushCatalogResource
    {
        #region Attributes
        TGIBlock brushTexture = null;
        #endregion

        #region Constructors
        public TerrainPaintBrushCatalogResource(int APIversion, Stream s) : base(APIversion, s) { }
        public TerrainPaintBrushCatalogResource(int APIversion, Stream unused, TerrainPaintBrushCatalogResource basis)
            : base(APIversion, null, basis)
        {
            this.brushTexture = (TGIBlock)basis.brushTexture.Clone(OnResourceChanged);
        }
        public TerrainPaintBrushCatalogResource(int APIversion, uint version, uint unknown2, Common common,
            byte unknown3, byte unknown4, uint unknown5, byte unknown6, byte unknown7, uint unknown8, uint unknown9, uint unknown10,
            TGIBlock brushShape, byte[] unknown11, float unknown12, float unknown13, byte[] unknown14,
            TGIBlock brushTexture)
            : base(APIversion, version, unknown2, common, unknown3, unknown4, unknown5, unknown6, unknown7, unknown8, unknown9, unknown10,
            brushShape, unknown11, unknown12, unknown13, unknown14)
        {
            this.brushTexture = (TGIBlock)brushTexture.Clone(OnResourceChanged);
        }
        #endregion

        #region Data I/O
        protected override void Parse(Stream s)
        {
            base.Parse(s);
            this.brushTexture = new TGIBlock(requestedApiVersion, OnResourceChanged, s);
        }

        protected override Stream UnParse()
        {
            Stream s = base.UnParse();
            if (brushTexture == null) brushTexture = new TGIBlock(requestedApiVersion, OnResourceChanged, 0, 0, 0);
            brushTexture.UnParse(s);

            return s;
        }
        #endregion

        #region Content Fields
        public TGIBlock BrushTexture
        {
            get { return brushTexture; }
            set { if (brushTexture != value) { brushTexture = new TGIBlock(requestedApiVersion, OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } }
        }
        #endregion
    }
}
