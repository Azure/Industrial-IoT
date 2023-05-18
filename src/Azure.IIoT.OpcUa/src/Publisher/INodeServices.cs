// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Node services expose the OPC UA service sets
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INodeServices<T>
    {
        /// <summary>
        /// Get the capabilities of the server
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            T endpoint, CancellationToken ct = default);

        /// <summary>
        /// Browse nodes on server
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request">Browse request</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseFirstResponseModel> BrowseFirstAsync(T endpoint,
            BrowseFirstRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse remainder of references
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request">Continuation token</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseNextResponseModel> BrowseNextAsync(T endpoint,
            BrowseNextRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Stream node and references
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request">Continuation token</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<BrowseStreamChunkModel> BrowseAsync(T endpoint,
            BrowseStreamRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse by path
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowsePathResponseModel> BrowsePathAsync(T endpoint,
            BrowsePathRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get the node metadata which includes the fields
        /// and meta data of the type and can be used when constructing
        /// event filters or calling methods to pass the correct arguments.
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<NodeMetadataResponseModel> GetMetadataAsync(T endpoint,
            NodeMetadataRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Compile a query into a filter
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request">The query to compile</param>
        /// <param name="ct"></param>
        /// <returns>The compiled query</returns>
        Task<QueryCompilationResponseModel> CompileQueryAsync(T endpoint,
            QueryCompilationRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node value
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueReadResponseModel> ValueReadAsync(T endpoint,
            ValueReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node value
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueWriteResponseModel> ValueWriteAsync(T endpoint,
            ValueWriteRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get meta data for method call (input and output arguments)
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseModel> GetMethodMetadataAsync(
            T endpoint, MethodMetadataRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodCallResponseModel> MethodCallAsync(T endpoint,
            MethodCallRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node attributes in batch
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ReadResponseModel> ReadAsync(T endpoint,
            ReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node attributes in batch
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WriteResponseModel> WriteAsync(T endpoint,
            WriteRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get history server capabilities
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            T endpoint, CancellationToken ct = default);

        /// <summary>
        /// Get a node's history configuration
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            T endpoint, HistoryConfigurationRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Read node history
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(T endpoint,
            HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read node history continuation
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            T endpoint, HistoryReadNextRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Update node history
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpdateAsync(T endpoint,
            HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct = default);
    }
}
