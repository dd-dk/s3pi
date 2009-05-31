﻿/***************************************************************************
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
using System.IO;
using s3pi.Interfaces;

namespace NameMapResource
{
    /// <summary>
    /// A resource wrapper that understands 0x0166038C resources
    /// </summary>
    public class NameMapResource : AResource, IDictionary<ulong, string>
    {
        static bool checking = s3pi.Settings.Settings.Checking;
        const Int32 recommendedApiVersion = 1;
        BinaryReader br = null;
        BinaryWriter bw = null;
        Dictionary<ulong, string> data = null;

        #region IResource Members
        /// <summary>
        /// The resource content as a Stream
        /// </summary>
        public override Stream Stream
        {
            get
            {
                if (dirty)
                {
                    stream = UnParse();
                    dirty = false;
                }
                return stream;
            }
        }
        #endregion

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
        public NameMapResource(int APIversion, Stream s)
            : base(APIversion, s)
        {
            if (stream != null)
            {
                br = new BinaryReader(stream);
                bw = new BinaryWriter(stream);
            }
            else
            {
                stream = new MemoryStream();
                br = new BinaryReader(stream);
                bw = new BinaryWriter(stream);
                bw.Write((uint)1);
                bw.Write((uint)0);
                bw.Flush();
            }
            Parse(stream);
        }

        void Parse(Stream s)
        {
            data = new Dictionary<ulong, string>();
            s.Position = 0;
            uint vsn = br.ReadUInt32();
            if (checking) if (vsn != 1)
                    throw new InvalidDataException(String.Format("{0}: unsupported 'version'.  Read '0x{1:X8}', supported: '0x00000001'", this.GetType().Name, vsn));

            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
                data.Add(br.ReadUInt64(), new String(br.ReadChars(br.ReadInt32())));

            if (checking) if (s.Position != s.Length)
                    throw new InvalidDataException(String.Format("{0}: Length {1} bytes, parsed {2} bytes", this.GetType().Name, s.Length, s.Position));
        }

        Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.Write(Version);
            w.Write(Count);
            foreach (KeyValuePair<ulong, string> kvp in this)
            {
                w.Write(kvp.Key);
                w.Write(kvp.Value.Length);
                w.Write(kvp.Value.ToCharArray());
            }
            w.Flush();
            return ms;
        }



        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public uint Version
        {
            get { stream.Position = 0; return br.ReadUInt32(); }
            set
            {
                if (Version == value) return;
                stream.Position = 0;
                bw.Write(value);
                OnResourceChanged(this, new EventArgs());
            }
        }

        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public string Value
        {
            get
            {
                string s = "";
                string fmt = "\n0x{0:X" + Count.ToString("X").Length + "}: 0x{1:X16} {2}";

                s = "Version: " + this["Version"];
                s += String.Format("\nCount: 0x{0:X8}", this.Count);

                int i = 0;
                foreach(KeyValuePair<ulong, string> kvp in this)
                    s += String.Format(fmt, i++, kvp.Key, kvp.Value);

                return s;
            }
        }

        #region IDictionary<ulong,string> Members

        public void Add(ulong key, string value)
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

        public bool TryGetValue(ulong key, out string value) { return data.TryGetValue(key, out value); }

        public ICollection<string> Values { get { return data.Values; } }

        public string this[ulong key]
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

        #region ICollection<KeyValuePair<ulong,string>> Members

        public void Add(KeyValuePair<ulong, string> item) { this.Add(item.Key, item.Value); }

        public void Clear()
        {
            data.Clear();
            OnResourceChanged(this, new EventArgs());
        }

        public bool Contains(KeyValuePair<ulong, string> item) { return data.ContainsKey(item.Key) && data[item.Key].Equals(item.Value); }

        public void CopyTo(KeyValuePair<ulong, string>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<ulong, string> kvp in data) array[arrayIndex++] = new KeyValuePair<ulong,string>(kvp.Key, kvp.Value);
        }

        public int Count { get { return data.Count; } }

        public bool IsReadOnly { get { return false; } }

        public bool Remove(KeyValuePair<ulong, string> item) { return Contains(item) ? this.Remove(item.Key) : false; }

        #endregion

        #region IEnumerable<KeyValuePair<ulong,string>> Members

        public IEnumerator<KeyValuePair<ulong, string>> GetEnumerator() { return data.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return data.GetEnumerator(); }

        #endregion
    }

    /// <summary>
    /// ResourceHandler for NameMapResource wrapper
    /// </summary>
    public class NameMapResourceHandler : AResourceHandler
    {
        /// <summary>
        /// Create the content of the Dictionary
        /// </summary>
        public NameMapResourceHandler()
        {
            this.Add(typeof(NameMapResource), new List<string>(new string[] { "0x0166038C" }));
        }
    }
}