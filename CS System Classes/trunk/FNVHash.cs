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

namespace System.Security.Cryptography
{
    // From the Sims3 wiki http://www.sims2wiki.info/wiki.php?title=FNV
    public abstract class FNVHash : HashAlgorithm
    {
        ulong prime;
        ulong offset;
        protected ulong hash;
        protected FNVHash(ulong prime, ulong offset) { this.prime = prime; this.offset = offset; hash = offset; }

        public byte[] ComputeHash(string value) { return this.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(value.ToLowerInvariant())); }

        public override void Initialize() { }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            for (int i = ibStart; i < ibStart + cbSize; i++) { hash *= prime; hash ^= array[i]; }
        }

        protected override byte[] HashFinal() { return BitConverter.GetBytes(hash); }
    }

    public class FNV32 : FNVHash
    {
        public FNV32() : base((uint)0x01000193, (uint)0x811C9DC5) { }
        public override byte[] Hash { get { return BitConverter.GetBytes((uint)hash); } }
        public override int HashSize { get { return 32; } }
        public static uint GetHash(string text) { return BitConverter.ToUInt32(new System.Security.Cryptography.FNV32().ComputeHash(text), 0); }
    }

    public class FNV64 : FNVHash
    {
        public FNV64() : base((ulong)0x00000100000001B3, (ulong)0xCBF29CE484222325) { }
        public override byte[] Hash { get { return BitConverter.GetBytes(hash); } }
        public override int HashSize { get { return 64; } }
        public static ulong GetHash(string text) { return BitConverter.ToUInt64(new System.Security.Cryptography.FNV64().ComputeHash(text), 0); }
    }
}
