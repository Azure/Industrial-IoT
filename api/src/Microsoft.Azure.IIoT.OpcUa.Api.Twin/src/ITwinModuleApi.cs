// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents OPC twin module api
    /// </summary>
    public interface ITwinModuleApi {

        /// <summary>
        /// Browse node on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseResponseApiModel> NodeBrowseFirstAsync(EndpointApiModel endpoint,
            BrowseRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse next references on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseNextResponseApiModel> NodeBrowseNextAsync(EndpointApiModel endpoint,
            BrowseNextRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse by path on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowsePathResponseApiModel> NodeBrowsePathAsync(EndpointApiModel endpoint,
            BrowsePathRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Call method on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodCallResponseApiModel> NodeMethodCallAsync(EndpointApiModel endpoint,
            MethodCallRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Get meta data for method call on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(EndpointApiModel endpoint,
            MethodMetadataRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node value on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueReadResponseApiModel> NodeValueReadAsync(EndpointApiModel endpoint,
            ValueReadRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node value on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueWriteResponseApiModel> NodeValueWriteAsync(EndpointApiModel endpoint,
            ValueWriteRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node attributes on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ReadResponseApiModel> NodeReadAsync(EndpointApiModel endpoint,
            ReadRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node attributes on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WriteResponseApiModel> NodeWriteAsync(EndpointApiModel endpoint,
            WriteRequestApiModel request, CancellationToken ct = default);
    }
}
