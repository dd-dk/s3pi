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

namespace ScriptResource
{
    /// <summary>
    /// A resource wrapper that understands Encrypted Signed Assembly (0x073FAA07) resources
    /// </summary>
    public class ScriptResource : AResource
    {
        const Int32 recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        static bool checking = s3pi.Settings.Settings.Checking;

        #region Attributes
        byte unknown1 = 1;
        uint unknown2 = 0x2BC4F79F;
        byte[] md5sum = new byte[64];
        byte[] md5table = new byte[0];
        byte[] md5data = new byte[0];
        byte[] cleardata = new byte[0];
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of the resource
        /// </summary>
        /// <param name="APIversion">Requested API version</param>
        /// <param name="s">Data stream to use, or null to create from scratch</param>
        public ScriptResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); dirty = true; } stream.Position = 0; Parse(stream); }
        #endregion

        #region Data I/O
        void Parse(Stream s)
        {
            BinaryReader br = new BinaryReader(s);
            unknown1 = br.ReadByte();
            unknown2 = br.ReadUInt32();
            md5sum = br.ReadBytes(64);
            ushort count = br.ReadUInt16();
            md5table = br.ReadBytes(count * 8);
            md5data = br.ReadBytes(count * 512);
            cleardata = decrypt();
        }

        byte[] decrypt()
        {
            ulong seed = 0;
            for (int i = 0; i < md5table.Length; i += 8) seed += BitConverter.ToUInt64(md5table, i);
            seed = (ulong)(md5table.Length - 1) & seed;

            MemoryStream w = new MemoryStream();
            MemoryStream r = new MemoryStream(md5data);

            for (int i = 0; i < md5table.Length; i += 8)
            {
                byte[] buffer = new byte[512];

                if ((md5table[i] & 1) == 0)
                {
                    r.Read(buffer, 0, buffer.Length);

                    for (int j = 0; j < 512; j++)
                    {
                        byte value = buffer[j];
                        buffer[j] ^= md5table[seed];
                        seed = (ulong)((seed + value) % (ulong)md5table.Length);
                    }
                }

                w.Write(buffer, 0, buffer.Length);
            }

            return w.ToArray();
        }

        Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(unknown1);
            bw.Write(unknown2);
            md5table = new byte[(((cleardata.Length & 0x1ff) == 0 ? 0 : 1) + (cleardata.Length >> 9)) << 3];
            md5data = encrypt();
            bw.Write(md5sum);
            bw.Write((ushort)(md5table.Length >> 3));
            bw.Write(md5table);
            bw.Write(md5data);
            return ms;
        }

        byte[] encrypt()
        {
            ulong seed = 0;
            for (int i = 0; i < md5table.Length; i += 8) seed += BitConverter.ToUInt64(md5table, i);
            seed = (ulong)(md5table.Length - 1) & seed;

            MemoryStream w = new MemoryStream();
            MemoryStream r = new MemoryStream(cleardata);

            for (int i = 0; i < md5table.Length; i += 8)
            {
                byte[] buffer = new byte[512];
                r.Read(buffer, 0, buffer.Length);

                for (int j = 0; j < 512; j++)
                {
                    buffer[j] ^= md5table[seed];
                    seed = (ulong)((seed + buffer[j]) % (ulong)md5table.Length);
                }

                w.Write(buffer, 0, buffer.Length);
            }

            return w.ToArray();
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
        /// <summary>
        /// The list of available field names on this API object
        /// </summary>
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
        #endregion

        #region Content Fields
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public string Value
        {
            get
            {
                string s = "";
                foreach (string f in this.ContentFields)
                {
                    if (f.Equals("Value") || f.Equals("Stream") || f.Equals("AsBytes") || f.Equals("DecryptedBytes")) continue;
                    if (f.Equals("Assembly"))
                    {
                        try
                        {
                            System.Reflection.Assembly assy = System.Reflection.Assembly.Load(cleardata);
                            string h = String.Format("\n---------\n---------\n{0}: {1}\n---------\n", assy.GetType().Name, f);
                            string t = "---------\n";
                            s += h;
                            s += assy.ToString() + "\n";
                            foreach (var p in typeof(System.Reflection.Assembly).GetProperties())
                            {
                                if (!p.CanRead) continue;
                                s += string.Format("  {0}: {1}\n", p.Name, "" + p.GetValue(assy, null));
                            }
                            foreach (var p in assy.GetReferencedAssemblies())
                                s += string.Format("  Ref: {0}\n", p.ToString());
                            try
                            {
                                foreach (var p in assy.GetExportedTypes())
                                    s += string.Format("  Type: {0}\n", p.ToString());
                            }
                            catch { }
                            s += t;
                        }
                        catch (Exception ex)
                        {
                            s += this.GetType().Assembly.FullName;
                            for (Exception inex = ex; inex != null; inex = ex.InnerException)
                            {
                                s += "\n" + inex.Message;
                                s += "\n" + inex.StackTrace;
                                s += "\n-----";
                            }
                        }
                    }
                    else
                        s += string.Format("{0}: {1}\n", f, "" + this[f]);
                }
                return s;
            }
        }

        public byte Unknown1 { get { return unknown1; } set { if (unknown1 != value) { unknown1 = value; OnResourceChanged(this, new EventArgs()); } } }
        public uint Unknown2 { get { return unknown2; } set { if (unknown2 != value) { unknown2 = value; OnResourceChanged(this, new EventArgs()); } } }
        public BinaryReader Assembly
        {
            get { return new BinaryReader(new MemoryStream(cleardata)); }
            set
            {
                if (value.BaseStream.CanSeek) { value.BaseStream.Position = 0; cleardata = value.ReadBytes((int)value.BaseStream.Length); }
                else
                {
                    MemoryStream ms = new MemoryStream();
                    byte[] buffer = new byte[1024*1024];
                    for (int read = value.BaseStream.Read(buffer, 0, buffer.Length); read > 0; read = value.BaseStream.Read(buffer, 0, buffer.Length))
                        ms.Write(buffer, 0, read);
                    cleardata = ms.ToArray();
                }
                OnResourceChanged(this, new EventArgs());
            }
        }
        #endregion
    }

    /// <summary>
    /// ResourceHandler for NameMapResource wrapper
    /// </summary>
    public class ScriptResourceHandler : AResourceHandler
    {
        /// <summary>
        /// Create the content of the Dictionary
        /// </summary>
        public ScriptResourceHandler()
        {
            this.Add(typeof(ScriptResource), new List<string>(new string[] { "0x073FAA07" }));
        }
    }
}
