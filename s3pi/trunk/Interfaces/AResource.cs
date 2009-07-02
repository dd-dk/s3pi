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
        public virtual byte[] AsBytes
        {
            get
            {
                MemoryStream s = this.Stream as MemoryStream;
                if (s != null) return s.ToArray();

                stream.Position = 0;
                return (new BinaryReader(stream)).ReadBytes((int)stream.Length);
            }
        }

        /// <summary>
        /// Raised if the resource is changed
        /// </summary>
        public event EventHandler ResourceChanged;

        #endregion

        #region Sub-classes
        public abstract class DependentList<T> : AHandlerList<T>
            where T : IEquatable<T>
        {
            #region Constructors
            // base class constructors...
            protected DependentList(EventHandler handler) : this(handler, -1) { }
            protected DependentList(EventHandler handler, long size) : base(handler, size) { }
            protected DependentList(EventHandler handler, IList<T> ilt) : this(handler, -1, ilt) { }
            protected DependentList(EventHandler handler, long size, IList<T> ilt) : base(handler, size, ilt) { }

            // Add stream-based constructors and support
            protected DependentList(EventHandler handler, Stream s) : this(handler, -1, s) { }
            protected DependentList(EventHandler handler, long size, Stream s) : base(handler, size) { Parse(s); }
            #endregion

            #region Data I/O
            /// <summary>
            /// Read list entries from a stream
            /// </summary>
            /// <param name="s">Stream containing list entries</param>
            protected virtual void Parse(Stream s) { base.Clear(); bool inc = true; for (uint i = ReadCount(s); i > 0; i = (uint)(i - (inc ? 1 : 0))) base.Add(CreateElement(s, out inc)); }
            protected virtual uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadUInt32(); }
            protected abstract T CreateElement(Stream s);
            protected virtual T CreateElement(Stream s, out bool inc) { inc = true; return CreateElement(s); }

            /// <summary>
            /// Write list entries to a stream
            /// </summary>
            /// <param name="s">Stream to receive list entries</param>
            public virtual void UnParse(Stream s) { WriteCount(s, (uint)Count); foreach (T element in this) WriteElement(s, element); }
            protected virtual void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write(count); }
            protected abstract void WriteElement(Stream s, T element);
            #endregion
        }

        public class TGIBlock : AHandlerElement, IComparable<TGIBlock>, IEqualityComparer<TGIBlock>, IEquatable<TGIBlock>
        {
            #region Attributes
            const int recommendedApiVersion = 1;
            string order = "TGI";
            uint resourceType;
            uint resourceGroup;
            ulong instance;
            #endregion

            #region Constructors
            public enum Order
            {
                TGI,
                TIG,
                GTI,
                GIT,
                ITG,
                IGT,
            }
            void ok(string v) { ok((Order)Enum.Parse(typeof(Order), v)); }
            void ok(Order v) { if (!Enum.IsDefined(typeof(Order), v)) throw new ArgumentException("Invalid value " + v, "order"); }

            public TGIBlock(int APIversion, EventHandler handler, uint resourceType, uint resourceGroup, ulong instance)
                : base(APIversion, handler)
            {
                this.resourceType = resourceType;
                this.resourceGroup = resourceGroup;
                this.instance = instance;
            }

            public TGIBlock(int APIversion, EventHandler handler, string order, uint resourceType, uint resourceGroup, ulong instance)
                : this(APIversion, handler, resourceType, resourceGroup, instance) { ok(order); this.order = order; }
            public TGIBlock(int APIversion, EventHandler handler, Order order, uint resourceType, uint resourceGroup, ulong instance)
                : this(APIversion, handler, resourceType, resourceGroup, instance) { ok(order); this.order = "" + order; }

            public TGIBlock(int APIversion, EventHandler handler, TGIBlock tgib) : this(APIversion, handler, "TGI", tgib) { }
            public TGIBlock(int APIversion, EventHandler handler, Order order, TGIBlock tgib) : this(APIversion, handler, order, tgib.resourceType, tgib.resourceGroup, tgib.instance) { }
            public TGIBlock(int APIversion, EventHandler handler, string order, TGIBlock tgib) : this(APIversion, handler, order, tgib.resourceType, tgib.resourceGroup, tgib.instance) { }

            // With stream, order is needed in the constructor for parsing
            public TGIBlock(int APIversion, EventHandler handler, Stream s) : this(APIversion, handler, "TGI", s) { }
            public TGIBlock(int APIversion, EventHandler handler, Order order, Stream s) : this(APIversion, handler, "" + order, s) { }
            public TGIBlock(int APIversion, EventHandler handler, string order, Stream s) : base(APIversion, handler) { ok(order); this.order = order; Parse(s); }
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

            #region IComparable<TGIBlock> Members

            public int CompareTo(TGIBlock other)
            {
                int res = resourceType.CompareTo(other.resourceType); if (res != 0) return res;
                res = resourceGroup.CompareTo(other.resourceGroup); if (res != 0) return res;
                return instance.CompareTo(other.instance);
            }

            #endregion

            #region IEqualityComparer<TGIBlock> Members

            public bool Equals(TGIBlock x, TGIBlock y) { return x.Equals(y); }

            public int GetHashCode(TGIBlock obj) { return obj.GetHashCode(); }

            public override int GetHashCode() { return resourceType.GetHashCode() ^ resourceGroup.GetHashCode() ^ instance.GetHashCode(); }

            #endregion

            #region IEquatable<TGIBlock> Members

            public bool Equals(TGIBlock other) { return this.CompareTo(other) == 0; }

            #endregion

            #region AHandlerElement
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override AHandlerElement Clone(EventHandler handler) { return new TGIBlock(requestedApiVersion, handler, this); }
            #endregion

            #region Content Fields
            public uint ResourceType { get { return resourceType; } set { if (resourceType != value) { resourceType = value; OnElementChanged(); } } }
            public uint ResourceGroup { get { return resourceGroup; } set { if (resourceGroup != value) { resourceGroup = value; OnElementChanged(); } } }
            public ulong Instance { get { return instance; } set { if (instance != value) { instance = value; OnElementChanged(); } } }

            public String Value { get { return String.Format("0x{0:X8}-0x{1:X8}-0x{2:X16}", resourceType, resourceGroup, instance); } }
            #endregion

            public override string ToString() { return Value; }
            public static implicit operator String(TGIBlock value) { return value.ToString(); }
        }

        /// <summary>
        /// A TGIBlock list class where the count of elements is separate from the stored list
        /// </summary>
        public class CountedTGIBlockList : DependentList<TGIBlock>
        {
            uint origCount; // count at the time the list was constructed, used to Parse() list from stream
            string order = "TGI";

            #region Constructors
            public CountedTGIBlockList(EventHandler handler) : base(handler) { }
            public CountedTGIBlockList(EventHandler handler, string order) : base(handler) { this.order = order; }
            public CountedTGIBlockList(EventHandler handler, TGIBlock.Order order) : this(handler, "" + order) { }

            public CountedTGIBlockList(EventHandler handler, IList<TGIBlock> lme) : base(handler, lme) { }
            public CountedTGIBlockList(EventHandler handler, string order, IList<TGIBlock> lme) : base(handler, lme) { this.order = order; }
            public CountedTGIBlockList(EventHandler handler, TGIBlock.Order order, IList<TGIBlock> lme) : this(handler, "" + order, lme) { }

            public CountedTGIBlockList(EventHandler handler, uint count, Stream s) : base(handler) { this.origCount = count; Parse(s); }
            public CountedTGIBlockList(EventHandler handler, string order, uint count, Stream s) : this(handler, order) { this.origCount = count; Parse(s); }
            public CountedTGIBlockList(EventHandler handler, TGIBlock.Order order, uint count, Stream s) : this(handler, "" + order, count, s) { }
            #endregion

            #region Data I/O
            protected override TGIBlock CreateElement(Stream s) { return new TGIBlock(0, handler, order, s); }
            protected override void WriteElement(Stream s, TGIBlock element) { element.UnParse(s); }

            protected override uint ReadCount(Stream s) { return origCount; } // creator supplies
            protected override void WriteCount(Stream s, uint count) { } // creator stores
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("{0:X8}: {1}\n", i, this[i].Value); return s; } }
            #endregion
        }

        /// <summary>
        /// A TGIBlock list class where the count and size of the list are stored separately (but managed by this class)
        /// </summary>
        /// <typeparam name="T">Class of the parent</typeparam>
        public class TGIBlockList : DependentList<TGIBlock>
        {
            #region Constructors
            public TGIBlockList(EventHandler handler) : base(handler) { }
            public TGIBlockList(EventHandler handler, IList<TGIBlock> lme) : base(handler, lme) { }
            public TGIBlockList(EventHandler handler, Stream s, long tgiOffset, long tgiSize) : base(handler) { Parse(s, tgiOffset, tgiSize); }
            #endregion

            #region Data I/O
            protected override TGIBlock CreateElement(Stream s) { return new TGIBlock(0, handler, s); }
            protected override void WriteElement(Stream s, TGIBlock element) { element.UnParse(s); }

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
