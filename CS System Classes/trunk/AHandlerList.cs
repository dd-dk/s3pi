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
        /// <summary>
        /// The list change event handler delegate.
        /// </summary>
        protected EventHandler handler;
        /// <summary>
        /// The maximum size of the list, or -1 for no limit.
        /// </summary>
        protected long maxSize = -1;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the AHandlerList&lt;T&gt; class
        /// that is empty
        /// and with an unlimited size.
        /// </summary>
        /// <param name="handler">The event handler to call on changes to the list.</param>
        protected AHandlerList(EventHandler handler) : base() { this.handler = handler; }
        /// <summary>
        /// Initializes a new instance of the AHandlerList&lt;T&gt; class,
        /// filled with the content of <paramref name="lt"/>
        /// and with an unlimited size.
        /// </summary>
        /// <param name="handler">The event handler to call on changes to the list.</param>
        /// <param name="lt">The IList&lt;T&gt; to use as the initial content of the list.</param>
        protected AHandlerList(EventHandler handler, IList<T> lt) : base(lt) { this.handler = handler; }
        /// <summary>
        /// Initializes a new instance of the AHandlerList&lt;T&gt; class
        /// that is empty
        /// and with maximum size of <paramref name="size"/>.
        /// </summary>
        /// <param name="handler">The event handler to call on changes to the list.</param>
        /// <param name="size">Maximum number of elements in the list.</param>
        protected AHandlerList(EventHandler handler, long size) : base() { this.handler = handler; this.maxSize = size; }
        /// <summary>
        /// Initializes a new instance of the AHandlerList&lt;T&gt; class,
        /// filled with the content of <paramref name="lt"/>
        /// and with maximum size of <paramref name="size"/>.
        /// </summary>
        /// <param name="handler">The event handler to call on changes to the list.</param>
        /// <param name="size">Maximum number of elements in the list.</param>
        /// <param name="lt">The IList&lt;T&gt; to use as the initial content of the list.</param>
        protected AHandlerList(EventHandler handler, long size, IList<T> lt) : base(lt) { this.handler = handler; this.maxSize = size; }
        #endregion

        #region List<T> Members
        /// <summary>
        /// Adds the elements of the specified collection to the end of the AHandlerList&lt;T&gt;.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to the end of the AHandlerList&lt;T&gt;.
        /// The collection itself cannot be null, but it can contain elements that are null, if type <typeparamref name="T"/> is a reference type.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="collection"/> is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when list size would be exceeded.</exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void AddRange(IEnumerable<T> collection)
        {
            int newElements = new List<T>(collection).Count;
            if (maxSize >= 0 && Count >= maxSize - newElements) throw new InvalidOperationException();
            base.AddRange(collection);
            OnListChanged();
        }
        /// <summary>
        /// Inserts the elements of a collection into the AHandlerList&lt;T&gt; at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="collection">The collection whose elements should be inserted into the AHandlerList&lt;T&gt;.
        /// The collection itself cannot be null, but it can contain elements that are null, if type <typeparamref name="T"/> is a reference type.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="collection"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than 0.
        /// -or-
        /// <paramref name="index"/> is greater than AHandlerList&lt;T&gt;.Count.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">Thrown when list size would be exceeded.</exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void InsertRange(int index, IEnumerable<T> collection)
        {
            int newElements = new List<T>(collection).Count;
            if (maxSize >= 0 && Count >= maxSize - newElements) throw new InvalidOperationException();
            base.InsertRange(index, collection);
            OnListChanged();
        }
        /// <summary>
        /// Removes the all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The System.Predicate&lt;T&gt; delegate that defines the conditions of the elements to remove.</param>
        /// <returns>The number of elements removed from the AHandlerList&lt;T&gt;.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="match"/> is null.</exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual int RemoveAll(Predicate<T> match) { int res = base.RemoveAll(match); if (res != 0) OnListChanged(); return res; }
        /// <summary>
        /// Removes a range of elements from the AHandlerList&lt;T&gt;.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the AHandlerList&lt;T&gt;.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than 0.
        /// -or-
        /// <paramref name="index"/> is greater than AHandlerList&lt;T&gt;.Count.
        /// </exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void RemoveRange(int index, int count) { base.RemoveRange(index, count); OnListChanged(); }
        /// <summary>
        /// Reverses the order of the elements in the entire AHandlerList&lt;T&gt;.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void Reverse() { base.Reverse(); OnListChanged(); }
        /// <summary>
        /// Reverses the order of the elements in the specified range.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to reverse.</param>
        /// <param name="count">The number of elements in the range to reverse.</param>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the AHandlerList&lt;T&gt;.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than 0.
        /// -or-
        /// <paramref name="index"/> is greater than AHandlerList&lt;T&gt;.Count.
        /// </exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void Reverse(int index, int count) { base.Reverse(index, count); OnListChanged(); }
        /// <summary>
        /// Sorts the elements in the entire AHandlerList&lt;T&gt; using the default comparer.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// The default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default
        /// cannot find an implementation of the System.IComparable&lt;T&gt; generic interface
        /// or the System.IComparable interface for type <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void Sort() { base.Sort(); OnListChanged(); }
        /// <summary>
        /// Sorts the elements in the entire AHandlerList&lt;T&gt; using the specified System.Comparison&lt;T&gt;.
        /// </summary>
        /// <param name="comparison">The System.Comparison&lt;T&gt; to use when comparing elements.</param>
        /// <exception cref="System.ArgumentException">The implementation of <paramref name="comparison"/> caused an error during the sort.
        /// For example, <paramref name="comparison"/> might not return 0 when comparing an item with itself.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="comparison"/> is null.</exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void Sort(Comparison<T> comparison) { base.Sort(comparison); OnListChanged(); }
        /// <summary>
        /// Sorts the elements in the entire AHandlerList&lt;T&gt; using the specified comparer.
        /// </summary>
        /// <param name="comparer">The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing elements,
        /// or null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.</param>
        /// <exception cref="System.ArgumentException">
        /// The implementation of <paramref name="comparer"/> caused an error during the sort.
        /// For example, <paramref name="comparer"/> might not return 0 when comparing an item with itself.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="comparer"/> is null, and the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default
        /// cannot find implementation of the System.IComparable&lt;T&gt; generic interface
        /// or the System.IComparable interface for type <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void Sort(IComparer<T> comparer) { base.Sort(comparer); OnListChanged(); }
        /// <summary>
        /// Sorts the elements in a range of elements in AHandlerList&lt;T&gt; using the specified comparer.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The number of elements in the range to sort.</param>
        /// <param name="comparer">The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing elements,
        /// or null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.</param>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the AHandlerList&lt;T&gt;.
        /// -or-
        /// The implementation of <paramref name="comparer"/> caused an error during the sort.
        /// For example, <paramref name="comparer"/> might not return 0 when comparing an item with itself.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than 0.
        /// -or-
        /// <paramref name="count"/> is less than 0.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="comparer"/> is null, and the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default
        /// cannot find implementation of the System.IComparable&lt;T&gt; generic interface
        /// or the System.IComparable interface for type <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void Sort(int index, int count, IComparer<T> comparer) { base.Sort(index, count, comparer); OnListChanged(); }
        #endregion

        #region IList<T> Members
        /// <summary>
        /// Inserts an item to the AHandlerList&lt;T&gt; at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert into the AHandlerList&lt;T&gt;.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the AHandlerList&lt;T&gt;.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when list size exceeded.</exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void Insert(int index, T item) { if (maxSize >= 0 && Count == maxSize) throw new InvalidOperationException(); base.Insert(index, item); OnListChanged(); }
        /// <summary>
        /// Removes the AHandlerList&lt;T&gt; item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the AHandlerList&lt;T&gt;.</exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void RemoveAt(int index) { base.RemoveAt(index); OnListChanged(); }
        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the AHandlerList&lt;T&gt;.</exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual T this[int index] { get { return base[index]; } set { if (!base[index].Equals(value)) { base[index] = value; OnListChanged(); } } }
        #endregion

        #region ICollection<T> Members
        /// <summary>
        /// Adds an object to the end of the AHandlerList&lt;T&gt;.
        /// </summary>
        /// <param name="item">The object to add to the AHandlerList&lt;T&gt;.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when list size exceeded.</exception>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void Add(T item) { if (maxSize >= 0 && Count == maxSize) throw new InvalidOperationException(); base.Add(item); OnListChanged(); }
        /// <summary>
        /// Removes all items from the AHandlerList&lt;T&gt;.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual void Clear() { base.Clear(); OnListChanged(); }
        /// <summary>
        /// Removes the first occurrence of a specific object from the AHandlerList&lt;T&gt;.
        /// </summary>
        /// <param name="item">The object to remove from the AHandlerList&lt;T&gt;.</param>
        /// <returns>
        /// true if item was successfully removed from the AHandlerList&lt;T&gt;
        /// otherwise, false. This method also returns false if item is not found in
        /// the original AHandlerList&lt;T&gt;.
        /// </returns>
        /// <exception cref="System.NotSupportedException">The AHandlerList&lt;T&gt; is read-only.</exception>
        public new virtual bool Remove(T item) { bool res = base.Remove(item); if (res) OnListChanged(); return res; }
        #endregion

        /// <summary>
        /// The maximum size of the list, or -1 for no limit (read-only).
        /// </summary>
        public long MaxSize { get { return maxSize; } }

        /// <summary>
        /// Invokes the list change event handler.
        /// </summary>
        protected void OnListChanged() { if (handler != null) handler(this, EventArgs.Empty); }
    }
}
