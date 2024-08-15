// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace FileSystem
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.IO;

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
        /// Is volume
        /// </summary>
        public bool IsVolume { get; }

        /// <summary>
        /// Create directory object
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeId"></param>
        /// <param name="path"></param>
        /// <param name="isVolume"></param>
        public DirectoryObjectState(ISystemContext context, NodeId nodeId,
            string path, bool isVolume) : base(null)
        {
            System.Diagnostics.Contracts.Contract.Assume(context != null);
            FullPath = path;
            IsVolume = isVolume;
            TypeDefinitionId = ObjectTypeIds.FileDirectoryType;
            SymbolicName = path;
            NodeId = nodeId;
            BrowseName = new QualifiedName(isVolume ? path : ModelUtils.GetName(path),
                nodeId.NamespaceIndex);
            DisplayName = new LocalizedText(isVolume ? path : ModelUtils.GetName(path));
            Description = null;
            WriteMask = 0;
            UserWriteMask = 0;
            EventNotifier = EventNotifiers.None;

            DeleteFileSystemObject = new DeleteFileMethodState(this);
            DeleteFileSystemObject.OnCall = new DeleteFileMethodStateMethodCallHandler(OnDeleteFileSystemObject);
            DeleteFileSystemObject.Executable = true;
            DeleteFileSystemObject.UserExecutable = true;
            DeleteFileSystemObject.Create(context, MethodIds.FileDirectoryType_DeleteFileSystemObject,
                BrowseNames.DeleteFileSystemObject, BrowseNames.DeleteFileSystemObject, false);

            CreateFile = new CreateFileMethodState(this);
            CreateFile.OnCall = new CreateFileMethodStateMethodCallHandler(OnCreateFile);
            CreateFile.Executable = true;
            CreateFile.UserExecutable = true;
            CreateFile.Create(context, MethodIds.FileDirectoryType_CreateFile,
                BrowseNames.CreateFile, BrowseNames.CreateFile, false);

            CreateDirectory = new CreateDirectoryMethodState(this);
            CreateDirectory.OnCall = new CreateDirectoryMethodStateMethodCallHandler(OnCreateDirectory);
            CreateDirectory.Executable = true;
            CreateDirectory.UserExecutable = true;
            CreateDirectory.Create(context, MethodIds.FileDirectoryType_CreateDirectory,
                BrowseNames.CreateDirectory, BrowseNames.CreateDirectory, false);

            MoveOrCopy = new MoveOrCopyMethodState(this);
            MoveOrCopy.OnCall = new MoveOrCopyMethodStateMethodCallHandler(OnMoveOrCopy);
            MoveOrCopy.Executable = true;
            MoveOrCopy.UserExecutable = true;
            MoveOrCopy.Create(context, MethodIds.FileDirectoryType_MoveOrCopy,
                BrowseNames.MoveOrCopy, BrowseNames.MoveOrCopy, false);
        }

        private ServiceResult OnMoveOrCopy(ISystemContext _context, MethodState _method,
            NodeId _objectId, NodeId objectToMoveOrCopy, NodeId targetDirectory, bool createCopy,
            string newName, ref NodeId newNodeId)
        {
            var objectToMoveOrCopy2 = ParsedNodeId.Parse(objectToMoveOrCopy);
            if (objectToMoveOrCopy2.RootType == ModelUtils.Volume)
            {
                return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                    "Source is not a directory or file");
            }
            var targetDirectory2 = ParsedNodeId.Parse(targetDirectory);
            if (targetDirectory2.RootType != ModelUtils.Directory)
            {
                return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                    "Target is not a directory");
            }
            var path = objectToMoveOrCopy2.RootId;
            var dst = Path.Combine(targetDirectory2.RootId, newName ?? Path.GetFileName(path));
            try
            {
                if (File.Exists(path))
                {
                    if (createCopy)
                    {
                        File.Copy(path, dst);
                    }
                    else
                    {
                        File.Move(path, dst);
                    }
                    newNodeId = ModelUtils.ConstructIdForFile(dst,
                        NodeId.NamespaceIndex);
                }
                else if (Directory.Exists(path))
                {
                    if (createCopy)
                    {
                        CopyDirectory(path, dst);
                    }
                    else
                    {
                        Directory.Move(path, dst);
                    }
                    newNodeId = ModelUtils.ConstructIdForDirectory(dst,
                        NodeId.NamespaceIndex);
                }
                else
                {
                    return ServiceResult.Create(StatusCodes.BadNotFound,
                        $"File sytem object {path} not found");
                }
                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                return ServiceResult.Create(ex, StatusCodes.BadUserAccessDenied,
                    "Failed to move or copy");
            }
        }

        private ServiceResult OnCreateDirectory(ISystemContext _context, MethodState _method,
            NodeId _objectId, string directoryName, ref NodeId directoryNodeId)
        {
            var name = Path.Combine(FullPath, directoryName);
            if (Path.Exists(name))
            {
                return ServiceResult.Create(StatusCodes.BadBrowseNameDuplicated,
                    "Directory or file with same name exists");
            }
            Directory.CreateDirectory(name);
            directoryNodeId = ModelUtils.ConstructIdForDirectory(name, NodeId.NamespaceIndex);
            return ServiceResult.Good;
        }

        private ServiceResult OnCreateFile(ISystemContext _context, MethodState _method,
            NodeId _objectId, string fileName, bool requestFileOpen, ref NodeId fileNodeId,
            ref uint fileHandle)
        {
            var name = Path.Combine(FullPath, fileName);
            if (Path.Exists(name))
            {
                return ServiceResult.Create(StatusCodes.BadBrowseNameDuplicated,
                    "Directory or file with same name exists");
            }
            fileNodeId = ModelUtils.ConstructIdForFile(name, NodeId.NamespaceIndex);
            if (requestFileOpen)
            {
                if (_context.SystemHandle is not FileSystem system ||
                    system.GetHandle(fileNodeId) is not FileHandle handle)
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidState,
                        "Failed to get handle");
                }

                return handle.Open(0x2, out fileHandle); // open for writing
            }
            try
            {
                using var f = File.Create(name);
            }
            catch (Exception ex)
            {
                return ServiceResult.Create(ex, null,
                    StatusCodes.BadUserAccessDenied);
            }
            fileHandle = 0;
            return StatusCodes.Good;
        }

        private ServiceResult OnDeleteFileSystemObject(ISystemContext _context,
            MethodState _method, NodeId _objectId, NodeId objectToDelete)
        {
            var objectToDelete2 = ParsedNodeId.Parse(objectToDelete);
            var path = objectToDelete2.RootId;
            try
            {
                switch (objectToDelete2.RootType)
                {
                    case ModelUtils.File:
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                            break;
                        }
                        return ServiceResult.Create(StatusCodes.BadNotFound,
                            $"File sytem object {path} not found");
                    case ModelUtils.Directory:
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                            break;
                        }
                        return ServiceResult.Create(StatusCodes.BadNotFound,
                            $"File sytem object {path} not found");
                    case ModelUtils.Volume:
                        return ServiceResult.Create(StatusCodes.BadUserAccessDenied,
                            "Cannot delete root of filesystem");
                    default:
                        return ServiceResult.Create(StatusCodes.BadInvalidState,
                            "Not a fileSystem object.");
                }
            }
            catch (Exception ex)
            {
                return ServiceResult.Create(ex, StatusCodes.BadUserAccessDenied,
                    "Failed to delete file system object.");
            }
            return ServiceResult.Good;
        }

        /// <summary>
        /// Create browser on directory
        /// </summary>
        /// <param name="context"></param>
        /// <param name="view"></param>
        /// <param name="referenceType"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="browseDirection"></param>
        /// <param name="browseName"></param>
        /// <param name="additionalReferences"></param>
        /// <param name="internalOnly"></param>
        /// <returns></returns>
        public override INodeBrowser CreateBrowser(
            ISystemContext context, ViewDescription view, NodeId referenceType,
            bool includeSubtypes, BrowseDirection browseDirection,
            QualifiedName browseName, IEnumerable<IReference> additionalReferences,
            bool internalOnly)
        {
            NodeBrowser browser = new DirectoryBrowser(
                context, view, referenceType, includeSubtypes,
                browseDirection, browseName, additionalReferences,
                internalOnly, this);

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
            if (browser.IsRequired(ReferenceTypeIds.Organizes, true) && IsVolume)
            {
                // add reference to server
                browser.Add(ReferenceTypeIds.Organizes, true, ObjectIds.Server);
            }
            else if (browser.IsRequired(ReferenceTypeIds.HasComponent, true) && !IsVolume)
            {
                var parent = Path.GetDirectoryName(FullPath);
                if (Path.GetPathRoot(FullPath) == parent)
                {
                    // add reference for parent volume.
                    browser.Add(ReferenceTypeIds.HasComponent, true,
                        ModelUtils.ConstructIdForVolume(parent, NodeId.NamespaceIndex));
                }
                else
                {
                    // add reference to parent directory
                    browser.Add(ReferenceTypeIds.HasComponent, true,
                        ModelUtils.ConstructIdForDirectory(parent, NodeId.NamespaceIndex));
                }
            }
        }

        private static void CopyDirectory(string sourcePath, string targetPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath, StringComparison.InvariantCulture));
            }
            foreach (string newPath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath, StringComparison.InvariantCulture), true);
            }
        }
    }
}
