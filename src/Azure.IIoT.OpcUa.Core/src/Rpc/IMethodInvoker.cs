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
    /// Represents a method that can be called. It is typically
    /// managed in a call table by a method server.
    /// </summary>
    public interface IMethodInvoker
    {
        /// <summary>
        /// Name of method
        /// </summary>
        string MethodName { get; }

        /// <summary>
        /// Invoke method and return result.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="contentType"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ReadOnlyMemory<byte>> InvokeAsync(ReadOnlyMemory<byte> payload,
            string contentType, IRpcHandler context, CancellationToken ct = default);
    }
}
