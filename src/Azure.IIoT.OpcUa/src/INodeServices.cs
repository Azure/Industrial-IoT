// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa {
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Serializers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Node services expose the OPC UA service sets
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INodeServices<T> {
        /// <summary>
        /// Get the capabilities of the server
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            T id, CancellationToken ct = default);

        /// <summary>
        /// Browse nodes on server
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request">Browse request</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseFirstResponseModel> BrowseFirstAsync(T id,
            BrowseFirstRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse remainder of references
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request">Continuation token</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseNextResponseModel> BrowseNextAsync(T id,
            BrowseNextRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse by path
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowsePathResponseModel> BrowsePathAsync(T id,
            BrowsePathRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get the node metadata which includes the fields
        /// and meta data of the type and can be used when constructing
        /// event filters or calling methods to pass the correct arguments.
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<NodeMetadataResponseModel> GetMetadataAsync(T id,
            NodeMetadataRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node value
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueReadResponseModel> ValueReadAsync(T id,
            ValueReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node value
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueWriteResponseModel> ValueWriteAsync(T id,
            ValueWriteRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get meta data for method call (input and output arguments)
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseModel> GetMethodMetadataAsync(
            T id, MethodMetadataRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodCallResponseModel> MethodCallAsync(T id,
            MethodCallRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node attributes in batch
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ReadResponseModel> ReadAsync(T id,
            ReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node attributes in batch
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WriteResponseModel> WriteAsync(T id,
            WriteRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get history server capabilities
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            T id, CancellationToken ct = default);

        /// <summary>
        /// Get a node's history configuration
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            T id, HistoryConfigurationRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Read node history
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(T id,
            HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read node history continuation
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(T id,
            HistoryReadNextRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Update node history
        /// </summary>
        /// <param name="id">Connection to server to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpdateAsync(T id,
            HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct = default);
    }
}
