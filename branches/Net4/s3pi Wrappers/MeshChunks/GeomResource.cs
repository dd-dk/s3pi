﻿/***************************************************************************
 *  Based on earlier work.                                                 *
 *  Copyright (C) 2012 by Peter L Jones                                    *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This is free software: you can redistribute it and/or modify           *
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
 *  along with this software.  If not, see <http://www.gnu.org/licenses/>. *
 ***************************************************************************/
using System;
using s3pi.Interfaces;
using System.IO;
using s3pi.GenericRCOLResource;
using System.Collections.Generic;

namespace meshExpImp.ModelBlocks
{
    public class GeometryResource : GenericRCOLResource
    {
        public GeometryResource(int APIversion, Stream s)
            : base(APIversion, s)
        {
            var chunk = ChunkEntries[0];
            var stream = chunk.RCOLBlock.Stream;
            stream.Position = 0L;
            var geom = new GEOM(0, OnResourceChanged, stream);
            ChunkEntries[0] = new ChunkEntry(0, OnResourceChanged, chunk.TGIBlock, geom);
        }
    }

    /// <summary>
    /// ResourceHandler for TxtcResource wrapper
    /// </summary>
    public class GeometryResourceHandler : AResourceHandler
    {
        public GeometryResourceHandler()
        {
            this.Add(typeof(GeometryResource), new List<string>(new string[] { "0x015A1849", }));
        }
    }
}
