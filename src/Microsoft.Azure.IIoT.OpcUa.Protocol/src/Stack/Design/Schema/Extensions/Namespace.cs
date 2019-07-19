/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Design.Schema {
    using System.Collections.Generic;

    /// <summary>
    /// Namespace extension
    /// </summary>
    public partial class Namespace {

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (obj is Namespace ns) {
                return
                    Version == ns.Version &&
                    PublicationDate == ns.PublicationDate &&
                    Value == ns.Value;
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = 834833178;
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Version);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(PublicationDate);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Value);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(Namespace ns1, Namespace ns2) => EqualityComparer<Namespace>.Default.Equals(ns1, ns2);

        /// <inheritdoc/>
        public static bool operator !=(Namespace ns1, Namespace ns2) => !(ns1 == ns2);
    }
}
