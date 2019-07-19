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

namespace HistoricalAccess {
    using System.Collections.Generic;
    using Opc.Ua;

    /// <summary>
    /// Browses the references for an archive folder.
    /// </summary>
    public class ArchiveFolderBrowser : NodeBrowser {

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
        public ArchiveFolderBrowser(
            ISystemContext context,
            ViewDescription view,
            NodeId referenceType,
            bool includeSubtypes,
            BrowseDirection browseDirection,
            QualifiedName browseName,
            IEnumerable<IReference> additionalReferences,
            bool internalOnly,
            ArchiveFolderState source)
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
                    _folders = _source.ArchiveFolder.GetChildFolders();
                    _stage = Stage.Folders;
                    _position = 0;
                }

                // don't start browsing huge number of references when only internal references are requested.
                if (InternalOnly) {
                    return null;
                }

                // enumerate folders.
                if (_stage == Stage.Folders) {
                    if (IsRequired(ReferenceTypeIds.Organizes, false)) {
                        reference = NextChild();

                        if (reference != null) {
                            return reference;
                        }
                    }

                    _items = _source.ArchiveFolder.GetItems();
                    _stage = Stage.Items;
                    _position = 0;
                }

                // enumerate items.
                if (_stage == Stage.Items) {
                    if (IsRequired(ReferenceTypeIds.Organizes, false)) {
                        reference = NextChild();

                        if (reference != null) {
                            return reference;
                        }

                        _stage = Stage.Parents;
                        _position = 0;
                    }
                }

                // enumerate parents.
                if (_stage == Stage.Parents) {
                    if (IsRequired(ReferenceTypeIds.Organizes, true)) {
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

                // look for matching folder.
                if (_stage == Stage.Folders && _folders != null) {
                    for (var ii = 0; ii < _folders.Length; ii++) {
                        if (BrowseName.Name == _folders[ii].Name) {
                            targetId = ArchiveFolderState.ConstructId(_folders[ii].UniquePath, _source.NodeId.NamespaceIndex);
                            break;
                        }
                    }
                }

                // look for matching item.
                else if (_stage == Stage.Items && _items != null) {
                    for (var ii = 0; ii < _items.Length; ii++) {
                        if (BrowseName.Name == _items[ii].Name) {
                            targetId = ArchiveItemState.ConstructId(_items[ii].UniquePath, _source.NodeId.NamespaceIndex);
                            break;
                        }
                    }
                }

                // look for matching parent.
                else if (_stage == Stage.Parents) {
                    var parent = _source.ArchiveFolder.GetParentFolder();

                    if (BrowseName.Name == parent.Name) {
                        targetId = ArchiveFolderState.ConstructId(parent.UniquePath, _source.NodeId.NamespaceIndex);
                    }
                }

                _position = int.MaxValue;
            }

            // return the child at the next position.
            else {
                // look for next folder.
                if (_stage == Stage.Folders && _folders != null) {
                    if (_position >= _folders.Length) {
                        return null;
                    }

                    targetId = ArchiveFolderState.ConstructId(_folders[_position++].UniquePath, _source.NodeId.NamespaceIndex);
                }

                // look for next item.
                else if (_stage == Stage.Items && _items != null) {
                    if (_position >= _items.Length) {
                        return null;
                    }

                    targetId = ArchiveItemState.ConstructId(_items[_position++].UniquePath, _source.NodeId.NamespaceIndex);
                }

                // look for matching parent.
                else if (_stage == Stage.Parents) {
                    var parent = _source.ArchiveFolder.GetParentFolder();

                    if (parent != null) {
                        targetId = ArchiveFolderState.ConstructId(parent.UniquePath, _source.NodeId.NamespaceIndex);
                    }
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
            Folders,
            Items,
            Parents,
            Done
        }



        private Stage _stage;
        private int _position;
        private readonly ArchiveFolderState _source;
        private ArchiveFolder[] _folders;
        private ArchiveItem[] _items;

    }
}
