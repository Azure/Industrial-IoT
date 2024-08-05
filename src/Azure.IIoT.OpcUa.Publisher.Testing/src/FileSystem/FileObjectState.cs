// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace FileSystem
{
    using Opc.Ua;
    using System;
    using System.IO;
    using System.Xml.Linq;

    /// <summary>
    /// A object which maps a segment to a UA object.
    /// </summary>
    public class FileObjectState : FileState
    {
        /// <summary>
        /// Gets the path to the file
        /// </summary>
        /// <value>The segment path.</value>
        public string FullPath { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryObjectState"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="nodeId">The node id.</param>
        /// <param name="path">The segment.</param>
        public FileObjectState(ISystemContext context, NodeId nodeId, string path)
            : base(null)
        {
            System.Diagnostics.Contracts.Contract.Assume(context != null);
            FullPath = path;

            TypeDefinitionId = ObjectTypeIds.FileType;
            SymbolicName = path;
            NodeId = nodeId;
            BrowseName = new QualifiedName(Path.GetFileName(path),
                nodeId.NamespaceIndex);
            DisplayName = new LocalizedText(Path.GetFileName(path));
            Description = null;
            WriteMask = 0;
            UserWriteMask = 0;
            EventNotifier = EventNotifiers.None;

            OpenCount = new PropertyState<ushort>(this);
            OpenCount.OnReadValue += OnOpenCount;
            OpenCount.AccessLevel = AccessLevels.CurrentRead;
            OpenCount.UserAccessLevel = AccessLevels.CurrentRead;
            OpenCount.Create(context, VariableIds.FileType_OpenCount,
                BrowseNames.OpenCount, BrowseNames.OpenCount, true);

            Writable = new PropertyState<bool>(this);
            Writable.OnReadValue += OnWritable;
            Writable.AccessLevel = AccessLevels.CurrentRead;
            Writable.UserAccessLevel = AccessLevels.CurrentRead;
            Writable.Create(context, VariableIds.FileType_Writable,
                BrowseNames.Writable, BrowseNames.Writable, true);

            UserWritable = new PropertyState<bool>(this);
            UserWritable.OnReadValue += OnWritable;
            UserWritable.AccessLevel = AccessLevels.CurrentRead;
            UserWritable.UserAccessLevel = AccessLevels.CurrentRead;
            UserWritable.Create(context, VariableIds.FileType_UserWritable,
                BrowseNames.UserWritable, BrowseNames.UserWritable, true);

            Size = new PropertyState<ulong>(this);
            Size.OnReadValue += OnSize;
            Size.AccessLevel = AccessLevels.CurrentRead;
            Size.UserAccessLevel = AccessLevels.CurrentRead;
            Size.Create(context, VariableIds.FileType_Size,
                BrowseNames.Size, BrowseNames.Size, true);

            MimeType = new PropertyState<string>(this);
            MimeType.OnReadValue += OnMimeType;
            MimeType.AccessLevel = AccessLevels.CurrentRead;
            MimeType.UserAccessLevel = AccessLevels.CurrentRead;
            MimeType.Create(context, VariableIds.FileType_MimeType,
                BrowseNames.MimeType, BrowseNames.MimeType, true);

#if OPTIONAL_MAX_BYTE_STRING
            MaxByteStringLength = new PropertyState<uint>(this);
            MaxByteStringLength.OnReadValue += OnMaxByteStringLength;
            MaxByteStringLength.AccessLevel = AccessLevels.CurrentRead;
            MaxByteStringLength.UserAccessLevel = AccessLevels.CurrentRead;
            MaxByteStringLength.Create(context, VariableIds.FileType_MaxByteStringLength,
                BrowseNames.MaxByteStringLength, BrowseNames.MaxByteStringLength, true);
#endif

            LastModifiedTime = new PropertyState<DateTime>(this);
            LastModifiedTime.OnReadValue += OnLastModifiedTime;
            LastModifiedTime.AccessLevel = AccessLevels.CurrentRead;
            LastModifiedTime.UserAccessLevel = AccessLevels.CurrentRead;
            LastModifiedTime.Create(context, VariableIds.FileType_LastModifiedTime,
                BrowseNames.LastModifiedTime, BrowseNames.LastModifiedTime, true);

            Open = new OpenMethodState(this);
            Open.OnCall = new OpenMethodStateMethodCallHandler(OnOpen);
            Open.Executable = true;
            Open.UserExecutable = true;
            Open.Create(context, MethodIds.FileType_Open,
                BrowseNames.Open, BrowseNames.Open, false);

            Write = new WriteMethodState(this);
            Write.OnCall = new WriteMethodStateMethodCallHandler(OnWrite);
            Write.Executable = true;
            Write.UserExecutable = true;
            Write.Create(context, MethodIds.FileType_Write,
                BrowseNames.Write, BrowseNames.Write, false);

            Read = new ReadMethodState(this);
            Read.OnCall = new ReadMethodStateMethodCallHandler(OnRead);
            Read.Executable = true;
            Read.UserExecutable = true;
            Read.Create(context, MethodIds.FileType_Read,
                BrowseNames.Read, BrowseNames.Read, false);

            Close = new CloseMethodState(this);
            Close.OnCall = new CloseMethodStateMethodCallHandler(OnClose);
            Close.Executable = true;
            Close.UserExecutable = true;
            Close.Create(context, MethodIds.FileType_Close,
                BrowseNames.Close, BrowseNames.Close, false);

            GetPosition = new GetPositionMethodState(this);
            GetPosition.OnCall = new GetPositionMethodStateMethodCallHandler(OnGetPosition);
            GetPosition.Executable = true;
            GetPosition.UserExecutable = true;
            GetPosition.Create(context, MethodIds.FileType_GetPosition,
                BrowseNames.GetPosition, BrowseNames.GetPosition, false);

            SetPosition = new SetPositionMethodState(this);
            SetPosition.OnCall = new SetPositionMethodStateMethodCallHandler(OnSetPosition);
            SetPosition.Executable = true;
            SetPosition.UserExecutable = true;
            SetPosition.Create(context, MethodIds.FileType_SetPosition,
                BrowseNames.SetPosition, BrowseNames.SetPosition, false);
        }

#if OPTIONAL_MAX_BYTE_STRING
        private ServiceResult OnMaxByteStringLength(ISystemContext context,
            NodeState node, NumericRange indexRange, QualifiedName dataEncoding,
            ref object value, ref StatusCode statusCode, ref DateTime timestamp)
        {
            if (GetFileHandle(context, NodeId, out var handle, out var result))
            {
                value = handle.MaxByteStringLength;
                timestamp = DateTime.UtcNow;
                statusCode = StatusCodes.Good;
            }
            return result;
        }
#endif
        private ServiceResult OnMimeType(ISystemContext context, NodeState node,
            NumericRange indexRange, QualifiedName dataEncoding, ref object value,
            ref StatusCode statusCode, ref DateTime timestamp)
        {
            if (GetFileHandle(context, NodeId, out var handle, out var result))
            {
                value = handle.MimeType;
                timestamp = DateTime.UtcNow;
                statusCode = StatusCodes.Uncertain;
            }
            return result;
        }

        private ServiceResult OnLastModifiedTime(ISystemContext context,
            NodeState node, NumericRange indexRange, QualifiedName dataEncoding,
            ref object value, ref StatusCode statusCode, ref DateTime timestamp)
        {
            if (GetFileHandle(context, NodeId, out var handle, out var result))
            {
                value = handle.LastModifiedTime;
                timestamp = DateTime.UtcNow;
                statusCode = StatusCodes.Good;
            }
            return result;
        }

        private ServiceResult OnWritable(ISystemContext context, NodeState node,
            NumericRange indexRange, QualifiedName dataEncoding, ref object value,
            ref StatusCode statusCode, ref DateTime timestamp)
        {
            if (GetFileHandle(context, NodeId, out var handle, out var result))
            {
                value = handle.IsWriteable;
                timestamp = DateTime.UtcNow;
                statusCode = StatusCodes.Good;
            }
            return result;
        }

        private ServiceResult OnSize(ISystemContext context, NodeState node,
            NumericRange indexRange, QualifiedName dataEncoding, ref object value,
            ref StatusCode statusCode, ref DateTime timestamp)
        {
            if (GetFileHandle(context, NodeId, out var handle, out var result))
            {
                value = handle.Length;
                timestamp = DateTime.UtcNow;
                statusCode = StatusCodes.Good;
            }
            return result;
        }

        private ServiceResult OnOpenCount(ISystemContext context, NodeState node,
            NumericRange indexRange, QualifiedName dataEncoding, ref object value,
            ref StatusCode statusCode, ref DateTime timestamp)
        {
            if (GetFileHandle(context, NodeId, out var handle, out var result))
            {
                value = handle.OpenCount;
                timestamp = DateTime.UtcNow;
                statusCode = StatusCodes.Good;
            }
            return result;
        }

        private ServiceResult OnOpen(ISystemContext _context, MethodState _method,
            NodeId _objectId, byte mode, ref uint fileHandle)
        {
            if (GetFileHandle(_context, _objectId, out var handle, out var result))
            {
                result = handle.Open(mode, out fileHandle);
            }
            return result;
        }

        private ServiceResult OnClose(ISystemContext _context, MethodState _method,
            NodeId _objectId, uint fileHandle)
        {
            if (GetFileHandle(_context, _objectId, out var handle, out var result)
                && !handle.Close(fileHandle))
            {
                return ServiceResult.Create(StatusCodes.BadInvalidState,
                   "File handle could not be closed.");
            }
            return result;
        }

        private ServiceResult OnSetPosition(ISystemContext _context, MethodState _method,
            NodeId _objectId, uint fileHandle, ulong position)
        {
            if (GetFileHandle(_context, _objectId, out var handle, out var result))
            {
                var stream = handle.GetStream(fileHandle);
                if (stream == null)
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidState,
                       "File handle not open.");
                }
                stream.Position = (long)position;
            }
            return result;
        }

        private ServiceResult OnGetPosition(ISystemContext _context,
            MethodState _method, NodeId _objectId, uint fileHandle, ref ulong position)
        {
            if (GetFileHandle(_context, _objectId, out var handle, out var result))
            {
                var stream = handle.GetStream(fileHandle);
                if (stream == null)
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidState,
                       "File handle not open.");
                }
                position = (ulong)stream.Position;
            }
            return result;
        }

        private ServiceResult OnRead(ISystemContext _context, MethodState _method,
            NodeId _objectId, uint fileHandle, int length, ref byte[] data)
        {
            if (GetFileHandle(_context, _objectId, out var handle, out var result))
            {
                var stream = handle.GetStream(fileHandle);
                if (stream == null)
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidState,
                       "File handle not open.");
                }
                var buffer = new Span<byte>(new byte[length]);
                var read = stream.Read(buffer);
                data = buffer.Slice(0, read).ToArray();
            }
            return result;
        }

        private ServiceResult OnWrite(ISystemContext _context, MethodState _method,
            NodeId _objectId, uint fileHandle, byte[] data)
        {
            if (GetFileHandle(_context, _objectId, out var handle, out var result))
            {
                var stream = handle.GetStream(fileHandle);
                if (stream == null)
                {
                    return StatusCodes.BadInvalidState;
                }
                stream.Write(data.AsSpan());
            }
            return result;
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
            if (browser.IsRequired(ReferenceTypeIds.HasComponent, true))
            {
                var directory = Path.GetDirectoryName(FullPath);
                if (Path.GetPathRoot(FullPath) == directory)
                {
                    browser.Add(ReferenceTypeIds.HasComponent, true,
                        ModelUtils.ConstructIdForVolume(directory, NodeId.NamespaceIndex));
                }
                else
                {
                    browser.Add(ReferenceTypeIds.HasComponent, true,
                        ModelUtils.ConstructIdForDirectory(directory, NodeId.NamespaceIndex));
                }
            }
        }

        private static bool GetFileHandle(ISystemContext context, NodeId nodeId,
            out FileHandle handle, out ServiceResult result)
        {
            if (context.SystemHandle is not FileSystem system ||
               system.GetHandle(nodeId) is not FileHandle h)
            {
                result = ServiceResult.Create(StatusCodes.BadInvalidState,
                    "Object is not a file.");
                handle = default;
                return false;
            }
            handle = h;
            result = ServiceResult.Good;
            return true;
        }
    }
}
