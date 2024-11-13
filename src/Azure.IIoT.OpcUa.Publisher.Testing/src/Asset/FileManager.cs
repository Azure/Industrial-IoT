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

#nullable enable

namespace Asset
{
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    public sealed class FileManager : IDisposable
    {
        public FileManager(AssetNodeManager nodeManager, WoTAssetFileTypeState file,
            string folder, ILogger logger)
        {
            _nodeManager = nodeManager;
            _folder = folder;
            _file = file;
            _logger = logger;
            _file.Size.Value = 0;
            if (_file.MaxByteStringLength != null)
            {
                _file.LastModifiedTime.Value = DateTime.MinValue;
            }

            _file.Writable.Value = false;
            _file.UserWritable.Value = false;
            _file.OpenCount.Value = 0;
            if (_file.MaxByteStringLength != null)
            {
                _file.MaxByteStringLength.Value = ushort.MaxValue;
            }

            _file.Open.OnCall =
                new OpenMethodStateMethodCallHandler(OnOpen);
            _file.Close.OnCall =
                new CloseMethodStateMethodCallHandler(OnClose);
            _file.Read.OnCall =
                new ReadMethodStateMethodCallHandler(OnRead);
            _file.Write.OnCall =
                new WriteMethodStateMethodCallHandler(OnWrite);
            _file.GetPosition.OnCall =
                new GetPositionMethodStateMethodCallHandler(OnGetPosition);
            _file.SetPosition.OnCall =
                new SetPositionMethodStateMethodCallHandler(OnSetPosition);
            _file.CloseAndUpdate.OnCall =
                new CloseAndUpdateMethodStateMethodCallHandler(OnCloseAndUpdate);
        }

        public void Dispose()
        {
            lock (_handles)
            {
                foreach (var handle in _handles.Values)
                {
                    handle.Dispose();
                }
                _handles.Clear();
            }
        }

        private Handle Find(ISystemContext _context, uint fileHandle)
        {
            lock (_handles)
            {
                if (!_handles.TryGetValue(fileHandle, out var handle))
                {
                    throw new ServiceResultException(
                        StatusCodes.BadInvalidArgument);
                }

                if (handle.SessionId != _context.SessionId)
                {
                    throw new ServiceResultException(
                        StatusCodes.BadUserAccessDenied);
                }
                return handle;
            }
        }

        private ServiceResult OnOpen(ISystemContext _context, MethodState _method,
            NodeId _objectId, byte mode, ref uint fileHandle)
        {
            if (mode != 1 && mode != 6)
            {
                return StatusCodes.BadNotSupported;
            }

            lock (_handles)
            {
                try
                {
                    if (_handles.Count >= 10)
                    {
                        return StatusCodes.BadTooManyOperations;
                    }

                    if (_writing && mode != 1)
                    {
                        return ServiceResult.Create(StatusCodes.BadInvalidState, "Writing already");
                    }

                    Handle handle;
                    if (mode == 6)
                    {
                        // Writing
                        handle = new Handle(_context.SessionId);
                        _writing = true;
                    }
                    else if (mode == 1)
                    {
                        // Reading
                        var path = Path.Combine(_folder, _file.Parent.DisplayName.Text + ".jsonld");
                        handle = new Handle(_context.SessionId, File.Open(path, FileMode.Open));
                    }
                    else
                    {
                        return ServiceResult.Create(StatusCodes.BadNotSupported, "Unsupported mode");
                    }
                    fileHandle = ++_nextHandle;
                    _handles.Add(fileHandle, handle);
                    _file.OpenCount.Value = (ushort)_handles.Count;
                }
                catch (Exception ex)
                {
                    return ServiceResult.Create(ex, StatusCodes.BadUserAccessDenied, ex.Message);
                }
            }
            return ServiceResult.Good;
        }

        private ServiceResult OnGetPosition(ISystemContext _context,
            MethodState _method, NodeId _objectId, uint fileHandle,
            ref ulong position)
        {
            var handle = Find(_context, fileHandle);
            position = (ulong)handle.Stream.Position;
            return ServiceResult.Good;
        }

        private ServiceResult OnSetPosition(ISystemContext _context,
            MethodState _method, NodeId _objectId, uint fileHandle,
            ulong position)
        {
            var handle = Find(_context, fileHandle);
            handle.Stream.Seek((long)position, SeekOrigin.Begin);
            return ServiceResult.Good;
        }

        private ServiceResult OnRead(ISystemContext _context,
            MethodState _method, NodeId _objectId, uint fileHandle,
            int length, ref byte[] data)
        {
            lock (_handles)
            {
                var handle = Find(_context, fileHandle);

                if (handle.Writing)
                {
                    return StatusCodes.BadInvalidState;
                }

                if (data?.Length > 0)
                {
                    var buffer = new byte[data.Length];
                    handle.Stream.ReadExactly(data);
                    data = buffer;
                }
                else
                {
                    data = Array.Empty<byte>();
                }
            }

            return ServiceResult.Good;
        }

        private ServiceResult OnWrite(ISystemContext _context,
            MethodState _method, NodeId _objectId, uint fileHandle, byte[] data)
        {
            lock (_handles)
            {
                var handle = Find(_context, fileHandle);

                if (!handle.Writing)
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidState, "Not writable");
                }

                if (data?.Length > 0)
                {
                    handle.Stream.Write(data, 0, data.Length);
                }
            }

            return ServiceResult.Good;
        }

        private ServiceResult OnClose(ISystemContext _context, MethodState _method,
            NodeId _objectId, uint fileHandle)
        {
            lock (_handles)
            {
                if (!_handles.TryGetValue(fileHandle, out var handle))
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                        "Bad file handle");
                }

                if (handle.SessionId != _context.SessionId)
                {
                    return ServiceResult.Create(StatusCodes.BadUserAccessDenied,
                        "Bad sessionid");
                }

                _writing = false;
                _handles.Remove(fileHandle);
                handle.Dispose();
                _file.OpenCount.Value = (ushort)_handles.Count;
                return ServiceResult.Good;
            }
        }

        private ServiceResult OnCloseAndUpdate(ISystemContext _context,
            MethodState _method, NodeId _objectId, uint fileHandle)
        {
            if (!TryGetHandle(_context, fileHandle, out var handle, out var sr))
            {
                return sr;
            }
            try
            {
                // Reset
                handle.Stream.Position = 0;

                var jsonSerializer = JsonSerializer.CreateDefault();
                using var text = new StreamReader(handle.Stream);
                using var reader = new JsonTextReader(text);

                var td = jsonSerializer.Deserialize<ThingDescription>(reader);
                if (td?.Context == null)
                {
                    throw new FormatException("Missing context");
                }
                _nodeManager.AddNodesForThingDescription(_file.Parent, td);
                File.WriteAllText(Path.Combine(_folder,
                    _file.Parent.DisplayName.Text + ".jsonld"), JsonConvert.SerializeObject(td));

                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                return new ServiceResult(StatusCodes.BadDecodingError, ex);
            }
            finally
            {
                handle.Dispose();
            }

            bool TryGetHandle(ISystemContext _context, uint fileHandle,
                [NotNullWhen(true)] out Handle? handle,
                [NotNullWhen(false)] out ServiceResult? sr)
            {
                lock (_handles)
                {
                    if (!_handles.TryGetValue(fileHandle, out handle))
                    {
                        sr = StatusCodes.BadInvalidArgument;
                        return false;
                    }

                    if (handle.SessionId != _context.SessionId)
                    {
                        sr = StatusCodes.BadUserAccessDenied;
                        return false;
                    }

                    _writing = false;
                    _handles.Remove(fileHandle);
                    _file.OpenCount.Value = (ushort)_handles.Count;
                    sr = null;
                    return true;
                }
            }
        }

        public void Delete()
        {
            try
            {
                File.Delete(Path.Combine(_folder,
                    _file.Parent.DisplayName.Text + ".jsonld"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
            }
        }

        private sealed record class Handle : IDisposable
        {
            public NodeId SessionId { get; }
            public Stream Stream { get; }
            public bool Writing { get; }

            public Handle(NodeId sessionId)
            {
                SessionId = sessionId;
                Writing = true;
                Stream = new MemoryStream();
            }

            public Handle(NodeId sessionId, Stream stream)
            {
                SessionId = sessionId;
                Stream = stream;
            }

            public void Dispose()
            {
                Stream.Dispose();
            }
        }

        private readonly AssetNodeManager _nodeManager;
        private readonly WoTAssetFileTypeState _file;
        private readonly ILogger _logger;
        private readonly string _folder;
        private readonly Dictionary<uint, Handle> _handles = new();
        private bool _writing;
        private uint _nextHandle = 1u;
    }
}
