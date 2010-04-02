/***************************************************************************
 *  Copyright (C) 2010 by Peter L Jones                                    *
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

namespace s3pi.GenericRCOLResource
{
    public class RSLT : ARCOLBlock
    {
        static bool checking = s3pi.Settings.Settings.Checking;
        const string TAG = "RSLT";

        #region Attributes
        uint tag = (uint)FOURCC(TAG);
        uint version = 4;
        PartList routes;
        SlottedPartList containers;
        PartList effects;
        PartList inverseKineticsTargets;
        #endregion

        #region Constructors
        public RSLT(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler, s) { }
        public RSLT(int APIversion, EventHandler handler, RSLT basis)
            : base(APIversion, null, null)
        {
            this.handler = handler;
            this.version = basis.version;
            this.routes = new PartList(handler, basis.routes);
            this.containers = new SlottedPartList(handler, basis.containers);
            this.effects = new PartList(handler, basis.effects);
            this.inverseKineticsTargets = new PartList(handler, basis.inverseKineticsTargets);
        }
        public RSLT(int APIversion, EventHandler handler)
            : base(APIversion, null, null)
        {
            this.handler = handler;
            routes = new PartList(handler);
            containers = new SlottedPartList(handler);
            effects = new PartList(handler);
            inverseKineticsTargets = new PartList(handler);
        }
        #endregion

        #region ARCOLBlock
        public override string Tag { get { return TAG; } }

        public override uint ResourceType { get { return 0xD3044521; } }

        protected override void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);
            tag = r.ReadUInt32();
            if (checking) if (tag != (uint)FOURCC(TAG))
                    throw new InvalidDataException(String.Format("Invalid Tag read: '{0}'; expected: '{1}'; at 0x{2:X8}", FOURCC(tag), TAG, s.Position));
            version = r.ReadUInt32();

            int nRoutes = r.ReadInt32();
            int nContainers = r.ReadInt32();
            int nEffects = r.ReadInt32();
            int nInverseKineticsTargets = r.ReadInt32();
            int zero = r.ReadInt32();
            if (checking) if (zero != 0)
                    throw new InvalidDataException(string.Format("Expected zero, read 0x{0:X8} at 0x{1:X8}", zero, s.Position));

            routes = new PartList(handler, s, nRoutes);
            containers = new SlottedPartList(handler, s, nContainers);
            effects = new PartList(handler, s, nEffects);
            inverseKineticsTargets = new PartList(handler, s, nInverseKineticsTargets);
        }

        public override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);

            w.Write(tag);
            w.Write(version);

            if (routes == null) routes = new PartList(handler);
            w.Write(routes.Count);
            if (containers == null) containers = new SlottedPartList(handler);
            w.Write(containers.Count);
            if (effects == null) effects = new PartList(handler);
            w.Write(effects.Count);
            if (inverseKineticsTargets == null) inverseKineticsTargets = new PartList(handler);
            w.Write(inverseKineticsTargets.Count);
            w.Write((int)0);

            routes.UnParse(ms);
            containers.UnParse(ms);
            effects.UnParse(ms);
            inverseKineticsTargets.UnParse(ms);

            return ms;
        }

        public override AHandlerElement Clone(EventHandler handler) { return new RSLT(requestedApiVersion, handler, this); }
        #endregion

        #region Sub-types
        public class Part : AHandlerElement, IEquatable<Part>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            protected uint slotName;
            protected uint boneName;
            float[] transformMatrix = new float[3 * 4];
            #endregion

            #region Constructors
            public Part(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public Part(int APIversion, EventHandler handler, Part basis) : this(APIversion, handler, basis.slotName, basis.boneName, basis.transformMatrix) { }
            public Part(int APIversion, EventHandler handler, uint slotName, uint boneName, float[] transformMatrix)
                : base(APIversion, handler)
            {
                this.slotName = slotName;
                this.boneName = boneName;
                if (checking) if (transformMatrix.Length != this.transformMatrix.Length)
                        throw new ArgumentLengthException("transformMatrix", transformMatrix.Length);
                this.transformMatrix = transformMatrix;
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override AHandlerElement Clone(EventHandler handler) { return new Part(requestedApiVersion, handler, this); }
            #endregion

            #region IEquatable<Route>
            public bool Equals(Part other) { return slotName.Equals(other.slotName) && boneName.Equals(other.boneName); }
            #endregion

            #region Content Fields
            [ElementPriority(1)]
            public uint SlotName { get { return slotName; } set { if (slotName != value) { slotName = value; OnElementChanged(); } } }
            [ElementPriority(2)]
            public uint BoneName { get { return boneName; } set { if (boneName != value) { boneName = value; OnElementChanged(); } } }
            public float[] TransformMatrix
            {
                get { return (float[])transformMatrix.Clone(); }
                set
                {
                    if (value.Length != this.transformMatrix.Length) throw new ArgumentLengthException("TransformMatrix", this.transformMatrix.Length);
                    if (!ArrayCompare(transformMatrix, value)) { transformMatrix = (float[])value.Clone(); OnElementChanged(); }
                }
            }
            public virtual string Value
            {
                get
                {
                    string s = "";
                    s += "SlotName: 0x" + slotName.ToString("X8");
                    s += "\nBoneName: 0x" + boneName.ToString("X8");
                    s += "\nTransformMatrix: " + this["TransformMatrix"];
                    return s;
                }
            }
            #endregion
        }
        public class PartList : AResource.DependentList<Part>
        {
            #region Constructors
            public PartList(EventHandler handler) : base(handler) { }
            public PartList(EventHandler handler, Stream s, int count) : base(null) { elementHandler = handler; Parse(s, count); this.handler = handler; }
            public PartList(EventHandler handler, IList<Part> lsb) : base(handler, lsb) { }
            #endregion

            #region Data I/O
            protected void Parse(Stream s, int count)
            {
                uint[] slotNames = new uint[count];
                uint[] boneNames = new uint[count];
                float[][] transforms = new float[count][];
                BinaryReader r = new BinaryReader(s);
                for (int i = 0; i < slotNames.Length; i++) slotNames[i] = r.ReadUInt32();
                for (int i = 0; i < boneNames.Length; i++) boneNames[i] = r.ReadUInt32();
                for (int i = 0; i < transforms.Length; i++) { transforms[i] = new float[3 * 4]; for (int j = 0; j < 3 * 4; j++) transforms[i][j] = r.ReadSingle(); }
                for (int i = 0; i < count; i++) this.Add(new Part(0, elementHandler, slotNames[i], boneNames[i], transforms[i]));
                if (count > 0)
                {
                    uint zero = r.ReadUInt32();
                    if (checking) if (zero != 0)
                            throw new InvalidDataException(string.Format("Expected zero, read 0x{0:X8} at 0x{1:X8}", zero, s.Position));
                }
            }
            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                for (int i = 0; i < Count; i++) w.Write(this[i].SlotName);
                for (int i = 0; i < Count; i++) w.Write(this[i].BoneName);
                for (int i = 0; i < Count; i++) for (int j = 0; j < this[i].TransformMatrix.Length; j++) w.Write(this[i].TransformMatrix[j]);
                if (Count > 0)
                    w.Write((uint)0);
            }

            protected override Part CreateElement(Stream s) { throw new NotImplementedException(); }
            protected override void WriteElement(Stream s, Part element) { throw new NotImplementedException(); }
            #endregion

            public override void Add() { this.Add(new Part(0, handler)); }
        }

        //SlotPlacement flags taken from ObjectCatalogResource.cs
        [Flags]
        public enum SlotPlacement : uint
        {
            //CheckFlags = 0xc3f38, 

            None = 0x01,
            //
            //
            Small = 0x08,

            Medium = 0x10,
            Large = 0x20,
            //
            //

            Sim = 0x0100,
            Chair = 0x0200,
            CounterSink = 0x0400,
            EndTable = 0x0800,

            Stool = 0x1000,
            CounterAppliance = 0x2000,
            //
            //

            //
            //
            Functional = 0x40000,
            Decorative = 0x80000,

            Upgrade = 0x1000000,
            //MatchFlags    = 0x2000000,
            Vertical = 0x2000000,
            PlacementOnly = 0x4000000,
            //

            //RotationFlags = 0x30000000,
            CardinalRotation = 0x10000000,
            FullRotation = 0x20000000,
            AlwaysUp = 0x40000000,
            //
        }
        public class SlottedPart : Part, IEquatable<SlottedPart>
        {
            SlotPlacement slotPlacementFlags;
            public SlottedPart(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public SlottedPart(int APIversion, EventHandler handler, SlottedPart basis) : base(APIversion, handler, basis) { slotPlacementFlags = basis.slotPlacementFlags; }
            public SlottedPart(int APIversion, EventHandler handler, uint slotName, uint boneName, SlotPlacement slotPlacementFlags, float[] transformMatrix)
                : base(APIversion, handler, slotName, boneName, transformMatrix) { this.slotPlacementFlags = slotPlacementFlags; }

            public bool Equals(SlottedPart other) { return ((Part)this).Equals((Part)other) && slotPlacementFlags.Equals(other.slotPlacementFlags); }

            [ElementPriority(3)]
            public SlotPlacement SlotPlacementFlags { get { return slotPlacementFlags; } set { if (slotPlacementFlags != value) { slotPlacementFlags = value; OnElementChanged(); } } }
            public override string Value
            {
                get
                {
                    string s = base.Value;
                    s += "\nSlotPlacementFlags: " + this["SlotPlacementFlags"];
                    return s;
                }
            }
        }
        public class SlottedPartList : AResource.DependentList<SlottedPart>
        {
            #region Constructors
            public SlottedPartList(EventHandler handler) : base(handler) { }
            public SlottedPartList(EventHandler handler, Stream s, int count) : base(null) { elementHandler = handler; Parse(s, count); this.handler = handler; }
            public SlottedPartList(EventHandler handler, IList<SlottedPart> lsbp) : base(handler, lsbp) { }
            #endregion

            #region Data I/O
            protected void Parse(Stream s, int count)
            {
                uint[] slotNames = new uint[count];
                uint[] boneNames = new uint[count];
                uint[] flags = new uint[count];
                float[][] transforms = new float[count][];
                BinaryReader r = new BinaryReader(s);
                for (int i = 0; i < slotNames.Length; i++) slotNames[i] = r.ReadUInt32();
                for (int i = 0; i < boneNames.Length; i++) boneNames[i] = r.ReadUInt32();
                for (int i = 0; i < flags.Length; i++) flags[i] = r.ReadUInt32();
                for (int i = 0; i < transforms.Length; i++) { transforms[i] = new float[3 * 4]; for (int j = 0; j < 3 * 4; j++) transforms[i][j] = r.ReadSingle(); }
                for (int i = 0; i < count; i++) this.Add(new SlottedPart(0, elementHandler, slotNames[i], boneNames[i], (SlotPlacement)flags[i], transforms[i]));
                if (count > 0)
                {
                    uint zero = r.ReadUInt32();
                    if (checking) if (zero != 0)
                            throw new InvalidDataException(string.Format("Expected zero, read 0x{0:X8} at 0x{1:X8}", zero, s.Position));
                }
            }
            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                for (int i = 0; i < Count; i++) w.Write(this[i].SlotName);
                for (int i = 0; i < Count; i++) w.Write(this[i].BoneName);
                for (int i = 0; i < Count; i++) w.Write((uint)this[i].SlotPlacementFlags);
                for (int i = 0; i < Count; i++) for (int j = 0; j < this[i].TransformMatrix.Length; j++) w.Write(this[i].TransformMatrix[j]);
                if (Count > 0)
                    w.Write((uint)0);
            }

            protected override SlottedPart CreateElement(Stream s) { throw new NotImplementedException(); }
            protected override void WriteElement(Stream s, SlottedPart element) { throw new NotImplementedException(); }
            #endregion

            public override void Add() { this.Add(new SlottedPart(0, handler)); }
        }
        #endregion

        #region Content Fields
        [ElementPriority(0)]
        public uint Version { get { return version; } set { if (version != value) { version = value; OnRCOLChanged(this, EventArgs.Empty); } } }
        [ElementPriority(1)]
        public PartList Routes { get { return routes; } set { if (routes != value) { routes = new PartList(handler, value); OnRCOLChanged(this, EventArgs.Empty); } } }
        [ElementPriority(2)]
        public SlottedPartList Containers { get { return containers; } set { if (containers != value) { containers = new SlottedPartList(handler, value); OnRCOLChanged(this, EventArgs.Empty); } } }
        [ElementPriority(3)]
        public PartList Effects { get { return effects; } set { if (effects != value) { effects = new PartList(handler, value); OnRCOLChanged(this, EventArgs.Empty); } } }
        [ElementPriority(4)]
        public PartList InverseKineticsTargets { get { return inverseKineticsTargets; } set { if (inverseKineticsTargets != value) { inverseKineticsTargets = new PartList(handler, value); OnRCOLChanged(this, EventArgs.Empty); } } }

        public string Value
        {
            get
            {
                string s = "";
                s += "Tag: 0x" + tag.ToString("X8");
                s += "\nVersion: 0x" + version.ToString("X8");
                s += "\n---Routes:---";
                for (int i = 0; i < routes.Count; i++) s += "\n--[" + i + "]--\n" + routes[i].Value;
                s += "\n---";
                s += "\n---Containers:---";
                for (int i = 0; i < containers.Count; i++) s += "\n--[" + i + "]--\n" + containers[i].Value;
                s += "\n---";
                s += "\n---Effects:---";
                for (int i = 0; i < effects.Count; i++) s += "\n--[" + i + "]--\n" + effects[i].Value;
                s += "\n---";
                s += "\n---InverseKineticsTargets:---";
                for (int i = 0; i < inverseKineticsTargets.Count; i++) s += "\n--[" + i + "]--\n" + inverseKineticsTargets[i].Value;
                s += "\n---";
                return s;
            }
        }
        #endregion
    }
}
