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
    /// File handle
    /// </summary>
    /// <param name="NodeId"></param>
    public sealed record FileHandle(ParsedNodeId NodeId) : IDisposable
    {
        private bool IsOpenForWrite => _write != null;

        private bool IsOpenForRead => _reads.Count > 0;

        /// <summary>
        /// Length
        /// </summary>
        public long Length => new FileInfo(NodeId.RootId).Length;

        /// <summary>
        /// Can be written to
        /// </summary>
        public bool IsWriteable => !IsOpenForRead && !IsOpenForWrite
            && !new FileInfo(NodeId.RootId).IsReadOnly;

        /// <summary>
        /// Last modification
        /// </summary>
        public DateTime LastModifiedTime => File.GetLastWriteTimeUtc(NodeId.RootId);

        /// <summary>
        /// How many file handles are open
        /// </summary>
        public ushort OpenCount => (ushort)(_reads.Count + (IsOpenForWrite ? 1 : 0));

        /// <summary>
        /// Mime type
        /// </summary>
        public string MimeType { get; } = "text/plain"; // TODO

        /// <summary>
        /// Max byte string length
        /// </summary>
        public uint MaxByteStringLength { get; } = 4 * 1024; // TODO

        /// <summary>
        /// Get stream
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public Stream GetStream(uint fileHandle)
        {
            lock (_lock)
            {
                if (_write != null && fileHandle == 1)
                {
                    return _write;
                }
                else if (_reads.TryGetValue(fileHandle, out var stream))
                {
                    return stream;
                }
                return null;
            }
        }

        /// <summary>
        /// Open
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public ServiceResult Open(byte mode, out uint fileHandle)
        {
            lock (_lock)
            {
                fileHandle = 0u;
                try
                {
                    if (mode == 0x1)
                    {
                        if (_write != null)
                        {
                            return ServiceResult.Create(StatusCodes.BadInvalidState,
                                "File already open for write");
                        }
                        // read
                        var stream = new FileStream(NodeId.RootId,
                            FileMode.Open, FileAccess.Read);
                        fileHandle = ++_handles;
                        _reads.Add(fileHandle, stream);
                    }
                    else if ((mode & 0x2) != 0)
                    {
                        if (_reads.Count != 0 || _write != null)
                        {
                            return ServiceResult.Create(StatusCodes.BadInvalidState,
                                "File already open for read or write");
                        }
                        if ((mode & 0x4) != 0)
                        {
                            // Erase = OpenOrCreate + Truncate
                            _write = new FileStream(NodeId.RootId,
                                FileMode.Create, FileAccess.Write);
                        }
                        else if ((mode & 0x8) != 0)
                        {
                            // Append
                            _write = new FileStream(NodeId.RootId,
                                FileMode.Append, FileAccess.Write);
                        }
                        else
                        {
                            // Open or create
                            _write = new FileStream(NodeId.RootId,
                                FileMode.OpenOrCreate, FileAccess.Write);
                        }
                        fileHandle = 1u;
                    }
                    else
                    {
                        return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                            "Unknown mode value.");
                    }
                }
                catch (Exception ex)
                {
                    return ServiceResult.Create(ex, StatusCodes.BadUserAccessDenied,
                        "Failed to open file");
                }
            }
            return ServiceResult.Good;
        }

        public bool Close(uint fileHandle)
        {
            lock (_lock)
            {
                if (_write != null && fileHandle == 1)
                {
                    _write.Dispose();
                    _write = null;
                    return true;
                }
                if (_reads.TryGetValue(fileHandle, out var stream))
                {
                    stream.Dispose();
                    _reads.Remove(fileHandle);
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            _write?.Dispose();
            foreach (var stream in _reads.Values)
            {
                stream.Dispose();
            }
            _reads.Clear();
        }

        private uint _handles = 1;
        private readonly Dictionary<uint, Stream> _reads = new();
        private readonly object _lock = new();
        private Stream _write;
    }
}
