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
#if true
            byte[] res;
            bool smaller = Tiger.Compression.Compress(data, out res);
            return smaller ? res : data;
#else
            if (data.Length < 10) return data;

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            int len = 8;
            if (data.LongLength >= 0x800000000000) { len = 8; }
            else if (data.LongLength >= 0x000080000000) { len = 6; }
            else if (data.LongLength >= 0x000001000000) { len = 4; }
            else { len = 3; }

            bw.Write((ushort)(0xFB10 | (len == 8 ? 0x81 : len == 6 ? 0x01 : len == 4 ? 0x80 : 0x00)));
            byte[] reallength = BitConverter.GetBytes(data.LongLength);
            for (int i = len; i > 0; i--) bw.Write(reallength[i - 1]);

            int pos = 0;
            for (; data.Length - pos >= 4; pos += Enchunk(bw, data, pos)) { }
            WriteChunk(bw, data, pos, data.Length - pos, -1, 0);//EOF mark

            bw.Flush();
            ms.Position = 0;

            return (ms.Length < data.Length) ? (new BinaryReader(ms)).ReadBytes((int)ms.Length) : data;
#endif
        }

        public static int Enchunk(BinaryWriter bw, byte[] buffer, int pos)
        {
            //if (buffer.Length - pos < 4)
            //    return WriteChunk(bw, buffer, pos, buffer.Length - pos, -1, 0);//EOF!

            if (buffer.Length - pos < 8)
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
    }
}




/*
 * The following code was provided by Tiger
**/

namespace Tiger
{
    public class CompressionLevel
    {
        public static readonly CompressionLevel Max = new CompressionLevel(1, 1, 10, 64);

        public readonly int BlockInterval;
        public readonly int SearchLength;
        public readonly int PrequeueLength;
        public readonly int QueueLength;
        public readonly int SameValToTrack;
        public readonly int BruteForceLength;

        public CompressionLevel(int blockInterval, int searchLength, int prequeueLength, int queueLength, int sameValToTrack, int bruteForceLength)
        {
            this.BlockInterval = blockInterval;
            this.SearchLength = searchLength;
            this.PrequeueLength = prequeueLength;
            this.QueueLength = queueLength;
            this.SameValToTrack = sameValToTrack;
            this.BruteForceLength = bruteForceLength;
        }

        public CompressionLevel(int blockInterval, int searchLength, int sameValToTrack, int bruteForceLength)
        {
            this.BlockInterval = blockInterval;
            this.SearchLength = searchLength;
            this.PrequeueLength = this.SearchLength / this.BlockInterval;
            this.QueueLength = 131000 / this.BlockInterval - this.PrequeueLength;
            this.SameValToTrack = sameValToTrack;
            this.BruteForceLength = bruteForceLength;
        }
    }

    public class Compression
    {
        public static bool Compress(byte[] input, out byte[] output)
        {
            return Compress(input, out output, CompressionLevel.Max);
        }

        public static bool Compress(byte[] input, out byte[] output, CompressionLevel level)
        {
            if (input.LongLength >= 0xFFFFFFFF)
            {
                throw new InvalidOperationException("input data is too large");
            }

            bool endIsValid = false;
            List<byte[]> compressedChunks = new List<byte[]>();
            int compressedIndex = 0;
            int compressedLength = 0;
            output = null;

            if (input.Length < 16)
            {
                return false;
            }

            Queue<KeyValuePair<int, int>> blockTrackingQueue = new Queue<KeyValuePair<int, int>>();
            Queue<KeyValuePair<int, int>> blockPretrackingQueue = new Queue<KeyValuePair<int, int>>();

            // So lists aren't being freed and allocated so much
            Queue<List<int>> unusedLists = new Queue<List<int>>();
            Dictionary<int, List<int>> latestBlocks = new Dictionary<int, List<int>>();
            int lastBlockStored = 0;

            while (compressedIndex < input.Length)
            {
                while (compressedIndex > lastBlockStored + level.BlockInterval && input.Length - compressedIndex > 16)
                {
                    if (blockPretrackingQueue.Count >= level.PrequeueLength)
                    {
                        KeyValuePair<int, int> tmppair = blockPretrackingQueue.Dequeue();
                        blockTrackingQueue.Enqueue(tmppair);

                        List<int> valueList;

                        if (!latestBlocks.TryGetValue(tmppair.Key, out valueList))
                        {
                            if (unusedLists.Count > 0)
                            {
                                valueList = unusedLists.Dequeue();
                            }
                            else
                            {
                                valueList = new List<int>();
                            }

                            latestBlocks[tmppair.Key] = valueList;
                        }

                        if (valueList.Count >= level.SameValToTrack)
                        {
                            int earliestIndex = 0;
                            int earliestValue = valueList[0];

                            for (int loop = 1; loop < valueList.Count; loop++)
                            {
                                if (valueList[loop] < earliestValue)
                                {
                                    earliestIndex = loop;
                                    earliestValue = valueList[loop];
                                }
                            }

                            valueList[earliestIndex] = tmppair.Value;
                        }
                        else
                        {
                            valueList.Add(tmppair.Value);
                        }

                        if (blockTrackingQueue.Count > level.QueueLength)
                        {
                            KeyValuePair<int, int> tmppair2 = blockTrackingQueue.Dequeue();
                            valueList = latestBlocks[tmppair2.Key];

                            for (int loop = 0; loop < valueList.Count; loop++)
                            {
                                if (valueList[loop] == tmppair2.Value)
                                {
                                    valueList.RemoveAt(loop);
                                    break;
                                }
                            }

                            if (valueList.Count == 0)
                            {
                                latestBlocks.Remove(tmppair2.Key);
                                unusedLists.Enqueue(valueList);
                            }
                        }
                    }

                    KeyValuePair<int, int> newBlock = new KeyValuePair<int, int>(BitConverter.ToInt32(input, lastBlockStored), lastBlockStored);
                    lastBlockStored += level.BlockInterval;
                    blockPretrackingQueue.Enqueue(newBlock);
                }

                if (input.Length - compressedIndex < 4)
                {
                    // Just copy the rest
                    byte[] chunk = new byte[input.Length - compressedIndex + 1];
                    chunk[0] = (byte)(0xFC | (input.Length - compressedIndex));
                    Array.Copy(input, compressedIndex, chunk, 1, input.Length - compressedIndex);

                    compressedChunks.Add(chunk);
                    compressedIndex += chunk.Length - 1;
                    compressedLength += chunk.Length;

                    // int toRead = 0;
                    // int toCopy2 = 0;
                    // int copyOffset = 0;

                    endIsValid = true;
                    continue;
                }

                // Search ahead the next 3 bytes for the "best" sequence to copy
                int sequenceStart = 0;
                int sequenceLength = 0;
                int sequenceIndex = 0;
                bool isSequence = false;

                if (FindSequence(input, compressedIndex, ref sequenceStart, ref sequenceLength, ref sequenceIndex, latestBlocks, level))
                {
                    isSequence = true;
                }
                else
                {
                    // Find the next sequence
                    for (int loop = compressedIndex + 4; !isSequence && loop + 3 < input.Length; loop += 4)
                    {
                        if (FindSequence(input, loop, ref sequenceStart, ref sequenceLength, ref sequenceIndex, latestBlocks, level))
                        {
                            sequenceIndex += loop - compressedIndex;
                            isSequence = true;
                        }
                    }

                    if (sequenceIndex == int.MaxValue)
                    {
                        sequenceIndex = input.Length - compressedIndex;
                    }

                    // Copy all the data skipped over
                    while (sequenceIndex >= 4)
                    {
                        int toCopy = (sequenceIndex & ~3);
                        if (toCopy > 112)
                        {
                            toCopy = 112;
                        }

                        byte[] chunk = new byte[toCopy + 1];
                        chunk[0] = (byte)(0xE0 | ((toCopy >> 2) - 1));
                        Array.Copy(input, compressedIndex, chunk, 1, toCopy);
                        compressedChunks.Add(chunk);
                        compressedIndex += toCopy;
                        compressedLength += chunk.Length;
                        sequenceIndex -= toCopy;

                        // int toRead = 0;
                        // int toCopy2 = 0;
                        // int copyOffset = 0;
                    }
                }

                if (isSequence)
                {
                    byte[] chunk = null;
                    /*
                     * 00-7F  0oocccpp oooooooo
                     *   Read 0-3
                     *   Copy 3-10
                     *   Offset 0-1023
                     *   
                     * 80-BF  10cccccc ppoooooo oooooooo
                     *   Read 0-3
                     *   Copy 4-67
                     *   Offset 0-16383
                     *   
                     * C0-DF  110cccpp oooooooo oooooooo cccccccc
                     *   Read 0-3
                     *   Copy 5-1028
                     *   Offset 0-131071
                     *   
                     * E0-FC  111ppppp
                     *   Read 4-128 (Multiples of 4)
                     *   
                     * FD-FF  111111pp
                     *   Read 0-3
                     */
                    if (FindRunLength(input, sequenceStart, compressedIndex + sequenceIndex) < sequenceLength)
                    {
                        break;
                    }

                    while (sequenceLength > 0)
                    {
                        int thisLength = sequenceLength;
                        if (thisLength > 1028)
                        {
                            thisLength = 1028;
                        }

                        sequenceLength -= thisLength;
                        int offset = compressedIndex - sequenceStart + sequenceIndex - 1;

                        if (thisLength > 67 || offset > 16383)
                        {
                            chunk = new byte[sequenceIndex + 4];
                            chunk[0] = (byte)(0xC0 | sequenceIndex | (((thisLength - 5) >> 6) & 0x0C) | ((offset >> 12) & 0x10));
                            chunk[1] = (byte)((offset >> 8) & 0xFF);
                            chunk[2] = (byte)(offset & 0xFF);
                            chunk[3] = (byte)((thisLength - 5) & 0xFF);
                        }
                        else if (thisLength > 10 || offset > 1023)
                        {
                            chunk = new byte[sequenceIndex + 3];
                            chunk[0] = (byte)(0x80 | ((thisLength - 4) & 0x3F));
                            chunk[1] = (byte)(((sequenceIndex << 6) & 0xC0) | ((offset >> 8) & 0x3F));
                            chunk[2] = (byte)(offset & 0xFF);
                        }
                        else
                        {
                            chunk = new byte[sequenceIndex + 2];
                            chunk[0] = (byte)((sequenceIndex & 0x3) | (((thisLength - 3) << 2) & 0x1C) | ((offset >> 3) & 0x60));
                            chunk[1] = (byte)(offset & 0xFF);
                        }

                        if (sequenceIndex > 0)
                        {
                            Array.Copy(input, compressedIndex, chunk, chunk.Length - sequenceIndex, sequenceIndex);
                        }

                        compressedChunks.Add(chunk);
                        compressedIndex += thisLength + sequenceIndex;
                        compressedLength += chunk.Length;

                        // int toRead = 0;
                        // int toCopy = 0;
                        // int copyOffset = 0;

                        sequenceStart += thisLength;
                        sequenceIndex = 0;
                    }
                }
            }

            if (compressedLength + 6 < input.Length)
            {
                int chunkPosition;

                if (input.Length > 0xFFFFFF)
                {
                    output = new byte[compressedLength + 5 + (endIsValid ? 0 : 1)];
                    output[0] = 0x10 | 0x80; // 0x80 = length is 4 bytes
                    output[1] = 0xFB;
                    output[2] = (byte)(input.Length >> 24);
                    output[3] = (byte)(input.Length >> 16);
                    output[4] = (byte)(input.Length >> 8);
                    output[5] = (byte)(input.Length);
                    chunkPosition = 6;
                }
                else
                {
                    output = new byte[compressedLength + 5 + (endIsValid ? 0 : 1)];
                    output[0] = 0x10;
                    output[1] = 0xFB;
                    output[2] = (byte)(input.Length >> 16);
                    output[3] = (byte)(input.Length >> 8);
                    output[4] = (byte)(input.Length);
                    chunkPosition = 5;
                }

                for (int loop = 0; loop < compressedChunks.Count; loop++)
                {
                    Array.Copy(compressedChunks[loop], 0, output, chunkPosition, compressedChunks[loop].Length);
                    chunkPosition += compressedChunks[loop].Length;
                }

                if (!endIsValid)
                {
                    output[output.Length - 1] = 0xFC;
                }

                return true;
            }

            return false;
        }

        private static bool FindSequence(byte[] data, int offset, ref int bestStart, ref int bestLength, ref int bestIndex, Dictionary<int, List<int>> blockTracking, CompressionLevel level)
        {
            int start;
            int end = -level.BruteForceLength;

            if (offset < level.BruteForceLength)
            {
                end = -offset;
            }

            if (offset > 4)
            {
                start = -3;
            }
            else
            {
                start = offset - 3;
            }

            bool foundRun = false;
            try
            {
                if (bestLength < 3)
                {
                    bestLength = 3;
                    bestIndex = int.MaxValue;
                }

                byte[] search = new byte[data.Length - offset > 4 ? 4 : data.Length - offset];

                for (int loop = 0; loop < search.Length; loop++)
                {
                    search[loop] = data[offset + loop];
                }

                while (start >= end && bestLength < 1028)
                {
                    byte currentByte = data[start + offset];

                    for (int loop = 0; loop < search.Length; loop++)
                    {
                        if (currentByte != search[loop] || start >= loop || start - loop < -131072)
                            continue;

                        int len = FindRunLength(data, offset + start, offset + loop);

                        if ((len > bestLength || len == bestLength && loop < bestIndex) &&
                            (len >= 5 ||
                            len >= 4 && start - loop > -16384 ||
                            len >= 3 && start - loop > -1024))
                        {
                            foundRun = true;
                            bestStart = offset + start;
                            bestLength = len;
                            bestIndex = loop;
                        }
                    }

                    start--;
                }

                if (blockTracking.Count > 0 && data.Length - offset > 16 && bestLength < 1028)
                {
                    for (int loop = 0; loop < 4; loop++)
                    {
                        int thisPosition = offset + 3 - loop;
                        int adjust = loop > 3 ? loop - 3 : 0;
                        int value = BitConverter.ToInt32(data, thisPosition);
                        List<int> positions;

                        if (blockTracking.TryGetValue(value, out positions))
                        {
                            foreach (int trypos in positions)
                            {
                                int localadjust = adjust;

                                if (trypos + 131072 < offset + 8)
                                {
                                    continue;
                                }

                                int length = FindRunLength(data, trypos + localadjust, thisPosition + localadjust);

                                if (length >= 5 && length > bestLength)
                                {
                                    foundRun = true;
                                    bestStart = trypos + localadjust;
                                    bestLength = length;
                                    if (loop < 3)
                                    {
                                        bestIndex = 3 - loop;
                                    }
                                    else
                                    {
                                        bestIndex = 0;
                                    }
                                }

                                if (bestLength > 1028)
                                {
                                    break;
                                }
                            }
                        }

                        if (bestLength > 1028)
                        {
                            break;
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            return foundRun;
        }

        private static int FindRunLength(byte[] data, int source, int destination)
        {
            int endSource = source + 1;
            int endDestination = destination + 1;

            while (endDestination < data.Length && data[endSource] == data[endDestination] && endDestination - destination < 1028)
            {
                endSource++;
                endDestination++;
            }

            return endDestination - destination;
        }
    }
}