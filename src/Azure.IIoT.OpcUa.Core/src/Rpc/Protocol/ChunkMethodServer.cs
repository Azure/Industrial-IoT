// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Protocol
{
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Buffers;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Simple chunk method server
    /// </summary>
    internal sealed class ChunkMethodServer : IRpcHandler, ICollection<IMethodInvoker>,
        IDisposable
    {
        /// <inheritdoc/>
        public string MountPoint { get; }

        /// <inheritdoc/>
        public int Count => _calltable.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <summary>
        /// Delegated server
        /// </summary>
        internal IRpcHandler Delegate { get; set; }

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="timeout"></param>
        /// <param name="timeProvider"></param>
        /// <param name="mount"></param>
        public ChunkMethodServer(ILogger logger, TimeSpan timeout,
            TimeProvider timeProvider, string? mount = null)
        {
            MountPoint = mount ?? string.Empty;
            Delegate = new NullDelegate(MountPoint);
            _chunks = new ChunkMethodInvoker(logger, timeout, timeProvider);
            Add(_chunks);
        }

        /// <inheritdoc/>
        public ValueTask<ReadOnlySequence<byte>> InvokeAsync(string method,
            ReadOnlySequence<byte> payload, string contentType, CancellationToken ct)
        {
            if (_calltable.TryGetValue(method.ToUpperInvariant(), out var invoker))
            {
                return InvokeAsyncCore(payload, this, invoker, contentType, ct);
            }
            return Delegate.InvokeAsync(method, payload, contentType, ct);

            static async ValueTask<ReadOnlySequence<byte>> InvokeAsyncCore(
                ReadOnlySequence<byte> payload, ChunkMethodServer server,
                IMethodInvoker invoker, string contentType, CancellationToken ct)
            {
                var result = await invoker.InvokeAsync(payload.IsSingleSegment ?
                    payload.First : payload.ToArray(), contentType, server,
                    ct).ConfigureAwait(false);
                return new ReadOnlySequence<byte>(result);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _chunks.Dispose();
        }

        /// <inheritdoc/>
        public void Add(IMethodInvoker item)
        {
            _calltable[item.MethodName.ToUpperInvariant()] = item;
        }

        /// <inheritdoc/>
        public void Add(string methodName, IMethodInvoker item)
        {
            _calltable[methodName.ToUpperInvariant()] = item;
        }

        /// <inheritdoc/>
        public bool TryGetValue(string methodName,
            [NotNullWhen(true)] out IMethodInvoker? invoker)
        {
            return _calltable.TryGetValue(methodName.ToUpperInvariant(), out invoker);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _calltable.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(IMethodInvoker item)
        {
            return _calltable.ContainsKey(item.MethodName.ToUpperInvariant());
        }

        /// <inheritdoc/>
        public void CopyTo(IMethodInvoker[] array, int arrayIndex)
        {
            _calltable.Values.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public bool Remove(IMethodInvoker item)
        {
            return _calltable.Remove(item.MethodName.ToUpperInvariant(), out _);
        }

        /// <inheritdoc/>
        public IEnumerator<IMethodInvoker> GetEnumerator()
        {
            return _calltable.Values.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_calltable.Values).GetEnumerator();
        }

        /// <inheritdoc/>
        private sealed class NullDelegate : IRpcHandler
        {
            /// <inheritdoc/>
            public string MountPoint { get; }

            /// <inheritdoc/>
            public NullDelegate(string topicRoot)
            {
                MountPoint = topicRoot;
            }

            /// <inheritdoc/>
            public ValueTask<ReadOnlySequence<byte>> InvokeAsync(string method,
                ReadOnlySequence<byte> payload, string contentType, CancellationToken ct)
            {
                throw new NotSupportedException($"{method} invoker not registered");
            }
        }

        private readonly ConcurrentDictionary<string, IMethodInvoker> _calltable = new();
        private readonly ChunkMethodInvoker _chunks;
    }
}
