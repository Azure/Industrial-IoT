// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace FileSystem
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

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

            OpenCount.OnReadValue += OnOpenCount;
            Writable.OnReadValue += OnWritable;
            UserWritable.OnReadValue += OnWritable;
            Size.OnReadValue += OnSize;
            MimeType.OnReadValue += OnMimeType;
            MaxByteStringLength.OnReadValue += OnMaxByteStringLength;
            LastModifiedTime.OnReadValue += OnLastModifiedTime;
            Open.OnCallMethod2 += OnOpen;
            Write.OnCallMethod2 += OnWrite;
            Read.OnCallMethod2 += OnRead;
            Close.OnCallMethod2 += OnClose;
            GetPosition.OnCallMethod2 += OnGetPosition;
            SetPosition.OnCallMethod2 += OnSetPosition;
        }

        private ServiceResult OnMaxByteStringLength(ISystemContext context,
            NodeState node, NumericRange indexRange, QualifiedName dataEncoding,
            ref object value, ref StatusCode statusCode, ref DateTime timestamp)
        {
            value = 8000;
            statusCode = StatusCodes.Good;
            return StatusCodes.Good;
        }

        private ServiceResult OnMimeType(ISystemContext context, NodeState node,
            NumericRange indexRange, QualifiedName dataEncoding, ref object value,
            ref StatusCode statusCode, ref DateTime timestamp)
        {
            value = "text/plain";
            timestamp = DateTime.UtcNow;
            statusCode = StatusCodes.Uncertain;
            return StatusCodes.Good;
        }

        private ServiceResult OnLastModifiedTime(ISystemContext context,
            NodeState node, NumericRange indexRange, QualifiedName dataEncoding,
            ref object value, ref StatusCode statusCode, ref DateTime timestamp)
        {
            value = File.GetLastWriteTimeUtc(FullPath);
            timestamp = DateTime.UtcNow;
            statusCode = StatusCodes.Good;
            return StatusCodes.Good;
        }

        private ServiceResult OnWritable(ISystemContext context, NodeState node,
            NumericRange indexRange, QualifiedName dataEncoding, ref object value,
            ref StatusCode statusCode, ref DateTime timestamp)
        {
            value = !new FileInfo(FullPath).IsReadOnly;
            timestamp = DateTime.UtcNow;
            statusCode = StatusCodes.Good;
            return StatusCodes.Good;
        }

        private ServiceResult OnSize(ISystemContext context, NodeState node,
            NumericRange indexRange, QualifiedName dataEncoding, ref object value,
            ref StatusCode statusCode, ref DateTime timestamp)
        {
            value = new FileInfo(FullPath).Length;
            timestamp = DateTime.UtcNow;
            statusCode = StatusCodes.Good;
            return StatusCodes.Good;
        }

        private ServiceResult OnOpenCount(ISystemContext context, NodeState node,
            NumericRange indexRange, QualifiedName dataEncoding, ref object value,
            ref StatusCode statusCode, ref DateTime timestamp)
        {
            value = 0;
            timestamp = DateTime.UtcNow;
            statusCode = StatusCodes.Good;
            return StatusCodes.Good;
        }

        private ServiceResult OnOpen(ISystemContext context, MethodState method,
            NodeId objectId, IList<object> inputArguments, IList<object> outputArguments)
        {
            var mode = (byte)inputArguments[0];
            if (mode == 0x1)
            {
                // read
                var stream = new FileStream(FullPath, FileMode.Open, FileAccess.Read);
                _reads.Add(_handles, stream);
                outputArguments.Add(_handles++);
            }
            else if ((mode & 0x2) != 0)
            {
                if (_reads.Count != 0 || _write != null)
                {
                    return StatusCodes.BadInvalidState;
                }
                if ((mode & 0x4) != 0)
                {
                    // Erase
                    _write = new FileStream(FullPath, FileMode.CreateNew, FileAccess.Write);
                }
                else if ((mode & 0x8) != 0)
                {
                    // Append
                    _write = new FileStream(FullPath, FileMode.Append, FileAccess.Write);
                }
                else
                {
                    // Open or create
                    _write = new FileStream(FullPath, FileMode.OpenOrCreate, FileAccess.Write);
                }
                outputArguments.Add(1u);
            }
            return StatusCodes.Good;
        }

        private ServiceResult OnClose(ISystemContext context, MethodState method,
            NodeId objectId, IList<object> inputArguments, IList<object> outputArguments)
        {
            var fileHandle = (uint)inputArguments[0];
            Stream stream;
            if (fileHandle == 1)
            {
                stream = _write;
                _write = null;
            }
            else if (_reads.TryGetValue(fileHandle, out stream))
            {
                _reads.Remove(fileHandle);
            }
            else
            {
                return StatusCodes.BadInvalidState;
            }
            try
            {
                stream.Close();
            }
            finally
            {
                stream.Dispose();
            }
            return StatusCodes.Good;
        }

        private ServiceResult OnSetPosition(ISystemContext context, MethodState method,
            NodeId objectId, IList<object> inputArguments, IList<object> outputArguments)
        {
            var fileHandle = (uint)inputArguments[0];
            Stream stream;
            if (fileHandle == 1)
            {
                stream = _write;
            }
            else if (!_reads.TryGetValue(fileHandle, out stream))
            {
                return StatusCodes.BadInvalidState;
            }
            stream.Position = (long)(ulong)inputArguments[1];
            return StatusCodes.Good;
        }

        private ServiceResult OnGetPosition(ISystemContext context, MethodState method,
            NodeId objectId, IList<object> inputArguments, IList<object> outputArguments)
        {
            var fileHandle = (uint)inputArguments[0];
            Stream stream;
            if (fileHandle == 1)
            {
                stream = _write;
            }
            else if (!_reads.TryGetValue(fileHandle, out stream))
            {
                return StatusCodes.BadInvalidState;
            }
            outputArguments.Add((ulong)stream.Position);
            return StatusCodes.Good;
        }

        private ServiceResult OnRead(ISystemContext context, MethodState method,
            NodeId objectId, IList<object> inputArguments, IList<object> outputArguments)
        {
            var fileHandle = (uint)inputArguments[0];
            Stream stream;
            if (fileHandle == 1)
            {
                return StatusCodes.BadInvalidState;
            }
            else if (!_reads.TryGetValue(fileHandle, out stream))
            {
                return StatusCodes.BadInvalidState;
            }
            var length = (int)inputArguments[1];
            var buffer = new Span<byte>(new byte[length]);
            var read = stream.Read(buffer);
            outputArguments.Add(buffer.Slice(0, read).ToArray());
            return StatusCodes.Good;
        }

        private ServiceResult OnWrite(ISystemContext context, MethodState method,
            NodeId objectId, IList<object> inputArguments, IList<object> outputArguments)
        {
            var fileHandle = (uint)inputArguments[0];
            Stream stream;
            if (fileHandle == 1)
            {
                stream = _write;
            }
            else
            {
                return StatusCodes.BadInvalidState;
            }
            var bytes = (byte[])inputArguments[1];
            stream.Write(bytes.AsSpan());
            return StatusCodes.Good;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _write?.Dispose();
                foreach (var stream in _reads.Values)
                {
                    stream.Dispose();
                }
                _reads.Clear();
            }
            base.Dispose(disposing);
        }

        private uint _handles = 1;
        private readonly Dictionary<uint, Stream> _reads = new();
        private Stream _write;
    }
}
