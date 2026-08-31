// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc
{
    using Azure.IIoT.OpcUa.Core;
    using System;
    using System.Buffers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The underlying rpc client used to invoke a method.
    /// The implementation is specific to a platform, e.g.,
    /// Mqtt v5 remote procedure calls or Http.
    /// </summary>
    public interface IRpcClient
    {
        /// <summary>
        /// Name of the technology implementing the rpc
        /// client, e.g., mqtt or kafka.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Max payload size in bytes. This is used by the
        /// chunking method client to ensure the methods
        /// are properly broken apart.
        /// </summary>
        int MaxMethodPayloadSizeInBytes { get; }

        /// <summary>
        /// Call a remote server on a target with the
        /// provided payload.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="payload"></param>
        /// <param name="contentType"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ReadOnlySequence<byte>> CallAsync(string target,
            string method, ReadOnlySequence<byte> payload,
            string contentType, TimeSpan? timeout = null,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Rpc client extensions
    /// </summary>
    public static class RpcClientExtension
    {
        /// <summary>
        /// Call a remote procedure on a target with the provided
        /// payload.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="payload"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns>response payload</returns>
        public static async ValueTask<string> CallMethodAsync(
            this IRpcClient client, string target, string method,
            string payload, TimeSpan? timeout = null,
            CancellationToken ct = default)
        {
            var result = await client.CallAsync(target, method,
                Encoding.UTF8.GetBytes(payload), ContentMimeType.Json,
                timeout, ct).ConfigureAwait(false);
            return Encoding.UTF8.GetString(result.Span);
        }

        /// <summary>
        /// Call a remote server on a target with the provided payload.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="payload"></param>
        /// <param name="contentType"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async ValueTask<ReadOnlyMemory<byte>> CallAsync(
            this IRpcClient client, string target, string method,
            ReadOnlyMemory<byte> payload, string contentType,
            TimeSpan? timeout = null, CancellationToken ct = default)
        {
            var result = await client.CallAsync(target, method,
                new ReadOnlySequence<byte>(payload), contentType,
                timeout, ct).ConfigureAwait(false);
            return result.IsSingleSegment ?
                result.First : (ReadOnlyMemory<byte>)result.ToArray();
        }
    }
}
