// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents OPC twin service api functions
    /// </summary>
    public interface ITwinServiceApi
    {
        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string> GetServiceStatusAsync(CancellationToken ct = default);

        /// <summary>
        /// Get the capabilities of the server
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            string endpointId, CancellationToken ct = default);

        /// <summary>
        /// Browse node on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseFirstResponseModel> NodeBrowseFirstAsync(string endpointId,
            BrowseFirstRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse next references on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseNextResponseModel> NodeBrowseNextAsync(string endpointId,
            BrowseNextRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse by path on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowsePathResponseModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get the node metadata which includes the fields
        /// and meta data of the type and can be used when constructing
        /// event filters or calling methods to pass the correct arguments.
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<NodeMetadataResponseModel> NodeGetMetadataAsync(string endpointId,
            NodeMetadataRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Call method on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodCallResponseModel> NodeMethodCallAsync(string endpointId,
            MethodCallRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get meta data for method call on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(string endpointId,
            MethodMetadataRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node value on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueReadResponseModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node value on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueWriteResponseModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node attributes on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ReadResponseModel> NodeReadAsync(string endpointId,
            ReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node attributes on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WriteResponseModel> NodeWriteAsync(string endpointId,
            WriteRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get history server capabilities
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            string endpointId, CancellationToken ct = default);

        /// <summary>
        /// Get a node's history configuration
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            string endpointId, HistoryConfigurationRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Read node history with custom encoded extension object details
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            string endpointId, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read history call with custom encoded extension object details
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            string endpointId, HistoryReadNextRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Update using extension object details
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            string endpointId, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct = default);
    }
}
