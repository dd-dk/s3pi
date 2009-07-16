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
    public abstract class ARCOLBlock : AHandlerElement, IRCOLBlock, IEquatable<ARCOLBlock>
    {
        const int recommendedApiVersion = 1;

        protected bool dirty;
        protected int requestedAPIversion;
        protected Stream stream;

        public ARCOLBlock(int APIversion, EventHandler handler, Stream s)
            : base(APIversion, handler)
        {
            stream = s;
            if (stream == null) { stream = UnParse(); dirty = true; }
            stream.Position = 0;
            Parse(stream);
        }

        protected abstract void Parse(Stream s);

        #region AHandlerElement
        /// <summary>
        /// The list of available field names on this API object
        /// </summary>
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        public abstract override AHandlerElement Clone(EventHandler handler);
        #endregion

        #region IRCOLBlock Members
        public abstract string Tag { get; }
        public abstract uint ResourceType { get; }
        public abstract Stream UnParse();
        #endregion

        #region IResource Members
        /// <summary>
        /// The resource content as a Stream
        /// </summary>
        public virtual Stream Stream
        {
            get
            {
                if (dirty)
                {
                    stream = UnParse();
                    dirty = false;
                }
                stream.Position = 0;
                return stream;
            }
        }

        /// <summary>
        /// The resource content as a byte array
        /// </summary>
        public virtual byte[] AsBytes
        {
            get
            {
                MemoryStream s = this.Stream as MemoryStream;
                if (s != null) return s.ToArray();

                stream.Position = 0;
                return (new BinaryReader(stream)).ReadBytes((int)stream.Length);
            }
        }

        /// <summary>
        /// Raised if the resource is changed
        /// </summary>
        public event EventHandler ResourceChanged;

        #endregion

        #region IEquatable<ARCOLBlock> Members

        public virtual bool Equals(ARCOLBlock other) { return ArrayCompare(this.AsBytes, other.AsBytes); }

        #endregion

        /// <summary>
        /// Used to indicate the RCOL has changed
        /// </summary>
        protected virtual void OnRCOLChanged(object sender, EventArgs e) { dirty = true; OnElementChanged(); }
    }
}
