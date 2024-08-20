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
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public sealed class FileManager : IDisposable
    {
        public FileManager(AssetNodeManager nodeManager, WoTAssetFileTypeState file,
            ILogger logger)
        {
            _nodeManager = nodeManager;
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
                _file.MaxByteStringLength.Value = UInt16.MaxValue;
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
                    handle.Stream.Close();
                    handle.Stream.Dispose();
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

        private ServiceResult OnOpen(ISystemContext _context,
            MethodState _method, NodeId _objectId, byte mode,
            ref uint fileHandle)
        {
            if (mode != 1 && mode != 6)
            {
                return StatusCodes.BadNotSupported;
            }

            lock (_handles)
            {
                if (_handles.Count >= 10)
                {
                    return StatusCodes.BadTooManyOperations;
                }

                if (_writing && mode != 1)
                {
                    return StatusCodes.BadInvalidState;
                }

                var handle = new Handle(_context.SessionId,
                    new MemoryStream(), mode == 6);

                if (mode == 6)
                {
                    _writing = true;
                }

                lock (_handles)
                {
                    fileHandle = ++_nextHandle;
                    _handles.Add(fileHandle, handle);
                    _file.OpenCount.Value = (ushort)_handles.Count;
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
                    handle.Stream.Read(data, 0, data.Length);
                    data = buffer;
                }
                else
                {
                    data = Array.Empty<byte>();
                }
            }

            return ServiceResult.Good;
        }

        private ServiceResult OnWrite(
            ISystemContext _context,
            MethodState _method,
            NodeId _objectId,
            uint fileHandle,
            byte[] data)
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

        private ServiceResult OnClose(
            ISystemContext _context,
            MethodState _method,
            NodeId _objectId,
            uint fileHandle)
        {
            Handle? handle;

            lock (_handles)
            {
                if (!_handles.TryGetValue(fileHandle, out handle))
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
                _file.OpenCount.Value = (ushort)_handles.Count;
            }

            handle.Stream.Close();
            handle.Stream.Dispose();

            return ServiceResult.Good;
        }

        private ServiceResult OnCloseAndUpdate(ISystemContext _context,
            MethodState _method, NodeId _objectId, uint fileHandle)
        {
            Handle? handle;

            lock (_handles)
            {
                if (!_handles.TryGetValue(fileHandle, out handle))
                {
                    return StatusCodes.BadInvalidArgument;
                }

                if (handle.SessionId != _context.SessionId)
                {
                    return StatusCodes.BadUserAccessDenied;
                }

                _writing = false;
                _handles.Remove(fileHandle);
                _file.OpenCount.Value = (ushort)_handles.Count;
            }
            try
            {
                handle.Stream.Close();

                var contents = Encoding.UTF8.GetString(handle.Stream.ToArray());

                _nodeManager.AddNodesForWoTProperties(_file.Parent, contents);

                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(),
                    "settings", _file.Parent.DisplayName.Text + ".jsonld"), contents);

                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new ServiceResult(StatusCodes.BadDecodingError, ex);
            }
            finally
            {
                handle.Stream.Dispose();
            }
        }
        private sealed record class Handle(NodeId SessionId,
            MemoryStream Stream, bool Writing)
        {
            public uint Position { get; set; }
        }

        private readonly AssetNodeManager _nodeManager;
        private readonly WoTAssetFileTypeState _file;
        private readonly ILogger _logger;
        private readonly Dictionary<uint, Handle> _handles = new();
        private bool _writing;
        private uint _nextHandle = 1u;
    }
}
