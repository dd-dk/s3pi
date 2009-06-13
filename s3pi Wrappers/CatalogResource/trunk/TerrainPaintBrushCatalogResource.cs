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
        TGIBlock<CatalogResource> brushTexture = null;
        #endregion

        #region Constructors
        public TerrainPaintBrushCatalogResource(int APIversion, Stream s) : base(APIversion, s) { }
        public TerrainPaintBrushCatalogResource(int APIversion, TerrainPaintBrushCatalogResource basis)
            : base(APIversion, basis)
        {
            this.brushTexture = (TGIBlock<CatalogResource>)basis.brushTexture.Clone();
        }
        public TerrainPaintBrushCatalogResource(int APIversion, uint unknown1, Common common,
            uint unknown2, byte unknown3, byte unknown4, uint unknown5, byte unknown6, byte unknown7, uint unknown8, uint unknown9, uint unknown10,
            TGIBlock<CatalogResource> brushShape, byte[] unknown11, float unknown12, float unknown13, byte[] unknown14,
            TGIBlock<CatalogResource> brushTexture)
            : base(APIversion, unknown1, common, unknown2, unknown3, unknown4, unknown5, unknown6, unknown7, unknown8, unknown9, unknown10,
            brushShape, unknown11, unknown12, unknown13, unknown14)
        {
            this.brushTexture = (TGIBlock<CatalogResource>)brushTexture.Clone();
        }
        #endregion

        #region Data I/O
        protected override void Parse(Stream s)
        {
            base.Parse(s);
            this.brushTexture = new TGIBlock<CatalogResource>(this, s);
        }

        protected override Stream UnParse()
        {
            Stream s = base.UnParse();
            brushTexture.UnParse(s);

            return s;
        }
        #endregion

        #region ICloneable Members

        public override object Clone() { return new TerrainPaintBrushCatalogResource(requestedApiVersion, this); }

        #endregion

        #region Content Fields
        public TGIBlock<CatalogResource> BrushTexture
        {
            get { return brushTexture; }
            set { if (brushTexture != value) { brushTexture = new TGIBlock<CatalogResource>(this, value); OnResourceChanged(this, new EventArgs()); } }
        }
        #endregion
    }
}
