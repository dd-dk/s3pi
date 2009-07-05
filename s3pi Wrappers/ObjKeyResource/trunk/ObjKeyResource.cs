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
            components = new ComponentList(OnResourceChanged);
            keys = new KeyList(OnResourceChanged);
            tgiBlocks = new TGIBlockList(OnResourceChanged);

            if (s == null) s = UnParse();
            s.Position = 0;
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

            components = new ComponentList(OnResourceChanged, s);
            keys = new KeyList(OnResourceChanged, s);
            unknown1 = r.ReadByte();

            tgiBlocks = new TGIBlockList(OnResourceChanged, s, tgiPosn, tgiSize);
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

        public class ComponentList : AResource.DependentList<uint>
        {
            #region Constructors
            public ComponentList(EventHandler handler) : base(handler, 255) { }
            public ComponentList(EventHandler handler, IList<uint> luint) : base(handler, 255, luint) { }
            internal ComponentList(EventHandler handler, Stream s) : base(handler, 255) { Parse(s); }
            #endregion

            #region Data I/O
            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadByte(); }
            protected override uint CreateElement(Stream s) { return (new BinaryReader(s)).ReadUInt32(); }

            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((byte)count); }
            protected override void WriteElement(Stream s, uint element) { (new BinaryWriter(s)).Write(element); }
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("0x{0:X2}: 0x{1:X8} ({2})\n", i, this[i], (Component)this[i]); return s; } }
            #endregion
        }

        public class Key : AHandlerElement, IComparable<Key>, IEqualityComparer<Key>, IEquatable<Key>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            string key;
            byte controlCode;
            bool hasCcString;
            string ccString;
            bool hasCcIndex;
            int ccIndex;
            #endregion

            #region Constructors
            internal Key(int APIversion, EventHandler handler, Stream s)
                : base(APIversion, handler) { Parse(s); }

            public Key(int APIversion, EventHandler handler, string key, byte controlCode, string ccString)
                : this(APIversion, handler, key, controlCode, true, ccString, false, -1) { }
            public Key(int APIversion, EventHandler handler, string key, byte controlCode, int ccIndex)
                : this(APIversion, handler, key, controlCode, false, null, true, ccIndex) { }

            private Key(int APIversion, EventHandler handler, string key, byte controlCode, bool hasCcString, string ccString, bool hasCcIndex, int ccIndex)
                : base(APIversion, handler)
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
            void Parse(Stream s)
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

            #region AHandlerElement Members
            public override AHandlerElement Clone(EventHandler handler) { return new Key(requestedApiVersion, handler, key, controlCode, hasCcString, ccString, hasCcIndex, ccIndex); }

            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            public override List<string> ContentFields { get { return AApiVersionedFields.GetContentFields(requestedApiVersion, this.GetType()); } }
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
            public string EntryName { get { return key; } set { if (key != value) { key = value; OnElementChanged(); } } }
            public byte ControlCode
            {
                get { return controlCode; }
                set
                {
                    if (controlCode == value) return;
                    switch (value)
                    {
                        case 0x00:
                        case 0x03:
                            hasCcString = true; hasCcIndex = false;
                            ccString = ""; ccIndex = -1;
                            break;
                        case 0x01:
                        case 0x02:
                            hasCcString = false; hasCcIndex = true;
                            ccString = null; ccIndex = 0;
                            break;
                        default:
                            throw new ArgumentException(String.Format("Unknown control code 0x{0:X2}", controlCode));
                    }
                    controlCode = value;
                    OnElementChanged();
                }
            }
            public bool HasCcString { get { return hasCcString; } set { } }
            public string CcString { get { return ccString; } set { if (!hasCcString) throw new InvalidOperationException(); if (ccString != value) { ccString = value; OnElementChanged(); } } }
            public bool HasCcIndex { get { return hasCcIndex; } set { } }
            public int CcIndex { get { return ccIndex; } set { if (!hasCcIndex) throw new InvalidOperationException(); if (ccIndex != value) { ccIndex = value; OnElementChanged(); } } }

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

        public class KeyList : AResource.DependentList<Key>
        {
            #region Constructors
            public KeyList(EventHandler handler) : base(handler, 255) { }
            public KeyList(EventHandler handler, IList<Key> luint) : base(handler, 255, luint) { }
            internal KeyList(EventHandler handler, Stream s) : base(handler, 255) { Parse(s); }
            #endregion

            #region Data I/O
            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadByte(); }
            protected override Key CreateElement(Stream s) { return new Key(0, handler, s); }

            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((byte)count); }
            protected override void WriteElement(Stream s, Key element) { element.UnParse(s); }
            #endregion

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("--0x{0:X2}--\n{1}\n", i, this[i].Value); return s; } }
            #endregion
        }

        #endregion


        #region Content Fields
        public uint Format { get { return format; } set { if (format != value) { format = value; OnResourceChanged(this, new EventArgs()); } } }
        public ComponentList Components { get { return components; } set { if (components != value) { components = new ComponentList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        public KeyList Keys { get { return keys; } set { if (keys != value) { keys = new KeyList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }
        public byte Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, new EventArgs()); } } }
        public TGIBlockList TGIBlocks { get { return tgiBlocks; } set { if (tgiBlocks != value) { tgiBlocks = new TGIBlockList(OnResourceChanged, value); OnResourceChanged(this, new EventArgs()); } } }

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
