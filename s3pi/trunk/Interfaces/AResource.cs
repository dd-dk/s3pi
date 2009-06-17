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

        public class TGIBlock<T> : AApiVersionedFields, IComparable<TGIBlock<T>>, IEqualityComparer<TGIBlock<T>>, IEquatable<TGIBlock<T>>, ICloneableWithParent
            where T : AResource
        {
            #region Attributes
            T parent = null;
            string order;
            uint resourceType;
            uint resourceGroup;
            ulong instance;
            #endregion

            #region Constructors
            public TGIBlock(T parent, Stream s) : this(parent, "TGI", s) { }
            public TGIBlock(T parent, string order, Stream s) { this.parent = parent; this.order = order; Parse(s); }

            public TGIBlock(T parent, TGIBlock<T> tgib) : this(parent, "TGI", tgib) { }
            public TGIBlock(T parent, string order, TGIBlock<T> tgib) : this(parent, order, tgib.resourceType, tgib.resourceGroup, tgib.instance) { }
            public TGIBlock(T parent, uint resourceType, uint resourceGroup, ulong instance) : this(parent, "TGI", resourceType, resourceGroup, instance) { }
            public TGIBlock(T parent, string order, uint resourceType, uint resourceGroup, ulong instance)
            {
                this.parent = parent;
                this.order = order;
                if (order.Length != 3)
                    throw new ArgumentLengthException("order", 3);
                foreach (char c in order) if ("TGI".IndexOf(c) < 0)
                        throw new ArgumentException(String.Format("Invalid character '{0}': only T, G and I allowed", c), "order");
                this.resourceType = resourceType;
                this.resourceGroup = resourceGroup;
                this.instance = instance;
            }
            #endregion

            #region Data I/O
            protected void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                foreach (char c in order)
                    switch (c)
                    {
                        case 'T': resourceType = r.ReadUInt32(); break;
                        case 'G': resourceGroup = r.ReadUInt32(); break;
                        case 'I': instance = r.ReadUInt64(); break;
                    }
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                foreach (char c in order)
                    switch (c)
                    {
                        case 'T': w.Write(resourceType); break;
                        case 'G': w.Write(resourceGroup); break;
                        case 'I': w.Write(instance); break;
                    }
            }
            #endregion

            #region IComparable<TGIBlock<T>> Members

            public int CompareTo(TGIBlock<T> other)
            {
                int res = resourceType.CompareTo(other.resourceType); if (res != 0) return res;
                res = resourceGroup.CompareTo(other.resourceGroup); if (res != 0) return res;
                return instance.CompareTo(other.instance);
            }

            #endregion

            #region IEqualityComparer<TGIBlock<T>> Members

            public bool Equals(TGIBlock<T> x, TGIBlock<T> y) { return x.Equals(y); }

            public int GetHashCode(TGIBlock<T> obj) { return obj.GetHashCode(); }

            public override int GetHashCode() { return resourceType.GetHashCode() ^ resourceGroup.GetHashCode() ^ instance.GetHashCode(); }

            #endregion

            #region IEquatable<TGIBlock<T>> Members

            public bool Equals(TGIBlock<T> other) { return this.CompareTo(other) == 0; }

            #endregion

            #region ICloneableWithParent Members

            public object Clone(object newParent) { return new TGIBlock<T>(newParent as T, this); }

            #endregion

            #region ICloneable Members

            public object Clone() { return Clone(parent); }

            #endregion

            #region AApiVersionedFields
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override int RecommendedApiVersion { get { return parent.RecommendedApiVersion; } }
            #endregion

            #region Content Fields
            public uint ResourceType { get { return resourceType; } set { if (resourceType != value) { resourceType = value; parent.OnResourceChanged(this, new EventArgs()); } } }
            public uint ResourceGroup { get { return resourceGroup; } set { if (resourceGroup != value) { resourceGroup = value; parent.OnResourceChanged(this, new EventArgs()); } } }
            public ulong Instance { get { return instance; } set { if (instance != value) { instance = value; parent.OnResourceChanged(this, new EventArgs()); } } }

            public String Value { get { return String.Format("0x{0:X8}-0x{1:X8}-0x{2:X16}", resourceType, resourceGroup, instance); } }
            #endregion

            public override string ToString() { return Value; }
            public static implicit operator String(TGIBlock<T> value) { return value.ToString(); }
        }

        public class TGIBlockList<T> : AResource.DependentList<TGIBlock<T>, T>
            where T : AResource
        {
            #region Constructors
            public TGIBlockList(T parent) : base(parent) { }
            public TGIBlockList(T parent, IList<TGIBlock<T>> lme) : base(parent, lme) { }
            public TGIBlockList(T parent, Stream s, long tgiOffset, long tgiSize)
                : base(parent) { Parse(s, tgiOffset, tgiSize); }
            #endregion

            #region Data I/O
            protected override TGIBlock<T> CreateElement(T parent, Stream s) { return new TGIBlock<T>(parent, s); }
            protected override void WriteElement(Stream s, TGIBlock<T> element) { element.UnParse(s); }

            protected void Parse(Stream s, long tgiPosn, long tgiSize)
            {
                bool checking = true;
                if (checking) if (tgiPosn != s.Position)
                        throw new InvalidDataException(String.Format("Position of TGIBlock read: 0x{0:X8}, actual: 0x{1:X8}",
                            tgiPosn, s.Position));

                if (tgiSize > 0) Parse(s);

                if (checking) if (tgiSize != s.Position - tgiPosn)
                        throw new InvalidDataException(String.Format("Size of TGIBlock read: 0x{0:X8}, actual: 0x{1:X8}; at 0x{2:X8}",
                            tgiSize, s.Position - tgiPosn, s.Position));
            }

            public void UnParse(Stream s, long ptgiO)
            {
                BinaryWriter w = new BinaryWriter(s);

                long tgiPosn = s.Position;
                UnParse(s);
                long pos = s.Position;

                s.Position = ptgiO;
                w.Write((uint)(tgiPosn - ptgiO - sizeof(uint)));
                w.Write((uint)(pos - tgiPosn));

                s.Position = pos;
            }
            #endregion

            #region ADependentList
            public override object Clone(T newParent) { return new TGIBlockList<T>(newParent, this); }
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("{0:X8}: {1}\n", i, this[i].Value); return s; } }
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
