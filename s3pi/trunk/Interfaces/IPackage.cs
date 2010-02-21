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

namespace s3pi.Interfaces
{
    public interface IPackage : IApiVersion, IContentFields
    {
        #region Whole package
        /// <summary>
        /// Tell the package to save itself to wherever it believes it came from
        /// </summary>
        void SavePackage();
        /// <summary>
        /// Tell the package to save itself to the stream <paramref name="s"/>
        /// </summary>
        /// <param name="s">A stream to which the package should be saved</param>
        void SaveAs(Stream s);
        /// <summary>
        /// Tell the package to save itself to a file with the name in <paramref name="path"/>
        /// </summary>
        /// <param name="path">A fully-qualified file name</param>
        void SaveAs(string path);
        #endregion

        #region Package header
        /// <summary>
        /// Package header: "DBPF" bytes
        /// </summary>
        byte[] Magic { get; }
        /// <summary>
        /// Package header: 0x00000002
        /// </summary>
        Int32 Major { get; }
        /// <summary>
        /// Package header: 0x00000000
        /// </summary>
        Int32 Minor { get; }
        /// <summary>
        /// Package header: unused
        /// </summary>
        byte[] Unknown1 { get; }
        /// <summary>
        /// Package header: number of entries in the package index
        /// </summary>
        Int32 Indexcount { get; }
        /// <summary>
        /// Package header: unused
        /// </summary>
        byte[] Unknown2 { get; }
        /// <summary>
        /// Package header: index size on disk in bytes
        /// </summary>
        Int32 Indexsize { get; }
        /// <summary>
        /// Package header: unused
        /// </summary>
        byte[] Unknown3 { get; }
        /// <summary>
        /// Package header: always 3?
        /// </summary>
        Int32 Indexversion { get; }
        /// <summary>
        /// Package header: index position in file
        /// </summary>
        Int32 Indexposition { get; }
        /// <summary>
        /// Package header: unused
        /// </summary>
        byte[] Unknown4 { get; }

        /// <summary>
        /// A MemoryStream covering the package header bytes
        /// </summary>
        Stream HeaderStream { get; }
        #endregion

        #region Package index
        /// <summary>
        /// Package index: the index format in use
        /// </summary>
        UInt32 Indextype { get; }

        /// <summary>
        /// Package index: the index
        /// </summary>
        IList<IResourceIndexEntry> GetResourceList { get; }

        /// <summary>
        /// Package index: raised when the result of a previous call to GetResourceList becomes invalid 
        /// </summary>
        event EventHandler ResourceIndexInvalidated;

        /// <summary>
        /// Searches for an element that matches the conditions defined by <paramref name="flags"/> and <paramref name="values"/>,
        /// and returns the first occurrence within the entire IPackage.
        /// </summary>
        /// <param name="flags">True bits enable matching against numerically equivalent <paramref name="values"/> entry</param>
        /// <param name="values">Fields to compare against</param>
        /// <returns>The first match, if any; otherwise null.</returns>
        IResourceIndexEntry Find(uint flags, IResourceIndexEntry values);

        /// <summary>
        /// Searches for an element that matches the conditions defined by <paramref name="names"/> and <paramref name="values"/>,
        /// and returns the first occurrence within the entire IPackage.
        /// </summary>
        /// <param name="names">Names of fields to compare</param>
        /// <param name="values">Fields to compare against</param>
        /// <returns>The first match, if any; otherwise null.</returns>
        IResourceIndexEntry Find(string[] names, TypedValue[] values);

        /// <summary>
        /// Searches for all element that matches the conditions defined by <paramref name="flags"/> and <paramref name="values"/>,
        /// within the entire IPackage.
        /// </summary>
        /// <param name="flags">True bits enable matching against numerically equivalent <paramref name="values"/> entry</param>
        /// <param name="values">Fields to compare against</param>
        /// <returns>Zero or more matches.</returns>
        IList<IResourceIndexEntry> FindAll(uint flags, IResourceIndexEntry values);

        /// <summary>
        /// Searches for all element that matches the conditions defined by <paramref name="names"/> and <paramref name="values"/>,
        /// within the entire IPackage.
        /// </summary>
        /// <param name="names">Names of fields to compare</param>
        /// <param name="values">Fields to compare against</param>
        /// <returns>Zero or more matches.</returns>
        IList<IResourceIndexEntry> FindAll(string[] names, TypedValue[] values);
        #endregion

        #region Package content
        /// <summary>
        /// Add a resource to the package
        /// </summary>
        /// <param name="rk">The resource key</param>
        /// <param name="stream">The stream that contains the resource data</param>
        /// <param name="rejectDups">If true, fail if the resource key already exists</param>
        /// <returns>Null if rejectDups and the resource key exists; else the new IResourceIndexEntry</returns>
        IResourceIndexEntry AddResource(IResourceKey rk, Stream stream, bool rejectDups);
        /// <summary>
        /// Tell the package to replace the data for the resource indexed by <paramref name="rc"/>
        /// with the data from the resource <paramref name="res"/>
        /// </summary>
        /// <param name="rc">Target resource index</param>
        /// <param name="res">Source resource</param>
        void ReplaceResource(IResourceIndexEntry rc, IResource res);
        /// <summary>
        /// Tell the package to delete the resource indexed by <paramref name="rc"/>
        /// </summary>
        /// <param name="rc">Target resource index</param>
        void DeleteResource(IResourceIndexEntry rc);
        #endregion
    }
}
