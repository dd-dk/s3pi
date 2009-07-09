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

namespace StblResource
{
    /// <summary>
    /// A resource wrapper that understands String Table resources
    /// </summary>
    public class StblResource : AResource, IDictionary<ulong, string>
    {
        const int recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

        static bool checking = s3pi.Settings.Settings.Checking;

        #region Attributes
        ushort unknown1;
        ushort unknown2;
        uint unknown3;
        Dictionary<ulong, string> entries;
        #endregion

        public StblResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); dirty = true; } stream.Position = 0; Parse(stream); }

        #region Data I/O
        void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);
            BinaryReader r2 = new BinaryReader(s, System.Text.Encoding.Unicode);

            uint magic = r.ReadUInt32();
            if (checking) if (magic != FOURCC("STBL"))
                    throw new InvalidDataException(String.Format("Expected magic tag 0x{0:X8}; read 0x{1:X8}; position 0x{2:X8}",
                        FOURCC("STBL"), magic, s.Position));
            byte version = r.ReadByte();
            if (checking) if (version != 0x02)
                    throw new InvalidDataException(String.Format("Expected version 0x02; read 0x{0:X2}; position 0x{1:X8}",
                        version, s.Position));
            
            unknown1 = r.ReadUInt16();

            uint count = r.ReadUInt32();

            unknown2 = r.ReadUInt16();
            unknown3 = r.ReadUInt32();

            entries = new Dictionary<ulong, string>(); 
            for (int i = 0; i < count; i++)
            {
                ulong key = r.ReadUInt64();
                string value = new string(r2.ReadChars(r.ReadInt32()));
                entries.Add(key, value);
            }
        }

        Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();

            BinaryWriter w = new BinaryWriter(ms);
            BinaryWriter w2 = new BinaryWriter(ms, System.Text.Encoding.Unicode);

            w.Write((uint)FOURCC("STBL"));
            w.Write((byte)0x02);

            w.Write(unknown1);

            if (entries == null) entries = new Dictionary<ulong, string>();
            w.Write(entries.Count);

            w.Write(unknown2);
            w.Write(unknown3);

            foreach (var kvp in entries)
            {
                w.Write(kvp.Key);
                w.Write(kvp.Value.Length);
                w2.Write(kvp.Value.ToCharArray());
            }

            return ms;
        }
        #endregion

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
                    stream.Position = 0;
                    dirty = false;
                }
                return stream;
            }
        }
        #endregion

        #region IDictionary<ulong,string> Members

        public void Add(ulong key, string value) { entries.Add(key, value); OnResourceChanged(this, EventArgs.Empty); }

        public bool ContainsKey(ulong key) { return entries.ContainsKey(key); }

        public ICollection<ulong> Keys { get { return entries.Keys; } }

        public bool Remove(ulong key) { try { return entries.Remove(key); } finally { OnResourceChanged(this, EventArgs.Empty); } }

        public bool TryGetValue(ulong key, out string value) { return entries.TryGetValue(key, out value); }

        public ICollection<string> Values { get { return entries.Values; } }

        public string this[ulong key]
        {
            get { return entries[key]; }
            set { if (entries[key] != value) { entries[key] = value; OnResourceChanged(this, EventArgs.Empty); } }
        }

        #endregion

        #region ICollection<KeyValuePair<ulong,string>> Members

        public void Add(KeyValuePair<ulong, string> item) { entries.Add(item.Key, item.Value); }

        public void Clear() { entries.Clear(); OnResourceChanged(this, EventArgs.Empty); }

        public bool Contains(KeyValuePair<ulong, string> item) { return entries.ContainsKey(item.Key) && entries[item.Key].Equals(item.Value); }

        public void CopyTo(KeyValuePair<ulong, string>[] array, int arrayIndex) { foreach (var kvp in entries) array[arrayIndex++] = kvp; }

        public int Count { get { return entries.Count; } }

        public bool IsReadOnly { get { return false; } }

        public bool Remove(KeyValuePair<ulong, string> item) { try { return Contains(item) ? entries.Remove(item.Key) : false; } finally { OnResourceChanged(this, EventArgs.Empty); } }

        #endregion

        #region IEnumerable<KeyValuePair<ulong,string>> Members

        public IEnumerator<KeyValuePair<ulong, string>> GetEnumerator() { return entries.GetEnumerator(); }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return entries.GetEnumerator(); }

        #endregion

        #region Content Fields
        public ushort Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public ushort Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public uint Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnResourceChanged(this, EventArgs.Empty); } } }

        public string Value
        {
            get
            {
                string s = "";
                s += "Unknown1: 0x" + unknown1.ToString("X4");
                s += "\nUnknown2: 0x" + unknown2.ToString("X4");
                s += "\nUnknown3: 0x" + unknown3.ToString("X8");
                foreach (var kvp in entries)
                    s += String.Format("\nKey: 0x{0:X16} = Value: '{1}'", kvp.Key, kvp.Value);
                return s;
            }
        }
        #endregion
    }

    /// <summary>
    /// ResourceHandler for StblResource wrapper
    /// </summary>
    public class StblResourceHandler : AResourceHandler
    {
        public StblResourceHandler()
        {
            this.Add(typeof(StblResource), new List<string>(new string[] { "0x220557DA", }));
        }
    }
}
