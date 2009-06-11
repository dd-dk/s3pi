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

namespace ObjKeyResource
{
    /// <summary>
    /// A resource wrapper that understands Catalog Entry resources
    /// </summary>
    public class ObjKeyResource : AResource
    {
        const Int32 recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        static bool checking = s3pi.Settings.Settings.Checking;

        #region Attributes
        uint format = 7;
        ComponentList components;
        KeyList keys;
        byte unknown1;
        TGIBlockList tgiBlocks;
        #endregion

        #region Constructors
        public ObjKeyResource(int APIversion, Stream s) : base(APIversion, s)
        {
            if (s == null)
            {
                components = new ComponentList(this);
                keys = new KeyList(this);
                tgiBlocks = new TGIBlockList(this);
                s = stream = UnParse();
                s.Position = 0;
            }
            Parse(s);
        }
        #endregion

        #region Data I/O
        void Parse(Stream s)
        {
            long tgiPosn, tgiSize;
            BinaryReader r = new BinaryReader(s);

            format = r.ReadUInt32();
            tgiPosn = r.ReadUInt32() + s.Position;
            tgiSize = r.ReadUInt32();

            components = new ComponentList(this, s);
            keys = new KeyList(this, s);
            unknown1 = r.ReadByte();

            tgiBlocks = new TGIBlockList(this, s, tgiPosn, tgiSize);
        }

        Stream UnParse()
        {
            long posn;
            MemoryStream s = new MemoryStream();
            BinaryWriter w = new BinaryWriter(s);

            w.Write(format);

            posn = s.Position;
            w.Write((uint)0);
            w.Write((uint)0);

            components.UnParse(s);
            keys.UnParse(s);
            w.Write(unknown1);

            tgiBlocks.UnParse(s, posn);

            s.Flush();

            return s;
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

        #region Sub-classes
        enum Component : uint
        {
            AnimationComponent = 0xee17c6ad,
            EffectComponent = 0x80d91e9e,
            FootprintComponent = 0xc807312a,
            LightingComponent = 0xda6c50fd,
            LocationComponent = 0x461922c8,
            LotObjectComponent = 0x6693c8b3,
            ModelComponent = 0x2954e734,
            PhysicsComponent = 0x1a8feb14,
            SacsComponent = 0x3ae9a8e7,
            ScriptComponent = 0x23177498,
            SimComponent = 0x22706efa,
            SlotComponent = 0x2ef1e401,
            SteeringComponent = 0x61bd317c,
            TransformComponent = 0x54cb7ebb,
            TreeComponent = 0xc602cd31,
            VisualStateComponent = 0x50b3d17c,
        }

        public class ComponentList : AResource.DependentList<uint, ObjKeyResource>
        {
            #region Constructors
            public ComponentList(ObjKeyResource parent) : base(parent, 255) { }
            public ComponentList(ObjKeyResource parent, IList<uint> luint) : base(parent, 255, luint) { }
            internal ComponentList(ObjKeyResource parent, Stream s) : base(parent, 255) { Parse(s); }
            #endregion

            #region Data I/O
            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadByte(); }
            protected override uint CreateElement(ObjKeyResource parent, Stream s) { return (new BinaryReader(s)).ReadUInt32(); }

            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((byte)count); }
            protected override void WriteElement(Stream s, uint element) { (new BinaryWriter(s)).Write(element); }
            #endregion

            #region ADependentList
            public override object Clone(ObjKeyResource newParent) { return new ComponentList(newParent, this); }
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("0x{0:X2}: 0x{1:X8} ({2})\n", i, this[i], (Component)this[i]); return s; } }
            #endregion
        }

        public struct Key : IComparable<Key>, IEqualityComparer<Key>, IEquatable<Key>
        {
            #region Attributes
            public readonly string key;
            public readonly byte controlCode;
            public readonly bool hasCcString;
            public readonly string ccString;
            public readonly bool hasCcIndex;
            public readonly int ccIndex;
            #endregion

            #region Constructors
            internal Key(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                key = new string(r.ReadChars(r.ReadInt32()));
                controlCode = r.ReadByte();
                switch (controlCode)
                {
                    case 0x00:
                    case 0x03: hasCcString = true; hasCcIndex = false; ccString = new string(r.ReadChars(r.ReadInt32())); ccIndex = -1; break;
                    case 0x01:
                    case 0x02: hasCcString = false; hasCcIndex = true; ccString = null; ccIndex = r.ReadInt32(); break;
                    default:
                        if (checking) throw new InvalidDataException(String.Format("Unknown control code 0x{0:X2} at position 0x{1:X8}", controlCode, s.Position));
                        hasCcString = false; hasCcIndex = false; ccString = null; ccIndex = -1;
                        break;
                }
            }

            public Key(string key, byte controlCode, string ccString) : this(key, controlCode, true, ccString, false, -1) { }
            public Key(string key, byte controlCode, int ccIndex) : this(key, controlCode, false, null, true, ccIndex) { }

            private Key(string key, byte controlCode, bool hasCcString, string ccString, bool hasCcIndex, int ccIndex)
            {
                this.key = key;
                this.controlCode = controlCode;
                this.hasCcString = hasCcString;
                if (checking) if (hasCcString && ccString == null)
                        throw new ArgumentNullException("ccString");
                this.ccString = ccString;
                this.hasCcIndex = hasCcIndex;
                this.ccIndex = ccIndex;

                if (checking) switch (controlCode)
                    {
                        case 0x00:
                        case 0x03:
                            if (!hasCcString || hasCcIndex) throw new ArgumentException(String.Format("controlCode 0x{0:X2} requires a string", controlCode));
                            break;
                        case 0x01:
                        case 0x02:
                            if (hasCcString || !hasCcIndex) throw new ArgumentException(String.Format("controlCode 0x{0:X2} requires an index", controlCode));
                            break;
                        default:
                            throw new ArgumentException(String.Format("Unknown control code 0x{0:X2}", controlCode));
                    }
            }
            #endregion

            #region Data I/O
            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(key.Length);
                w.Write(key.ToCharArray());
                w.Write(controlCode);
                if (hasCcString)
                {
                    w.Write(ccString.Length);
                    w.Write(ccString.ToCharArray());
                }
                if (hasCcIndex) w.Write(ccIndex);
            }
            #endregion

            #region IComparable<Key> Members

            public int CompareTo(Key other)
            {
                int res = key.CompareTo(other.key); if (res != 0) return res;
                res = controlCode.CompareTo(other.controlCode); if (res != 0) return res;
                if (hasCcString) { res = ccString.CompareTo(other.ccString); if (res != 0) return res; }
                if (hasCcIndex) { res = ccIndex.CompareTo(other.ccIndex); if (res != 0) return res; }
                return 0;
            }

            #endregion

            #region IEqualityComparer<Key> Members

            public bool Equals(Key x, Key y) { return x.Equals(y); }

            public int GetHashCode(Key obj) { return key.GetHashCode() ^ controlCode ^ (hasCcString ? ccString.GetHashCode() : 0) ^ (hasCcIndex ? ccIndex : 0); }

            #endregion

            #region IEquatable<Key> Members

            public bool Equals(Key other) { return this.CompareTo(other) == 0; }

            #endregion

            #region Content Fields
            public string Value
            {
                get
                {
                    string s = "";
                    s += "Key: \"" + key + "\"" +
                        "\nControl code: 0x" + controlCode.ToString("X2") +
                        "\nData: " +
                        (hasCcString ? "\"" + ccString + "\"" : "") +
                        (hasCcIndex ? "0x" + ccIndex.ToString("X8") : "") +
                        "\n"
                        ;
                    return s;
                }
            }
            #endregion
        }

        public class KeyList : AResource.DependentList<Key, ObjKeyResource>
        {
            #region Constructors
            public KeyList(ObjKeyResource parent) : base(parent, 255) { }
            public KeyList(ObjKeyResource parent, IList<Key> luint) : base(parent, 255, luint) { }
            internal KeyList(ObjKeyResource parent, Stream s) : base(parent, 255) { Parse(s); }
            #endregion

            #region Data I/O
            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadByte(); }
            protected override Key CreateElement(ObjKeyResource parent, Stream s) { return new Key(s); }

            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((byte)count); }
            protected override void WriteElement(Stream s, Key element) { element.UnParse(s); }
            #endregion

            #region ADependentList
            public override object Clone(ObjKeyResource newParent) { return new KeyList(newParent, this); }
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("--0x{0:X2}--\n{1}\n", i, this[i].Value); return s; } }
            #endregion
        }

        public class TGIBlock : AApiVersionedFields, IComparable<TGIBlock>, IEqualityComparer<TGIBlock>, IEquatable<TGIBlock>, ICloneableWithParent
        {
            #region Attributes
            ObjKeyResource parent = null;
            uint resourceType;
            uint resourceGroup;
            ulong instance;
            #endregion

            #region Constructors
            internal TGIBlock(ObjKeyResource parent, Stream s) { this.parent = parent; Parse(s); }
            internal TGIBlock(ObjKeyResource parent, TGIBlock tgib) : this(parent, tgib.resourceType, tgib.resourceGroup, tgib.instance) { }

            public TGIBlock(ObjKeyResource parent, uint resourceType, uint resourceGroup, ulong instance)
            {
                this.parent = parent;
                this.resourceType = resourceType;
                this.resourceGroup = resourceGroup;
                this.instance = instance;
            }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                resourceType = r.ReadUInt32();
                resourceGroup = r.ReadUInt32();
                instance = r.ReadUInt64();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(resourceType);
                w.Write(resourceGroup);
                w.Write(instance);
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

            #region ICloneableWithParent Members

            public object Clone(object newParent) { return new TGIBlock(newParent as ObjKeyResource, this); }

            #endregion

            #region ICloneable Members

            public object Clone() { return Clone(parent); }

            #endregion

            #region AApiVersionedFields
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            #endregion

            #region Content Fields
            public uint ResourceType { get { return resourceType; } set { if (resourceType != value) { resourceType = value; parent.OnResourceChanged(this, new EventArgs()); } } }
            public uint ResourceGroup { get { return resourceGroup; } set { if (resourceGroup != value) { resourceGroup = value; parent.OnResourceChanged(this, new EventArgs()); } } }
            public ulong Instance { get { return instance; } set { if (instance != value) { instance = value; parent.OnResourceChanged(this, new EventArgs()); } } }

            public String Value { get { return String.Format("0x{0:X8}-0x{1:X8}-0x{2:X16}", resourceType, resourceGroup, instance); } }
            #endregion
        }

        public class TGIBlockList : AResource.DependentList<TGIBlock, ObjKeyResource>
        {
            #region Constructors
            public TGIBlockList(ObjKeyResource parent) : base(parent) { }
            public TGIBlockList(ObjKeyResource parent, IList<TGIBlock> lme) : base(parent, lme) { }
            internal TGIBlockList(ObjKeyResource parent, Stream s, long tgiOffset, long tgiSize)
                : base(parent) { Parse(s, tgiOffset, tgiSize); }
            #endregion

            #region Data I/O
            protected override TGIBlock CreateElement(ObjKeyResource parent, Stream s) { return new TGIBlock(parent, s); }
            protected override void WriteElement(Stream s, TGIBlock element) { element.UnParse(s); }

            protected void Parse(Stream s, long tgiPosn, long tgiSize)
            {
                if (checking) if (tgiPosn != s.Position)
                        throw new InvalidDataException(String.Format("Position of TGIBlock read: 0x{0:X8}, actual: 0x{1:X8}",
                            tgiPosn, s.Position));

                Parse(s);

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
            public override object Clone(ObjKeyResource newParent) { return new TGIBlockList(newParent, this); }
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("0x{0:X8}: {1}\n", i, this[i].Value); return s; } }
            #endregion
        }

        #endregion


        #region Content Fields
        public uint Format { get { return format; } set { if (format != value) { format = value; OnResourceChanged(this, new EventArgs()); } } }
        public ComponentList Components { get { return components; } set { if (components != value) { components = new ComponentList(this, value); OnResourceChanged(this, new EventArgs()); } } }
        public KeyList Keys { get { return keys; } set { if (keys != value) { keys = new KeyList(this, value); OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, new EventArgs()); } } }
        public TGIBlockList TGIBlocks { get { return tgiBlocks; } set { if (tgiBlocks != value) { tgiBlocks = new TGIBlockList(this, value); OnResourceChanged(this, new EventArgs()); } } }

        public string Value
        {
            get
            {
                string s = "";
                s += String.Format("Format: 0x{0:X8}", format);
                s += "\nComponents:\n" + components.Value + "----\n";
                s += "\nKeys:\n" + keys.Value + "----\n";
                s += String.Format("\nUnknown1: 0x{0:X2}", unknown1);
                s += "\nTGIBlocks:\n" + tgiBlocks.Value + "----\n";
                return s;
            }
        }
        #endregion
    }

    /// <summary>
    /// ResourceHandler for CatalogResource wrapper
    /// </summary>
    public class ObjKeyResourceHandler : AResourceHandler
    {
        public ObjKeyResourceHandler()
        {
            this.Add(typeof(ObjKeyResource), new List<string>(new string[] { "0x02DC343F" }));
        }
    }
}
