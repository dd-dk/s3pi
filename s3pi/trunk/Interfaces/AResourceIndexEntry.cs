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

namespace s3pi.Interfaces
{
    public abstract class AResourceIndexEntry : AApiVersionedFields, IResourceIndexEntry
    {
        #region AApiVersionedFields
        /// <summary>
        /// The list of available field names on this API object
        /// </summary>
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
        #endregion

        #region IResourceIndexEntry Members
        /// <summary>
        /// The "type" of the resource
        /// </summary>
        public abstract uint ResourceType { get; set; }
        /// <summary>
        /// The "group" the resource is part of
        /// </summary>
        public abstract uint ResourceGroup { get; set; }
        /// <summary>
        /// The "instance" number of the resource
        /// </summary>
        public abstract ulong Instance { get; set; }
        /// <summary>
        /// If the resource was read from a package, the location in the package the resource was read from
        /// </summary>
        public abstract uint Chunkoffset { get; set; }
        /// <summary>
        /// The number of bytes the resource uses within the package
        /// </summary>
        public abstract uint Filesize { get; set; }
        /// <summary>
        /// The number of bytes the resource uses in memory
        /// </summary>
        public abstract uint Memsize { get; set; }
        /// <summary>
        /// 0xFFFF if Filesize != Memsize, else 0x0000
        /// </summary>
        public abstract ushort Compressed { get; set; }
        /// <summary>
        /// Always 0x0001
        /// </summary>
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
        public abstract bool IsDeleted { get; }

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
