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
using System.Text;
using System.IO;
using s3pi.Interfaces;

namespace s3pi.Package
{
    /// <summary>
    /// Implementation of a package
    /// </summary>
    public class Package : APackage
    {
        static bool checking = Settings.Settings.Checking;

        const Int32 recommendedApiVersion = 1;

        #region AApiVersionedFields
        /// <summary>
        /// The version of the API in use
        /// </summary>
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        //No ContentFields override as we don't want to make anything more public than APackage provides
        #endregion

        #region APackage
        #region Whole package
        /// <summary>
        /// Tell the package to save itself to wherever it believes it came from
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override void SavePackage()
        {
            if (checking) if (packageStream == null)
                    throw new InvalidOperationException("Package has no stream to save to");
            if (!packageStream.CanWrite)
                throw new InvalidOperationException("Package is read-only");

            string tmpfile = Path.GetTempFileName();
            SaveAs(tmpfile);


            // Lock the header while we save to prevent other processes saving concurrently
            FileStream fs = packageStream as FileStream; // if it's not a file, it's probably safe not to lock it...
            if (fs != null) fs.Lock(0, header.Length);

            packageStream.Position = 0;
            BinaryReader r = new BinaryReader(new FileStream(tmpfile, FileMode.Open));
            BinaryWriter w = new BinaryWriter(packageStream);
            w.Write(r.ReadBytes((int)r.BaseStream.Length));
            packageStream.SetLength(packageStream.Position);
            w.Flush();

            if (fs != null) fs.Unlock(0, header.Length);


            packageStream.Position = 0;
            header = (new BinaryReader(packageStream)).ReadBytes(header.Length);
            headerReader = new BinaryReader(new MemoryStream(header));
            if (checking) CheckHeader();

            bool wasnull = index == null;
            index = null;
            if (!wasnull) OnResourceIndexInvalidated(this, new EventArgs());
        }

        /// <summary>
        /// Save the package to a given stream
        /// </summary>
        /// <param name="s">Stream to save to</param>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override void SaveAs(Stream s)
        {
            BinaryWriter w = new BinaryWriter(s);
            w.Write(header);

            PackageIndex newIndex = new PackageIndex((uint)((Indextype & 4) > 0 ? 4 : 0));
            foreach (IResourceIndexEntry ie in this.Index)
            {
                if (ie.IsDeleted) continue;

                ResourceIndexEntry newIE = (ie as ResourceIndexEntry).Clone();
                ((List<IResourceIndexEntry>)newIndex).Add(newIE);
                byte[] value = packedChunk(ie as ResourceIndexEntry);

                newIE.Chunkoffset = (uint)s.Position;
                w.Write(value);
                w.Flush();

                if (value.Length < newIE.Memsize)
                {
                    newIE.Compressed = 0xffff;
                    newIE.Filesize = (uint)value.Length;
                }
                else
                {
                    newIE.Compressed = 0x0000;
                    newIE.Filesize = newIE.Memsize;
                }
            }
            long indexpos = s.Position;
            newIndex.Save(w);
            setIndexcount(w, newIndex.Count);
            setIndexsize(w, newIndex.Size);
            setIndexposition(w, (int)indexpos);
            s.Flush();
        }

        /// <summary>
        /// Save the package to a given file
        /// </summary>
        /// <param name="path">File to save to - will be overwritten or created</param>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override void SaveAs(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Create);
            SaveAs(fs);
            fs.Close();
        }

        // Static so cannot be defined on the interface

        /// <summary>
        /// Initialise a new, empty package and return the IPackage reference
        /// </summary>
        /// <param name="APIversion">(unused)</param>
        /// <returns>IPackage reference to an empty package</returns>
        public static new IPackage NewPackage(int APIversion)
        {
            return new Package(APIversion);
        }

        /// <summary>
        /// Open an existing package by filename, read only
        /// </summary>
        /// <param name="APIversion">(unused)</param>
        /// <param name="packagePath">Fully qualified filename of the package</param>
        /// <returns>IPackage reference to an existing package on disk</returns>
        /// <exception cref="InvalidDataException">Thrown if the package header is malformed.</exception>
        public static new IPackage OpenPackage(int APIversion, string packagePath) { return OpenPackage(APIversion, packagePath, false); }
        /// <summary>
        /// Open an existing package by filename, optionally readwrite
        /// </summary>
        /// <param name="APIversion">(unused)</param>
        /// <param name="PackagePath">Fully qualified filename of the package</param>
        /// <param name="readwrite">True to indicate read/write access required</param>
        /// <returns>IPackage reference to an existing package on disk</returns>
        /// <exception cref="InvalidDataException">Thrown if the package header is malformed.</exception>
        public static new IPackage OpenPackage(int APIversion, string PackagePath, bool readwrite)
        {
            return new Package(APIversion, new FileStream(PackagePath, FileMode.Open, readwrite ? FileAccess.ReadWrite : FileAccess.Read, FileShare.ReadWrite));
        }

        /// <summary>
        /// Releases any internal references associated with the given package
        /// </summary>
        /// <param name="APIversion">(unused)</param>
        /// <param name="pkg">IPackage reference to close</param>
        public static new void ClosePackage(int APIversion, IPackage pkg)
        {
            Package p = pkg as Package;
            if (p == null) return;
            if (p.packageStream != null) { try { p.packageStream.Close(); } catch { } p.packageStream = null; }
            p.header = null;
            p.headerReader = null;
            p.index = null;
        }
        #endregion

        #region Package header
        /// <summary>
        /// Package header: "DBPF" bytes
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override byte[] Magic { get { headerReader.BaseStream.Position = 0; return headerReader.ReadBytes(4); } }
        /// <summary>
        /// Package header: 0x00000002
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override int Major { get { headerReader.BaseStream.Position = 4; return headerReader.ReadInt32(); } }
        /// <summary>
        /// Package header: 0x00000000
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override int Minor { get { headerReader.BaseStream.Position = 8; return headerReader.ReadInt32(); } }
        /// <summary>
        /// Package header: unused
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override byte[] Unknown1 { get { headerReader.BaseStream.Position = 12; return headerReader.ReadBytes(24); } }
        /// <summary>
        /// Package header: number of entries in the package index
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override int Indexcount { get { headerReader.BaseStream.Position = 36; return headerReader.ReadInt32(); } }
        /// <summary>
        /// Package header: unused
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override byte[] Unknown2 { get { headerReader.BaseStream.Position = 40; return headerReader.ReadBytes(4); } }
        /// <summary>
        /// Package header: index size on disk in bytes
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override int Indexsize { get { headerReader.BaseStream.Position = 44; return headerReader.ReadInt32(); } }
        /// <summary>
        /// Package header: unused
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override byte[] Unknown3 { get { headerReader.BaseStream.Position = 48; return headerReader.ReadBytes(12); } }
        /// <summary>
        /// Package header: always 3?
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override int Indexversion { get { headerReader.BaseStream.Position = 60; return headerReader.ReadInt32(); } }
        /// <summary>
        /// Package header: index position in file
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override int Indexposition { get { headerReader.BaseStream.Position = 64; return headerReader.ReadInt32(); } }
        /// <summary>
        /// Package header: unused
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override byte[] Unknown4 { get { headerReader.BaseStream.Position = 68; return headerReader.ReadBytes(28); } }

        /// <summary>
        /// A MemoryStream covering the package header bytes
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override Stream HeaderStream { get { return headerReader.BaseStream; } }
        #endregion

        #region Package index
        /// <summary>
        /// Package index: the index format in use
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override UInt32 Indextype { get { return (GetResourceList as PackageIndex).Indextype; } }

        /// <summary>
        /// Package index: the index
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override IList<IResourceIndexEntry> GetResourceList { get { return Index; } }

        class FlagMatch
        {
            Boolset flags;
            IResourceIndexEntry values;
            public FlagMatch(Boolset flags, IResourceIndexEntry values)
            {
                if (flags.Length > values.ContentFields.Count) throw new ArgumentLengthException("flags", values.ContentFields.Count);
                this.flags = flags;
                this.values = values;
            }
            public bool Match(IResourceIndexEntry rie)
            {
                if (flags == 0) return true;

                for (int i = 0; i < flags.Length; i++)
                {
                    if (!flags[i]) continue;
                    string f = values.ContentFields[i];
                    if (!values[f].Equals(rie[f])) return false;
                }
                return true;
            }
        }

        class NameMatch
        {
            string[] names;
            TypedValue[] values;
            public NameMatch(string[] names, TypedValue[] values)
            {
                foreach (string n in names) if (!GetContentFields(0, typeof(ResourceIndexEntry)).Contains(n)) throw new ArgumentOutOfRangeException("names", String.Format("'{0}' is an invalid IResourceIndexEntry ContentField", n));
                this.names = names; this.values = values;
            }
            public bool Match(IResourceIndexEntry rie)
            {
                for (int i = 0; i < names.Length; i++) if (!values[i].Equals(rie[names[i]])) return false;
                return true;
            }
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by <paramref name="flags"/> and <paramref name="values"/>,
        /// and returns the first occurrence within the package index./>.
        /// </summary>
        /// <param name="flags">True bits enable matching against numerically equivalent <paramref name="values"/> entry</param>
        /// <param name="values">Fields to compare against</param>
        /// <returns>The first match, if any; otherwise null.</returns>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override IResourceIndexEntry Find(uint flags, IResourceIndexEntry values) { return Index.Find((new FlagMatch(flags, values)).Match); }

        /// <summary>
        /// Searches for an element that matches the conditions defined by <paramref name="names"/> and <paramref name="values"/>,
        /// and returns the first occurrence within the package index./>.
        /// </summary>
        /// <param name="names">Names of fields to compare</param>
        /// <param name="values">Fields to compare against</param>
        /// <returns>The first match, if any; otherwise null.</returns>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override IResourceIndexEntry Find(string[] names, TypedValue[] values) { return Index.Find((new NameMatch(names, values)).Match); }

        /// <summary>
        /// Searches the entire <see cref="IPackage"/>
        /// for the first <see cref="IResourceIndexEntry"/> that matches the conditions defined by
        /// the <c>Predicate&lt;IResourceIndexEntry&gt;</c> <paramref name="Match"/>.
        /// </summary>
        /// <param name="Match"><c>Predicate&lt;IResourceIndexEntry&gt;</c> defining matching conditions.</param>
        /// <returns>The first matching <see cref="IResourceIndexEntry"/>, if any; otherwise null.</returns>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override IResourceIndexEntry Find(Predicate<IResourceIndexEntry> Match) { return Index.Find(x => !x.IsDeleted && Match(x)); }

        /// <summary>
        /// Searches the entire <see cref="IPackage"/>
        /// for all <see cref="IResourceIndexEntry"/>s that matches the conditions defined by
        /// <paramref name="flags"/> and <paramref name="values"/>.
        /// </summary>
        /// <param name="flags">True bits enable matching against numerically equivalent <paramref name="values"/> entry.</param>
        /// <param name="values">Field values to compare against.</param>
        /// <returns>An <c>IList&lt;IResourceIndexEntry&gt;</c> of zero or more matches.</returns>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override IList<IResourceIndexEntry> FindAll(uint flags, IResourceIndexEntry values) { return Index.FindAll((new FlagMatch(flags, values)).Match); }

        /// <summary>
        /// Searches the entire <see cref="IPackage"/>
        /// for all <see cref="IResourceIndexEntry"/>s that matches the conditions defined by
        /// <paramref name="names"/> and <paramref name="values"/>.
        /// </summary>
        /// <param name="names">Names of <see cref="IResourceIndexEntry"/> fields to compare.</param>
        /// <param name="values">Field values to compare against.</param>
        /// <returns>An <c>IList&lt;IResourceIndexEntry&gt;</c> of zero or more matches.</returns>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override IList<IResourceIndexEntry> FindAll(string[] names, TypedValue[] values) { return Index.FindAll((new NameMatch(names, values)).Match); }

        /// <summary>
        /// Searches the entire <see cref="IPackage"/>
        /// for all <see cref="IResourceIndexEntry"/>s that matches the conditions defined by
        /// the <c>Predicate&lt;IResourceIndexEntry&gt;</c> <paramref name="Match"/>.
        /// </summary>
        /// <param name="Match"><c>Predicate&lt;IResourceIndexEntry&gt;</c> defining matching conditions.</param>
        /// <returns>Zero or more matches.</returns>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override IList<IResourceIndexEntry> FindAll(Predicate<IResourceIndexEntry> Match) { return Index.FindAll(x => !x.IsDeleted && Match(x)); }
        #endregion

        #region Package content
        /// <summary>
        /// Add a resource to the package
        /// </summary>
        /// <param name="rk">The resource key</param>
        /// <param name="stream">The stream that contains the resource data</param>
        /// <param name="rejectDups">If true, fail if the resource key already exists</param>
        /// <returns>Null if rejectDups and the resource key exists; else the new IResourceIndexEntry</returns>
        public override IResourceIndexEntry AddResource(IResourceKey rk, Stream stream, bool rejectDups)
        {
            if (rejectDups && Index[rk] != null && !Index[rk].IsDeleted) return null;
            IResourceIndexEntry newrc = Index.Add(rk);
            if (stream != null) (newrc as ResourceIndexEntry).ResourceStream = stream;

            return newrc;
        }
        /// <summary>
        /// Tell the package to replace the data for the resource indexed by <paramref name="rc"/>
        /// with the data from the resource <paramref name="res"/>
        /// </summary>
        /// <param name="rc">Target resource index</param>
        /// <param name="res">Source resource</param>
        public override void ReplaceResource(IResourceIndexEntry rc, IResource res) { (rc as ResourceIndexEntry).ResourceStream = res.Stream; }
        /// <summary>
        /// Tell the package to delete the resource indexed by <paramref name="rc"/>
        /// </summary>
        /// <param name="rc">Target resource index</param>
        public override void DeleteResource(IResourceIndexEntry rc)
        {
            if (!rc.IsDeleted)
                (rc as ResourceIndexEntry).Delete();
        }
        #endregion
        #endregion


        #region Package implementation
        Stream packageStream = null;

        private Package(int requestedVersion)
        {
            this.requestedApiVersion = requestedVersion;
            header = new byte[96];
            headerReader = new BinaryReader(new MemoryStream(header));

            BinaryWriter bw = new BinaryWriter(new MemoryStream(header));
            bw.Write(stringToBytes(magic));
            bw.Write(major);
            bw.Write(minor);
            setIndexsize(bw, (new PackageIndex()).Size);
            setIndexversion(bw);
            setIndexposition(bw, header.Length);
        }

        private Package(int requestedVersion, Stream s)
        {
            this.requestedApiVersion = requestedVersion;
            packageStream = s;
            s.Position = 0;
            header = (new BinaryReader(s)).ReadBytes(header.Length);
            headerReader = new BinaryReader(new MemoryStream(header));
            if (checking) CheckHeader();
        }

        private byte[] packedChunk(ResourceIndexEntry ie)
        {
            byte[] chunk = null;
            if (ie.IsDirty)
            {
                Stream res = GetResource(ie);
                BinaryReader r = new BinaryReader(res);

                res.Position = 0;
                chunk = r.ReadBytes((int)ie.Memsize);
                if (checking) if (chunk.Length != (int)ie.Memsize)
                        throw new OverflowException(String.Format("packedChunk, dirty resource - T: 0x{0:X}, G: 0x{1:X}, I: 0x{2:X}: Length expected: 0x{3:X}, read: 0x{4:X}",
                            ie.ResourceType, ie.ResourceGroup, ie.Instance, ie.Memsize, chunk.Length));

                byte[] comp = ie.Compressed != 0 ? Compression.CompressStream(chunk) : chunk;
                if (comp.Length < chunk.Length)
                    chunk = comp;
            }
            else
            {
                if (checking) if (packageStream == null)
                        throw new InvalidOperationException(String.Format("Clean resource with undefined \"current package\" - T: 0x{0:X}, G: 0x{1:X}, I: 0x{2:X}",
                            ie.ResourceType, ie.ResourceGroup, ie.Instance));
                packageStream.Position = ie.Chunkoffset;
                chunk = (new BinaryReader(packageStream)).ReadBytes((int)ie.Filesize);
                if (checking) if (chunk.Length != (int)ie.Filesize)
                        throw new OverflowException(String.Format("packedChunk, clean resource - T: 0x{0:X}, G: 0x{1:X}, I: 0x{2:X}: Length expected: 0x{3:X}, read: 0x{4:X}",
                            ie.ResourceType, ie.ResourceGroup, ie.Instance, ie.Filesize, chunk.Length));
            }
            return chunk;
        }
        #endregion

        #region Header implementation
        static byte[] stringToBytes(string s) { byte[] bytes = new byte[s.Length]; int i = 0; foreach (char c in s) bytes[i++] = (byte)c; return bytes; }
        static string bytesToString(byte[] bytes) { string s = ""; foreach (byte b in bytes) s += (char)b; return s; }

        const string magic = "DBPF";
        const int major = 2;
        const int minor = 0;

        byte[] header = new byte[96];
        BinaryReader headerReader = null;

        void setIndexcount(BinaryWriter w, int c) { w.BaseStream.Position = 36; w.Write(c); }
        void setIndexsize(BinaryWriter w, int c) { w.BaseStream.Position = 44; w.Write(c); }
        void setIndexversion(BinaryWriter w) { w.BaseStream.Position = 60; w.Write(3); }
        void setIndexposition(BinaryWriter w, int c) { w.BaseStream.Position = 64; w.Write(c); }

        void CheckHeader()
        {
            if (headerReader.BaseStream.Length != 96)
                throw new InvalidDataException("Hit unexpected end of file at " + headerReader.BaseStream.Position);

            if (bytesToString(Magic) != magic)
                throw new InvalidDataException("Expected magic tag '" + magic + "'.  Found '" + bytesToString(Magic) + "'.");

            if (Major != major)
                throw new InvalidDataException("Expected major version '" + major + "'.  Found '" + Major.ToString() + "'.");

            if (Minor != minor)
                throw new InvalidDataException("Expected major version '" + minor + "'.  Found '" + Minor.ToString() + "'.");
        }
        #endregion

        #region Index implementation
        PackageIndex index = null;
        private PackageIndex Index
        {
            get
            {
                if (index == null)
                {
                    index = new PackageIndex(packageStream, Indexposition, Indexsize, Indexcount);
                    OnResourceIndexInvalidated(this, new EventArgs());
                }
                return index;
            }
        }
        #endregion


        // Required by API, not user tools

        /// <summary>
        /// Used by WrapperDealer to get the data for a resource
        /// </summary>
        /// <param name="rc">IResourceIndexEntry of resource</param>
        /// <returns>The resource data (uncompressed, if necessary)</returns>
        public override Stream GetResource(IResourceIndexEntry rc)
        {
            ResourceIndexEntry rie = rc as ResourceIndexEntry;
            if (rie == null) return null;
            if (rie.ResourceStream != null) return rie.ResourceStream;

            if (rc.Chunkoffset == 0xffffffff) return null;
            packageStream.Position = rc.Chunkoffset;

            byte[] data = null;
            if (rc.Filesize == 1 && rc.Memsize == 0xFFFFFFFF) return null;//{ data = new byte[0]; }
            else if (rc.Filesize == rc.Memsize)
            {
                data = (new BinaryReader(packageStream)).ReadBytes((int)rc.Filesize);
            }
            else
            {
                data = Compression.UncompressStream(packageStream, (int)rc.Filesize, (int)rc.Memsize);
            }

            MemoryStream ms = new MemoryStream();
            ms.Write(data, 0, data.Length);
            ms.Position = 0;
            return ms;
        }

    }
}