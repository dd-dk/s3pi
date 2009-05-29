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

namespace s3pi.Interfaces
{
    /// <summary>
    /// Standardised interface to API objects (hiding the reflection)
    /// </summary>
    public interface IContentFields
    {
#if UNDEF
        /// <summary>
        /// The list of method names available on this object
        /// </summary>
        List<string> Methods { get; }

        /// <summary>
        /// Invoke a method on this object by name
        /// </summary>
        /// <param name="method">The method name</param>
        /// <param name="parms">The array of TypedValue parameters</param>
        /// <returns>The TypedValue result of invoking the method (or null if the method is void)</returns>
        TypedValue Invoke(string method, params TypedValue[] parms);
#endif
        
        /// <summary>
        /// The list of available field names on this API object
        /// </summary>
        List<string> ContentFields { get; }

        /// <summary>
        /// A typed value on this object
        /// </summary>
        /// <param name="index">The name of the field (i.e. one of the values from ContentFields)</param>
        /// <returns>The typed value of the named field</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown index name is requested</exception>
        TypedValue this[string index] { get; set; }
    }
}
