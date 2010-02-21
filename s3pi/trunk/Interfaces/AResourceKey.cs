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
    public abstract class AResourceKey : AHandlerElement, IResourceKey
    {
        #region Attributes
        protected uint resourceType;
        protected uint resourceGroup;
        protected ulong instance;
        #endregion

        public AResourceKey(int APIversion, EventHandler handler) : base(APIversion, handler) { }
        public AResourceKey(int APIversion, EventHandler handler, IResourceKey basis)
            : this(APIversion, handler, basis.ResourceType, basis.ResourceGroup, basis.Instance) { }
        public AResourceKey(int APIversion, EventHandler handler, uint resourceType, uint resourceGroup, ulong instance)
            : base(APIversion, handler)
        {
            this.resourceType = resourceType;
            this.resourceGroup = resourceGroup;
            this.instance = instance;
        }

        #region IResourceKey Members
        /// <summary>
        /// The "type" of the resource
        /// </summary>
        [ElementPriority(1)]
        public virtual uint ResourceType { get { return resourceType; } set { if (resourceType != value) { resourceType = value; OnElementChanged(); } } }
        /// <summary>
        /// The "group" the resource is part of
        /// </summary>
        [ElementPriority(2)]
        public virtual uint ResourceGroup { get { return resourceGroup; } set { if (resourceGroup != value) { resourceGroup = value; OnElementChanged(); } } }
        /// <summary>
        /// The "instance" number of the resource
        /// </summary>
        [ElementPriority(3)]
        public virtual ulong Instance { get { return instance; } set { if (instance != value) { instance = value; OnElementChanged(); } } }
        #endregion

        #region IEqualityComparer<IResourceKey> Members

        public bool Equals(IResourceKey x, IResourceKey y) { return x.Equals(y); }

        public int GetHashCode(IResourceKey obj) { return obj.GetHashCode(); }

        public override int GetHashCode() { return ResourceType.GetHashCode() ^ ResourceGroup.GetHashCode() ^ Instance.GetHashCode(); }

        #endregion

        #region IEquatable<IResourceKey> Members

        public bool Equals(IResourceKey other) { return this.CompareTo(other) == 0; }

        #endregion

        #region IComparable<IResourceKey> Members

        public int CompareTo(IResourceKey other)
        {
            int res = ResourceType.CompareTo(other.ResourceType); if (res != 0) return res;
            res = ResourceGroup.CompareTo(other.ResourceGroup); if (res != 0) return res;
            return Instance.CompareTo(other.Instance);
        }

        #endregion

        /// <summary>
        /// Converts to a string representation of this resource key.
        /// </summary>
        /// <param name="value">The resource key to convert</param>
        /// <returns>The 42 character string representation of this resource key,<br/>
        /// of the form 0xXXXXXXXX-0xXXXXXXXX-0xXXXXXXXXXXXXXXXX.</returns>
        public static implicit operator String(AResourceKey value) { return String.Format("0x{0:X8}-0x{1:X8}-0x{2:X16}", value.ResourceType, value.ResourceGroup, value.Instance); }
        /// <summary>
        /// Converts to a string representation of this resource key.
        /// </summary>
        /// <returns>The 42 character string representation of this resource key,<br/>
        /// of the form 0xXXXXXXXX-0xXXXXXXXX-0xXXXXXXXXXXXXXXXX.</returns>
        public override string ToString() { return this; }
    }
}
