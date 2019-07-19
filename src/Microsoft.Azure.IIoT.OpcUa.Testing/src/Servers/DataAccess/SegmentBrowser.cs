/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
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

namespace DataAccess {
    using System.Collections.Generic;
    using Opc.Ua;

    /// <summary>
    /// Browses the children of a segment.
    /// </summary>
    public class SegmentBrowser : NodeBrowser {

        /// <summary>
        /// Creates a new browser object with a set of filters.
        /// </summary>
        /// <param name="context">The system context to use.</param>
        /// <param name="view">The view which may restrict the set of references/nodes found.</param>
        /// <param name="referenceType">The type of references being followed.</param>
        /// <param name="includeSubtypes">Whether subtypes of the reference type are followed.</param>
        /// <param name="browseDirection">Which way the references are being followed.</param>
        /// <param name="browseName">The browse name of a specific target (used when translating browse paths).</param>
        /// <param name="additionalReferences">Any additional references that should be included.</param>
        /// <param name="internalOnly">If true the browser should not making blocking calls to external systems.</param>
        /// <param name="source">The segment being accessed.</param>
        public SegmentBrowser(
            ISystemContext context,
            ViewDescription view,
            NodeId referenceType,
            bool includeSubtypes,
            BrowseDirection browseDirection,
            QualifiedName browseName,
            IEnumerable<IReference> additionalReferences,
            bool internalOnly,
            SegmentState source)
        :
            base(
                context,
                view,
                referenceType,
                includeSubtypes,
                browseDirection,
                browseName,
                additionalReferences,
                internalOnly) {
            _source = source;
            _stage = Stage.Begin;
        }



        /// <summary>
        /// Returns the next reference.
        /// </summary>
        /// <returns>The next reference that meets the browse criteria.</returns>
        public override IReference Next() {
            var system = (UnderlyingSystem)SystemContext.SystemHandle;

            lock (DataLock) {
                IReference reference = null;

                // enumerate pre-defined references.
                // always call first to ensure any pushed-back references are returned first.
                reference = base.Next();

                if (reference != null) {
                    return reference;
                }

                if (_stage == Stage.Begin) {
                    _segments = system.FindSegments(_source.SegmentPath);
                    _stage = Stage.Segments;
                    _position = 0;
                }

                // don't start browsing huge number of references when only internal references are requested.
                if (InternalOnly) {
                    return null;
                }

                // enumerate segments.
                if (_stage == Stage.Segments) {
                    if (IsRequired(ReferenceTypeIds.Organizes, false)) {
                        reference = NextChild();

                        if (reference != null) {
                            return reference;
                        }
                    }

                    _blocks = system.FindBlocks(_source.SegmentPath);
                    _stage = Stage.Blocks;
                    _position = 0;
                }

                // enumerate blocks.
                if (_stage == Stage.Blocks) {
                    if (IsRequired(ReferenceTypeIds.Organizes, false)) {
                        reference = NextChild();

                        if (reference != null) {
                            return reference;
                        }

                        _stage = Stage.Done;
                        _position = 0;
                    }
                }

                // all done.
                return null;
            }
        }



        /// <summary>
        /// Returns the next child.
        /// </summary>
        private IReference NextChild() {
            var system = (UnderlyingSystem)SystemContext.SystemHandle;

            NodeId targetId = null;

            // check if a specific browse name is requested.
            if (!QualifiedName.IsNull(BrowseName)) {
                // check if match found previously.
                if (_position == int.MaxValue) {
                    return null;
                }

                // browse name must be qualified by the correct namespace.
                if (_source.BrowseName.NamespaceIndex != BrowseName.NamespaceIndex) {
                    return null;
                }

                // look for matching segment.
                if (_stage == Stage.Segments && _segments != null) {
                    for (var ii = 0; ii < _segments.Count; ii++) {
                        if (BrowseName.Name == _segments[ii].Name) {
                            targetId = ModelUtils.ConstructIdForSegment(_segments[ii].Id, _source.NodeId.NamespaceIndex);
                            break;
                        }
                    }
                }

                // look for matching block.
                if (_stage == Stage.Blocks && _blocks != null) {
                    for (var ii = 0; ii < _blocks.Count; ii++) {
                        var block = system.FindBlock(_blocks[ii]);

                        if (block != null && BrowseName.Name == block.Name) {
                            targetId = ModelUtils.ConstructIdForBlock(_blocks[ii], _source.NodeId.NamespaceIndex);
                            break;
                        }
                    }
                }

                _position = int.MaxValue;
            }

            // return the child at the next position.
            else {
                // look for next segment.
                if (_stage == Stage.Segments && _segments != null) {
                    if (_position >= _segments.Count) {
                        return null;
                    }

                    targetId = ModelUtils.ConstructIdForSegment(_segments[_position++].Id, _source.NodeId.NamespaceIndex);
                }

                // look for next block.
                else if (_stage == Stage.Blocks && _blocks != null) {
                    if (_position >= _blocks.Count) {
                        return null;
                    }

                    targetId = ModelUtils.ConstructIdForBlock(_blocks[_position++], _source.NodeId.NamespaceIndex);
                }
            }

            // create reference.
            if (targetId != null) {
                return new NodeStateReference(ReferenceTypeIds.Organizes, false, targetId);
            }

            return null;
        }

        /// <summary>
        /// The stages available in a browse operation.
        /// </summary>
        private enum Stage {
            Begin,
            Segments,
            Blocks,
            Done
        }

        private Stage _stage;
        private int _position;
        private readonly SegmentState _source;
        private IList<UnderlyingSystemSegment> _segments;
        private IList<string> _blocks;
    }
}
