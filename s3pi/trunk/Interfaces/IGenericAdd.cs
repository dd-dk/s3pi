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
using System.Collections;

namespace s3pi.Interfaces
{
    /// <summary>
    /// Classes implementing this interface can have elements added
    /// with an empty parameter list.
    /// </summary>
    public interface IGenericAdd : IList
    {
        /// <summary>
        /// Adds an entry to a <seealso cref="s3pi.Interfaces.AResource.DependentList&lt;T&gt;"/>.
        /// </summary>
        /// <param name="fields">
        /// Either the object to add or the generic type's constructor arguments.
        /// Where the object is an <seealso cref="s3pi.Interfaces.AHandlerElement"/>, it will be cloned with
        /// this list's handler specified.
        /// </param>
        /// <returns>True on success</returns>
        bool Add(params object[] fields);
        /// <summary>
        /// Add a default element to the list.
        /// </summary>
        /// <exception cref="NotImplementedException">Lists of abstract classes will fail
        /// with a NotImplementedException.</exception>
        void Add();
    }
}
