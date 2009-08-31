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

namespace s3pi.GenericRCOLResource
{
    public class FTPT : ARCOLBlock
    {
        static bool checking = s3pi.Settings.Settings.Checking;

        #region Attributes
        uint tag = (uint)FOURCC("FTPT");
        uint version = 6;
        AreaList footprintAreas;
        AreaList slotAreas;
        #endregion

        public FTPT(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler, s) { }
        public FTPT(int APIversion, EventHandler handler, FTPT basis)
            : base(APIversion, handler, null)
        {
            this.version = basis.version;
            this.footprintAreas = new AreaList(handler, basis.footprintAreas);
            this.slotAreas = new AreaList(handler, basis.slotAreas);
        }
        public FTPT(int APIversion, EventHandler handler)
            : base(APIversion, handler, null)
        {
            this.footprintAreas = new AreaList(handler);
            this.slotAreas = new AreaList(handler);
        }

        #region ARCOLBlock
        public override string Tag { get { return "FTPT"; } }

        public override uint ResourceType { get { return 0xD382BF57; } }

        protected override void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);
            tag = r.ReadUInt32();
            if (checking) if (tag != (uint)FOURCC("FTPT"))
                    throw new InvalidDataException(String.Format("Invalid Tag read: '{0}'; expected: 'FTPT'; at 0x{1:X8}", FOURCC(tag), s.Position));
            version = r.ReadUInt32();
            footprintAreas = new AreaList(handler, s);
            slotAreas = new AreaList(handler, s);
        }

        public override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);

            w.Write(tag);
            w.Write(version);
            if (footprintAreas == null) footprintAreas = new AreaList(handler);
            footprintAreas.UnParse(ms);
            if (slotAreas == null) slotAreas = new AreaList(handler);
            slotAreas.UnParse(ms);

            return ms;
        }

        public override AHandlerElement Clone(EventHandler handler) { return new FTPT(requestedApiVersion, handler, this); }
        #endregion

        #region Sub-types
        public class PolygonPoint : AHandlerElement, IEquatable<PolygonPoint>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            float x;
            float y;
            #endregion
            
            #region Constructors
            public PolygonPoint(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public PolygonPoint(int APIversion, EventHandler handler, PolygonPoint basis)
                : base(APIversion, handler)
            {
                this.x = basis.x;
                this.y = basis.y;
            }
            public PolygonPoint(int APIversion, EventHandler handler, float X, float Y)
                : base(APIversion, handler)
            {
                this.x = X;
                this.y = Y;
            }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.x = r.ReadSingle();
                this.y = r.ReadSingle();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(x);
                w.Write(y);
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            /// <summary>
            /// The list of available field names on this API object
            /// </summary>
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override AHandlerElement Clone(EventHandler handler) { return new PolygonPoint(requestedApiVersion, handler, this); }
            #endregion

            #region IEquatable<PolygonPoint> Members

            public bool Equals(PolygonPoint other)
            {
                return this.x == other.x && this.y == other.y;
            }

            #endregion

            #region Content Fields
            public float X { get { return x; } set { if (x != value) { x = value; OnElementChanged(); } } }
            public float Y { get { return y; } set { if (y != value) { y = value; OnElementChanged(); } } }

            public string Value { get { return String.Format("[X: {0}] [Y: {1}]", x, y); } }
            #endregion
        }
        public class PolygonPointList : AResource.DependentList<PolygonPoint>
        {
            #region Constructors
            public PolygonPointList(EventHandler handler) : base(handler, 255) { }
            public PolygonPointList(EventHandler handler, Stream s) : base(handler, 255, s) { }
            public PolygonPointList(EventHandler handler, IList<PolygonPoint> lpp) : base(handler, 255, lpp) { }
            #endregion

            #region Data I/O
            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadByte(); }
            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((byte)count); }

            protected override PolygonPoint CreateElement(Stream s) { return new PolygonPoint(0, elementHandler, s); }

            protected override void WriteElement(Stream s, PolygonPoint element) { element.UnParse(s); }
            #endregion

            public override void Add() { this.Add(new PolygonPoint(0, elementHandler, 0, 0)); }
        }

        [Flags]
        public enum AreaType : uint
        {
            Unknown = 0,
            Placement = 1,
            Pathing = 2,
            Shape = 4,
        }
        public class Area : AHandlerElement, IEquatable<Area>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            uint name;
            byte unknown1;
            AreaType areaType;
            PolygonPointList closedPolygon;
            uint placementFlags1;
            uint placementFlags2;
            uint placementFlags3;
            byte unknown2;
            float lowerX;
            float lowerY;
            float upperX;
            float upperY;
            #endregion
            
            #region Constructors
            public Area(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public Area(int APIversion, EventHandler handler, Area basis)
                : base(APIversion, handler)
            {
                this.name = basis.name;
                this.unknown1 = basis.unknown1;
                this.areaType = basis.areaType;
                this.closedPolygon = new PolygonPointList(handler, basis.closedPolygon);
                this.placementFlags1 = basis.placementFlags1;
                this.placementFlags2 = basis.placementFlags2;
                this.placementFlags3 = basis.placementFlags3;
                this.unknown2 = basis.unknown2;
                this.lowerX = basis.lowerX;
                this.lowerY = basis.lowerY;
                this.upperX = basis.upperX;
                this.upperY = basis.upperY;
            }
            /*public Area(int APIversion, EventHandler handler,
                uint name, byte unknown1, AreaType areaType, uint placementFlags1, uint placementFlags2, uint placementFlags3,
                byte unknown2, float lowerX, float lowerY, float upperX, float upperY)
                : this(APIversion, handler, name, unknown1, areaType, new List<PolygonPoint>(),
                placementFlags1, placementFlags2, placementFlags3, unknown2, lowerX, lowerY, upperX, upperY) { }/**/
            public Area(int APIversion, EventHandler handler,
                uint name, byte unknown1, AreaType areaType, IList<PolygonPoint> closedPolygon, uint placementFlags1, uint placementFlags2, uint placementFlags3,
                byte unknown2, float lowerX, float lowerY, float upperX, float upperY)
                : base(APIversion, handler)
            {
                this.name = name;
                this.unknown1 = unknown1;
                this.areaType = areaType;
                this.closedPolygon = new PolygonPointList(handler, closedPolygon);
                this.placementFlags1 = placementFlags1;
                this.placementFlags2 = placementFlags2;
                this.placementFlags3 = placementFlags3;
                this.unknown2 = unknown2;
                this.lowerX = lowerX;
                this.lowerY = lowerY;
                this.upperX = upperX;
                this.upperY = upperY;
            }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.name = r.ReadUInt32();
                this.unknown1 = r.ReadByte();
                this.areaType = (AreaType)r.ReadUInt32();
                this.closedPolygon = new PolygonPointList(handler, s);
                this.placementFlags1 = r.ReadUInt32();
                this.placementFlags2 = r.ReadUInt32();
                this.placementFlags3 = r.ReadUInt32();
                this.unknown2 = r.ReadByte();
                this.lowerX = r.ReadSingle();
                this.lowerY = r.ReadSingle();
                this.upperX = r.ReadSingle();
                this.upperY = r.ReadSingle();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(name);
                w.Write(unknown1);
                w.Write((uint)areaType);
                if (closedPolygon == null) closedPolygon = new PolygonPointList(handler);
                closedPolygon.UnParse(s);
                w.Write(placementFlags1);
                w.Write(placementFlags2);
                w.Write(placementFlags3);
                w.Write(unknown2);
                w.Write(lowerX);
                w.Write(lowerY);
                w.Write(upperX);
                w.Write(upperY);
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            /// <summary>
            /// The list of available field names on this API object
            /// </summary>
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override AHandlerElement Clone(EventHandler handler) { return new Area(requestedApiVersion, handler, this); }
            #endregion

            #region IEquatable<Area> Members

            public bool Equals(Area other)
            {
                return name == other.name &&
                    unknown1 == other.unknown1 &&
                    areaType == other.areaType &&
                    closedPolygon == other.closedPolygon &&
                    placementFlags1 == other.placementFlags1 &&
                    placementFlags2 == other.placementFlags2 &&
                    placementFlags3 == other.placementFlags3 &&
                    unknown2 == other.unknown2 &&
                    lowerX == other.lowerX &&
                    lowerY == other.lowerY &&
                    upperX == other.upperX &&
                    upperY == other.upperY;
            }

            #endregion

            #region Content Fields
            public uint Name { get { return name; } set { if (name != value) { name = value; OnElementChanged(); } } }
            public byte Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnElementChanged(); } } }
            public AreaType AreaType { get { return areaType; } set { if (areaType != value) { areaType = value; OnElementChanged(); } } }
            public PolygonPointList ClosedPolygon { get { return closedPolygon; } set { if (closedPolygon != value) { closedPolygon = new PolygonPointList(handler, value); OnElementChanged(); } } }
            public uint PlacementFlags1 { get { return placementFlags1; } set { if (placementFlags1 != value) { placementFlags1 = value; OnElementChanged(); } } }
            public uint PlacementFlags2 { get { return placementFlags2; } set { if (placementFlags2 != value) { placementFlags2 = value; OnElementChanged(); } } }
            public uint PlacementFlags3 { get { return placementFlags3; } set { if (placementFlags3 != value) { placementFlags3 = value; OnElementChanged(); } } }
            public byte Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnElementChanged(); } } }
            public float LowerX { get { return lowerX; } set { if (lowerX != value) { lowerX = value; OnElementChanged(); } } }
            public float LowerY { get { return lowerY; } set { if (lowerY != value) { lowerY = value; OnElementChanged(); } } }
            public float UpperX { get { return upperX; } set { if (upperX != value) { upperX = value; OnElementChanged(); } } }
            public float UpperY { get { return upperY; } set { if (upperY != value) { upperY = value; OnElementChanged(); } } }

            public string Value
            {
                get
                {
                    string s = "";
                    s += "Name: 0x" + name.ToString("X8");
                    s += "\nUnknown1: 0x" + unknown1.ToString("X8");
                    s += "\nAreaType: " + new TypedValue(areaType.GetType(), areaType, "X");
                    s += "\nClosedPolygon: "; foreach (PolygonPoint pp in closedPolygon) s += pp.Value + "; "; s = s.TrimEnd(';', ' ');
                    s += "\nPlacementFlags1: 0x" + placementFlags1.ToString("X8");
                    s += "\nPlacementFlags2: 0x" + placementFlags2.ToString("X8");
                    s += "\nPlacementFlags3: 0x" + placementFlags3.ToString("X8");
                    s += "\nUnknown2: 0x" + unknown2.ToString("X8");
                    s += "\nLowerX: " + lowerX;
                    s += "\nLowerY: " + lowerY;
                    s += "\nUpperX: " + upperX;
                    s += "\nUpperY: " + upperY;
                    return s;
                }
            }
            #endregion
        }
        public class AreaList : AResource.DependentList<Area>
        {
            #region Constructors
            public AreaList(EventHandler handler) : base(handler, 255) { }
            public AreaList(EventHandler handler, Stream s) : base(handler, 255, s) { }
            public AreaList(EventHandler handler, IList<Area> lfpa) : base(handler, 255, lfpa) { }
            #endregion

            #region Data I/O
            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadByte(); }
            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((byte)count); }

            protected override Area CreateElement(Stream s) { return new Area(0, elementHandler, s); }

            protected override void WriteElement(Stream s, Area element) { element.UnParse(s); }
            #endregion

            public override void Add() { this.Add(new Area(0, elementHandler, 0, 0, (AreaType)0, new List<PolygonPoint>(), 0, 0, 0, 0, 0, 0, 0, 0)); }
        }
        #endregion

        #region Content Fields
        public uint Version { get { return version; } set { if (version != value) { version = value; OnRCOLChanged(this, EventArgs.Empty); } } }
        public AreaList FootprintAreas { get { return footprintAreas; } set { if (footprintAreas != value) { footprintAreas = new AreaList(handler, value); OnRCOLChanged(this, EventArgs.Empty); } } }
        public AreaList SlotAreas { get { return slotAreas; } set { if (slotAreas != value) { slotAreas = new AreaList(handler, value); OnRCOLChanged(this, EventArgs.Empty); } } }

        public string Value
        {
            get
            {
                string s = "";
                s += "Tag: 0x" + tag.ToString("X8");
                s += "\nVersion: 0x" + version.ToString("X8");

                s += "\n--\nFootprintAreas:";
                for (int i = 0; i < footprintAreas.Count; i++)
                    s += "\n-[" + i + "]-\n" + footprintAreas[i].Value;

                s += "\n--\nSlotAreas:";
                for (int i = 0; i < slotAreas.Count; i++)
                    s += "\n-[" + i + "]-\n" + slotAreas[i].Value;
                return s;
            }
        }
        #endregion
    }
}
