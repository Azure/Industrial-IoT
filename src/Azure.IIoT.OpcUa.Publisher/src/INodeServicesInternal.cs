// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher {
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Internal node services interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INodeServicesInternal<T> {
        /// <summary>
        /// Read node history
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="request"></param>
        /// <param name="decode"></param>
        /// <param name="encode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<O>> HistoryReadAsync<I, O>(
            T connectionId, HistoryReadRequestModel<I> request,
            Func<I, ISessionHandle, ExtensionObject> decode,
            Func<ExtensionObject, ISessionHandle, O> encode,
            CancellationToken ct = default)
            where I : class where O : class;

        /// <summary>
        /// Read node history continuation
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="request"></param>
        /// <param name="encode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<O>> HistoryReadNextAsync<O>(
            T connectionId, HistoryReadNextRequestModel request,
            Func<ExtensionObject, ISessionHandle, O> encode,
            CancellationToken ct = default)
            where O : class;

        /// <summary>
        /// Update node history
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="request"></param>
        /// <param name="decode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpdateAsync<I>(
            T connectionId, HistoryUpdateRequestModel<I> request,
            Func<NodeId, I, ISessionHandle, Task<ExtensionObject>> decode,
            CancellationToken ct = default) where I : class;
    }
}
