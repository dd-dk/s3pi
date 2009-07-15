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
    public class GenericRCOLResource : AResource
    {
        static bool checking = s3pi.Settings.Settings.Checking;
        const Int32 recommendedApiVersion = 1;

        uint version;
        uint dataType;
        uint unused;
        ChunkEntryList blockList;
        CountedTGIBlockList resources;

        #region Constructors
        public GenericRCOLResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); dirty = true; } stream.Position = 0; Parse(stream); }
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

            blockList = new ChunkEntryList(OnResourceChanged);
            for (int i = 0; i < countChunks; i++)
            {
                s.Position = index[i].Position;
                byte[] data = r.ReadBytes(index[i].Length);
                MemoryStream ms = new MemoryStream();
                ms.Write(data, 0, data.Length);
                ms.Position = 0;

                blockList.Add(new GenericRCOLResource.ChunkEntry(requestedApiVersion, OnResourceChanged,
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
                    stream.Position = 0;
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

        public class ChunkEntry : AHandlerElement, IEquatable<ChunkEntry>
        {
            const Int32 recommendedApiVersion = 1;
            AResource.TGIBlock tgiBlock;
            ARCOLBlock rcolBlock;
            public ChunkEntry(int APIversion, EventHandler handler, AResource.TGIBlock tgiBlock, ARCOLBlock rcolBlock)
                : base(APIversion, handler) { this.tgiBlock = tgiBlock; this.rcolBlock = rcolBlock; }

            #region AHandlerElement Members
            public override AHandlerElement Clone(EventHandler handler) { return new ChunkEntry(requestedApiVersion, handler, this.tgiBlock, this.rcolBlock); }

            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<ChunkEntry> Members

            public bool Equals(ChunkEntry other) { return tgiBlock == other.tgiBlock && rcolBlock == other.rcolBlock; }

            #endregion

            public AResource.TGIBlock TGIBlock { get { return tgiBlock; } set { if (tgiBlock != value) { tgiBlock = new TGIBlock(0, handler, value); OnElementChanged(); } } }
            public ARCOLBlock RCOLBlock { get { return rcolBlock; } set { if (rcolBlock != value) { rcolBlock = (ARCOLBlock)rcolBlock.Clone(handler); OnElementChanged(); } } }

            public string Value
            {
                get
                {
                    string s = "";
                    s += "--- " + tgiBlock + ((rcolBlock.Equals("*")) ? "" : " - " + rcolBlock.Tag) + " ---";
                    if (AApiVersionedFields.GetContentFields(0, rcolBlock.GetType()).Contains("Value"))
                        s += "\n" + rcolBlock["Value"];
                    else foreach (string field in AApiVersionedFields.GetContentFields(0, rcolBlock.GetType()))
                        {
                            if (!(new List<string>(new string[] { "ResourceType", "Tag", "Value", "Stream", "AsBytes", })).Contains(field))
                                s += "\n  " + field + ": " + rcolBlock[field];
                        }
                    s += "\n----";
                    return s;
                }
            }
        }

        public class ChunkEntryList : DependentList<ChunkEntry>
        {
            public ChunkEntryList(EventHandler handler) : base(handler) { }
            public ChunkEntryList(EventHandler handler, IList<ChunkEntry> ice) : base(handler, ice) { }

            protected override ChunkEntry CreateElement(EventHandler handler, Stream s) { throw new NotImplementedException(); }
            protected override void WriteElement(Stream s, ChunkEntry element) { throw new NotImplementedException(); }
        }

        #endregion

        #region Content Fields
        public uint Version { get { return version; } set { if (version != value) { version = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public uint DataType { get { return dataType; } set { if (dataType != value) { dataType = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public uint Unused { get { return unused; } set { if (unused != value) { unused = value; OnResourceChanged(this, EventArgs.Empty); } } }
        public CountedTGIBlockList Resources { get { return resources; } set { if (resources != value) { resources = new CountedTGIBlockList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        public ChunkEntryList ChunkEntries { get { return blockList; } set { if (blockList != value) { blockList = new ChunkEntryList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }

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
                    s += "[" + i + "] " + blockList[i].Value;
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
