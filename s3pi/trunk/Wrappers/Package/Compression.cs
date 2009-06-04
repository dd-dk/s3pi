/***************************************************************************
 *  Copyright (C) 2009 by Peter L Jones                                    *
 *  peter@users.sf.net                                                     *
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
using System.Collections;
using System.IO;

namespace s3pi.Package
{
    /// <summary>
    /// Internal -- used by Package to handle compression routines
    /// </summary>
    internal static class Compression
    {
        static bool checking = Settings.Settings.Checking;

        public static byte[] UncompressStream(Stream stream, int filesize, int memsize)
        {
            BinaryReader r = new BinaryReader(stream);
            long end = stream.Position + filesize;

            byte[] uncdata = new byte[memsize];
            BinaryWriter bw = new BinaryWriter(new MemoryStream(uncdata));

            byte[] data = r.ReadBytes(2);
            if (checking) if (data.Length != 2)
                    throw new InvalidDataException("Hit unexpected end of file at " + stream.Position);

            int datalen = (((data[0] & 0x80) != 0) ? 4 : 3) * (((data[0] & 0x01) != 0) ? 2 : 1);
            data = r.ReadBytes(datalen);
            if (checking) if (data.Length != datalen)
                    throw new InvalidDataException("Hit unexpected end of file at " + stream.Position);

            long realsize = 0;
            for (int i = 0; i < data.Length; i++) realsize = (realsize << 8) + data[i];

            if (checking) if (realsize != memsize)
                    throw new InvalidDataException(String.Format(
                        "Resource data indicates size does not match index at 0x{0}.  Read 0x{1}.  Expected 0x{2}.",
                        stream.Position.ToString("X8"), realsize.ToString("X8"), memsize.ToString("X8")));

            while (stream.Position < end) { Dechunk(stream, bw); }

            if (checking) if (bw.BaseStream.Position != memsize)
                    throw new InvalidDataException(String.Format("Read 0x{0:X8} bytes.  Expected 0x{1:X8}.", bw.BaseStream.Position, memsize));

            bw.Close();

            return uncdata;
        }

        public static void Dechunk(Stream stream, BinaryWriter bw)
        {
            BinaryReader r = new BinaryReader(stream);
            int copysize = 0;
            int copyoffset = 0;
            int datalen;
            byte[] data;

            byte packing = r.ReadByte();

            #region Compressed
            if (packing < 0x80) // 0.......; new data 3; copy data 10 (min 3); offset 1024
            {
                data = r.ReadBytes(1);
                if (checking) if (data.Length != 1)
                        throw new InvalidDataException("Hit unexpected end of file at " + stream.Position);
                datalen = packing & 0x03;
                copysize = ((packing >> 2) & 0x07) + 3;
                copyoffset = (((packing << 3) & 0x300) | data[0]) + 1;
            }
            else if (packing < 0xC0) // 10......; new data 3; copy data 67 (min 4); offset 16384
            {
                data = r.ReadBytes(2);
                if (checking) if (data.Length != 2)
                        throw new InvalidDataException("Hit unexpected end of file at " + stream.Position);
                datalen = (data[0] >> 6) & 0x03;
                copysize = (packing & 0x3F) + 4;
                copyoffset = (((data[0] << 8) & 0x3F00) | data[1]) + 1;
            }
            else if (packing < 0xE0) // 110.....; new data 3; copy data 1028 (min 5); offset 131072
            {
                data = r.ReadBytes(3);
                if (checking) if (data.Length != 3)
                        throw new InvalidDataException("Hit unexpected end of file at " + stream.Position);
                datalen = packing & 0x03;
                copysize = (((packing << 6) & 0x300) | data[2]) + 5;
                copyoffset = (((packing << 12) & 0x10000) | data[0] << 8 | data[1]) + 1;
            }
            #endregion
            #region Uncompressed
            else if (packing < 0xFC) // 1110000 - 11101111; new data 4-128
                datalen = (((packing & 0x1F) + 1) << 2);
            else // 111111..; new data 3
                datalen = packing & 0x03;
            #endregion

            if (datalen > 0)
            {
                data = r.ReadBytes(datalen);
                if (checking) if (data.Length != datalen)
                        throw new InvalidDataException("Hit unexpected end of file at " + stream.Position);
                bw.Write(data);
            }

            if (checking) if (copyoffset > bw.BaseStream.Position)
                throw new InvalidDataException(String.Format("Invalid copy offset 0x{0:X8} at {1}.", copyoffset, stream.Position));

            if (copysize < copyoffset && copyoffset > 8) CopyA(bw.BaseStream, copyoffset, copysize); else CopyB(bw.BaseStream, copyoffset, copysize);
        }

        static void CopyA(Stream s, int offset, int len)
        {
            while (len > 0)
            {
                long dst = s.Position;
                byte[] b = new byte[Math.Min(offset, len)];
                len -= b.Length;

                s.Position -= offset;
                s.Read(b, 0, b.Length);

                s.Position = dst;
                s.Write(b, 0, b.Length);
            }
        }

        static void CopyB(Stream s, int offset, int len)
        {
            while (len > 0)
            {
                long dst = s.Position;
                len--;

                s.Position -= offset;
                byte b = (byte)s.ReadByte();

                s.Position = dst;
                s.WriteByte(b);
            }
        }

        public static byte[] CompressStream(byte[] data)
        {
            if (data.Length < 10) return data;

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            int len = 8;
            if (data.LongLength >= 0x800000000000) { len = 8; }
            else if (data.LongLength >= 0x000080000000) { len = 6; }
            else if (data.LongLength >= 0x000001000000) { len = 4; }
            else { len = 3; }

            bw.Write((ushort)(0xFB10 | (len == 8 ? 0x81 : len == 6 ? 0x01 : len == 4 ? 0x80 : 0x00)));
            byte[] reallength=BitConverter.GetBytes(data.LongLength);
            for (int i = len; i > 0; i--) bw.Write(reallength[i - 1]);

            for (int i = 0; i < data.Length; i += Enchunk(data, i, bw)) { }

            bw.Flush();
            ms.Position = 0;

            return (ms.Length < data.Length) ? (new BinaryReader(ms)).ReadBytes((int)ms.Length) : data;
        }

        public static int Enchunk(byte[] buffer, int pos, BinaryWriter bw)
        {
            if (buffer.Length - pos < 4)
                return WriteChunk(bw, buffer, pos, buffer.Length - pos, -1, 0);//EOF!

            if (buffer.Length - pos < 6)
                return WriteChunk(bw, buffer, pos, (buffer.Length - pos) & ~0x03, -1, 0);//too near EOF!

            int copysize = 3; // don't try to compress less than 3 bytes
            int uncsize = pos < 3 ? 3 : 0; // need at least copysize uncompressed bytes to copy!
            int buflen = (buffer.Length & ~0x03) - 1; // truncate to multiple of four and sub one as it's always needed


            int hit = Search(buffer, pos + uncsize, copysize, -1);
            while (hit == -1 /*not found*/ && uncsize < 0x70 /*max uncomp*/ && pos + uncsize + copysize < buflen /*EOF!*/)
            {
                uncsize++; /*skip*/
                hit = Search(buffer, pos + uncsize, copysize, -1); /*keep trying*/
            }

            int copypos = hit; /*remember last found position, if any*/
            if (hit != -1) /*found*/
            {
                while (copysize <= 0x403 /*max buffer - 1(lookahead)*/
                    && copysize < pos + uncsize /*max avail data*/
                    && pos + uncsize + copysize < buflen /*EOF!*/)
                {
                    hit = Search(buffer, pos + uncsize, copysize + 1 /*lookahead*/, copypos);
                    if (hit == -1) break; /*no more hits*/
                    /*record success*/
                    copysize++;
                    copypos = hit;
                }
            }
            else
                if (uncsize + copysize <= 0x70) uncsize += copysize;

            

            /*
             * uncsize -- bytes skipped before match, if any
             * copypos -- -1: nothing found; else position in buffer
             * copysize -- if copypos != -1, length of data matched
             * precomp -- uncompressed data passed with compressed
             */

            int precomp = uncsize & 0x03; // uncsize must be multiple of 4
            uncsize &= ~0x03;

            /*Write uncompressed*/
            if (uncsize > 0)
                uncsize = WriteChunk(bw, buffer, pos, uncsize, -1, 0);

            /*Write compressed*/
            if (/*precomp != 0 || */copypos != -1)
                uncsize += WriteChunk(bw, buffer, pos + uncsize, precomp, copypos, copypos == -1 ? 0 : copysize);

            return uncsize;
            /**/
        }

        /// <summary>
        /// Find a byte string in a byte array, return position of least distant match
        /// </summary>
        /// <param name="buffer">Byte array to search</param>
        /// <param name="keypos">Position in <paramref name="buffer"/> of key to find</param>
        /// <param name="keylen">Length of key to find</param>
        /// <param name="start">Position in <paramref name="buffer"/> to start searching, -1 to search from <paramref name="keylen"/> bytes before <paramref name="keypos"/></param>
        /// <returns></returns>
        static int Search(byte[] buffer, int keypos, int keylen, int start)
        {
            if (checking) if (keypos < keylen) // Otherwise we start before the start of the buffer
                    throw new InvalidOperationException(
                        String.Format("At position 0x{0:X8}, requested key length 0x{1:X4} exceeds current position.",
                        keypos, keylen));

            if (checking) if (keypos + keylen - 1 > buffer.Length) // The end of the key must be within the buffer
                    throw new InvalidOperationException(
                        String.Format("At position 0x{0:X8}, requested key length 0x{1:X4} exceeds input data length 0x{2:X8}.",
                        keypos, keylen, buffer.Length));

            //if (start == -1) start = keypos - keylen; // need at least keylen bytes before keypos to compare and copy
            if (start == -1) start = keypos - 1; // have to start with data already output

            /*if (checking) if (start + keylen > keypos)
                    throw new InvalidOperationException(
                        String.Format("At position 0x{0:X8}, requested start position 0x{1:X4} plus key length 0x{2:X4} exceeds current position.",
                        start, keylen, keypos));/**/

            int limit = keylen < 4 ? 1024 : keylen < 5 ? 16384 : 131072;

            retry:
            /*find first byte*/
            while (buffer[start] != buffer[keypos]) /*not found*/
            {
                if (start == 0 || keypos - start == limit) return -1;
                start--;
            }

            /*found first byte; check remainder*/
            for (int i = 1; i < keylen; i++)
            {
                if (buffer[start + i] == buffer[keypos + i]) continue; /*found*/
                if (start == 0 || keypos - start == limit) return -1; /*out of data*/
                start--;
                goto retry;
            }
            return start;
        }

        static int WriteChunk(BinaryWriter bw, byte[] data, int posn, int datalen, int copypos, int copysize)
        {
            #region Assertions
            if (checking) if (posn + datalen > data.Length)
                    throw new InvalidOperationException(
                        String.Format("At position 0x{0:X8}, requested uncompressed length 0x{1:X4} exceeds input data length 0x{2:X8}.",
                        posn, datalen, data.Length));
            #endregion

            byte packing = 0;
            byte[] parm = null;
            int retval = datalen + copysize; // save copysize from the ravages of compression

            if (copypos == -1)
            {
                #region No compression

                #region Assertions
                if (checking)
                {
                    if (datalen > 112)
                        throw new InvalidOperationException(
                            String.Format("At position 0x{0:X8}, requested uncompressed length 0x{1:X4} greater than 112.",
                            posn, datalen));

                    if (copysize != 0)
                        throw new ArgumentException(
                            String.Format("At position 0x{0:X8}, must pass zero copysize (got 0x{1:X4}) when copypos is -1.",
                            posn, copysize));
                }
                #endregion

                if (datalen > 3)
                {
                    #region Assertions
                    if (checking) if ((datalen & 0x03) != 0)
                            throw new InvalidOperationException(
                                String.Format("At position 0x{0:X8}, requested uncompressed length 0x{1:X4} not a multiple of 4.",
                                posn, datalen));
                    if (checking) if (datalen > 0x70)
                            throw new InvalidOperationException(
                                String.Format("At position 0x{0:X8}, requested uncompressed length 0x{1:X4} greater than 0x70.",
                                posn, datalen));
                    #endregion

                    packing = (byte)((datalen >> 2) - 1); //00000000 - 01110000 >> 00000000 - 00001111
                    packing |= 0xE0; // 0000aaaa >> 1110aaaa
                }
                else // Should only happen at end of file
                {
                    #region Assertions
                    if (checking) if (data.Length - posn > 3)
                            throw new InvalidOperationException(
                                String.Format("At position 0x{0:X8}, requested end of file with 0x{1:X4} bytes remaining: must be 3 or less.",
                                posn, data.Length - posn));
                    #endregion
                    packing = (byte)datalen;//(uncsize & 0x03)
                    packing |= 0xFC;
                }
                #endregion
            }
            else
            {
                #region Compression
                int copyoffset = posn + datalen - copypos - 1;

                #region Assertions
                if (checking)
                {
                    if (copypos > posn + datalen)
                        throw new InvalidOperationException(
                            String.Format("At position 0x{0:X8}, invalid copy position 0x{1:X8}.",
                            posn + datalen, copypos));

                    /*if (copypos + copysize > posn + datalen)
                        throw new InvalidOperationException(
                            String.Format("At position 0x{0:X8}, invalid copy length 0x{1:X4}.",
                            posn + datalen, copysize, copypos));/**/

                    if (copyoffset > 0x1ffff)
                        throw new InvalidOperationException(
                            String.Format("At position 0x{0:X8}, requested copy offset 0x{1:X8} exceeds 0x1ffff.",
                            posn, copyoffset));

                    if (copyoffset + 1 > posn + datalen)
                        throw new InvalidOperationException(
                            String.Format("At position 0x{0:X8}, requested copy offset 0x{1:X8} exceeds uncompressed position.",
                            posn, copyoffset));

                    if (datalen > 0x03)
                        throw new InvalidOperationException(
                            String.Format("At position 0x{0:X8}, requested uncompressed length 0x{1:X4} greater than 3.",
                            posn, datalen));
                }
                #endregion

                if (copyoffset < 0x400 && copysize <= 0x0A)
                {
                    parm = new byte[1];

                    packing = (byte)((copyoffset & 0x300) >> 3); // aa ........ >> 0aa.....
                    parm[0] = (byte)(copyoffset & 0xFF);

                    copysize -= 3;
                    packing |= (byte)((copysize & 0x07) << 2); // .....bbb >> ...bbb..

                    packing |= (byte)(datalen & 0x03); // >> ......cc
                }
                else if (copyoffset < 0x4000 && copysize <= 0x43)
                {
                    parm = new byte[2];

                    parm[0] = (byte)((copyoffset & 0x3f00) >> 8);
                    parm[1] = (byte)(copyoffset & 0xFF);

                    copysize -= 4;
                    packing = (byte)(copysize & 0x3F);

                    parm[0] |= (byte)((datalen & 0x03) << 6);

                    packing |= 0x80;
                }
                else // copyoffset < 0x20000 && copysize <= 0x404
                {
                    parm = new byte[3];

                    packing = (byte)((copyoffset & 0x10000) >> 12);
                    parm[0] = (byte)((copyoffset & 0x0FF00) >> 8);
                    parm[1] = (byte)(copyoffset & 0x000FF);

                    copysize -= 5;
                    packing |= (byte)((copysize & 0x300) >> 6);
                    parm[2] = (byte)(copysize & 0x0FF);

                    packing |= (byte)(datalen & 0x03);

                    packing |= 0xC0;
                }
                #endregion
            }

            bw.Write(packing);
            if (parm != null) bw.Write(parm);
            if (datalen > 0) bw.BaseStream.Write(data, posn, datalen);

            return retval;
        }

        static string[] foo(MemoryStream bar, byte[] data, long srcpos, long pos)
        {
            MemoryStream ms = new MemoryStream((byte[])data.Clone());
            ms.Position = srcpos;

            long curpos = bar.Position;
            bar.Position = pos;
            Dechunk(bar, new BinaryWriter(ms));
            long newpos1 = bar.Position;
            bar.Position = curpos;

            curpos = ms.Position;
            ms.Position = srcpos;
            return new string[] {
                new string((new BinaryReader(ms)).ReadChars((int)(curpos - srcpos))),
                (curpos-srcpos).ToString("X"),
                curpos.ToString("X") + ", " + newpos1.ToString("X"),
            };
        }
    }
}
