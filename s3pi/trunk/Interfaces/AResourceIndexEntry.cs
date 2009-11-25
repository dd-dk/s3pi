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

namespace s3pi.Interfaces
{
    public abstract class AResourceIndexEntry : AResourceKey, IResourceIndexEntry
    {
        public AResourceIndexEntry() : this(0, null) { }
        public AResourceIndexEntry(int APIversion, EventHandler handler) : base(APIversion, handler) { }

        #region AApiVersionedFields
        /// <summary>
        /// The list of available field names on this API object
        /// </summary>
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
        #endregion

        #region IResourceIndexEntry Members
        /// <summary>
        /// If the resource was read from a package, the location in the package the resource was read from
        /// </summary>
        [ElementPriority(5)]
        public abstract uint Chunkoffset { get; set; }
        /// <summary>
        /// The number of bytes the resource uses within the package
        /// </summary>
        [ElementPriority(6)]
        public abstract uint Filesize { get; set; }
        /// <summary>
        /// The number of bytes the resource uses in memory
        /// </summary>
        [ElementPriority(7)]
        public abstract uint Memsize { get; set; }
        /// <summary>
        /// 0xFFFF if Filesize != Memsize, else 0x0000
        /// </summary>
        [ElementPriority(8)]
        public abstract ushort Compressed { get; set; }
        /// <summary>
        /// Always 0x0001
        /// </summary>
        [ElementPriority(9)]
        public abstract ushort Unknown2 { get; set; }

        /// <summary>
        /// Raised to indicate one of the index entry fields values has changed
        /// </summary>
        public event EventHandler ResourceIndexEntryChanged;

        /// <summary>
        /// A MemoryStream covering the index entry bytes
        /// </summary>
        public abstract System.IO.Stream Stream { get; }

        /// <summary>
        /// True if the index entry has been deleted from the package index
        /// </summary>
        public abstract bool IsDeleted { get; set; }

        #endregion

        /// <summary>
        /// True if the index entry should be treated as dirty - e.g. the ResourceStream has been replaced
        /// </summary>
        protected bool isDirty = false;

        /// <summary>
        /// Used to indicate one of the index entry fields values has changed
        /// </summary>
        /// <param name="sender">Object causing the entry to change value</param>
        /// <param name="e">(not used)</param>
        protected virtual void OnResourceIndexEntryChanged(object sender, EventArgs e) { isDirty = true; if (ResourceIndexEntryChanged != null) ResourceIndexEntryChanged(sender, e); }
    }
}
