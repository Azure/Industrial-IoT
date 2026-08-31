// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using System;
    using System.Buffers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles method call invocation by dispatching to a method
    /// invoker implementation based on the name of the method
    /// provided.
    /// </summary>
    public interface IRpcHandler
    {
        /// <summary>
        /// Topic root at which to mount the handler. The handler
        /// will handle method invocations at the registered
        /// topic root. Multiple handlers can share the same
        /// topic root but handling is first come first serve.
        /// </summary>
        string MountPoint { get; }

        /// <summary>
        /// Handles the invocation of a method via a registered
        /// method invoker.
        /// </summary>
        /// <param name="method">Method name</param>
        /// <param name="payload">Method argument</param>
        /// <param name="contentType">Content type defining the
        /// payload argument</param>
        /// <param name="ct"></param>
        /// <exception cref="MethodCallStatusException">for any
        /// error that occurred during method call.
        /// </exception>
        /// <exception cref="NotSupportedException">When no method
        /// target was found that could be called.</exception>
        /// <returns>The response encoded in the same encoding
        /// as the payload.</returns>
        ValueTask<ReadOnlySequence<byte>> InvokeAsync(string method,
            ReadOnlySequence<byte> payload, string contentType,
            CancellationToken ct = default);
    }
}
