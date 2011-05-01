﻿/***************************************************************************
 *  Copyright (C) 2011 by Peter L Jones                                    *
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

namespace NGMPHashMapResource
{
    /// <summary>
    /// A resource wrapper that understands 0x0166038C resources
    /// </summary>
    [ConstructorParameters(new object[] {(ulong)0, (ulong)0})]
    public class NGMPHashMapResource : AResource, IDictionary<ulong, ulong>, System.Collections.IDictionary
    {
        static bool checking = s3pi.Settings.Settings.Checking;
        const Int32 recommendedApiVersion = 1;
        ushort version = 1;
        Dictionary<ulong, ulong> data = new Dictionary<ulong, ulong>();

        #region AApiVersionedFields
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        /// <summary>
        /// The list of available field names on this API object
        /// </summary>
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
        #endregion

        /// <summary>
        /// Create a new instance of the resource
        /// </summary>
        /// <param name="APIversion">Requested API version</param>
        /// <param name="s">Data stream to use, or null to create from scratch</param>
        public NGMPHashMapResource(int APIversion, Stream s)
            : base(APIversion, s)
        {
            if (stream == null) { stream = UnParse(); OnResourceChanged(this, new EventArgs()); }
            stream.Position = 0;
            Parse(stream);
        }

        void Parse(Stream s)
        {
            BinaryReader br = new BinaryReader(s);

            version = br.ReadUInt16();
            if (checking) if (version != 1)
                    throw new InvalidDataException(String.Format("{0}: unsupported 'version'.  Read '0x{1:X8}', supported: '0x00000001'", this.GetType().Name, version));

            for (int i = br.ReadInt16(); i > 0; i--)
                data.Add(br.ReadUInt64(), br.ReadUInt64());

            if (checking) if (s.Position != s.Length)
                    throw new InvalidDataException(String.Format("{0}: Length {1} bytes, parsed {2} bytes", this.GetType().Name, s.Length, s.Position));
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.Write(version);
            w.Write((short)Count);
            foreach (KeyValuePair<ulong, ulong> kvp in this)
            {
                w.Write(kvp.Key);
                w.Write(kvp.Value);
            }
            w.Flush();
            return ms;
        }



        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public ushort Version { get { return version; } set { if (Version == value) return; version = value; OnResourceChanged(this, new EventArgs()); } }

        public String Value { get { return ValueBuilder; } }

        #region IDictionary<ulong,ulong> Members

        public void Add(ulong key, ulong value)
        {
            data.Add(key, value);
            OnResourceChanged(this, new EventArgs());
        }

        public bool ContainsKey(ulong key) { return data.ContainsKey(key); }

        public ICollection<ulong> Keys { get { return data.Keys; } }

        public bool Remove(ulong key)
        {
            bool res = data.Remove(key);
            if (res)
                OnResourceChanged(this, new EventArgs());
            return res;
        }

        public bool TryGetValue(ulong key, out ulong value) { return data.TryGetValue(key, out value); }

        public ICollection<ulong> Values { get { return data.Values; } }

        public ulong this[ulong key]
        {
            get { return data[key]; }
            set
            {
                if (data[key] == value) return;
                data[key] = value;
                OnResourceChanged(this, new EventArgs());
            }
        }

        #endregion

        #region ICollection<KeyValuePair<ulong,ulong>> Members

        public void Add(KeyValuePair<ulong, ulong> item) { this.Add(item.Key, item.Value); }

        public void Clear()
        {
            data.Clear();
            OnResourceChanged(this, new EventArgs());
        }

        public bool Contains(KeyValuePair<ulong, ulong> item) { return data.ContainsKey(item.Key) && data[item.Key].Equals(item.Value); }

        public void CopyTo(KeyValuePair<ulong, ulong>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<ulong, ulong> kvp in data) array[arrayIndex++] = new KeyValuePair<ulong, ulong>(kvp.Key, kvp.Value);
        }

        public int Count { get { return data.Count; } }

        public bool IsReadOnly { get { return false; } }

        public bool Remove(KeyValuePair<ulong, ulong> item) { return Contains(item) ? this.Remove(item.Key) : false; }

        #endregion

        #region IEnumerable<KeyValuePair<ulong,ulong>> Members

        public IEnumerator<KeyValuePair<ulong, ulong>> GetEnumerator() { return data.GetEnumerator(); }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return data.GetEnumerator(); }

        #endregion

        #region IDictionary Members

        public void Add(object key, object value) { this.Add((ulong)key, (ulong)value); }

        public bool Contains(object key) { return ContainsKey((ulong)key); }

        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { return data.GetEnumerator(); }

        public bool IsFixedSize { get { return false; } }

        System.Collections.ICollection System.Collections.IDictionary.Keys { get { return data.Keys; } }

        public void Remove(object key) { Remove((ulong)key); }

        System.Collections.ICollection System.Collections.IDictionary.Values { get { return data.Values; } }

        public object this[object key] { get { return this[(ulong)key]; } set { this[(ulong)key] = (ulong)value; } }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index) { CopyTo((KeyValuePair<ulong, ulong>[])array, index); }

        public bool IsSynchronized { get { return false; } }

        public object SyncRoot { get { return null; } }

        #endregion
    }

    /// <summary>
    /// ResourceHandler for NameMapResource wrapper
    /// </summary>
    public class NGMPHashMapResourceHandler : AResourceHandler
    {
        /// <summary>
        /// Create the content of the Dictionary
        /// </summary>
        public NGMPHashMapResourceHandler()
        {
            this.Add(typeof(NGMPHashMapResource), new List<string>(new string[] { "0xF3A38370" }));
        }
    }
}
