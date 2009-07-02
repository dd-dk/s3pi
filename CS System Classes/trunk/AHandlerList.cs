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

namespace System.Collections.Generic
{
    /// <summary>
    /// Abstract extension of List&lt;<typeparamref name="T"/>&gt; providing
    /// feedback on list updates through the supplied EventHandler
    /// </summary>
    /// <typeparam name="T">Type of list element</typeparam>
    public abstract class AHandlerList<T> : List<T>
        where T : IEquatable<T>
    {
        protected EventHandler handler;
        protected long maxSize = -1;

        #region Constructors
        protected AHandlerList(EventHandler handler) : base() { this.handler = handler; }
        protected AHandlerList(EventHandler handler, IList<T> lt) : base(lt) { this.handler = handler; }
        protected AHandlerList(EventHandler handler, long size) : base() { this.handler = handler; this.maxSize = size; }
        protected AHandlerList(EventHandler handler, long size, IList<T> lt) : base(lt) { this.handler = handler; this.maxSize = size; }
        #endregion

        #region IList<T> Members
        public virtual new void Insert(int index, T item) { if (maxSize >= 0 && Count == maxSize) throw new InvalidOperationException(); base.Insert(index, item); handler(this, EventArgs.Empty); }
        public new virtual void RemoveAt(int index) { base.RemoveAt(index); handler(this, EventArgs.Empty); }
        public new virtual T this[int index] { get { return base[index]; } set { if (!base[index].Equals(value)) { base[index] = value; handler(this, EventArgs.Empty); } } }
        #endregion

        #region ICollection<T> Members
        public virtual new void Add(T item) { if (maxSize >= 0 && Count == maxSize) throw new InvalidOperationException(); base.Add(item); handler(this, EventArgs.Empty); }
        public new virtual void Clear() { base.Clear(); handler(this, EventArgs.Empty); }
        public new virtual bool Remove(T item) { try { return base.Remove(item); } finally { handler(this, EventArgs.Empty); } }
        #endregion

        public long MaxSize { get { return maxSize; } }
    }
}
