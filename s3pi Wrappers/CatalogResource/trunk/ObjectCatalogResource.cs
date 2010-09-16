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

namespace CatalogResource
{
    public class ObjectCatalogResource : CatalogResourceTGIBlockList
    {
        #region Attributes
        MaterialList materialList = null;
        string unknown1 = "";
        uint unknown2;
        byte unknown3;
        Fire fireType;
        byte isStealable;
        byte isReposessable;
        uint inWorldEditable;
        uint objkIndex;
        Misc8 unknown8;
        Misc9 unknown9;
        Misc10 unknown10;
        Misc11 unknown11;
        uint unknown12;
        MTDoorList mtDoorList = null;
        byte unknown13;
        uint diagonalIndex;
        uint hash;
        RoomCategory roomCategoryFlags;
        FunctionCategory functionCategoryFlags;
        FunctionSubCategory functionSubCategoryFlags;
        RoomSubCategory roomSubCategoryFlags;
        BuildCategory buildCategoryFlags;
        uint sinkDDSIndex;
        uint unknown16;
        uint unknown17;
        uint unknown18;
        SlotPlacement slotPlacementFlags;
        string materialGrouping1 = "";
        string materialGrouping2 = "";
        Moodlet moodletGiven;
        int moodletScore;
        uint unknown21;
        TopicRating[] topicRatings = new TopicRating[5];
        uint fallbackIndex;
        #endregion

        #region Constructors
        public ObjectCatalogResource(int APIversion, Stream s) : base(APIversion, s) { }
        public ObjectCatalogResource(int APIversion, Stream unused, ObjectCatalogResource basis)
            : base(APIversion, basis.version, basis.list)
        {
            this.unknown1 = (this.version >= 0x00000016) ? basis.unknown1 : null;
            this.materialList = new MaterialList(OnResourceChanged, basis.materialList);
            this.common = new Common(requestedApiVersion, OnResourceChanged, basis.common);
            this.unknown2 = basis.unknown2;
            this.unknown3 = basis.unknown3;
            this.fireType = basis.fireType;
            this.isStealable = basis.isStealable;
            this.isReposessable = basis.isReposessable;
            this.inWorldEditable = basis.inWorldEditable;
            this.objkIndex = basis.objkIndex;
            this.unknown8 = basis.unknown8;
            this.unknown9 = basis.unknown9;
            this.unknown10 = basis.unknown10;
            this.unknown11 = basis.unknown11;
            this.unknown12 = basis.unknown12;
            this.mtDoorList = new MTDoorList(OnResourceChanged, basis.mtDoorList);
            this.unknown13 = basis.unknown13;
            this.diagonalIndex = basis.diagonalIndex;
            this.hash = basis.hash;
            this.roomCategoryFlags = basis.roomCategoryFlags;
            this.functionCategoryFlags = basis.functionCategoryFlags;
            this.functionSubCategoryFlags = basis.functionSubCategoryFlags;
            this.roomSubCategoryFlags = basis.roomSubCategoryFlags;
            this.buildCategoryFlags = basis.buildCategoryFlags;
            this.sinkDDSIndex = basis.sinkDDSIndex;
            this.slotPlacementFlags = basis.slotPlacementFlags;
            this.materialGrouping1 = basis.materialGrouping1;
            this.materialGrouping2 = basis.materialGrouping2;
            this.moodletGiven = basis.moodletGiven;
            this.moodletScore = basis.moodletScore;
            this.unknown21 = basis.unknown21;
            this.topicRatings = (TopicRating[])basis.topicRatings.Clone();
            this.fallbackIndex = basis.fallbackIndex;
        }
        public ObjectCatalogResource(int APIversion,
            uint version, IList<Material> materialList, Common common, uint unknown2, byte unknown3, Fire fireType,
            byte isStealable, byte isReposessable, uint inWorldEditable, uint objkIndex,
            Misc8 unknown8, Misc9 unknown9, Misc10 unknown10, Misc11 unknown11,
            uint unknown12, IList<MTDoor> mtDoorList, byte unknown13, uint diagonalIndex, uint hash,
            uint roomFlags, uint functionCategoryFlags, ulong subFunctionFlags, ulong subRoomFlags, uint buildCategoryFlags, uint sinkDDSIndex,
            uint slotPlacementFlags, string materialGrouping1, string materialGrouping2, Moodlet moodletGiven, int moodletScore, uint unknown21,
            TopicRating[] topicRatings,
            uint fallbackIndex, TGIBlockList ltgib)
            : this(APIversion,
            version, materialList,
            "",
            common, unknown2, unknown3, fireType,
            isStealable, isReposessable, inWorldEditable, objkIndex,
            unknown8, unknown9, unknown10, unknown11,
            unknown12, mtDoorList, unknown13, diagonalIndex, hash,
            roomFlags, functionCategoryFlags, subFunctionFlags, subRoomFlags, buildCategoryFlags, sinkDDSIndex,
            0, 0, 0,
            slotPlacementFlags, materialGrouping1, materialGrouping2, moodletGiven, moodletScore, unknown21,
            topicRatings,
            fallbackIndex, ltgib)
        {
            if (checking) if (version >= 0x00000016)
                    throw new InvalidOperationException(String.Format("Constructor requires Unknown1 for version {0}", version));
        }
        public ObjectCatalogResource(int APIversion,
            uint version, IList<Material> materialList,
            string unknown1,
            Common common, uint unknown2, byte unknown3, Fire fireType,
            byte isStealable, byte isReposessable, uint inWorldEditable, uint objkIndex,
            Misc8 unknown8, Misc9 unknown9, Misc10 unknown10, Misc11 unknown11,
            uint unknown12, IList<MTDoor> mtDoorList, byte unknown13, uint diagonalIndex, uint hash,
            uint roomFlags, uint functionCategoryFlags, ulong subFunctionFlags, ulong subRoomFlags, uint buildCategoryFlags, uint sinkDDSIndex,
            uint slotPlacementFlags, string materialGrouping1, string materialGrouping2, Moodlet moodletGiven, int moodletScore, uint unknown21,
            TopicRating[] topicRatings,
            uint fallbackIndex, TGIBlockList ltgib)
            : this(APIversion,
            version, materialList,
            unknown1,
            common, unknown2, unknown3, fireType,
            isStealable, isReposessable, inWorldEditable, objkIndex,
            unknown8, unknown9, unknown10, unknown11,
            unknown12, mtDoorList, unknown13, diagonalIndex, hash,
            roomFlags, functionCategoryFlags, subFunctionFlags, subRoomFlags, buildCategoryFlags, sinkDDSIndex,
            0, 0, 0,
            slotPlacementFlags, materialGrouping1, materialGrouping2, moodletGiven, moodletScore, unknown21,
            topicRatings,
            fallbackIndex, ltgib)
        {
            if (checking) if (version >= 0x00000017)
                    throw new InvalidOperationException(String.Format("Constructor requires Unknown16, Unknown17 and Unknown18 for version {0}", version));
        }
        public ObjectCatalogResource(int APIversion,
            uint version, IList<Material> materialList,
            string unknown1,
            Common common, uint unknown2, byte unknown3, Fire fireType,
            byte isStealable, byte isReposessable, uint inWorldEditable, uint objkIndex,
            Misc8 unknown8, Misc9 unknown9, Misc10 unknown10, Misc11 unknown11,
            uint unknown12, IList<MTDoor> mtDoorList, byte unknown13, uint diagonalIndex, uint hash,
            uint roomFlags, uint functionCategoryFlags, ulong subFunctionFlags, ulong subRoomFlags, uint buildCategoryFlags, uint sinkDDSIndex,
            uint unknown16, uint unknown17, uint unknown18,
            uint slotPlacementFlags, string materialGrouping1, string materialGrouping2, Moodlet moodletGiven, int moodletScore, uint unknown21,
            TopicRating[] topicRatings,
            uint fallbackIndex, TGIBlockList ltgib)
            : base(APIversion, version, ltgib)
        {
            this.materialList = new MaterialList(OnResourceChanged, materialList);
            this.unknown1 = unknown1;
            this.common = new Common(requestedApiVersion, OnResourceChanged, common);
            this.unknown2 = unknown2;
            this.unknown3 = unknown3;
            this.fireType = fireType;
            this.isStealable = (byte)(isStealable == 0 ? 0 : 1);
            this.isReposessable = (byte)(isReposessable == 0 ? 0 : 1);
            this.inWorldEditable = inWorldEditable;
            this.objkIndex = objkIndex;
            this.unknown8 = unknown8;
            this.unknown9 = unknown9;
            this.unknown10 = unknown10;
            this.unknown11 = unknown11;
            this.unknown12 = unknown12;
            this.mtDoorList = new MTDoorList(OnResourceChanged, mtDoorList);
            this.unknown13 = unknown13;
            this.diagonalIndex = diagonalIndex;
            this.hash = hash;
            this.roomCategoryFlags = (RoomCategory)roomFlags;
            this.functionCategoryFlags = (FunctionCategory)functionCategoryFlags;
            this.functionSubCategoryFlags = (FunctionSubCategory)subFunctionFlags;
            this.roomSubCategoryFlags = (RoomSubCategory)subRoomFlags;
            this.buildCategoryFlags = (BuildCategory)buildCategoryFlags;
            this.sinkDDSIndex = sinkDDSIndex;
            this.unknown16 = unknown16;
            this.unknown17 = unknown17;
            this.unknown18 = unknown18;
            this.slotPlacementFlags = (SlotPlacement)slotPlacementFlags;
            this.materialGrouping1 = materialGrouping1;
            this.materialGrouping2 = materialGrouping2;
            this.moodletGiven = moodletGiven;
            this.moodletScore = moodletScore;
            this.unknown21 = unknown21;
            if (checking) if (topicRatings.Length != 5)
                    throw new ArgumentLengthException("TopicRatings", this.topicRatings.Length);
            this.topicRatings = (TopicRating[])topicRatings.Clone();
            this.fallbackIndex = fallbackIndex;
        }
        #endregion

        #region Data I/O
        protected override void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);
            BinaryReader r2 = new BinaryReader(s, System.Text.Encoding.BigEndianUnicode);

            base.Parse(s);
            this.materialList = new MaterialList(OnResourceChanged, s);
            this.unknown1 = (this.version >= 0x00000016) ? r2.ReadString() : null;
            this.common = new Common(requestedApiVersion, OnResourceChanged, s);
            this.unknown2 = r.ReadUInt32();
            this.unknown3 = r.ReadByte();
            this.fireType = (Fire)r.ReadUInt32();
            this.isStealable = r.ReadByte();
            this.isReposessable = r.ReadByte();
            this.inWorldEditable = r.ReadUInt32();
            this.objkIndex = r.ReadUInt32();
            this.unknown8 = (Misc8)r.ReadUInt32();
            this.unknown9 = (Misc9)r.ReadUInt32();
            this.unknown10 = (Misc10)r.ReadUInt32();
            this.unknown11 = (Misc11)r.ReadUInt32();
            this.unknown12 = r.ReadUInt32();
            this.mtDoorList = new MTDoorList(OnResourceChanged, s);
            this.unknown13 = r.ReadByte();
            this.diagonalIndex = r.ReadUInt32();
            this.hash = r.ReadUInt32();
            this.roomCategoryFlags = (RoomCategory)r.ReadUInt32();
            this.functionCategoryFlags = (FunctionCategory)r.ReadUInt32();
            this.functionSubCategoryFlags = (FunctionSubCategory)r.ReadUInt64();
            this.roomSubCategoryFlags = (RoomSubCategory)r.ReadUInt64();
            this.buildCategoryFlags = (BuildCategory)r.ReadUInt32();
            this.sinkDDSIndex = r.ReadUInt32();
            if (this.version >= 0x00000017)
            {
                this.unknown16 = r.ReadUInt32();
                this.unknown17 = r.ReadUInt32();
                this.unknown18 = r.ReadUInt32();
            }
            this.slotPlacementFlags = (SlotPlacement)r.ReadUInt32();
            this.materialGrouping1 = r2.ReadString();
            this.materialGrouping2 = r2.ReadString();
            this.moodletGiven = (Moodlet)r.ReadUInt32();
            this.moodletScore = r.ReadInt32();
            this.unknown21 = r.ReadUInt32();
            for (int i = 0; i < topicRatings.Length; i++)
                topicRatings[i] = new TopicRating(requestedApiVersion, OnResourceChanged, s);
            this.fallbackIndex = r.ReadUInt32();

            list = new TGIBlockList(OnResourceChanged, s, tgiPosn, tgiSize);

            if (checking) if (this.GetType().Equals(typeof(ObjectCatalogResource)) && s.Position != s.Length)
                    throw new InvalidDataException(String.Format("Data stream length 0x{0:X8} is {1:X8} bytes longer than expected at {2:X8}",
                        s.Length, s.Length - s.Position, s.Position));
        }

        protected override Stream UnParse()
        {
            Stream s = base.UnParse();
            BinaryWriter w = new BinaryWriter(s);

            if (materialList == null) materialList = new MaterialList(OnResourceChanged);
            materialList.UnParse(s);
            if (this.version >= 0x00000016) Write7BitStr(s, unknown1, System.Text.Encoding.BigEndianUnicode);
            if (common == null) common = new Common(requestedApiVersion, OnResourceChanged);
            common.UnParse(s);
            w.Write(unknown2);
            w.Write(unknown3);
            w.Write((uint)fireType);
            w.Write(isStealable);
            w.Write(isReposessable);
            w.Write(inWorldEditable);
            w.Write(objkIndex);
            w.Write((uint)unknown8);
            w.Write((uint)unknown9);
            w.Write((uint)unknown10);
            w.Write((uint)unknown11);
            w.Write(unknown12);
            if (mtDoorList == null) mtDoorList = new MTDoorList(OnResourceChanged);
            mtDoorList.UnParse(s);
            w.Write(unknown13);
            w.Write(diagonalIndex);
            w.Write(hash);
            w.Write((uint)roomCategoryFlags);
            w.Write((uint)functionCategoryFlags);
            w.Write((ulong)functionSubCategoryFlags);
            w.Write((ulong)roomSubCategoryFlags);
            w.Write((uint)buildCategoryFlags);
            w.Write(sinkDDSIndex);
            if (this.version >= 0x00000017)
            {
                w.Write(unknown16);
                w.Write(unknown17);
                w.Write(unknown18);
            }
            w.Write((uint)slotPlacementFlags);
            Write7BitStr(s, materialGrouping1, System.Text.Encoding.BigEndianUnicode);
            Write7BitStr(s, materialGrouping2, System.Text.Encoding.BigEndianUnicode);
            w.Write((uint)moodletGiven);
            w.Write(moodletScore);
            w.Write(unknown21);
            for (int i = 0; i < topicRatings.Length; i++)
            {
                if (topicRatings[i] == null) topicRatings[i] = new TopicRating(0, OnResourceChanged);
                topicRatings[i].UnParse(s);
            }
            w.Write(fallbackIndex);

            base.UnParse(s);

            w.Flush();

            return s;
        }
        #endregion

        #region AApiVersionedFields
        /// <summary>
        /// The list of available field names on this API object
        /// </summary>
        public override List<string> ContentFields
        {
            get
            {
                List<string> res = base.ContentFields;
                if (this.version < 0x00000017)
                {
                    res.Remove("Unknown16");
                    res.Remove("Unknown17");
                    res.Remove("Unknown18");
                    if (this.version < 0x00000016)
                        res.Remove("Unknown1");
                }
                return res;
            }
        }
        #endregion

        #region Sub-classes
        public enum Fire : uint
        {
            DoesNotBurn = 0,
            Chars,
            AshPile
        }

        [Flags]
        public enum Misc8 : uint
        {
            KeepBuying = 0x00000002,
            FadeOutFromBack = 0x00000040,
            HangFromCeiling = 0x00000400,
            ColumnarSupport = 0x00004000,
            CanBeSteppedOver = 0x00010000,
        }

        [Flags]
        public enum Misc9 : uint
        {
            RequiresWallBehind = 0x00000002,
            NoWallToRight = 0x00000040,
            NoWallToFront = 0x00000200,
            Awning0A = 0x00000400,
            Awning0B = 0x00000800,
            Awning0E = 0x00004000,
            Awning0F = 0x00008000,
            CenterOnDiagonalWall = 0x00010000,
        }

        [Flags]
        public enum Misc10 : uint
        {
            CannotBuyFromCatalog = 0x00000010,
        }

        public enum Misc11 : uint
        {
            Windows1Tile = 0x00000001,
            Windows2Tile = 0x00000002,
            Windows3Tile = 0x00000003,
        }

        [Flags]
        public enum RoomCategory : uint
        {
            Unused1 = 0x00000001,
            Living = 0x00000002,
            Dining = 0x00000004,
            Kitchen = 0x00000008,

            Nursery = 0x00000010,
            Bathroom = 0x00000020,
            Bedroom = 0x00000040,
            Study = 0x00000080,

            Outside = 0x00000100,
            Community = 0x00000200,
            Residential = 0x00000400,
            Pool = 0x00000800,

            Default = 0x80000000,

            //All         = 0xFFFFFFFF,
        }

        [Flags]
        public enum RoomSubCategory : ulong
        {
            //Low DWORD
            Unused1 = 0x00000001,
            Dishwashers = 0x00000002,
            SmallApps = 0x00000004,
            Fridges = 0x00000008,

            Trash = 0x00000010,
            Alarms = 0x00000020,
            Phones = 0x00000040,
            TVs = 0x00000080,

            SmokeAlarm = 0x00000100,
            Unused10 = 0x00000200,
            Audio = 0x00000400,
            Computers = 0x00000800,

            HobbiesSkills = 0x00001000,
            IndoorActivities = 0x00002000,
            LivingChairs = 0x00004000,
            OfficeChairs = 0x00008000,

            Stoves = 0x00010000,
            EatingOut = 0x00020000,
            OutdoorActivities = 0x00040000,
            CeilingLights = 0x00080000,

            FloorLamps = 0x00100000,
            TableLamps = 0x00200000,
            WallLamps = 0x00400000,
            OutdoorLights = 0x00800000,

            Showers = 0x01000000,
            Sinks = 0x02000000,
            Toilets = 0x04000000,
            Tubs = 0x08000000,

            Accents = 0x10000000,
            LawnDecor = 0x20000000,
            WallArtAdult = 0x40000000,
            Plants = 0x80000000,

            //High DWORD
            Mirrors = 0x0000000100000000,
            VideoGames = 0x0000000200000000,
            WallArtKids = 0x0000000400000000,
            Bookshelves = 0x0000000800000000,

            Cabinets = 0x0000001000000000,
            Dressers = 0x0000002000000000,
            DiningChairs = 0x0000004000000000,
            Sofas = 0x0000008000000000,

            OutdoorSeating = 0x0000010000000000,
            RoofDecorations = 0x0000020000000000,
            Beds = 0x0000040000000000,
            BarStools = 0x0000080000000000,

            CoffeeTables = 0x0000100000000000,
            Counters = 0x0000200000000000,
            Desks = 0x0000400000000000,
            EndTables = 0x0000800000000000,

            DiningTables = 0x0001000000000000,
            Furniture = 0x0002000000000000,
            Toys = 0x0004000000000000,
            Transport = 0x0008000000000000,

            Bars = 0x0010000000000000,
            Clocks = 0x0020000000000000,
            WindowDecor = 0x0040000000000000,
            KidsDecor = 0x0080000000000000,

            MiscDecor = 0x0100000000000000,
            Rugs = 0x0200000000000000,
            Laundry = 0x0400000000000000,
            Unused60 = 0x0800000000000000,

            Unused61 = 0x1000000000000000,
            Unused62 = 0x2000000000000000,
            Unused63 = 0x4000000000000000,
            Default = 0x8000000000000000,
        }

        [Flags]
        public enum FunctionCategory : uint
        {
            Unused1 = 0x00000001,
            Appliances = 0x00000002,
            Electronics = 0x00000004,
            Entertainment = 0x00000008,

            Unused5 = 0x00000010,
            Lighting = 0x00000020,
            Plumbing = 0x00000040,
            Decor = 0x00000080,

            Kids = 0x00000100,
            Storage = 0x00000200,
            Unused11 = 0x00000400,
            Comfort = 0x00000800,

            Surfaces = 0x00001000,
            Vehicles = 0x00002000,
            Unused15 = 0x00004000,
            Unused16 = 0x00008000,

            //
            //
            //
            //

            //
            //
            //
            //

            //
            //
            //
            //

            Unused29 = 0x10000000,
            Unused30 = 0x20000000,
            Debug = 0x40000000,
            Default = 0x80000000,
        }

        [Flags]
        public enum FunctionSubCategory : ulong
        {
            //Low DWORD
            Unused1 = 0x00000001,
            AppliancesMisc = 0x00000002,
            AppliancesSmall = 0x00000004,
            AppliancesLarge = 0x00000008,

            DebugTombObjects = 0x00000010,
            DebugFishSpawners = 0x00000020,
            DebugPlantSeedSpawners = 0x00000040,
            ElectronicsTVs = 0x00000080,

            ElectronicsMisc = 0x00000100,
            DebugRockGemMetalSpawners = 0x00000200,
            ElectronicsAudio = 0x00000400,
            ElectronicsComputers = 0x00000800,

            EntertainmentHobbiesSkills = 0x00001000,
            EntertainmentSports = 0x00002000,
            ComfortLivingChairs = 0x00004000,
            ComfortDeskChairs = 0x00008000,

            DebugInsectSpawners = 0x00010000,
            EntertainmentParties = 0x00020000,
            EntertainmentMisc = 0x00040000,
            LightingCeiling = 0x00080000,

            LightingFloor = 0x00100000,
            LightingTable = 0x00200000,
            LightingWall = 0x00400000,
            LightingOutdoor = 0x00800000,

            ComfortLoungeChairs = 0x01000000,
            PlumbingSinks = 0x02000000,
            PlumbingToilets = 0x04000000,
            PlumbingShowersAndTubs = 0x08000000,

            DecorMisc = 0x10000000,
            DecorSculptures = 0x20000000,
            DecorWallArt = 0x40000000,
            DecorPlants = 0x80000000,

            //High DWORD
            DecorMirrors = 0x0000000100000000,
            Unused34 = 0x0000000200000000,
            DebugMisc = 0x0000000400000000,
            StorageBookshelves = 0x0000000800000000,

            SurfacesDisplays = 0x0000001000000000,
            StorageDressers = 0x0000002000000000,
            ComfortDiningChairs = 0x0000004000000000,
            ComfortSofas = 0x0000008000000000,

            ComfortMisc = 0x0000010000000000,
            DecorRoof = 0x0000020000000000,
            ComfortBeds = 0x0000040000000000,
            Unused44 = 0x0000080000000000,

            SurfacesCoffeeTables = 0x0000100000000000,
            SurfacesCounters = 0x0000200000000000,
            SurfacesDesks = 0x0000400000000000,
            SurfacesEndTables = 0x0000800000000000,

            SurfacesDiningTables = 0x0001000000000000,
            KidsFurniture = 0x0002000000000000,
            KidsToys = 0x0004000000000000,
            VehiclesCars = 0x0008000000000000,

            VehiclesBicycles = 0x0010000000000000,
            SurfacesCabinets = 0x0020000000000000,
            DecorWindowDecor = 0x0040000000000000,
            KidsMisc = 0x0080000000000000,

            MiscLighting = 0x0100000000000000,
            MiscPlumbing = 0x0200000000000000,
            MiscStorage = 0x0400000000000000,
            MiscSurfaces = 0x0800000000000000,

            MiscVehicles = 0x1000000000000000,
            DecorRugs = 0x2000000000000000,
            Unused63 = 0x4000000000000000,
            Default = 0x8000000000000000,
        }

        [Flags]
        public enum BuildCategory : uint
        {
            Unused1 = 0x00000001,
            Door = 0x00000002,
            Window = 0x00000004,
            Gate = 0x00000008,

            Column = 0x00000010,
            RabbitHole = 0x00000020,
            Fireplace = 0x00000040,
            Chimney = 0x00000080,

            Arch = 0x00000100,
            Flower = 0x00000200,
            Shrub = 0x00000400,
            Tree = 0x00000800,

            Rug = 0x00001000,
            Rock = 0x00002000,
            Unused15 = 0x00004000,
            Landmark = 0x00008000,

            //
            //
            //
            //

            //
            //
            //
            //

            //
            //
            //
            //


            //
            //
            //
            Default = 0x80000000,
        }

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

        public enum Moodlet : uint
        {
            Unused00 = 0x00000000,
            Seating,
            Sleeping,
            Music,
        }

        public enum TopicCategory : uint
        {
            EndOfTopics = 0x00000000,
            Environment,
            Hunger,
            Bladder,
            Energy,
            StressRelief,
            Fun,
            Hygiene,
            Logic,
            Charisma,
            Cooking,
            Athletic,
            Painting,
            Guitar,
            Handiness,
            GroupActivity,
            Upgradable,
            LearnCookingFaster,
            ChildOnly,
            unused13,
            Gardening,
            Fishing,
            SelfCleaning,
            NeverBreaks,
            Portable,
            Speed,
            Inventing,
            Sculpting,
        }

        public class TopicRating : AHandlerElement, IEquatable<TopicRating>
        {
            #region Attributes
            TopicCategory topic;
            int rating;
            #endregion

            #region Constructors
            public TopicRating(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public TopicRating(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public TopicRating(int APIversion, EventHandler handler, TopicRating basis)
                : this(APIversion, handler, basis.topic, basis.rating) { }
            public TopicRating(int APIversion, EventHandler handler, TopicCategory topic, int rating)
                : base(APIversion, handler)
            {
                this.topic = topic;
                this.rating = rating;
            }
            #endregion

            #region Data I/O
            private void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.topic = (TopicCategory)r.ReadUInt32();
                this.rating = r.ReadInt32();
            }
            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write((uint)topic);
                w.Write(rating);
            }
            #endregion

            #region IEquatable<MTDoorEntry> Members

            public bool Equals(TopicRating other)
            {
                return (topic == other.topic && rating == other.rating);
            }

            #endregion

            #region AHandlerElement
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            public override AHandlerElement Clone(EventHandler handler) { return new TopicRating(requestedApiVersion, handler, this); }
            #endregion

            #region Content Fields
            public TopicCategory Topic { get { return topic; } set { if (topic != value) { topic = value; OnElementChanged(); } } }
            public int Rating { get { return rating; } set { if (rating != value) { rating = value; OnElementChanged(); } } }

            public String Value
            {
                get
                {
                    string s = "";
                    s += topic;
                    if (rating != 0)
                    {
                        if (rating == 11) s = "+ " + s;
                        else
                            s += ": " + rating;
                    }
                    return s;
                }
            }
            #endregion
        }

        public class MTDoor : AHandlerElement, IEquatable<MTDoor>
        {
            #region Attributes
            float[] unknown1 = new float[4];
            uint unknown2;
            uint wallMaskIndex;
            #endregion

            #region Constructors
            public MTDoor(int APIversion, EventHandler handler) : base(APIversion, handler) { }
            public MTDoor(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            public MTDoor(int APIversion, EventHandler handler, MTDoor basis)
                : this(APIversion, handler, basis.unknown1, basis.unknown2, basis.wallMaskIndex) { }
            public MTDoor(int APIversion, EventHandler handler, float[] unknown1, uint unknownX, uint wallMaskIndex)
                : base(APIversion, handler)
            {
                if (unknown1.Length != this.unknown1.Length) throw new ArgumentLengthException("unknown1", this.unknown1.Length);
                this.unknown1 = (float[])unknown1.Clone();
                this.unknown2 = unknownX;
                this.wallMaskIndex = wallMaskIndex;
            }
            #endregion

            #region Data I/O
            private void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.unknown1 = new float[4]; for (int i = 0; i < unknown1.Length; i++) unknown1[i] = r.ReadSingle();
                this.unknown2 = r.ReadUInt32();
                this.wallMaskIndex = r.ReadUInt32();
            }
            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                foreach (float f in unknown1) w.Write(f);
                w.Write(unknown2);
                w.Write(wallMaskIndex);
            }
            #endregion

            #region IEquatable<MTDoorEntry> Members

            public bool Equals(MTDoor other)
            {
                for (int i = 0; i < unknown1.Length; i++) if (unknown1[i] != other.unknown1[i]) return false;
                return (unknown2 == other.unknown2 && wallMaskIndex == other.wallMaskIndex);
            }

            #endregion

            #region AHandlerElement
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

            public override AHandlerElement Clone(EventHandler handler) { return new MTDoor(requestedApiVersion, handler, this); }
            #endregion

            #region Content Fields
            public float[] Unknown1
            {
                get { return (float[])unknown1.Clone(); }
                set
                {
                    if (value.Length != unknown1.Length) throw new ArgumentLengthException("unknown1", this.unknown1.Length);
                    if (!ArrayCompare(unknown1, value)) { unknown1 = (float[])value.Clone(); OnElementChanged(); }
                }
            }
            public uint Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnElementChanged(); } } }
            public uint WallMaskIndex { get { return wallMaskIndex; } set { if (wallMaskIndex != value) { wallMaskIndex = value; OnElementChanged(); } } }

            public String Value
            {
                get
                {
                    string s = "";
                    foreach (string f in this.ContentFields)
                    {
                        if (f.Equals("Value")) continue;
                        if (f.Equals("Unknown1")) { for (int i = 0; i < unknown1.Length; i++)s += string.Format("{0}[{1}]: {2}\n", f, "" + i, "" + unknown1[i]); }
                        else s += String.Format("{0}: {1}\n", f, "" + this[f]);
                    }
                    return s;
                }
            }
            #endregion
        }

        public class MTDoorList : AResource.DependentList<MTDoor>
        {
            #region Constructors
            public MTDoorList(EventHandler handler) : base(handler, 256) { }
            public MTDoorList(EventHandler handler, IList<MTDoor> mtDoorList) : base(handler, 256, mtDoorList) { }
            public MTDoorList(EventHandler handler, Stream s) : base(handler, 256, s) { }
            #endregion

            #region Data I/O
            protected override uint ReadCount(Stream s) { return (new BinaryReader(s)).ReadByte(); }
            protected override MTDoor CreateElement(Stream s) { return new MTDoor(0, elementHandler, s); }
            protected override void WriteCount(Stream s, uint count) { (new BinaryWriter(s)).Write((byte)count); }
            protected override void WriteElement(Stream s, MTDoor element) { element.UnParse(s); }
            #endregion

            public override void Add() { this.Add(new MTDoor(0, null)); }

            #region Content Fields
            public String Value { get { string s = ""; for (int i = 0; i < Count; i++) s += string.Format("\n--{0}--\n", i) + this[i].Value; return s; } }
            #endregion
        }
        #endregion

        #region Content Fields
        [ElementPriority(2)]
        public MaterialList Materials { get { return materialList; } set { if (materialList != value) { materialList = value == null ? null : new MaterialList(OnResourceChanged, value); } OnResourceChanged(this, new EventArgs()); } }
        [ElementPriority(3)]
        public string Unknown1
        {
            get { if (version < 0x00000016) throw new InvalidOperationException(); return unknown1; }
            set { if (version < 0x00000016) throw new InvalidOperationException(); if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, new EventArgs()); } }
        }
        [ElementPriority(12)]
        public uint Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(13)]
        public byte Unknown3 { get { return unknown3; } set { if (unknown3 != value) { unknown3 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(14)]
        public Fire FireType { get { return fireType; } set { if (fireType != value) { fireType = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(15)]
        public bool IsStealable { get { return isStealable != 0; } set { if (IsStealable != value) { isStealable = (byte)(value ? 1 : 0); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(16)]
        public bool IsReposessable { get { return isReposessable != 0; } set { if (IsReposessable != value) { isReposessable = (byte)(value ? 1 : 0); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(17)]
        public bool InWorldEditable { get { return inWorldEditable != 0; } set { if (InWorldEditable != value) { inWorldEditable = (uint)(value ? 1 : 0); OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(18), TGIBlockListContentField("TGIBlocks")]
        public uint OBJKIndex { get { return objkIndex; } set { if (objkIndex != value) { objkIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(19)]
        public Misc8 Unknown8 { get { return unknown8; } set { if (unknown8 != value) { unknown8 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(20)]
        public Misc9 Unknown9 { get { return unknown9; } set { if (unknown9 != value) { unknown9 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(21)]
        public Misc10 Unknown10 { get { return unknown10; } set { if (unknown10 != value) { unknown10 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(22)]
        public Misc11 Unknown11 { get { return unknown11; } set { if (unknown11 != value) { unknown11 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(23)]
        public uint Unknown12 { get { return unknown12; } set { if (unknown12 != value) { unknown12 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(24)]
        public MTDoorList MTDoors { get { return mtDoorList; } set { if (mtDoorList != value) { mtDoorList = value == null ? null : new MTDoorList(OnResourceChanged, value); } OnResourceChanged(this, new EventArgs()); } }
        [ElementPriority(25)]
        public byte Unknown13 { get { return unknown13; } set { if (unknown13 != value) { unknown13 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(28), TGIBlockListContentField("TGIBlocks")]
        public uint DiagonalIndex { get { return diagonalIndex; } set { if (diagonalIndex != value) { diagonalIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(29)]
        public uint Hash { get { return hash; } set { if (hash != value) { hash = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(30)]
        public RoomCategory RoomCategoryFlags { get { return roomCategoryFlags; } set { if (roomCategoryFlags != value) { roomCategoryFlags = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(31)]
        public FunctionCategory FunctionCategoryFlags { get { return functionCategoryFlags; } set { if (functionCategoryFlags != value) { functionCategoryFlags = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(32)]
        public FunctionSubCategory FunctionSubCategoryFlags { get { return functionSubCategoryFlags; } set { if (functionSubCategoryFlags != value) { functionSubCategoryFlags = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(33)]
        public RoomSubCategory RoomSubCategoryFlags { get { return roomSubCategoryFlags; } set { if (roomSubCategoryFlags != value) { roomSubCategoryFlags = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(34)]
        public BuildCategory BuildCategoryFlags { get { return buildCategoryFlags; } set { if (buildCategoryFlags != value) { buildCategoryFlags = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(35), TGIBlockListContentField("TGIBlocks")]
        public uint SinkDDSIndex { get { return sinkDDSIndex; } set { if (sinkDDSIndex != value) { sinkDDSIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(36)]
        public uint Unknown16
        {
            get { if (version < 0x00000017) throw new InvalidOperationException(); return unknown16; }
            set { if (version < 0x00000017) throw new InvalidOperationException(); if (unknown16 != value) { unknown16 = value; OnResourceChanged(this, new EventArgs()); } }
        }
        [ElementPriority(37)]
        public uint Unknown17
        {
            get { if (version < 0x00000017) throw new InvalidOperationException(); return unknown17; }
            set { if (version < 0x00000017) throw new InvalidOperationException(); if (unknown17 != value) { unknown17 = value; OnResourceChanged(this, new EventArgs()); } }
        }
        [ElementPriority(38)]
        public uint Unknown18
        {
            get { if (version < 0x00000017) throw new InvalidOperationException(); return unknown18; }
            set { if (version < 0x00000017) throw new InvalidOperationException(); if (unknown18 != value) { unknown18 = value; OnResourceChanged(this, new EventArgs()); } }
        }
        [ElementPriority(39)]
        public SlotPlacement SlotPlacementFlags { get { return slotPlacementFlags; } set { if (slotPlacementFlags != value) { slotPlacementFlags = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(40)]
        public string MaterialGrouping1 { get { return materialGrouping1; } set { if (materialGrouping1 != value) { materialGrouping1 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(41)]
        public string MaterialGrouping2 { get { return materialGrouping2; } set { if (materialGrouping2 != value) { materialGrouping2 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(42)]
        public Moodlet MoodletGiven { get { return moodletGiven; } set { if (moodletGiven != value) { moodletGiven = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(43)]
        public int MoodletScore { get { return moodletScore; } set { if (moodletScore != value) { moodletScore = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(44)]
        public uint Unknown21 { get { return unknown21; } set { if (unknown21 != value) { unknown21 = value; OnResourceChanged(this, new EventArgs()); } } }
        [ElementPriority(45)]
        public TopicRating[] TopicRatings
        {
            get { return topicRatings; }
            set
            {
                if (value.Length != this.topicRatings.Length) throw new ArgumentLengthException("TopicRatings", this.topicRatings.Length);
                if (!ArrayCompare(topicRatings, value)) { topicRatings = value == null ? null : (TopicRating[])value.Clone(); OnResourceChanged(this, new EventArgs()); }
            }
        }
        [ElementPriority(46), TGIBlockListContentField("TGIBlocks")]
        public uint FallbackIndex { get { return fallbackIndex; } set { if (fallbackIndex != value) { fallbackIndex = value; OnResourceChanged(this, new EventArgs()); } } }
        #endregion
    }
}
