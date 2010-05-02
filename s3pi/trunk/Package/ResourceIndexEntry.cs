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
using System.Text;
using System.IO;
using System.Reflection;
using s3pi.Interfaces;

namespace s3pi.Package
{
    /// <summary>
    /// Implementation of an index entry
    /// </summary>
    public class ResourceIndexEntry : AResourceIndexEntry
    {
        const Int32 recommendedApiVersion = 2;

        #region AApiVersionedFields
        /// <summary>
        /// The version of the API in use
        /// </summary>
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        //No ContentFields override as we don't want to make anything more public than AResourceIndexEntry provides
        #endregion

        #region AResourceIndexEntry
        /// <summary>
        /// The "type" of the resource
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override UInt32 ResourceType
        {
            get { ms.Position = 4; return indexReader.ReadUInt32(); }
            set { ms.Position = 4; indexWriter.Write(value); OnElementChanged(); }
        }
        /// <summary>
        /// The "group" the resource is part of
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override UInt32 ResourceGroup
        {
            get { ms.Position = 8; return indexReader.ReadUInt32(); }
            set { ms.Position = 8; indexWriter.Write(value); OnElementChanged(); }
        }
        /// <summary>
        /// The "instance" number of the resource
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override UInt64 Instance
        {
            get { ms.Position = 12; return ((ulong)indexReader.ReadUInt32() << 32) | (ulong)indexReader.ReadUInt32(); }
            set { ms.Position = 12; indexWriter.Write((uint)(value >> 32)); indexWriter.Write((uint)(value & 0xffffffff)); OnElementChanged(); }
        }
        /// <summary>
        /// If the resource was read from a package, the location in the package the resource was read from
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override UInt32 Chunkoffset
        {
            get { ms.Position = 20; return indexReader.ReadUInt32(); }
            set { ms.Position = 20; indexWriter.Write(value); OnElementChanged(); }
        }
        /// <summary>
        /// The number of bytes the resource uses within the package
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override UInt32 Filesize
        {
            get { ms.Position = 24; return indexReader.ReadUInt32() & 0x7fffffff; }
            set { ms.Position = 24; indexWriter.Write(value | 0x80000000); OnElementChanged(); }
        }
        /// <summary>
        /// The number of bytes the resource uses in memory
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override UInt32 Memsize
        {
            get { ms.Position = 28; return indexReader.ReadUInt32(); }
            set { ms.Position = 28; indexWriter.Write(value); OnElementChanged(); }
        }
        /// <summary>
        /// 0xFFFF if Filesize != Memsize, else 0x0000
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override UInt16 Compressed
        {
            get { ms.Position = 32; return indexReader.ReadUInt16(); }
            set { ms.Position = 32; indexWriter.Write(value); OnElementChanged(); }
        }
        /// <summary>
        /// Always 0x0001
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override UInt16 Unknown2
        {
            get { ms.Position = 34; return indexReader.ReadUInt16(); }
            set { ms.Position = 34; indexWriter.Write(value); OnElementChanged(); }
        }

        /// <summary>
        /// A MemoryStream covering the index entry bytes
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override Stream Stream { get { return ms; } }

        /// <summary>
        /// True if the index entry has been deleted from the package index
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override bool IsDeleted { get { return isDeleted; } set { if (isDeleted != value) { isDeleted = value; OnElementChanged(); } } }

        /// <summary>
        /// Get a copy of this element but with a new change event handler
        /// </summary>
        /// <param name="handler">Element change event handler</param>
        /// <returns>Return a copy of this element but with a new change event handler</returns>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override AHandlerElement Clone(EventHandler handler) { return new ResourceIndexEntry(indexEntry); }
        #endregion


        #region Implementation
        /// <summary>
        /// The index entry data
        /// </summary>
        byte[] indexEntry = null;
        /// <summary>
        /// indexEntry as a (fixed size) memory stream
        /// </summary>
        MemoryStream ms = null;
        /// <summary>
        /// Used to read from the indexEntry
        /// </summary>
        BinaryReader indexReader = null;
        /// <summary>
        /// Used to write to the indexEntry
        /// </summary>
        BinaryWriter indexWriter = null;

        /// <summary>
        /// True if the index entry should be treated as deleted
        /// </summary>
        bool isDeleted = false;

        /// <summary>
        /// The uncompressed resource data associated with this index entry
        /// (used to save having to uncompress the same entry again if it's requested more than once)
        /// </summary>
        Stream resourceStream = null;

        /// <summary>
        /// Create a new index entry as a byte-for-byte copy of <paramref name="indexEntry"/>
        /// </summary>
        /// <param name="indexEntry">The source index entry</param>
        private ResourceIndexEntry(byte[] indexEntry)
        {
            this.indexEntry = (byte[])indexEntry.Clone();
            ms = new MemoryStream(this.indexEntry);
            indexReader = new BinaryReader(ms);
            indexWriter = new BinaryWriter(ms);
        }

        /// <summary>
        /// Create a new expanded index entry from the header and entry data passed
        /// </summary>
        /// <param name="header">header ints (same for each index entry); [0] is the index type</param>
        /// <param name="entry">entry ints (specific to this entry)</param>
        internal ResourceIndexEntry(Int32[] header, Int32[] entry)
        {
            indexEntry = new byte[(header.Length + entry.Length) * 4];
            ms = new MemoryStream(indexEntry);
            BinaryWriter w = new BinaryWriter(ms);

            w.Write(header[0]);

            int hc = 1;// header[0] is indexType, already written!
            int ec = 0;
            Boolset IhGT = (uint)header[0];
            w.Write(IhGT[0] ? header[hc++] : entry[ec++]);
            w.Write(IhGT[1] ? header[hc++] : entry[ec++]);
            w.Write(IhGT[2] ? header[hc++] : entry[ec++]);

            for (; hc < header.Length - 1; hc++)
                w.Write(header[hc]);

            for (; ec < entry.Length; ec++)
                w.Write(entry[ec]);

            indexReader = new BinaryReader(ms);
            indexWriter = new BinaryWriter(ms);
        }

        /// <summary>
        /// Return a new index entry as a copy of this one
        /// </summary>
        /// <returns>A copy of this index entry</returns>
        internal ResourceIndexEntry Clone() { return (ResourceIndexEntry)this.Clone(null); }

        /// <summary>
        /// Flag this index entry as deleted
        /// </summary>
        /// <remarks>Use APackage.RemoveResource() from user code</remarks>
        internal void Delete()
        {
            if (s3pi.Settings.Settings.Checking) if (isDeleted)
                    throw new InvalidOperationException("Index entry already deleted!");

            isDeleted = true;
            OnElementChanged();
        }

        /// <summary>
        /// The uncompressed resource data associated with this index entry
        /// (used to save having to uncompress the same entry again if it's requested more than once)
        /// Setting the stream updates the Memsize
        /// </summary>
        /// <remarks>Use Package.ReplaceResource() from user code</remarks>
        internal Stream ResourceStream
        {
            get { return resourceStream; }
            set { if (resourceStream != value) { resourceStream = value; if (Memsize != (uint)resourceStream.Length) Memsize = (uint)resourceStream.Length; else OnElementChanged(); } }
        }

        /// <summary>
        /// True if the index entry should be treated as dirty - e.g. the ResourceStream has been replaced
        /// </summary>
        internal bool IsDirty { get { return dirty; } }
        #endregion
    }
}
