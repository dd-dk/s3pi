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
using s3pi.Interfaces;
using System.Reflection;

namespace s3pi.GenericRCOLResource
{
    /// <summary>
    /// A resource wrapper that understands generic RCOL resources
    /// </summary>
    public class GenericRCOLResource : AResource, IList<KeyValuePair<AResource.TGIBlock, ARCOLBlock>>
    {
        static bool checking = s3pi.Settings.Settings.Checking;
        const Int32 recommendedApiVersion = 1;

        uint version;
        uint dataType;
        uint unused;
        List<KeyValuePair<TGIBlock, ARCOLBlock>> blockList;
        CountedTGIBlockList resources;

        #region Constructors
        public GenericRCOLResource(int APIversion, Stream s)
            : base(APIversion, s)
        {
            if (stream == null) { stream = UnParse(); dirty = true; }
            stream.Position = 0;
            Parse(stream);
        }
        #endregion

        #region Data I/O
        void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);

            version = r.ReadUInt32();
            dataType = r.ReadUInt32();
            unused = r.ReadUInt32();
            uint countResources = r.ReadUInt32();
            uint countChunks = r.ReadUInt32();
            TGIBlock[] chunks = new TGIBlock[countChunks];
            for (int i = 0; i < countChunks; i++) chunks[i] = new TGIBlock(0, OnResourceChanged, "ITG", s);
            resources = new CountedTGIBlockList(OnResourceChanged, "ITG", countResources, s);

            RCOLIndexEntry[] index = new RCOLIndexEntry[countChunks];
            for (int i = 0; i < countChunks; i++) { index[i].Position = r.ReadUInt32(); index[i].Length = r.ReadInt32(); }

            blockList = new List<KeyValuePair<AResource.TGIBlock, ARCOLBlock>>();
            for (int i = 0; i < countChunks; i++)
            {
                s.Position = index[i].Position;
                byte[] data = r.ReadBytes(index[i].Length);
                MemoryStream ms = new MemoryStream();
                ms.Write(data, 0, data.Length);
                ms.Position = 0;

                blockList.Add(new KeyValuePair<AResource.TGIBlock, ARCOLBlock>(
                    chunks[i], GenericRCOLResourceHandler.RCOLDealer(requestedApiVersion, OnResourceChanged, chunks[i].ResourceType, ms)));
            }
        }

        Stream UnParse()
        {
            long rcolIndexPos;
            
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);

            w.Write(version);
            w.Write(dataType);
            w.Write(unused);
            if (resources == null) resources = new CountedTGIBlockList(OnResourceChanged, "ITG");
            w.Write(resources.Count);
            if (blockList == null) blockList = new List<KeyValuePair<AResource.TGIBlock, ARCOLBlock>>();
            w.Write(blockList.Count);
            foreach (var kvp in blockList) kvp.Key.UnParse(ms);
            resources.UnParse(ms);

            rcolIndexPos = ms.Position;
            RCOLIndexEntry[] index = new RCOLIndexEntry[blockList.Count];
            for (int i = 0; i < blockList.Count; i++) { w.Write((uint)0); w.Write((uint)0); } // Pad for the index

            int j = 0;
            foreach (var kvp in blockList)
            {
                byte[] data = kvp.Value.AsBytes;
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

        #region Sub-types
        struct RCOLIndexEntry
        {
            public uint Position;
            public int Length;
        }
        #endregion

        #region IList<KeyValuePair<TGIBlock,ARCOLBlock>> Members

        public int IndexOf(KeyValuePair<AResource.TGIBlock, ARCOLBlock> item) { return blockList.IndexOf(item); }

        public void Insert(int index, KeyValuePair<AResource.TGIBlock, ARCOLBlock> item)
        {
            if (item.Key.ResourceType != item.Value.ResourceType)
                throw new ArgumentException();
            blockList.Insert(index, item);
            OnResourceChanged(this, EventArgs.Empty);
        }

        public void RemoveAt(int index)
        {
            blockList.RemoveAt(index);
            OnResourceChanged(this, EventArgs.Empty);
        }

        public KeyValuePair<AResource.TGIBlock, ARCOLBlock> this[int index]
        {
            get
            {
                return blockList[index];
            }
            set
            {
                KeyValuePair<AResource.TGIBlock, ARCOLBlock> item =
                    new KeyValuePair<TGIBlock, ARCOLBlock>(new TGIBlock(requestedApiVersion, OnResourceChanged, value.Key),
                        (ARCOLBlock)value.Value.Clone(OnResourceChanged));
                throw new NotImplementedException();
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TGIBlock,ARCOLBlock>> Members

        public void Add(KeyValuePair<AResource.TGIBlock, ARCOLBlock> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<AResource.TGIBlock, ARCOLBlock> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<AResource.TGIBlock, ARCOLBlock>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(KeyValuePair<AResource.TGIBlock, ARCOLBlock> item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<KeyValuePair<TGIBlock,ARCOLBlock>> Members

        public IEnumerator<KeyValuePair<AResource.TGIBlock, ARCOLBlock>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Content Fields
        public uint Version { get { return version; } set { if (version != value) { version = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public uint DataType { get { return dataType; } set { if (dataType != value) { dataType = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public uint Unused { get { return unused; } set { if (unused != value) { unused = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public CountedTGIBlockList Resources { get; set; }

        public string Value
        {
            get
            {
                string s = "";
                s += "Version: 0x" + version.ToString("X8");
                s += "\nDataType: 0x" + dataType.ToString("X8");
                s += "\nUnused: 0x" + unused.ToString("X8");
                s += "\n--\nResources:";
                for (int i = 0; i < resources.Count; i++)
                    s += "\n[" + i + "]: " + resources[i];
                s += "\n----";
                s += "\nRCOL Blocks:";
                for (int i = 0; i < blockList.Count; i++)
                {
                    if (blockList[i].Value.Tag.Equals("*"))
                        s += "\n--- " + i + ": " + blockList[i].Key + " (unknown type) ---";
                    else
                    {
                        s += "\n--- " + i + ": " + blockList[i].Key + " - " + blockList[i].Value.Tag + " ---";
                        if (AApiVersionedFields.GetContentFields(0, blockList[i].Value.GetType()).Contains("Value"))
                            s += "\n" + blockList[i].Value["Value"];
                        else foreach (string field in AApiVersionedFields.GetContentFields(0, blockList[i].Value.GetType()))
                        {
                            if (!(new List<string>(new string[] { "ResourceType", "Tag", "Value", "Stream", "AsBytes", })).Contains(field))
                                s += "\n  " + field + ": " + blockList[i].Value[field];
                        }
                    }
                    s += "\n----";
                }
                return s;
            }
        }
        #endregion
    }

    /// <summary>
    /// ResourceHandler for GenericRCOLResource wrapper
    /// </summary>
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
                try
                {
                    Assembly dotNetDll = Assembly.LoadFile(path);
                    Type[] types = dotNetDll.GetTypes();
                    foreach (Type t in types)
                    {
                        if (!t.IsSubclassOf(typeof(ARCOLBlock))) continue;

                        ARCOLBlock arb = (ARCOLBlock)t.GetConstructor(new Type[] { typeof(int), typeof(EventHandler), typeof(Stream), }).Invoke(new object[] { 0, null, null });
                        if (arb == null) continue;

                        if (!typeRegistry.ContainsKey(arb.ResourceType)) typeRegistry.Add(arb.ResourceType, arb.GetType());
                        if (!tagRegistry.ContainsKey(arb.Tag)) tagRegistry.Add(arb.Tag, arb.GetType());
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
                string[] t = s.Split(new char[] { ' ' }, 4);
                if (t[2].Equals("Y")) resourceTypes.Add(t[0]);
            }
        }

        public static ARCOLBlock RCOLDealer(int APIversion, EventHandler handler, uint type, Stream s)
        {
            if (GenericRCOLResourceHandler.typeRegistry.ContainsKey(type))
            {
                Type t = GenericRCOLResourceHandler.typeRegistry[type];
                return (ARCOLBlock)t.GetConstructor(new Type[] { typeof(int), typeof(EventHandler), typeof(Stream), }).Invoke(new object[] { APIversion, handler, s });
            }
            if (GenericRCOLResourceHandler.tagRegistry.ContainsKey("*"))
            {
                Type t = GenericRCOLResourceHandler.tagRegistry["*"];
                return (ARCOLBlock)t.GetConstructor(new Type[] { typeof(int), typeof(EventHandler), typeof(Stream), }).Invoke(new object[] { APIversion, handler, s });
            }
            return null;
        }

        public GenericRCOLResourceHandler()
        {
            this.Add(typeof(GenericRCOLResource), new List<string>(resourceTypes.ToArray()));
        }
    }
}
