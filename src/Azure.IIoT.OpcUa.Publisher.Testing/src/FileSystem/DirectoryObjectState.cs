// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace FileSystem
{
    using DeterministicAlarms.Configuration;
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;


    /// <summary>
    /// A object which maps a segment to directory
    /// </summary>
    public class DirectoryObjectState : FileDirectoryState
    {
        /// <summary>
        /// Gets the full path
        /// </summary>
        /// <value>The segment path.</value>
        public string FullPath { get; }

        /// <summary>
        /// Create directory object
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeId"></param>
        /// <param name="path"></param>
        /// <param name="isVolume"></param>
        public DirectoryObjectState(ISystemContext context, NodeId nodeId,
            string path, bool isVolume = false) : base(null)
        {
            System.Diagnostics.Contracts.Contract.Assume(context != null);
            FullPath = path;

            TypeDefinitionId = ObjectTypeIds.FileDirectoryType;
            SymbolicName = path;
            NodeId = nodeId;
            BrowseName = new QualifiedName(isVolume ? path : Path.GetDirectoryName(path), nodeId.NamespaceIndex);
            DisplayName = new LocalizedText(isVolume ? path : Path.GetDirectoryName(path));
            Description = null;
            WriteMask = 0;
            UserWriteMask = 0;
            EventNotifier = EventNotifiers.None;

            DeleteFileSystemObject.OnCallMethod2 += OnDeleteFileSystemObject;
            CreateFile.OnCallMethod2 += OnCreateFile;
            CreateDirectory.OnCallMethod2 += OnCreateDirectory;
            MoveOrCopy.OnCallMethod2 += OnMoveOrCopy;
        }

        private ServiceResult OnMoveOrCopy(ISystemContext context, MethodState method,
            NodeId objectId, IList<object> inputArguments, IList<object> outputArguments)
        {
            var objectToMoveOrCopy = ParsedNodeId.Parse((NodeId)inputArguments[0]);
            if (objectToMoveOrCopy.RootType == ModelUtils.Volume)
            {
                return StatusCodes.BadInvalidArgument;
            }
            var targetDirectory = ParsedNodeId.Parse((NodeId)inputArguments[1]);
            if (targetDirectory.RootType != ModelUtils.Directory)
            {
                return StatusCodes.BadInvalidArgument;
            }
            var path = Path.Combine(FullPath, objectToMoveOrCopy.RootId);
            var dst = Path.Combine(FullPath, targetDirectory.RootId);
            var copy = (bool)inputArguments[2];
            if (File.Exists(path))
            {
                dst = Path.Combine(dst, Path.GetFileName(path));
                if (copy)
                {
                    File.Copy(path, dst);
                }
                else
                {
                    File.Move(path, dst);
                }
                outputArguments.Add(ModelUtils.ConstructIdForFile(dst, NodeId.NamespaceIndex));
                return StatusCodes.Good;
            }
            if (Directory.Exists(path))
            {
                if (copy)
                {
                    return StatusCodes.BadNotSupported;
                    // System.IO.Directory.Copy(path, dst);
                }
                else
                {
                    Directory.Move(path, dst);
                }
                outputArguments.Add(ModelUtils.ConstructIdForDirectory(dst, NodeId.NamespaceIndex));
                return StatusCodes.Good;
            }
            return StatusCodes.BadNotFound;
        }

        private ServiceResult OnCreateDirectory(ISystemContext context, MethodState method,
            NodeId objectId, IList<object> inputArguments, IList<object> outputArguments)
        {
            var name = Path.Combine(FullPath, (string)inputArguments[0]);
            if (Path.Exists(name))
            {
                return StatusCodes.BadBrowseNameDuplicated;
            }
            Directory.CreateDirectory(name);
            outputArguments.Add(ModelUtils.ConstructIdForDirectory(name, NodeId.NamespaceIndex));
            return StatusCodes.Good;
        }

        private ServiceResult OnCreateFile(ISystemContext context, MethodState method,
            NodeId objectId, IList<object> inputArguments, IList<object> outputArguments)
        {
            if ((bool)inputArguments[1])
            {
                // requestFileOpen
                return StatusCodes.BadNotSupported;
            }
            var name = Path.Combine(FullPath, (string)inputArguments[0]);
            if (Path.Exists(name))
            {
                return StatusCodes.BadBrowseNameDuplicated;
            }
            using var f = File.Create(name);
            outputArguments.Add(ModelUtils.ConstructIdForFile(name, NodeId.NamespaceIndex));
            outputArguments.Add(0);
            return StatusCodes.Good;
        }

        private ServiceResult OnDeleteFileSystemObject(ISystemContext context, MethodState method,
            NodeId objectId, IList<object> inputArguments, IList<object> outputArguments)
        {
            var path = Path.Combine(FullPath, (string)inputArguments[0]);
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return StatusCodes.Good;
                }
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    return StatusCodes.Good;
                }
                return StatusCodes.BadNotFound;
            }
            catch
            {
                return StatusCodes.BadInvalidState;
            }
        }

        /// <summary>
        /// Creates a browser that explores the structure of the volume.
        /// </summary>
        /// <param name="context">The system context to use.</param>
        /// <param name="view">The view which may restrict the set of references/nodes found.</param>
        /// <param name="referenceType">The type of references being followed.</param>
        /// <param name="includeSubtypes">Whether subtypes of the reference type are followed.</param>
        /// <param name="browseDirection">Which way the references are being followed.</param>
        /// <param name="browseName">The browse name of a specific target (used when translating browse paths).</param>
        /// <param name="additionalReferences">Any additional references that should be included.</param>
        /// <param name="internalOnly">If true the browser should not making blocking calls to external systems.</param>
        /// <returns>The browse object (must be disposed).</returns>
        public override INodeBrowser CreateBrowser(
            ISystemContext context,
            ViewDescription view,
            NodeId referenceType,
            bool includeSubtypes,
            BrowseDirection browseDirection,
            QualifiedName browseName,
            IEnumerable<IReference> additionalReferences,
            bool internalOnly)
        {
            NodeBrowser browser = new FileSystemBrowser(
                context,
                view,
                referenceType,
                includeSubtypes,
                browseDirection,
                browseName,
                additionalReferences,
                internalOnly,
                this);

            PopulateBrowser(context, browser);

            return browser;
        }

        /// <summary>
        /// Populates the browser with references that meet the criteria.
        /// </summary>
        /// <param name="context">The context for the system being accessed.</param>
        /// <param name="browser">The browser to populate.</param>
        protected override void PopulateBrowser(ISystemContext context, NodeBrowser browser)
        {
            base.PopulateBrowser(context, browser);

            // check if the parent segments need to be returned.
            if (browser.IsRequired(ReferenceTypeIds.Organizes, true))
            {
                // add reference for parent segment.
                browser.Add(ReferenceTypeIds.Organizes, true,
                    ModelUtils.ConstructIdForVolume(FullPath, NodeId.NamespaceIndex));
            }
        }
    }
}
