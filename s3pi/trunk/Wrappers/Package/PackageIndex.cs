/***************************************************************************
 *  Copyright (C) 2009 by Peter L Jones                                    *
 *  peter@users.sf.net                                                     *
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
using System.Text;
using System.IO;
using s3pi.Interfaces;

namespace s3pi.Package
{
    /// <summary>
    /// Internal -- used by Package to manage the package index
    /// </summary>
    internal class PackageIndex : List<IResourceIndexEntry>
    {
        const int numFields = 9;

        Boolset indextype = (UInt32)0;
        public UInt32 Indextype { get { return indextype; } }

        int Hdrsize
        {
            get
            {
                int hc = 1;
                for (int i = 0; i < indextype.Length; i++) if (indextype[i]) hc++;
                return hc;
            }
        }

        public PackageIndex() { }
        public PackageIndex(UInt32 type) { indextype = type; }
        public PackageIndex(Stream s, Int32 indexposition, Int32 indexsize, Int32 indexcount)
        {
            if (s == null) return;
            if (indexposition == 0) return;

            BinaryReader r = new BinaryReader(s);
            s.Position = indexposition;
            indextype = r.ReadUInt32();

            Int32[] hdr = new Int32[Hdrsize];
            Int32[] entry = new Int32[numFields - Hdrsize];

            hdr[0] = indextype;
            for (int i = 1; i < hdr.Length; i++)
                hdr[i] = r.ReadInt32();

            for (int i = 0; i < indexcount; i++)
            {
                for (int j = 0; j < entry.Length; j++)
                    entry[j] = r.ReadInt32();
                this.Add(new ResourceIndexEntry(hdr, entry));
            }
        }

        public IResourceIndexEntry Add(uint type, uint group, ulong instance)
        {
            ResourceIndexEntry rc = new ResourceIndexEntry(new Int32[Hdrsize], new Int32[numFields - Hdrsize]);

            rc.ResourceType = type;
            rc.ResourceGroup = group;
            rc.Instance = instance;
            rc.Chunkoffset = 0xffffffff;
            rc.Unknown2 = 1;
            rc.ResourceStream = null;

            this.Add(rc);
            return rc;
        }

        public Int32 Size { get { return (Count * (numFields - Hdrsize) + Hdrsize) * 4; } }
        public void Save(BinaryWriter w)
        {
            BinaryReader r = null;
            if (Count == 0)
            {
                r = new BinaryReader(new MemoryStream(new byte[numFields * 4]));
            }
            else
            {
                r = new BinaryReader(this[0].Stream);
            }
            
            r.BaseStream.Position = 4;
            w.Write((int)indextype);
            if (indextype[0]) w.Write(r.ReadUInt32()); else r.BaseStream.Position += 4;
            if (indextype[1]) w.Write(r.ReadUInt32()); else r.BaseStream.Position += 4;
            if (indextype[2]) w.Write(r.ReadUInt32()); else r.BaseStream.Position += 4;

            foreach (IResourceIndexEntry ie in this)
            {
                r = new BinaryReader(ie.Stream);
                r.BaseStream.Position = 4;
                if (!indextype[0]) w.Write(r.ReadUInt32()); else r.BaseStream.Position += 4;
                if (!indextype[1]) w.Write(r.ReadUInt32()); else r.BaseStream.Position += 4;
                if (!indextype[2]) w.Write(r.ReadUInt32()); else r.BaseStream.Position += 4;
                w.Write(r.ReadBytes((int)(ie.Stream.Length - ie.Stream.Position)));
            }
        }

        /// <summary>
        /// Sort the index by the given field
        /// </summary>
        /// <param name="index">Field to sort by</param>
        public void Sort(string index) { base.Sort(new AApiVersionedFields.Comparer<IResourceIndexEntry>(index)); }

        /// <summary>
        /// Return the index entry with the match TGI
        /// </summary>
        /// <param name="type">Entry type</param>
        /// <param name="group">Entry group</param>
        /// <param name="instance">Entry instance</param>
        /// <returns>Matching entry</returns>
        public IResourceIndexEntry this[uint type, uint group, ulong instance]
        {
            get {
                foreach(ResourceIndexEntry rie in this)
                {
                    if (rie.ResourceType != type) continue;
                    if (rie.ResourceGroup != group) continue;
                    if (rie.Instance == instance) return rie;
                }
                return null;
            }
        }
    }
}
