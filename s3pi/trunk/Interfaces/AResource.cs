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
using System.Reflection;

namespace s3pi.Interfaces
{
    /// <summary>
    /// A resource contained in a package.
    /// </summary>
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
        public virtual Stream Stream
        {
            get
            {
                if (dirty || s3pi.Settings.Settings.AsBytesWorkaround)
                {
                    stream = UnParse();
                    dirty = false;
                    //Console.WriteLine(this.GetType().Name + " flushed.");
                }
                stream.Position = 0;
                return stream;
            }
        }
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
        public abstract class DependentList<T> : AHandlerList<T>, IGenericAdd
            where T : IEquatable<T>
        {
            protected EventHandler elementHandler; // Work around list event handler triggering during stream constructor

            #region Constructors
            // base class constructors...
            protected DependentList(EventHandler handler) : this(handler, -1) { }
            protected DependentList(EventHandler handler, long size) : base(handler, size) { }
            protected DependentList(EventHandler handler, IList<T> ilt) : this(handler, -1, ilt) { }
            protected DependentList(EventHandler handler, long size, IList<T> ilt) : base(handler, size, ilt) { }

            // Add stream-based constructors and support
            protected DependentList(EventHandler handler, Stream s) : this(handler, -1, s) { }
            protected DependentList(EventHandler handler, long size, Stream s) : base(null, size) { elementHandler = handler; Parse(s); this.handler = handler; }
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

            public virtual bool Add(params object[] fields)
            {
                if (fields == null) return false;
                Type elementType = typeof(T);
                if (fields.Length == 1 && elementType.IsAssignableFrom(fields[0].GetType()) && !typeof(AHandlerElement).IsAssignableFrom(elementType))
                {
                    base.Add((T)fields[0]);
                    return true;
                }

                if (elementType.IsAbstract) elementType = GetElementType(fields);

                Type[] types = new Type[2 + fields.Length];
                types[0] = typeof(int);
                types[1] = typeof(EventHandler);
                for (int i = 0; i < fields.Length; i++) types[2 + i] = fields[i].GetType();

                object[] args = new object[2 + fields.Length];
                args[0] = (int)0;
                args[1] = elementHandler;
                Array.Copy(fields, 0, args, 2, fields.Length);

                System.Reflection.ConstructorInfo ci = elementType.GetConstructor(types);
                if (ci == null) return false;
                base.Add((T)(elementType.GetConstructor(types).Invoke(args)));
                return true;
            }

            /// <summary>
            /// Return the type to get the constructor from, for the given set of fields.
            /// </summary>
            /// <param name="fields">Constructor parameters</param>
            /// <returns>Class on which to invoke constructor</returns>
            /// <remarks>fields[0] could be an instance of the abstract class: it should provide a constructor that accepts a "template"
            /// object and creates a new instance on that basis.</remarks>
            protected virtual Type GetElementType(params object[] fields) { throw new NotImplementedException(); }

            public abstract void Add();
        }

        public class TGIBlock : AResourceKey, IEquatable<TGIBlock>
        {
            #region Attributes
            const int recommendedApiVersion = 1;
            string order = "TGI";
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

            public TGIBlock(int APIversion, EventHandler handler, TGIBlock basis) : this(APIversion, handler, basis.order, (IResourceKey)basis) { }

            // With EPFlags
            public TGIBlock(int APIversion, EventHandler handler, uint resourceType, EPFlags epflags, uint resourceGroup, ulong instance)
                : base(APIversion, handler, resourceType, epflags, resourceGroup, instance) { }
            public TGIBlock(int APIversion, EventHandler handler, string order, uint resourceType, EPFlags epflags, uint resourceGroup, ulong instance)
                : this(APIversion, handler, resourceType, epflags, resourceGroup, instance) { ok(order); this.order = order; }
            public TGIBlock(int APIversion, EventHandler handler, Order order, uint resourceType, EPFlags epflags, uint resourceGroup, ulong instance)
                : this(APIversion, handler, resourceType, epflags, resourceGroup, instance) { ok(order); this.order = "" + order; }

            // Without EPFlags... not sure about these...
            public TGIBlock(int APIversion, EventHandler handler, uint resourceType, uint resourceGroup, ulong instance)
                : this(APIversion, handler, resourceType, (EPFlags)(resourceGroup >> 24), resourceGroup & 0x00FFFFFF, instance) { }
            public TGIBlock(int APIversion, EventHandler handler, string order, uint resourceType, uint resourceGroup, ulong instance)
                : this(APIversion, handler, resourceType, resourceGroup, instance) { ok(order); this.order = order; }
            public TGIBlock(int APIversion, EventHandler handler, Order order, uint resourceType, uint resourceGroup, ulong instance)
                : this(APIversion, handler, resourceType, resourceGroup, instance) { ok(order); this.order = "" + order; }

            public TGIBlock(int APIversion, EventHandler handler, IResourceKey rk) : base(APIversion, handler, rk) { }
            public TGIBlock(int APIversion, EventHandler handler, string order, IResourceKey rk) : this(APIversion, handler, rk) { ok(order); this.order = order; }
            public TGIBlock(int APIversion, EventHandler handler, Order order, IResourceKey rk) : this(APIversion, handler, rk) { ok(order); this.order = "" + order; }

            public TGIBlock(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public TGIBlock(int APIversion, EventHandler handler, string order, Stream s) : base(APIversion, handler) { ok(order); this.order = order; Parse(s); }
            public TGIBlock(int APIversion, EventHandler handler, Order order, Stream s) : base(APIversion, handler) { ok(order); this.order = "" + order; Parse(s); }
            #endregion

            #region Data I/O
            protected void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                UInt32 temp = 0;
                foreach (char c in order)
                    switch (c)
                    {
                        case 'T': resourceType = r.ReadUInt32(); break;
                        case 'G': temp = r.ReadUInt32(); break;
                        case 'I': instance = r.ReadUInt64(); break;
                    }
                epFlags = (EPFlags)(temp >> 24);
                resourceGroup = temp & 0x00FFFFFF;
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                foreach (char c in order)
                    switch (c)
                    {
                        case 'T': w.Write(resourceType); break;
                        case 'G': w.Write((uint)epFlags << 24 | resourceGroup); break;
                        case 'I': w.Write(instance); break;
                    }
            }
            #endregion

            #region AHandlerElement
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override AHandlerElement Clone(EventHandler handler) { return new TGIBlock(requestedApiVersion, handler, this); }
            #endregion

            #region IEquatable<TGIBlock> Members

            public bool Equals(TGIBlock other) { return this.Equals((IResourceKey)other); }

            #endregion

            #region Content Fields
            public String Value { get { return this.ToString(); } }
            #endregion
        }

        /// <summary>
        /// A TGIBlock list class where the count of elements is separate from the stored list
        /// </summary>
        public class CountedTGIBlockList : DependentList<TGIBlock>
        {
            uint origCount; // count at the time the list was constructed, used to Parse() list from stream
            string order = "TGI";

            #region Constructors
            public CountedTGIBlockList(EventHandler handler) : this(handler, -1, "TGI") { }
            public CountedTGIBlockList(EventHandler handler, IList<TGIBlock> lme) : this(handler, -1, "TGI", lme) { }
            public CountedTGIBlockList(EventHandler handler, uint count, Stream s) : this(handler, -1, "TGI", count, s) { }

            public CountedTGIBlockList(EventHandler handler, TGIBlock.Order order) : this(handler, -1, order) { }
            public CountedTGIBlockList(EventHandler handler, TGIBlock.Order order, IList<TGIBlock> lme) : this(handler, -1, order, lme) { }
            public CountedTGIBlockList(EventHandler handler, TGIBlock.Order order, uint count, Stream s) : this(handler, -1, order, count, s) { }

            public CountedTGIBlockList(EventHandler handler, string order) : this(handler, -1, order) { }
            public CountedTGIBlockList(EventHandler handler, string order, IList<TGIBlock> lme) : this(handler, -1, order, lme) { }
            public CountedTGIBlockList(EventHandler handler, string order, uint count, Stream s) : this(handler, -1, order, count, s) { }

            public CountedTGIBlockList(EventHandler handler, long max) : this(handler, max, "TGI") { }
            public CountedTGIBlockList(EventHandler handler, long max, IList<TGIBlock> lme) : this(handler, max, "TGI", lme) { }
            public CountedTGIBlockList(EventHandler handler, long max, uint count, Stream s) : this(handler, max, "TGI", count, s) { }

            public CountedTGIBlockList(EventHandler handler, long max, TGIBlock.Order order) : this(handler, max, "" + order) { }
            public CountedTGIBlockList(EventHandler handler, long max, TGIBlock.Order order, IList<TGIBlock> lme) : this(handler, max, "" + order, lme) { }
            public CountedTGIBlockList(EventHandler handler, long max, TGIBlock.Order order, uint count, Stream s) : this(handler, max, "" + order, count, s) { }

            public CountedTGIBlockList(EventHandler handler, long max, string order) : base(handler, max) { this.order = order; }
            public CountedTGIBlockList(EventHandler handler, long max, string order, IList<TGIBlock> lme) : base(handler, max, lme) { this.order = order; }
            public CountedTGIBlockList(EventHandler handler, long max, string order, uint count, Stream s) : base(null, max) { this.origCount = count; this.order = order; elementHandler = handler; Parse(s); this.handler = handler; }
            #endregion

            #region Data I/O
            protected override TGIBlock CreateElement(Stream s) { return new TGIBlock(0, elementHandler, order, s); }
            protected override void WriteElement(Stream s, TGIBlock element) { element.UnParse(s); }

            protected override uint ReadCount(Stream s) { return origCount; } // creator supplies
            protected override void WriteCount(Stream s, uint count) { } // creator stores
            #endregion

            public override void Add() { this.Add((uint)0, (uint)0, (ulong)0); }

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("0x{0:X8}: {1}\n", i, this[i].Value); return s; } }
            #endregion
        }

        /// <summary>
        /// A TGIBlock list class where the count and size of the list are stored separately (but managed by this class)
        /// </summary>
        public class TGIBlockList : DependentList<TGIBlock>
        {

            #region Constructors
            public TGIBlockList(EventHandler handler) : base(handler) { }
            public TGIBlockList(EventHandler handler, IList<TGIBlock> lme) : base(handler, lme) { }
            public TGIBlockList(EventHandler handler, Stream s, long tgiPosn, long tgiSize) : base(null) { elementHandler = handler; Parse(s, tgiPosn, tgiSize); this.handler = handler; }
            #endregion

            #region Data I/O
            protected override TGIBlock CreateElement(Stream s) { return new TGIBlock(0, elementHandler, s); }
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

            public override void Add() { this.Add(new TGIBlock(0, elementHandler, 0, 0, 0)); }

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("0x{0:X8}: {1}\n", i, this[i].Value); return s; } }
            #endregion
        }
        #endregion

        /// <summary>
        /// AResource classes must supply an UnParse() method that serializes the class to a stream that is returned.
        /// </summary>
        /// <returns>Stream containing serialized class data.</returns>
        protected abstract Stream UnParse();

        /// <summary>
        /// Used to indicate the resource has changed
        /// </summary>
        /// <param name="sender">The resource that has changed</param>
        /// <param name="e">(not used)</param>
        protected virtual void OnResourceChanged(object sender, EventArgs e)
        {
            dirty = true;
            //Console.WriteLine(this.GetType().Name + " dirtied.");
            if (ResourceChanged != null) ResourceChanged(sender, e);
        }
    }
}
