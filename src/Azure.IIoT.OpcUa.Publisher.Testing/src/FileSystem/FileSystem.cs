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
    using System.Threading;

    /// <summary>
    /// File system and handle management
    /// </summary>
    public sealed class FileSystem : IDisposable
    {
        public void Dispose()
        {
            lock (_syncRoot)
            {
                foreach (var handle in _handles.Values)
                {
                    handle.Dispose();
                }
                _handles.Clear();
            }
        }

        public FileHandle GetHandle(NodeId nodeId)
        {
            lock (_syncRoot)
            {
                if (_handles.TryGetValue(nodeId, out var handle))
                {
                    return handle;
                }
                var parsed = ParsedNodeId.Parse(nodeId);
                if (parsed == null || parsed.RootType != ModelUtils.File)
                {
                    return null;
                }
                handle = new FileHandle(parsed);
                _handles.Add(nodeId, handle);
                return handle;
            }
        }

        private readonly Lock _syncRoot = new();
        private readonly Dictionary<NodeId, FileHandle> _handles = [];
    }
}
