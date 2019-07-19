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

namespace Opc.Ua.Design.Resolver {
    using Opc.Ua.Design.Schema;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;

    /// <summary>
    /// Default composite resolver
    /// </summary>
    public class CompositeModelResolver : INodeResolver, IEnumerable<INodeResolver> {

        /// <summary>
        /// Create resolver
        /// </summary>
        /// <param name="resolvers"></param>
        public CompositeModelResolver(IEnumerable<INodeResolver> resolvers = null) {
            if (resolvers != null) {
                _resolvers.AddRange(resolvers);
            }
            _resolvers.Add((INodeResolver)Model.Standard);
        }

        /// <inheritdoc/>
        public NodeDesign TryResolve(Namespace ns, XmlQualifiedName symbolicId) {
            if (ns == null) {
                throw new ArgumentNullException(nameof(ns));
            }
            if (symbolicId.IsNullOrEmpty()) {
                throw new ArgumentNullException(nameof(symbolicId));
            }
            foreach (var resolver in _resolvers) {
                try {
                    // Try to delegate to one of the resolvers
                    var node = resolver.TryResolve(ns, symbolicId);
                    if (node != null) {
                        return node;
                    }
                }
                catch {
                    continue;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public IEnumerator<INodeResolver> GetEnumerator() {
            return ((ICollection<INodeResolver>)_resolvers).GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() {
            return ((ICollection<INodeResolver>)_resolvers).GetEnumerator();
        }

        /// <summary> All paths to search </summary>
        private readonly List<INodeResolver> _resolvers =
            new List<INodeResolver>();
    }
}
