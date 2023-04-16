// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Opc.Ua;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Internal node services interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INodeServicesInternal<T>
    {
        /// <summary>
        /// Read node history
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="connectionId"></param>
        /// <param name="request"></param>
        /// <param name="decode"></param>
        /// <param name="encode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<TOutput>> HistoryReadAsync<TInput, TOutput>(
            T connectionId, HistoryReadRequestModel<TInput> request,
            Func<TInput, IOpcUaSession, ExtensionObject> decode,
            Func<ExtensionObject, IOpcUaSession, TOutput> encode,
            CancellationToken ct = default)
            where TInput : class where TOutput : class;

        /// <summary>
        /// Read node history continuation
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="connectionId"></param>
        /// <param name="request"></param>
        /// <param name="encode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<TOutput>> HistoryReadNextAsync<TOutput>(
            T connectionId, HistoryReadNextRequestModel request,
            Func<ExtensionObject, IOpcUaSession, TOutput> encode,
            CancellationToken ct = default)
            where TOutput : class;

        /// <summary>
        /// Update node history
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="connectionId"></param>
        /// <param name="request"></param>
        /// <param name="decode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpdateAsync<TInput>(
            T connectionId, HistoryUpdateRequestModel<TInput> request,
            Func<NodeId, TInput, IOpcUaSession, Task<ExtensionObject>> decode,
            CancellationToken ct = default) where TInput : class;
    }
}
