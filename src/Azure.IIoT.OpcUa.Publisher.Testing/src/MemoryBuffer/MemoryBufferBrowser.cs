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

namespace MemoryBuffer
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A class to browse the references for a memory buffer.
    /// </summary>
    public class MemoryBufferBrowser : NodeBrowser
    {
        /// <summary>
        /// Creates a new browser object with a set of filters.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="view"></param>
        /// <param name="referenceType"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="browseDirection"></param>
        /// <param name="browseName"></param>
        /// <param name="additionalReferences"></param>
        /// <param name="internalOnly"></param>
        /// <param name="buffer"></param>
        public MemoryBufferBrowser(
            ISystemContext context,
            ViewDescription view,
            NodeId referenceType,
            bool includeSubtypes,
            BrowseDirection browseDirection,
            QualifiedName browseName,
            IEnumerable<IReference> additionalReferences,
            bool internalOnly,
            MemoryBufferState buffer)
        :
            base(
                context,
                view,
                referenceType,
                includeSubtypes,
                browseDirection,
                browseName,
                additionalReferences,
                internalOnly)
        {
            _buffer = buffer;
            _stage = Stage.Begin;
        }

        /// <summary>
        /// Returns the next reference.
        /// </summary>
        /// <returns></returns>
        public override IReference Next()
        {
            lock (DataLock)
            {
                // enumerate pre-defined references.
                // always call first to ensure any pushed-back references are returned first.
                var reference = base.Next();

                if (reference != null)
                {
                    return reference;
                }

                if (_stage == Stage.Begin)
                {
                    _stage = Stage.Components;
                    _position = 0;
                }

                // don't start browsing huge number of references when only internal references are requested.
                if (InternalOnly)
                {
                    return null;
                }

                // enumerate components.
                if (_stage == Stage.Components)
                {
                    if (IsRequired(ReferenceTypeIds.HasComponent, false))
                    {
                        reference = NextChild();

                        if (reference != null)
                        {
                            return reference;
                        }
                    }

                    _stage = Stage.ModelParents;
                    _position = 0;
                }

                // all done.
                return null;
            }
        }

        /// <summary>
        /// Returns the next child.
        /// </summary>
        private NodeStateReference NextChild()
        {
            MemoryTagState tag;

            // check if a specific browse name is requested.
            if (!QualifiedName.IsNull(BrowseName))
            {
                // check if match found previously.
                if (_position == uint.MaxValue)
                {
                    return null;
                }

                // browse name must be qualified by the correct namespace.
                if (_buffer.TypeDefinitionId.NamespaceIndex != BrowseName.NamespaceIndex)
                {
                    return null;
                }

                var name = BrowseName.Name;

                for (var ii = 0; ii < name.Length; ii++)
                {
#pragma warning disable CA2249 // Consider using 'string.Contains' instead of 'string.IndexOf'
                    if ("0123456789ABCDEF".IndexOf(name[ii], StringComparison.Ordinal) == -1)
                    {
#pragma warning restore CA2249 // Consider using 'string.Contains' instead of 'string.IndexOf'
                        return null;
                    }
                }

                _position = Convert.ToUInt32(name, 16);

                // check for memory overflow.
                if (_position >= _buffer.SizeInBytes.Value)
                {
                    return null;
                }

                tag = new MemoryTagState(_buffer, _position);
                _position = uint.MaxValue;
            }

            // return the child at the next position.
            else
            {
                if (_position >= _buffer.SizeInBytes.Value)
                {
                    return null;
                }

                tag = new MemoryTagState(_buffer, _position);
                _position += _buffer.ElementSize;

                // check for memory overflow.
                if (_position >= _buffer.SizeInBytes.Value)
                {
                    return null;
                }
            }

            return new NodeStateReference(ReferenceTypeIds.HasComponent, false, tag);
        }

        /// <summary>
        /// The stages available in a browse operation.
        /// </summary>
        private enum Stage
        {
            Begin,
            Components,
            ModelParents,
            Done
        }

        private Stage _stage;
        private uint _position;
        private readonly MemoryBufferState _buffer;
    }
}
