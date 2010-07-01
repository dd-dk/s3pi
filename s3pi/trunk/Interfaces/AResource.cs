﻿/***************************************************************************
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
        /// Resource data <see cref="System.IO.Stream"/>
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
        /// <param name="s"><see cref="System.IO.Stream"/> to use, or null to create from scratch.</param>
        protected AResource(int APIversion, Stream s)
        {
            requestedApiVersion = APIversion;
            stream = s;
        }
        #endregion

        #region AApiVersionedFields
        /// <summary>
        /// A <c>List&lt;string&gt;</c> of available field names on object
        /// </summary>
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
        #endregion

        #region IResource Members
        /// <summary>
        /// The resource content as a <see cref="System.IO.Stream"/>.
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
        /// The resource content as a <see cref="byte"/> array
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
        /// <summary>
        /// Abstract extension to <see cref="AHandlerList{T}"/> adding support for <see cref="System.IO.Stream"/> IO
        /// and partially implementing <see cref="IGenericAdd"/>.
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> of list element</typeparam>
        /// <seealso cref="AHandlerList{T}"/>
        /// <seealso cref="IGenericAdd"/>
        public abstract class DependentList<T> : AHandlerList<T>, IGenericAdd
            where T : IEquatable<T>
        {
            /// <summary>
            /// Holds the <see cref="EventHandler"/> delegate to invoke if an element in the <see cref="DependentList{T}"/> changes.
            /// </summary>
            /// <remarks>Work around for list event handler triggering during stream constructor and other places.</remarks>
            protected EventHandler elementHandler;

            #region Constructors
            // base class constructors...
            /// <summary>
            /// Initializes a new instance of the <see cref="DependentList{T}"/> class
            /// that is empty
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            protected DependentList(EventHandler handler) : this(handler, -1) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="DependentList{T}"/> class
            /// that is empty
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            protected DependentList(EventHandler handler, long size) : base(handler, size) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="DependentList{T}"/> class
            /// filled with the content of <paramref name="ilt"/>
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="ilt">The <see cref="IList{T}"/> to use as the initial content of the list.</param>
            protected DependentList(EventHandler handler, IList<T> ilt) : this(handler, -1, ilt) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="DependentList{T}"/> class
            /// filled with the content of <paramref name="ilt"/>
            /// and with maximum size of <paramref name="size"/>.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            /// <param name="ilt">The <see cref="IList{T}"/> to use as the initial content of the list.</param>
            /// <remarks>Does not throw an exception if <paramref name="ilt"/>.Count is greater than <paramref name="size"/>.
            /// An exception will be thrown on any attempt to add further items unless the Count is reduced first.</remarks>
            protected DependentList(EventHandler handler, long size, IList<T> ilt) : base(handler, size, ilt) { }

            // Add stream-based constructors and support
            /// <summary>
            /// Initializes a new instance of the <see cref="DependentList{T}"/> class
            /// filled from <see cref="System.IO.Stream"/> <paramref name="s"/>
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
            protected DependentList(EventHandler handler, Stream s) : this(handler, -1, s) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="DependentList{T}"/> class
            /// filled from <see cref="System.IO.Stream"/> <paramref name="s"/>
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
            /// <exception cref="System.InvalidOperationException">Thrown when list size exceeded.</exception>
            protected DependentList(EventHandler handler, long size, Stream s) : base(null, size) { elementHandler = handler; Parse(s); this.handler = handler; }
            #endregion

            #region Data I/O
            /// <summary>
            /// Read list entries from a stream
            /// </summary>
            /// <param name="s">Stream containing list entries</param>
            /// <remarks>This method bypasses <see cref="DependentList{T}.Add(object[])"/>
            /// because <see cref="CreateElement(Stream, out bool)"/> must take care of the same issues.</remarks>
            protected virtual void Parse(Stream s) { base.Clear(); bool inc = true; for (uint i = ReadCount(s); i > 0; i = (uint)(i - (inc ? 1 : 0))) base.Add(CreateElement(s, out inc)); }
            /// <summary>
            /// Return the number of elements to be created.
            /// </summary>
            /// <param name="s"><see cref="System.IO.Stream"/> being processed.</param>
            /// <returns>The number of elements to be created.</returns>
            protected virtual uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadUInt32(); }
            /// <summary>
            /// Create a new element from the <see cref="System.IO.Stream"/>.
            /// </summary>
            /// <param name="s">Stream containing element data.</param>
            /// <returns>A new element.</returns>
            protected abstract T CreateElement(Stream s);
            /// <summary>
            /// Create a new element from the <see cref="System.IO.Stream"/> and indicates whether it counts towards the number of elements to be created.
            /// </summary>
            /// <param name="s"><see cref="System.IO.Stream"/> containing element data.</param>
            /// <param name="inc">Whether this call towards the number of elements to be created.</param>
            /// <returns>A new element.</returns>
            protected virtual T CreateElement(Stream s, out bool inc) { inc = true; return CreateElement(s); }

            /// <summary>
            /// Write list entries to a stream
            /// </summary>
            /// <param name="s">Stream to receive list entries</param>
            public virtual void UnParse(Stream s) { WriteCount(s, (uint)Count); foreach (T element in this) WriteElement(s, element); }
            /// <summary>
            /// Write the count of list elements to the stream.
            /// </summary>
            /// <param name="s"><see cref="System.IO.Stream"/> to write <paramref name="count"/> to.</param>
            /// <param name="count">Value to write to <see cref="System.IO.Stream"/> <paramref name="s"/>.</param>
            protected virtual void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write(count); }
            /// <summary>
            /// Write an element to the stream.
            /// </summary>
            /// <param name="s"><see cref="System.IO.Stream"/> to write <paramref name="element"/> to.</param>
            /// <param name="element">Value to write to <see cref="System.IO.Stream"/> <paramref name="s"/>.</param>
            protected abstract void WriteElement(Stream s, T element);
            #endregion

            #region IGenericAdd
            /// <summary>
            /// Add a default element to a <see cref="DependentList{T}"/>.
            /// </summary>
            /// <exception cref="NotImplementedException">Lists of abstract classes will fail
            /// with a NotImplementedException.</exception>
            /// <exception cref="InvalidOperationException">Thrown when list size exceeded.</exception>
            /// <exception cref="NotSupportedException">The <see cref="DependentList{T}"/> is read-only.</exception>
            public abstract void Add();

            /// <summary>
            /// Adds an entry to an <see cref="DependentList{T}"/>.
            /// </summary>
            /// <param name="fields">
            /// Either the object to add or the generic type&apos;s constructor arguments.
            /// </param>
            /// <returns>True on success</returns>
            /// <exception cref="InvalidOperationException">Thrown when list size exceeded.</exception>
            /// <exception cref="NotSupportedException">The <see cref="DependentList{T}"/> is read-only.</exception>
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
            /// <remarks><paramref name="fields"/>[0] could be an instance of the abstract class: it should provide a constructor that accepts a "template"
            /// object and creates a new instance on that basis.</remarks>
            protected virtual Type GetElementType(params object[] fields) { throw new NotImplementedException(); }
            #endregion
        }

        /// <summary>
        /// An implementation of AResourceKey that supports storing the Type, Group and Instance in any order.
        /// </summary>
        public class TGIBlock : AResourceKey, IEquatable<TGIBlock>
        {
            #region Attributes
            const int recommendedApiVersion = 1;
            string order = "TGI";
            #endregion

            #region Constructors
            /// <summary>
            /// Options for the order of the Type, Group and Instance elements of a TGIBlock
            /// </summary>
            public enum Order
            {
                /// <summary>
                /// Type, Group, Instance
                /// </summary>
                TGI,
                /// <summary>
                /// Type, Instance, Group
                /// </summary>
                TIG,
                /// <summary>
                /// Group, Type, Instance
                /// </summary>
                GTI,
                /// <summary>
                /// Group, Instance, Type
                /// </summary>
                GIT,
                /// <summary>
                /// Instance, Type, Group
                /// </summary>
                ITG,
                /// <summary>
                /// Instance, Group, Type
                /// </summary>
                IGT,
            }
            void ok(string v) { ok((Order)Enum.Parse(typeof(Order), v)); }
            void ok(Order v) { if (!Enum.IsDefined(typeof(Order), v)) throw new ArgumentException("Invalid value " + v, "order"); }

            /// <summary>
            /// Initialize a new TGIBlock
            /// with the order and values
            /// based on <paramref name="basis"/>.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="basis">The TGIBlock to use for the <see cref="Order"/> and <see cref="IResourceKey"/> values.</param>
            public TGIBlock(int APIversion, EventHandler handler, TGIBlock basis) : this(APIversion, handler, basis.order, (IResourceKey)basis) { }

            /// <summary>
            /// Initialize a new TGIBlock
            /// with the default order ("TGI").
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            public TGIBlock(int APIversion, EventHandler handler) : base(APIversion, handler, 0, 0, 0) { }
            /// <summary>
            /// Initialize a new TGIBlock
            /// with the specified order.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="order">A <see cref="string"/> representing the <see cref="Order"/> to use to store the <see cref="IResourceKey"/> values.</param>
            public TGIBlock(int APIversion, EventHandler handler, string order) : this(APIversion, handler) { ok(order); this.order = order; }
            /// <summary>
            /// Initialize a new TGIBlock
            /// with the specified order.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="order">The <see cref="Order"/> to use to store the <see cref="IResourceKey"/> values.</param>
            public TGIBlock(int APIversion, EventHandler handler, Order order) : this(APIversion, handler) { ok(order); this.order = "" + order; }

            /// <summary>
            /// Initialize a new TGIBlock
            /// with the default order ("TGI") and specified values.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="resourceType">The resource type value.</param>
            /// <param name="resourceGroup">The resource group value.</param>
            /// <param name="instance">The resource instance value.</param>
            public TGIBlock(int APIversion, EventHandler handler, uint resourceType, uint resourceGroup, ulong instance)
                : base(APIversion, handler, resourceType, resourceGroup, instance) { }
            /// <summary>
            /// Initialize a new TGIBlock
            /// with the specified order and values.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="order">A <see cref="string"/> representing the <see cref="Order"/> to use to store the <see cref="IResourceKey"/> values.</param>
            /// <param name="resourceType">The resource type value.</param>
            /// <param name="resourceGroup">The resource group value.</param>
            /// <param name="instance">The resource instance value.</param>
            public TGIBlock(int APIversion, EventHandler handler, string order, uint resourceType, uint resourceGroup, ulong instance)
                : this(APIversion, handler, resourceType, resourceGroup, instance) { ok(order); this.order = order; }
            /// <summary>
            /// Initialize a new TGIBlock
            /// with the specified order and values.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="order">The <see cref="Order"/> to use to store the <see cref="IResourceKey"/> values.</param>
            /// <param name="resourceType">The resource type value.</param>
            /// <param name="resourceGroup">The resource group value.</param>
            /// <param name="instance">The resource instance value.</param>
            public TGIBlock(int APIversion, EventHandler handler, Order order, uint resourceType, uint resourceGroup, ulong instance)
                : this(APIversion, handler, resourceType, resourceGroup, instance) { ok(order); this.order = "" + order; }

            /// <summary>
            /// Initialize a new TGIBlock
            /// with the default order ("TGI") and specified <see cref="IResourceKey"/> values.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="rk">The <see cref="IResourceKey"/> values.</param>
            public TGIBlock(int APIversion, EventHandler handler, IResourceKey rk) : base(APIversion, handler, rk) { }
            /// <summary>
            /// Initialize a new TGIBlock
            /// with the specified order and <see cref="IResourceKey"/> values.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="order">A <see cref="string"/> representing the <see cref="Order"/> to use to store the <see cref="IResourceKey"/> values.</param>
            /// <param name="rk">The <see cref="IResourceKey"/> values.</param>
            public TGIBlock(int APIversion, EventHandler handler, string order, IResourceKey rk) : this(APIversion, handler, rk) { ok(order); this.order = order; }
            /// <summary>
            /// Initialize a new TGIBlock
            /// with the specified order and <see cref="IResourceKey"/> values.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="order">The <see cref="Order"/> to use to store the <see cref="IResourceKey"/> values.</param>
            /// <param name="rk">The <see cref="IResourceKey"/> values.</param>
            public TGIBlock(int APIversion, EventHandler handler, Order order, IResourceKey rk) : this(APIversion, handler, rk) { ok(order); this.order = "" + order; }

            /// <summary>
            /// Initialize a new TGIBlock
            /// with the default order ("TGI") and values read from the specified <see cref="System.IO.Stream"/>.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> from which to read the <see cref="IResourceKey"/> values.</param>
            public TGIBlock(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            /// <summary>
            /// Initialize a new TGIBlock
            /// with the specified order and values read from the specified <see cref="System.IO.Stream"/>.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="order">A <see cref="string"/> representing the <see cref="Order"/> of the <see cref="IResourceKey"/> values.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> from which to read the <see cref="IResourceKey"/> values.</param>
            public TGIBlock(int APIversion, EventHandler handler, string order, Stream s) : base(APIversion, handler) { ok(order); this.order = order; Parse(s); }
            /// <summary>
            /// Initialize a new TGIBlock
            /// with the specified order and values read from the specified <see cref="System.IO.Stream"/>.
            /// </summary>
            /// <param name="APIversion">The requested API version.</param>
            /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
            /// <param name="order">The <see cref="Order"/> of the <see cref="IResourceKey"/> values.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> from which to read the <see cref="IResourceKey"/> values.</param>
            public TGIBlock(int APIversion, EventHandler handler, Order order, Stream s) : base(APIversion, handler) { ok(order); this.order = "" + order; Parse(s); }
            #endregion

            #region Data I/O
            /// <summary>
            /// Used by the <see cref="TGIBlock"/> constructor to inialise a new <see cref="TGIBlock"/> from a <see cref="System.IO.Stream"/>.
            /// </summary>
            /// <param name="s"><see cref="System.IO.Stream"/> containing <see cref="TGIBlock"/> values in known order.</param>
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

            /// <summary>
            /// Writes the <see cref="TGIBlock"/> to the specified <see cref="System.IO.Stream"/> in known order.
            /// </summary>
            /// <param name="s"><see cref="System.IO.Stream"/> to write <see cref="TGIBlock"/> values to.</param>
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

            #region AHandlerElement
            /// <summary>
            /// The list of available field names on this API object
            /// </summary>
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            /// <summary>
            /// The best supported version of the API available
            /// </summary>
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            /// <summary>
            /// Get a copy of the <see cref="TGIBlock"/> but with a new change <see cref="EventHandler"/>.
            /// </summary>
            /// <param name="handler">The replacement <see cref="EventHandler"/> delegate.</param>
            /// <returns>Return a copy of the <see cref="TGIBlock"/> but with a new change <see cref="EventHandler"/>.</returns>
            public override AHandlerElement Clone(EventHandler handler) { return new TGIBlock(requestedApiVersion, handler, this); }
            #endregion

            #region IEquatable<TGIBlock> Members

            /// <summary>
            /// Indicates whether the current <see cref="TGIBlock"/> instance is equal to another <see cref="TGIBlock"/> instance.
            /// </summary>
            /// <param name="other">An <see cref="TGIBlock"/> instance to compare with this instance.</param>
            /// <returns>true if the current instance is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
            public bool Equals(TGIBlock other) { return this.Equals((IResourceKey)other); }

            #endregion

            #region Content Fields
            /// <summary>
            /// A display-ready string representing the <see cref="TGIBlock"/>.
            /// </summary>
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
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// that is empty
            /// with <see cref="TGIBlock.Order"/> of "TGI"
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            public CountedTGIBlockList(EventHandler handler) : this(handler, -1, "TGI") { }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with the content of <paramref name="ilt"/>
            /// with <see cref="TGIBlock.Order"/> of "TGI"
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="ilt">The <c>IList&lt;TGIBlock&gt;</c> to use as the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, IList<TGIBlock> ilt) : this(handler, -1, "TGI", ilt) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with <paramref name="count"/> elements from <see cref="System.IO.Stream"/> <paramref name="s"/>
            /// with <see cref="TGIBlock.Order"/> of "TGI"
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="count">The number of list elements to read.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, uint count, Stream s) : this(handler, -1, "TGI", count, s) { }

            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// that is empty
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="order">The <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            public CountedTGIBlockList(EventHandler handler, TGIBlock.Order order) : this(handler, -1, order) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with the content of <paramref name="ilt"/>
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="order">The <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            /// <param name="ilt">The <c>IList&lt;TGIBlock&gt;</c> to use as the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, TGIBlock.Order order, IList<TGIBlock> ilt) : this(handler, -1, order, ilt) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with <paramref name="count"/> elements from <see cref="System.IO.Stream"/> <paramref name="s"/>
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="order">The <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            /// <param name="count">The number of list elements to read.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, TGIBlock.Order order, uint count, Stream s) : this(handler, -1, order, count, s) { }

            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// that is empty
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="order">A string representing the <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            public CountedTGIBlockList(EventHandler handler, string order) : this(handler, -1, order) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with the content of <paramref name="ilt"/>
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="order">A string representing the <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            /// <param name="ilt">The <c>IList&lt;TGIBlock&gt;</c> to use as the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, string order, IList<TGIBlock> ilt) : this(handler, -1, order, ilt) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with <paramref name="count"/> elements from <see cref="System.IO.Stream"/> <paramref name="s"/>
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with an unlimited size.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="order">A string representing the <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            /// <param name="count">The number of list elements to read.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, string order, uint count, Stream s) : this(handler, -1, order, count, s) { }

            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// that is empty
            /// with <see cref="TGIBlock.Order"/> of "TGI"
            /// and with a specified maximum number of entries.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            public CountedTGIBlockList(EventHandler handler, long size) : this(handler, size, "TGI") { }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with the content of <paramref name="ilt"/>
            /// with <see cref="TGIBlock.Order"/> of "TGI"
            /// and with a specified maximum number of entries.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            /// <param name="ilt">The <c>IList&lt;TGIBlock&gt;</c> to use as the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, long size, IList<TGIBlock> ilt) : this(handler, size, "TGI", ilt) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with <paramref name="count"/> elements from <see cref="System.IO.Stream"/> <paramref name="s"/>
            /// with <see cref="TGIBlock.Order"/> of "TGI"
            /// and with a specified maximum number of entries.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            /// <param name="count">The number of list elements to read.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, long size, uint count, Stream s) : this(handler, size, "TGI", count, s) { }

            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// that is empty
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with a specified maximum number of entries.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            /// <param name="order">The <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            public CountedTGIBlockList(EventHandler handler, long size, TGIBlock.Order order) : this(handler, size, "" + order) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with the content of <paramref name="ilt"/>
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with a specified maximum number of entries.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            /// <param name="order">The <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            /// <param name="ilt">The <c>IList&lt;TGIBlock&gt;</c> to use as the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, long size, TGIBlock.Order order, IList<TGIBlock> ilt) : this(handler, size, "" + order, ilt) { }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with <paramref name="count"/> elements from <see cref="System.IO.Stream"/> <paramref name="s"/>
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with a specified maximum number of entries.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            /// <param name="order">The <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            /// <param name="count">The number of list elements to read.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, long size, TGIBlock.Order order, uint count, Stream s) : this(handler, size, "" + order, count, s) { }

            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// that is empty
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with a specified maximum number of entries.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            /// <param name="order">A string representing the <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            public CountedTGIBlockList(EventHandler handler, long size, string order) : base(handler, size) { this.order = order; }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with the content of <paramref name="ilt"/>
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with a specified maximum number of entries.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            /// <param name="order">A string representing the <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            /// <param name="ilt">The <c>IList&lt;TGIBlock&gt;</c> to use as the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, long size, string order, IList<TGIBlock> ilt) : base(handler, size, ilt) { this.order = order; }
            /// <summary>
            /// Initializes a new instance of the <see cref="CountedTGIBlockList"/> class
            /// filled with <paramref name="count"/> elements from <see cref="System.IO.Stream"/> <paramref name="s"/>
            /// with the specified <see cref="TGIBlock.Order"/>
            /// and with a specified maximum number of entries.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="size">Maximum number of elements in the list.</param>
            /// <param name="order">A string representing the <see cref="TGIBlock.Order"/> of the <see cref="TGIBlock"/> values.</param>
            /// <param name="count">The number of list elements to read.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
            public CountedTGIBlockList(EventHandler handler, long size, string order, uint count, Stream s) : base(null, size) { this.origCount = count; this.order = order; elementHandler = handler; Parse(s); this.handler = handler; }
            #endregion

            #region Data I/O
            /// <summary>
            /// Create a new element from the <see cref="System.IO.Stream"/>.
            /// </summary>
            /// <param name="s">Stream containing element data.</param>
            /// <returns>A new element.</returns>
            protected override TGIBlock CreateElement(Stream s) { return new TGIBlock(0, elementHandler, order, s); }
            /// <summary>
            /// Write an element to the stream.
            /// </summary>
            /// <param name="s"><see cref="System.IO.Stream"/> to write <paramref name="element"/> to.</param>
            /// <param name="element">Value to write to <see cref="System.IO.Stream"/> <paramref name="s"/>.</param>
            protected override void WriteElement(Stream s, TGIBlock element) { element.UnParse(s); }

            /// <summary>
            /// Return the number of elements to be created.
            /// </summary>
            /// <param name="s"><see cref="System.IO.Stream"/> being processed -- ignored.</param>
            /// <returns>The number of elements to be created, as provided to the <see cref="CountedTGIBlockList"/> constructor.</returns>
            protected override uint ReadCount(Stream s) { return origCount; }
            /// <summary>
            /// This list does not manage a count within the <see cref="System.IO.Stream"/>.
            /// </summary>
            /// <param name="s">Ignored.</param>
            /// <param name="count">Ignored.</param>
            protected override void WriteCount(Stream s, uint count) { }
            #endregion

            /// <summary>
            /// Add a new default element to the list.
            /// </summary>
            public override void Add() { base.Add(new TGIBlock(0, null, order)); } // Need to pass "order"

            /// <summary>
            /// Adds a new <see cref="TGIBlock"/> to the list using the values of the specified <see cref="TGIBlock"/>.
            /// </summary>
            /// <param name="item">The <see cref="TGIBlock"/> to use as a basis for the new <see cref="TGIBlock"/>.</param>
            /// <exception cref="System.InvalidOperationException">Thrown when list size exceeded.</exception>
            /// <exception cref="System.NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
            /// <remarks>A new element is created rather than using the element passed
            /// as the order (TGI/ITG/etc) may be different.</remarks>
            public override void Add(TGIBlock item) { base.Add(new TGIBlock(0, elementHandler, order, item)); }

            /// <summary>
            /// Adds a new TGIBlock to the list using the values of the IResourceKey.
            /// </summary>
            /// <param name="rk">The ResourceKey values to use for the TGIBlock.</param>
            /// <remarks>A new element is created rather than using the element passed
            /// as the order (TGI/ITG/etc) may be different.</remarks>
            public void Add(IResourceKey rk) { base.Add(new TGIBlock(0, elementHandler, order, rk)); }

            /// <summary>
            /// Inserts a new <see cref="TGIBlock"/> to the list at the specified index using the values of the specified <see cref="TGIBlock"/>.
            /// </summary>
            /// <param name="index">The zero-based index at which the new <see cref="TGIBlock"/> should be inserted.</param>
            /// <param name="item">The <see cref="TGIBlock"/> to use as a basis for the new <see cref="TGIBlock"/>.</param>
            /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index
            /// in the <see cref="CountedTGIBlockList"/>.</exception>
            /// <exception cref="System.InvalidOperationException">Thrown when list size exceeded.</exception>
            /// <exception cref="System.NotSupportedException">The <see cref="CountedTGIBlockList"/> is read-only.</exception>
            /// <remarks>A new element is created rather than using the element passed
            /// as the order (TGI/ITG/etc) may be different.</remarks>
            public override void Insert(int index, TGIBlock item) { base.Insert(index, new TGIBlock(0, elementHandler, order, item)); }

            #region Content Fields
            /// <summary>
            /// A displayable string representing the list.
            /// </summary>
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("0x{0:X8}: {1}\n", i, this[i].Value); return s; } }
            #endregion
        }

        /// <summary>
        /// A TGIBlock list class where the count and size of the list are stored separately (but managed by this class)
        /// </summary>
        public class TGIBlockList : DependentList<TGIBlock>
        {
            bool addEight = false;

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="TGIBlockList"/> class
            /// that is empty.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="addEight">When true, invoke fudge factor in parse/unparse</param>
            public TGIBlockList(EventHandler handler, bool addEight = false) : base(handler) { this.addEight = addEight; }
            /// <summary>
            /// Initializes a new instance of the <see cref="TGIBlockList"/> class
            /// filled with the content of <paramref name="ilt"/>.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="ilt">The <c>IList&lt;TGIBlock&gt;</c> to use as the initial content of the list.</param>
            /// <param name="addEight">When true, invoke fudge factor in parse/unparse</param>
            public TGIBlockList(EventHandler handler, IList<TGIBlock> ilt, bool addEight = false) : base(handler, ilt) { this.addEight = addEight; }
            /// <summary>
            /// Initializes a new instance of the <see cref="TGIBlockList"/> class
            /// filled with elements from <see cref="System.IO.Stream"/> <paramref name="s"/>.
            /// </summary>
            /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
            /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
            /// <param name="tgiPosn">Position in the <see cref="System.IO.Stream"/> where the list of <see cref="TGIBlock"/>s starts.</param>
            /// <param name="tgiSize">Size (in bytes) of the stored list.</param>
            /// <param name="addEight">When true, invoke fudge factor in parse/unparse</param>
            public TGIBlockList(EventHandler handler, Stream s, long tgiPosn, long tgiSize, bool addEight = false) : base(null) { elementHandler = handler; this.addEight = addEight; Parse(s, tgiPosn, tgiSize); this.handler = handler; }
            #endregion

            #region Data I/O
            /// <summary>
            /// Create a new element from the <see cref="System.IO.Stream"/>.
            /// </summary>
            /// <param name="s">Stream containing element data.</param>
            /// <returns>A new element.</returns>
            protected override TGIBlock CreateElement(Stream s) { return new TGIBlock(0, elementHandler, s); }
            /// <summary>
            /// Write an element to the stream.
            /// </summary>
            /// <param name="s"><see cref="System.IO.Stream"/> to write <paramref name="element"/> to.</param>
            /// <param name="element">Value to write to <see cref="System.IO.Stream"/> <paramref name="s"/>.</param>
            protected override void WriteElement(Stream s, TGIBlock element) { element.UnParse(s); }

            /// <summary>
            /// Read list entries from a stream
            /// </summary>
            /// <param name="s">Stream containing list entries</param>
            /// <param name="tgiPosn">Position in the <see cref="System.IO.Stream"/> where the list of <see cref="TGIBlock"/>s starts.</param>
            /// <param name="tgiSize">Size (in bytes) of the stored list.</param>
            protected void Parse(Stream s, long tgiPosn, long tgiSize)
            {
                bool checking = true;
                if (checking) if (tgiPosn != s.Position)
                        throw new InvalidDataException(String.Format("Position of TGIBlock read: 0x{0:X8}, actual: 0x{1:X8}",
                            tgiPosn, s.Position));

                if (tgiSize > 0) Parse(s);

                if (checking) if (tgiSize != s.Position - tgiPosn + (addEight ? 8 : 0))
                        throw new InvalidDataException(String.Format("Size of TGIBlock read: 0x{0:X8}, actual: 0x{1:X8}; at 0x{2:X8}",
                            tgiSize, s.Position - tgiPosn, s.Position));
            }

            /// <summary>
            /// Write list entries to a stream
            /// </summary>
            /// <param name="s">Stream to receive list entries</param>
            /// <param name="ptgiO">Position in <see cref="System.IO.Stream"/> to write list position and size values.</param>
            public void UnParse(Stream s, long ptgiO)
            {
                BinaryWriter w = new BinaryWriter(s);

                long tgiPosn = s.Position;
                UnParse(s);
                long pos = s.Position;

                s.Position = ptgiO;
                w.Write((uint)(tgiPosn - ptgiO - sizeof(uint)));
                w.Write((uint)(pos - tgiPosn) + (addEight ? 8 : 0));

                s.Position = pos;
            }
            #endregion

            /// <summary>
            /// Add a new default element to the list
            /// </summary>
            public override void Add() { this.Add(new TGIBlock(0, null)); }

            #region Content Fields
            /// <summary>
            /// A displayable string representing the list.
            /// </summary>
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("0x{0:X8}: {1}\n", i, this[i].Value); return s; } }
            #endregion
        }
        #endregion

        /// <summary>
        /// AResource classes must supply an <see cref="UnParse()"/> method that serializes the class to a <see cref="System.IO.Stream"/> that is returned.
        /// </summary>
        /// <returns><see cref="System.IO.Stream"/> containing serialized class data.</returns>
        protected abstract Stream UnParse();

        /// <summary>
        /// AResource classes must use this to indicate the resource has changed.
        /// </summary>
        /// <param name="sender">The resource (or sub-class) that has changed.</param>
        /// <param name="e">(Empty) event data object.</param>
        protected virtual void OnResourceChanged(object sender, EventArgs e)
        {
            dirty = true;
            //Console.WriteLine(this.GetType().Name + " dirtied.");
            if (ResourceChanged != null) ResourceChanged(sender, e);
        }
    }
}
