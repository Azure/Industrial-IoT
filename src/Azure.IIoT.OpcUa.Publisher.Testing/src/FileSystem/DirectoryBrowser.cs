// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace FileSystem
{
    using Opc.Ua;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Browses the file system folder and files
    /// </summary>
    public class DirectoryBrowser : NodeBrowser
    {
        /// <summary>
        /// Create browser
        /// </summary>
        /// <param name="context"></param>
        /// <param name="view"></param>
        /// <param name="referenceType"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="browseDirection"></param>
        /// <param name="browseName"></param>
        /// <param name="additionalReferences"></param>
        /// <param name="internalOnly"></param>
        /// <param name="source"></param>
        public DirectoryBrowser(ISystemContext context, ViewDescription view,
            NodeId referenceType, bool includeSubtypes, BrowseDirection browseDirection,
            QualifiedName browseName, IEnumerable<IReference> additionalReferences,
            bool internalOnly, DirectoryObjectState source)
            : base(context, view, referenceType, includeSubtypes, browseDirection,
                browseName, additionalReferences, internalOnly)
        {
            _source = source;
            _stage = Stage.Begin;
        }

        /// <summary>
        /// Returns the next reference.
        /// </summary>
        /// <returns>The next reference that meets the browse criteria.</returns>
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

                // don't start browsing huge number of references when only internal references are requested.
                if (InternalOnly)
                {
                    return null;
                }

                if (!IsRequired(ReferenceTypeIds.HasComponent, false))
                {
                    return null;
                }

                if (_stage == Stage.Begin)
                {
                    _directories = System.IO.Directory.GetDirectories(_source.FullPath).ToList();
                    _stage = Stage.Directories;
                }

                // enumerate segments.
                if (_stage == Stage.Directories)
                {
                    reference = NextChild();

                    if (reference != null)
                    {
                        return reference;
                    }

                    _files = System.IO.Directory.GetFiles(_source.FullPath).ToList();
                    _stage = Stage.Files;
                }

                // enumerate files.
                if (_stage == Stage.Files)
                {
                    reference = NextChild();

                    if (reference != null)
                    {
                        return reference;
                    }

                    _stage = Stage.Done;
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
            NodeId targetId = null;

            // check if a specific browse name is requested.
            if (!QualifiedName.IsNull(BrowseName))
            {
                // browse name must be qualified by the correct namespace.
                if (_source.BrowseName.NamespaceIndex != BrowseName.NamespaceIndex)
                {
                    return null;
                }

                // look for matching directory.
                if (_stage == Stage.Directories && _directories != null)
                {
                    foreach (var name in _directories)
                    {
                        if (BrowseName.Name == Path.GetFileName(name))
                        {
                            targetId = ModelUtils.ConstructIdForDirectory(name, _source.NodeId.NamespaceIndex);
                            _directories = null;
                            break;
                        }
                    }
                    _directories = null;
                }

                // look for matching file.
                if (_stage == Stage.Files && _files != null)
                {
                    foreach (var name in _files)
                    {
                        if (BrowseName.Name == Path.GetFileName(name))
                        {
                            targetId = ModelUtils.ConstructIdForFile(name, _source.NodeId.NamespaceIndex);
                            _files = null;
                            break;
                        }
                    }
                    _files = null;
                }
            }
            // return the child at the next position.
            else
            {
                // look for next directory.
                if (_stage == Stage.Directories && _directories != null && _directories.Count > 0)
                {
                    var name = _directories[0];
                    _directories = _directories[1..];
                    targetId = ModelUtils.ConstructIdForDirectory(name, _source.NodeId.NamespaceIndex);
                }

                // look for next file.
                else if (_stage == Stage.Files && _files != null && _files.Count > 0)
                {
                    var name = _files[0];
                    _files = _files[1..];
                    targetId = ModelUtils.ConstructIdForFile(name, _source.NodeId.NamespaceIndex);
                }
            }

            // create reference.
            if (targetId != null)
            {
                return new NodeStateReference(ReferenceTypeIds.HasComponent, false, targetId);
            }

            return null;
        }

        /// <summary>
        /// The stages available in a browse operation.
        /// </summary>
        private enum Stage
        {
            Begin,
            Directories,
            Files,
            Done
        }

        private Stage _stage;
        private readonly DirectoryObjectState _source;
        private List<string> _files;
        private List<string> _directories;
    }
}
