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

namespace s3pi.Interfaces
{
    public abstract class AResource : AApiVersionedFields, IResource
    {
        #region Attributes
        /// <summary>
        /// Resource data stream
        /// </summary>
        protected Stream stream = null;

        /// <summary>
        /// Indicates the resource stream may no longer reflect the resource content
        /// </summary>
        protected bool dirty = false;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of the resource
        /// </summary>
        /// <param name="APIversion">Requested API version</param>
        /// <param name="s">Data stream to use, or null to create from scratch</param>
        protected AResource(int APIversion, Stream s)
        {
            requestedApiVersion = APIversion;
            stream = s;
        }
        #endregion

        #region AApiVersionedFields
        /// <summary>
        /// The list of available field names on this API object
        /// </summary>
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
        #endregion

        #region IResource Members
        /// <summary>
        /// The resource content as a Stream
        /// </summary>
        public virtual Stream Stream { get { return stream; } }
        /// <summary>
        /// The resource content as a byte array
        /// </summary>
        public virtual byte[] AsBytes { get { stream.Position = 0; return (new BinaryReader(Stream)).ReadBytes((int)Stream.Length); } }

        /// <summary>
        /// Raised if the resource is changed
        /// </summary>
        public event EventHandler ResourceChanged;

        #endregion

        #region Sub-classes
        public abstract class DependentList<T, U> : ADependentList<T, U>
            where U : AResource
            where T : IEquatable<T>
        {
            #region Constructors
            protected DependentList(U parent) : this(parent, -1) { }
            protected DependentList(U parent, long size) : base(parent, size) { }
            protected DependentList(U parent, IList<T> ilt) : this(parent, -1, ilt) { }
            protected DependentList(U parent, long size, IList<T> ilt) : base(parent, size, ilt) { }
            /// <summary>
            /// Allow contruction from a stream
            /// </summary>
            /// <param name="parent">Resource that parents this list</param>
            /// <param name="s">Stream containing list entries</param>
            protected DependentList(U parent, Stream s) : this(parent, -1, s) { }
            protected DependentList(U parent, long size, Stream s) : base(parent, size) { Parse(s); }
            #endregion

            #region Data I/O
            /// <summary>
            /// Read list entries from a stream
            /// </summary>
            /// <param name="s">Stream containing list entries</param>
            protected virtual void Parse(Stream s) { base.Clear(); bool inc = true; for (uint i = ReadCount(s); i > 0; i = (uint)(i - (inc ? 1 : 0))) base.Add(CreateElement(parent, s, out inc)); }
            protected virtual uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadUInt32(); }
            protected abstract T CreateElement(U parent, Stream s);
            protected virtual T CreateElement(U parent, Stream s, out bool inc) { inc = true; return CreateElement(parent, s); }

            /// <summary>
            /// Write list entries to a stream
            /// </summary>
            /// <param name="s">Stream to receive list entries</param>
            public virtual void UnParse(Stream s) { WriteCount(s, (uint)Count); foreach (T element in this) WriteElement(s, element); }
            protected virtual void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write(count); }
            protected abstract void WriteElement(Stream s, T element);
            #endregion

            #region ResourceChanged overrides for list
            public virtual new void Insert(int index, T item) { base.Insert(index, item); parent.OnResourceChanged(this, new EventArgs()); }

            public virtual new void RemoveAt(int index) { base.RemoveAt(index); parent.OnResourceChanged(this, new EventArgs()); }

            public virtual new T this[int index]
            {
                get { return base[index]; }
                set
                {
                    if (base[index].Equals(value)) return;
                    base[index] = value;
                    parent.OnResourceChanged(this, new EventArgs());
                }
            }

            public virtual new void Add(T item) { base.Add(item); parent.OnResourceChanged(this, new EventArgs()); }

            public virtual new void Clear() { base.Clear(); parent.OnResourceChanged(this, new EventArgs()); }

            public virtual new bool Remove(T item)
            {
                bool res = base.Remove(item);
                if (res) parent.OnResourceChanged(this, new EventArgs());
                return res;
            }
            #endregion
        }
        #endregion

        /// <summary>
        /// Used to indicate the resource has changed
        /// </summary>
        /// <param name="sender">The resource that has changed</param>
        /// <param name="e">(not used)</param>
        protected virtual void OnResourceChanged(object sender, EventArgs e) { dirty = true; if (ResourceChanged != null) ResourceChanged(sender, e); }
    }
}
