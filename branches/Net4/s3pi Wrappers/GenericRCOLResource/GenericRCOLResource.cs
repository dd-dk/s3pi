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
using s3pi.Interfaces;
using System.Reflection;

namespace s3pi.GenericRCOLResource
{
    /// <summary>
    /// A resource wrapper that understands RCOL resources and manages the blocks within.
    /// </summary>
    public class GenericRCOLResource : AResource
    {
        static bool checking = s3pi.Settings.Settings.Checking;
        const Int32 recommendedApiVersion = 1;

        #region Attributes
        uint version;
        int publicChunks;
        uint unused;
        ChunkEntryList blockList;
        CountedTGIBlockList resources;
        #endregion

        #region Constructors
        /// <summary>
        /// Instantiate a new GenericRCOLResource from the supplied <see cref="Stream"/>.
        /// </summary>
        /// <param name="APIversion">Unused; requested API version.</param>
        /// <param name="s">The <see cref="Stream"/> to read the resource in from.</param>
        public GenericRCOLResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); OnResourceChanged(this, new EventArgs()); } stream.Position = 0; Parse(stream); }
        #endregion

        #region Data I/O
        void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);

            version = r.ReadUInt32();
            publicChunks = r.ReadInt32();
            unused = r.ReadUInt32();
            int countResources = r.ReadInt32();
            int countChunks = r.ReadInt32();
            TGIBlock[] chunks = new TGIBlock[countChunks];
            for (int i = 0; i < countChunks; i++) chunks[i] = new TGIBlock(0, OnResourceChanged, "ITG", s);
            resources = new CountedTGIBlockList(OnResourceChanged, "ITG", countResources, s);

            RCOLIndexEntry[] index = new RCOLIndexEntry[countChunks];
            for (int i = 0; i < countChunks; i++) { index[i].Position = r.ReadUInt32(); index[i].Length = r.ReadInt32(); }

            blockList = new ChunkEntryList(requestedApiVersion, OnResourceChanged, s, chunks, index);
        }

        /// <summary>
        /// Return a <see cref="Stream"/> containing the data in this GenericRCOLResource instance.
        /// </summary>
        /// <returns>A <see cref="Stream"/> containing the data in this GenericRCOLResource instance.</returns>
        protected override Stream UnParse()
        {
            long rcolIndexPos;

            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);

            w.Write(version);
            w.Write(publicChunks);
            w.Write(unused);
            if (resources == null) resources = new CountedTGIBlockList(OnResourceChanged, "ITG");
            w.Write(resources.Count);
            if (blockList == null) blockList = new ChunkEntryList(OnResourceChanged);
            w.Write(blockList.Count);
            foreach (ChunkEntry ce in blockList) ce.TGIBlock.UnParse(ms);
            resources.UnParse(ms);

            rcolIndexPos = ms.Position;
            RCOLIndexEntry[] index = new RCOLIndexEntry[blockList.Count];
            for (int i = 0; i < blockList.Count; i++) { w.Write((uint)0); w.Write((uint)0); } // Pad for the index

            int j = 0;
            foreach (ChunkEntry ce in blockList)
            {
                byte[] data = ce.RCOLBlock.AsBytes;
                while (w.BaseStream.Position % 4 != 0) w.Write((byte)0);
                index[j].Position = (uint)ms.Position;
                index[j].Length = data.Length;
                w.Write(data);
                j++;
            }

            ms.Position = rcolIndexPos;
            foreach (RCOLIndexEntry entry in index) { w.Write(entry.Position); w.Write(entry.Length); }

            return ms;
        }
        #endregion

        #region AApiVersionedFields
        /// <summary>
        /// Return the version number that this wrapper prefers to be called with (the default if passed zero).
        /// </summary>
        /// <remarks>This wrapper returns <c>1</c> and is not sensitive to API version.</remarks>
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        #endregion

        #region Sub-types
        internal struct RCOLIndexEntry
        {
            public uint Position;
            public int Length;
        }

        /// <summary>
        /// <see cref="ChunkEntry"/> couples the
        /// <see cref="TGIBlock"/> identifier with the <see cref="ARCOLBlock"/> for ease of handling in a single list.
        /// </summary>
        public class ChunkEntry : AHandlerElement, IEquatable<ChunkEntry>
        {
            const Int32 recommendedApiVersion = 1;

            #region Attributes
            TGIBlock tgiBlock;
            ARCOLBlock rcolBlock;
            #endregion

            /// <summary>
            /// Create a ChunkEntry from an existing <see cref="ChunkEntry"/>.
            /// </summary>
            /// <param name="APIversion">Unused; the requested API version.</param>
            /// <param name="handler">The change event handler.</param>
            /// <param name="basis">An existing <see cref="ChunkEntry"/> to use as a basis.</param>
            public ChunkEntry(int APIversion, EventHandler handler, ChunkEntry basis)
                : this(APIversion, handler, basis.tgiBlock, basis.rcolBlock) { }
            /// <summary>
            /// Create a ChunkEntry from an existing <see cref="TGIBlock"/> and an existing <see cref="ARCOLBlock"/>.
            /// </summary>
            /// <param name="APIversion">Unused; the requested API version.</param>
            /// <param name="handler">The change event handler.</param>
            /// <param name="tgiBlock">An existing <see cref="T:TGIBlock"/>.</param>
            /// <param name="rcolBlock">An existing <see cref="ARCOLBlock"/>.</param>
            public ChunkEntry(int APIversion, EventHandler handler, TGIBlock tgiBlock, ARCOLBlock rcolBlock)
                : base(APIversion, handler)
            {
                this.tgiBlock = (TGIBlock)tgiBlock.Clone(handler);
                this.rcolBlock = (ARCOLBlock)rcolBlock.Clone(handler);
            }

            #region AHandlerElement Members
            /// <summary>
            /// Get a copy of the <see cref="ChunkEntry"/> but with a new change <see cref="EventHandler"/>.
            /// </summary>
            /// <param name="handler">The replacement <see cref="EventHandler"/> delegate.</param>
            /// <returns>Return a copy of the <see cref="ChunkEntry"/> but with a new change <see cref="EventHandler"/>.</returns>
            public override AHandlerElement Clone(EventHandler handler) { return new ChunkEntry(requestedApiVersion, handler, this); }

            /// <summary>
            /// Return the version number that this class prefers to be called with (the default if passed zero).
            /// </summary>
            /// <remarks>This class returns <c>1</c> and is not sensitive to API version.</remarks>
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            /// <summary>
            /// The visible-to-API list of fields in this class.
            /// </summary>
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<ChunkEntry> Members
            /// <summary>
            /// Indicates whether the current <see cref="ChunkEntry"/> is equal to another instance.
            /// </summary>
            /// <param name="other">Another instance to compare with this <see cref="ChunkEntry"/>.</param>
            /// <returns><c>true</c> if the current <see cref="ChunkEntry"/> is equal to the <paramref name="other"/> parameter;
            /// otherwise, <c>false</c>.</returns>
            public bool Equals(ChunkEntry other) { return tgiBlock.Equals(other.tgiBlock) && rcolBlock.Equals(other.rcolBlock); }

            /// <summary>
            /// Indicates whether the current <see cref="ChunkEntry"/> is equal to another object of the same type.
            /// </summary>
            /// <param name="obj">An object to compare with this <see cref="ChunkEntry"/>.</param>
            /// <returns><c>true</c> if the current <see cref="ChunkEntry"/> is equal to the <paramref name="obj"/> parameter;
            /// otherwise, <c>false</c>.</returns>
            public override bool Equals(object obj)
            {
                return obj as ChunkEntry != null ? this.Equals(obj as ChunkEntry) : false;
            }

            /// <summary>
            /// Serves as a hash function for an <see cref="ChunkEntry"/>.
            /// </summary>
            /// <returns>A hash code for the current <see cref="ChunkEntry"/>.</returns>
            public override int GetHashCode()
            {
                return tgiBlock.GetHashCode() ^ rcolBlock.GetHashCode();
            }

            #endregion

            /// <summary>
            /// The <see cref="T:TGIBlock"/> that identifies the RCOL block.
            /// </summary>
            [ElementPriority(1)]
            public TGIBlock TGIBlock { get { return tgiBlock; } set { if (tgiBlock != value) { tgiBlock = new TGIBlock(0, handler, value); OnElementChanged(); } } }
            /// <summary>
            /// The RCOL block.
            /// </summary>
            [ElementPriority(2)]
            public ARCOLBlock RCOLBlock { get { return rcolBlock; } set { if (rcolBlock != value) { rcolBlock = (ARCOLBlock)rcolBlock.Clone(handler); OnElementChanged(); } } }

            /// <summary>
            /// Returns a formatted string containing the <see cref="TGIBlock"/> and, where possible, <see cref="RCOLBlock"/> values.
            /// </summary>
            public string Value
            {
                get
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("--- " + tgiBlock + ((rcolBlock.Equals("*")) ? "" : " - " + rcolBlock.Tag) + " ---");
                    if (AApiVersionedFields.GetContentFields(0, rcolBlock.GetType()).Contains("Value") &&
                            typeof(string).IsAssignableFrom(AApiVersionedFields.GetContentFieldTypes(requestedApiVersion, rcolBlock.GetType())["Value"]))
                        sb.Append("\n" + (string)rcolBlock["Value"].Value);
                    sb.Append("\n----");
                    return sb.ToString();
                }
            }
        }

        /// <summary>
        /// A <see cref="DependentList{T}"/> containing <see cref="ChunkEntry"/> values.
        /// </summary>
        public class ChunkEntryList : DependentList<ChunkEntry>
        {
            internal ChunkEntryList(int requestedApiVersion, EventHandler handler, Stream s, TGIBlock[] chunks, RCOLIndexEntry[] index)
                : base(null, -1)
            {
                elementHandler = handler;

                BinaryReader r = new BinaryReader(s);
                for (int i = 0; i < index.Length; i++)
                {
                    s.Position = index[i].Position;
                    byte[] data = r.ReadBytes(index[i].Length);
                    MemoryStream ms = new MemoryStream();
                    ms.Write(data, 0, data.Length);
                    ms.Position = 0;

                    this.Add(chunks[i], GenericRCOLResourceHandler.RCOLDealer(requestedApiVersion, elementHandler, chunks[i].ResourceType, ms));
                }

                this.handler = handler;
            }
            /// <summary>
            /// Create an empty list.
            /// </summary>
            /// <param name="handler">The list change <see cref="EventHandler"/> delegate.</param>
            public ChunkEntryList(EventHandler handler) : base(handler) { }
            /// <summary>
            /// Create a new list, with the contents from the provided enumeration.
            /// </summary>
            /// <param name="handler">The list change <see cref="EventHandler"/> delegate.</param>
            /// <param name="ice">An <see cref="IEnumerable{T}"/> of <see cref="ChunkEntry"/> values.</param>
            public ChunkEntryList(EventHandler handler, IEnumerable<ChunkEntry> ice) : base(handler, ice) { }

            /// <summary>
            /// CreateElement is not implemented.
            /// </summary>
            /// <param name="s">Unused.</param>
            /// <returns>Not implemented.</returns>
            /// <exception cref="NotImplementedException">Thrown if CreateElement is invoked.</exception>
            protected override ChunkEntry CreateElement(Stream s) { throw new NotImplementedException(); }
            /// <summary>
            /// WriteElement is not implemented.
            /// </summary>
            /// <param name="s">Unused.</param>
            /// <param name="element">Unused.</param>
            /// <exception cref="NotImplementedException">Thrown if WriteElement is invoked.</exception>
            protected override void WriteElement(Stream s, ChunkEntry element) { throw new NotImplementedException(); }

            internal EventHandler listEventHandler { set { handler = value; } }

            /// <summary>
            /// <see cref="IGenericAdd.Add()"/> is not implemented.
            /// </summary>
            /// <exception cref="NotImplementedException">Thrown if <see cref="Add()"/> is invoked.</exception>
            public override void Add() { throw new NotImplementedException(); }
        }

        /// <summary>
        /// The indicator bits for a <see cref="ChunkReference"/>.
        /// </summary>
        public enum ReferenceType : byte
        {
            /// <summary>
            /// Indicates the reference is to this resource, in <see cref="GenericRCOLResource.ChunkEntries"/>, below <see cref="GenericRCOLResource.PublicChunks"/>.
            /// </summary>
            Public = 0x0,
            /// <summary>
            /// Indicates the reference is to this resource, in <see cref="GenericRCOLResource.ChunkEntries"/>, at or above <see cref="GenericRCOLResource.PublicChunks"/>.
            /// </summary>
            Private = 0x1,
            /// <summary>
            /// No known usage.
            /// </summary>
            External = 0x2,
            /// <summary>
            /// Indicates the reference is to another resource, via <see cref="GenericRCOLResource.Resources"/>
            /// </summary>
            Delayed = 0x3,
        }

        /// <summary>
        /// Manages RCOL references to other RCOL blocks or other resources.
        /// </summary>
        public class ChunkReference : AHandlerElement,
            IEquatable<ChunkReference>, IEqualityComparer<ChunkReference>,
            IComparer<ChunkReference>, IComparable<ChunkReference>
        {
            const Int32 recommendedApiVersion = 1;

            #region Attributes
            uint chunkReference;
            #endregion

            #region Constructors
            /// <summary>
            /// Create a new instance from data in the provided <see cref="Stream"/>.
            /// </summary>
            /// <param name="APIversion">Unused; requested API version.</param>
            /// <param name="handler">Change <see cref="EventHandler"/> delegate.</param>
            /// <param name="s">The <see cref="Stream"/> containing the data.</param>
            public ChunkReference(int APIversion, EventHandler handler, Stream s)
                : base(APIversion, handler) { Parse(s); }
            /// <summary>
            /// Create a new instance based on the provided existing instance.
            /// </summary>
            /// <param name="APIversion">Unused; requested API version.</param>
            /// <param name="handler">Change <see cref="EventHandler"/> delegate.</param>
            /// <param name="basis">An existing instance.</param>
            public ChunkReference(int APIversion, EventHandler handler, ChunkReference basis)
                : this(APIversion, handler, basis.chunkReference) { }
            /// <summary>
            /// Create a new instance from the provided <see cref="uint"/> value.
            /// </summary>
            /// <param name="APIversion">Unused; requested API version.</param>
            /// <param name="handler">Change <see cref="EventHandler"/> delegate.</param>
            /// <param name="chunkReference">The chunk reference as a &quot;raw&quot; <see cref="uint"/>.</param>
            public ChunkReference(int APIversion, EventHandler handler, uint chunkReference)
                : base(APIversion, handler) { this.chunkReference = chunkReference; }
            #endregion

            #region Data I/O
            void Parse(Stream s) { this.chunkReference = new BinaryReader(s).ReadUInt32(); }

            /// <summary>
            /// Write this instance to the provided <see cref="Stream"/>.
            /// </summary>
            /// <param name="s">The <see cref="Stream"/> to write out to.</param>
            public void UnParse(Stream s) { new BinaryWriter(s).Write(chunkReference); }
            #endregion

            #region AHandlerElement Members
            /// <summary>
            /// Return a new instance with the same value as the current instance but with the given change <see cref="EventHandler"/>.
            /// </summary>
            /// <param name="handler">The change <see cref="EventHandler"/>.</param>
            /// <returns>A new instance with the same value as the current instance but with the given change <see cref="EventHandler"/>.</returns>
            public override AHandlerElement Clone(EventHandler handler) { return new ChunkReference(requestedApiVersion, handler, this); }

            /// <summary>
            /// Return the version number that this wrapper prefers to be called with (the default if passed zero).
            /// </summary>
            /// <remarks>This wrapper returns <c>1</c> and is not sensitive to API version.</remarks>
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            /// <summary>
            /// The list of API-visible fields.
            /// </summary>
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<ChunkReference> Members

            /// <summary>
            /// Returns a value indicating whether this instance is equal to a specified
            /// <see cref="ChunkReference"/>.
            /// </summary>
            /// <param name="other">A <see cref="ChunkReference"/> value to compare to this instance.</param>
            /// <returns>
            /// <c>true</c> if obj has the same value as this instance;
            /// otherwise, <c>false</c>.
            /// </returns>
            public bool Equals(ChunkReference other) { return chunkReference.Equals(other.chunkReference); }
            /// <summary>
            /// Returns a value indicating whether this instance is equal to a specified
            /// <see cref="object"/>.
            /// </summary>
            /// <param name="obj">An <see cref="object"/> to compare with this instance.</param>
            /// <returns>
            /// <c>true</c> if <paramref name="obj"/> is an instance of <see cref="ChunkReference"/>
            /// and equals the value of this instance;
            /// otherwise, <c>false</c>.
            /// </returns>
            public override bool Equals(object obj) { return (obj as ChunkReference != null) ? this.Equals(obj as ChunkReference) : false; }
            /// <summary>
            /// Returns the hash code for this instance.
            /// </summary>
            /// <returns>A 32-bit signed integer hash code.</returns>
            public override int GetHashCode() { return chunkReference.GetHashCode(); }

            #endregion

            #region IEqualityComparer<ChunkReference>
            /// <summary>
            /// Determines whether the specified <see cref="ChunkReference"/> instances are equal.
            /// </summary>
            /// <param name="x">The first <see cref="ChunkReference"/> to compare.</param>
            /// <param name="y">The second <see cref="ChunkReference"/> to compare.</param>
            /// <returns>
            /// <c>true</c> if the specified <see cref="ChunkReference"/> instances are equal;
            /// otherwise, <c>false</c>.
            /// </returns>
            public bool Equals(ChunkReference x, ChunkReference y) { return x.Equals(y); }
            /// <summary>
            /// Returns a hash code for the specified object.
            /// </summary>
            /// <param name="obj">The <see cref="System.Object"/> for which a hash code is to be returned.</param>
            /// <returns>A hash code for the specified object.</returns>
            /// <exception cref="ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is <c>null</c>.</exception>
            public int GetHashCode(ChunkReference obj) { return obj.GetHashCode(); }

            #endregion

            #region IComparer<ChunkReference>
            /// <summary>
            /// Compares the current <see cref="ChunkReference"/> with another <see cref="ChunkReference"/>.
            /// </summary>
            /// <param name="other">Another <see cref="ChunkReference"/> to compare with this one.</param>
            /// <returns>
            /// A 32-bit signed integer that indicates the relative order of the <see cref="ChunkReference"/> instances
            /// being compared. The return value has the following meanings:
            /// <table>
            /// <thead>
            /// <tr><td>Value</td><td>Meaning</td></tr>
            /// </thead>
            /// <tbody>
            /// <tr><td>Less than zero</td><td>This object is less than the <paramref name="other"/> parameter.</td></tr>
            /// <tr><td>Zero</td><td>This object is equal to <paramref name="other"/>.</td></tr>
            /// <tr><td>Greater than zero</td><td>This object is greater than <paramref name="other"/>.</td></tr>
            /// </tbody>
            /// </table> 
            /// </returns>
            public int CompareTo(ChunkReference other) { return chunkReference.CompareTo(other.chunkReference); }

            #endregion

            #region IComparable<ChunkReference>
            /// <summary>
            /// Compares two <see cref="ChunkReference"/> instances and returns a value indicating whether one is
            /// less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first <see cref="ChunkReference"/> instance to compare.</param>
            /// <param name="y">The second <see cref="ChunkReference"/> instance to compare.</param>
            /// <returns>
            /// <table>
            /// <thead>
            /// <tr><td>Value</td><td>Condition</td></tr>
            /// </thead>
            /// <tbody>
            /// <tr><td>Less than zero</td><td><paramref name="x"/> is less than <paramref name="y"/>.</td></tr>
            /// <tr><td>Zero</td><td><paramref name="x"/> equals <paramref name="y"/>.</td></tr>
            /// <tr><td>Greater than zero</td><td><paramref name="x"/> is greater than <paramref name="y"/>.</td></tr>
            /// </tbody>
            /// </table> 
            /// </returns>
            public int Compare(ChunkReference x, ChunkReference y) { return x.CompareTo(y); }

            #endregion

            /// <summary>
            /// Return the <see cref="IResourceKey"/> indexed by the given <see cref="ChunkReference"/>.
            /// </summary>
            /// <param name="rcol">The RCOL resource to inspect.</param>
            /// <param name="reference">The <see cref="ChunkReference"/> to look up.</param>
            /// <returns>The <see cref="IResourceKey"/> indexed by the given <see cref="ChunkReference"/>.</returns>
            /// <exception cref="NotImplementedException">The <paramref name="reference"/> passed
            /// does not have a supported <see cref="ReferenceType"/> value.</exception>
            public static IResourceKey GetKey(GenericRCOLResource rcol, ChunkReference reference)
            {
                if (reference.chunkReference == 0)
                    return null;

                switch (reference.RefType)
                {
                    case ReferenceType.Public:
                        return rcol.ChunkEntries[reference.TGIBlockIndex].TGIBlock;
                    case ReferenceType.Private:
                        return rcol.ChunkEntries[reference.TGIBlockIndex + rcol.PublicChunks].TGIBlock;
                    case ReferenceType.Delayed:
                        return rcol.resources[reference.TGIBlockIndex];
                }
                throw new NotImplementedException(String.Format("Reference Type {0} is not supported.", reference.RefType));
            }

            /// <summary>
            /// Return the RCOL block indexed by the given <see cref="ChunkReference"/>.
            /// </summary>
            /// <param name="rcol">The RCOL resource to inspect.</param>
            /// <param name="reference">The <see cref="ChunkReference"/> to look up.</param>
            /// <returns>The RCOL block indexed by the given <see cref="ChunkReference"/>.</returns>
            /// <exception cref="NotImplementedException">The <paramref name="reference"/> passed
            /// does not have a supported <see cref="ReferenceType"/> value.</exception>
            /// <remarks>Only <see cref="ReferenceType.Public"/> and <see cref="ReferenceType.Private"/>
            /// are &quot;sane&quot; types.</remarks>
            public static IRCOLBlock GetBlock(GenericRCOLResource rcol, ChunkReference reference)
            {
                if (reference.chunkReference == 0)
                    return null;

                switch (reference.RefType)
                {
                    case ReferenceType.Public:
                        return rcol.ChunkEntries[reference.TGIBlockIndex].RCOLBlock;
                    case ReferenceType.Private:
                        return rcol.ChunkEntries[reference.TGIBlockIndex + rcol.PublicChunks].RCOLBlock;
                }
                throw new NotImplementedException(String.Format("Reference Type {0} is not supported.", reference.RefType));
            }

            /// <summary>
            /// Determine the <see cref="ChunkReference"/> value for the given <see cref="IResourceKey"/> value.
            /// </summary>
            /// <param name="rcol">The RCOL resource to inspect.</param>
            /// <param name="rk">The <see cref="IResourceKey"/> to encode.</param>
            /// <returns>The <see cref="ChunkReference"/> value for the given <see cref="IResourceKey"/> value.</returns>
            /// <remarks>Note that the value will be zero (i.e. indicating an invalid entry) if the <paramref name="rk"/>
            /// value supplied is not found either in the <see cref="GenericRCOLResource.ChunkEntries"/> or
            /// <see cref="GenericRCOLResource.Resources"/> lists.</remarks>
            public static ChunkReference CreateReference(GenericRCOLResource rcol, IResourceKey rk)
            {
                return new ChunkReference(0, null, CreateReferenceHelper(rcol, rk));
            }
            static uint CreateReferenceHelper(GenericRCOLResource rcol, IResourceKey rk)
            {
                int i = rcol.ChunkEntries.FindIndex(x => x.TGIBlock.Equals(rk));
                if (i < 0)
                {
                    i = rcol.Resources.FindIndex(x => x.Equals(rk));
                    return i < 0 ? 0 : (uint)(i + 1) | 0x30000000;
                }
                else
                {
                    i++;
                    return i < rcol.publicChunks ? (uint)i : (uint)i | 0x10000000;
                }
            }

            #region Content Fields
            /// <summary>
            /// The index into either the <see cref="GenericRCOLResource.Resources"/>
            /// or <see cref="GenericRCOLResource.ChunkEntries"/> lists.
            /// </summary>
            [ElementPriority(1)]
            public int TGIBlockIndex { get { return (int)(chunkReference & 0x0FFFFFFF) - 1; } set { if (TGIBlockIndex != value) { if (value == -1) chunkReference = 0; else chunkReference = (chunkReference & 0xF0000000) | (uint)((value + 1) & 0x0FFFFFFF); OnElementChanged(); } } }
            /// <summary>
            /// The <see cref="ReferenceType"/> of the instance.
            /// </summary>
            [ElementPriority(2)]
            public ReferenceType RefType { get { return (ReferenceType)(chunkReference == 0 ? -1 : (byte)(chunkReference >> 28)); } set { if (RefType != value) { chunkReference = (((uint)value) << 28) | (chunkReference & 0x0FFFFFFF); OnElementChanged(); } } }

            /// <summary>
            /// A displayable string for the instance.
            /// </summary>
            public string Value { get { return chunkReference > 0 ? this["TGIBlockIndex"] + " (" + this["RefType"] + ")" : "(unset)"; } }
            #endregion
        }

        #endregion

        #region Content Fields
        /// <summary>
        /// The version of this RCOL resource.
        /// </summary>
        [ElementPriority(1)]
        public uint Version { get { return version; } set { if (version != value) { version = value; OnResourceChanged(this, EventArgs.Empty); } } }
        /// <summary>
        /// The number of &quot;public&quot; RCOL blocks in the resource.
        /// </summary>
        [ElementPriority(2)]
        public int PublicChunks { get { return publicChunks; } set { if (publicChunks != value) { publicChunks = value; OnResourceChanged(this, EventArgs.Empty); } } }
        /// <summary>
        /// Unknown.
        /// </summary>
        [ElementPriority(3)]
        public uint Unused { get { return unused; } set { if (unused != value) { unused = value; OnResourceChanged(this, EventArgs.Empty); } } }
        /// <summary>
        /// The list of <see cref="TGIBlock"/>s referenced for resources external to this resource.
        /// </summary>
        [ElementPriority(4)]
        public CountedTGIBlockList Resources { get { return resources; } set { if (resources != value) { resources = new CountedTGIBlockList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        /// <summary>
        /// The list of <see cref="ChunkEntry"/> values for RCOL blocks within this resource.
        /// </summary>
        [ElementPriority(5)]
        public ChunkEntryList ChunkEntries { get { return blockList; } set { if (blockList != value) { blockList = new ChunkEntryList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }

        /// <summary>
        /// A displayable string representing the content of this resource.
        /// </summary>
        public string Value { get { return ValueBuilder; } }
        #endregion
    }

    /// <summary>
    /// ResourceHandler for GenericRCOLResource wrapper.
    /// </summary>
    /// <remarks>
    /// It has a pluggable API to support handlers for the blocks within an RCOL resource,
    /// similar to WrapperDealer's support for resources within a package.
    /// </remarks>
    public class GenericRCOLResourceHandler : AResourceHandler
    {
        static Dictionary<uint, Type> typeRegistry;
        static Dictionary<string, Type> tagRegistry;

        static List<string> resourceTypes;

        static GenericRCOLResourceHandler()
        {
            typeRegistry = new Dictionary<uint, Type>();
            tagRegistry = new Dictionary<string, Type>();

            string folder = Path.GetDirectoryName(typeof(GenericRCOLResourceHandler).Assembly.Location);
            foreach (string path in Directory.GetFiles(folder, "*.dll"))
            {
                //Protect load of DLL
                try
                {
                    Assembly dotNetDll = Assembly.LoadFile(path);
                    Type[] types = dotNetDll.GetTypes();
                    foreach (Type t in types)
                    {
                        if (t.IsAbstract) continue;
                        if (!t.IsSubclassOf(typeof(ARCOLBlock))) continue;

                        //Protect instantiating class
                        try
                        {
                            ConstructorInfo ctor = t.GetConstructor(new Type[] { typeof(int), typeof(EventHandler), typeof(Stream), });
                            if (ctor == null) continue;

                            ARCOLBlock arb = (ARCOLBlock)ctor.Invoke(new object[] { 0, null, null });
                            if (!typeRegistry.ContainsKey(arb.ResourceType)) typeRegistry.Add(arb.ResourceType, arb.GetType());
                            if (!tagRegistry.ContainsKey(arb.Tag)) tagRegistry.Add(arb.Tag, arb.GetType());
                        }
                        catch { }
                    }
                }
                catch { }
            }

            StreamReader sr = new StreamReader(Path.Combine(folder, "RCOLResources.txt"));
            resourceTypes = new List<string>();
            string s;
            while ((s = sr.ReadLine()) != null)
            {
                if (s.StartsWith(";")) continue;
                string[] t = s.Split(new char[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                if (t[2].Equals("Y")) resourceTypes.Add(t[0]);
            }
        }

        /// <summary>
        /// Invoke the &quot;default&quot; constructor for an RCOL block.
        /// </summary>
        /// <param name="APIversion">Requested API version.</param>
        /// <param name="handler">Change <see cref="EventHandler"/> delegate.</param>
        /// <param name="type">Resource type of RCOL block.</param>
        /// <returns>A new, initialised instance of the requested RCOL block <paramref name="type"/>,
        /// or <c>null</c> if the <paramref name="type"/> is not supported.</returns>
        public static ARCOLBlock CreateRCOLBlock(int APIversion, EventHandler handler, uint type)
        {
            Type[] types = new Type[] { typeof(int), typeof(EventHandler), };
            object[] args = new object[] { APIversion, handler, };
            if (GenericRCOLResourceHandler.typeRegistry.ContainsKey(type))
            {
                Type t = GenericRCOLResourceHandler.typeRegistry[type];
                return (ARCOLBlock)t.GetConstructor(types).Invoke(args);
            }
            if (GenericRCOLResourceHandler.tagRegistry.ContainsKey("*"))
            {
                Type t = GenericRCOLResourceHandler.tagRegistry["*"];
                return (ARCOLBlock)t.GetConstructor(types).Invoke(args);
            }
            return null;
        }

        /// <summary>
        /// Return a new instance of the requested RCOL block <paramref name="type"/>,
        /// initialised from the content of the supplied <see cref="Stream"/>.
        /// </summary>
        /// <param name="APIversion">Requested API version.</param>
        /// <param name="handler">Change <see cref="EventHandler"/> delegate.</param>
        /// <param name="type">Resource type of RCOL block.</param>
        /// <param name="s"><see cref="Stream"/> containing data content for the RCOL block.</param>
        /// <returns>A new instance of the requested RCOL block <paramref name="type"/>,
        /// initialised from the supplied <see cref="Stream"/>,
        /// or <c>null</c> if the <paramref name="type"/> is not supported.</returns>
        public static ARCOLBlock RCOLDealer(int APIversion, EventHandler handler, uint type, Stream s)
        {
            Type[] types = new Type[] { typeof(int), typeof(EventHandler), typeof(Stream), };
            object[] args = new object[] { APIversion, handler, s };
            if (GenericRCOLResourceHandler.typeRegistry.ContainsKey(type))
            {
                Type t = GenericRCOLResourceHandler.typeRegistry[type];
                return (ARCOLBlock)t.GetConstructor(types).Invoke(args);
            }
            if (GenericRCOLResourceHandler.tagRegistry.ContainsKey("*"))
            {
                Type t = GenericRCOLResourceHandler.tagRegistry["*"];
                return (ARCOLBlock)t.GetConstructor(types).Invoke(args);
            }
            return null;
        }

        /// <summary>
        /// Return a new instance of the requested RCOL block <paramref name="type"/>,
        /// initialised from the supplied <paramref name="fields"/>.
        /// </summary>
        /// <param name="APIversion">Requested API version.</param>
        /// <param name="handler">Change <see cref="EventHandler"/> delegate.</param>
        /// <param name="type">Resource type of RCOL block.</param>
        /// <param name="fields">The fields to pass to the RCOL block constructor.</param>
        /// <returns>A new instance of the requested RCOL block <paramref name="type"/>,
        /// initialised from the supplied <paramref name="fields"/>,
        /// or <c>null</c> if the <paramref name="type"/> is not supported.</returns>
        public static ARCOLBlock RCOLDealer(int APIversion, EventHandler handler, uint type, params object[] fields)
        {
            Type[] types = new Type[2 + fields.Length];
            types[0] = typeof(int);
            types[1] = typeof(EventHandler);
            for (int i = 0; i < fields.Length; i++) types[2 + i] = fields[i].GetType();

            object[] args = new object[2 + fields.Length];
            args[0] = APIversion;
            args[1] = handler;
            Array.Copy(fields, 0, args, 2, fields.Length);

            if (GenericRCOLResourceHandler.typeRegistry.ContainsKey(type))
            {
                Type t = GenericRCOLResourceHandler.typeRegistry[type];
                return (ARCOLBlock)t.GetConstructor(types).Invoke(args);
            }
            if (GenericRCOLResourceHandler.tagRegistry.ContainsKey("*"))
            {
                Type t = GenericRCOLResourceHandler.tagRegistry["*"];
                return (ARCOLBlock)t.GetConstructor(types).Invoke(args);
            }
            return null;
        }

        /// <summary>
        /// ResourceHandler for GenericRCOLResource wrapper
        /// </summary>
        public GenericRCOLResourceHandler()
        {
            this.Add(typeof(GenericRCOLResource), new List<string>(resourceTypes.ToArray()));
        }
    }
}