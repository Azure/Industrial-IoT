// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Calls method on a rpc server with buffer and return
    /// payload as byte buffer.
    /// </summary>
    public interface IMethodClient
    {
        /// <summary>
        /// Call method on a rpc server
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="payload"></param>
        /// <param name="contentType"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ReadOnlyMemory<byte>> CallMethodAsync(string target,
            string method, ReadOnlyMemory<byte> payload, string contentType,
            TimeSpan? timeout = null, CancellationToken ct = default);
    }
}
