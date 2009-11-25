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
using System.IO;

namespace s3pi.Interfaces
{
    [Flags]
    public enum EPFlags : byte
    {
        //Unk = 0x00,
        EP1 = 0x08,
    }
    public interface IResourceKey : System.Collections.Generic.IEqualityComparer<IResourceKey>, IEquatable<IResourceKey>, IComparable<IResourceKey>
    {
        /// <summary>
        /// The "type" of the resource
        /// </summary>
        UInt32 ResourceType { get; set; }
        /// <summary>
        /// The EP this resource belongs to
        /// </summary>
        EPFlags EpFlags { get; set; }
        /// <summary>
        /// The "group" the resource is part of
        /// </summary>
        UInt32 ResourceGroup { get; set; }
        /// <summary>
        /// The "instance" number of the resource
        /// </summary>
        UInt64 Instance { get; set; }
    }
}
