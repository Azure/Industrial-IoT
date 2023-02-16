// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api {
    using Microsoft.Azure.IIoT.Api.Models;
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
        Task<BrowseResponseModel> NodeBrowseFirstAsync(ConnectionModel endpoint,
            BrowseRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse next references on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseNextResponseModel> NodeBrowseNextAsync(ConnectionModel endpoint,
            BrowseNextRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse by path on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowsePathResponseModel> NodeBrowsePathAsync(ConnectionModel endpoint,
            BrowsePathRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Call method on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodCallResponseModel> NodeMethodCallAsync(ConnectionModel endpoint,
            MethodCallRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get meta data for method call on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(ConnectionModel endpoint,
            MethodMetadataRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node value on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueReadResponseModel> NodeValueReadAsync(ConnectionModel endpoint,
            ValueReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node value on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueWriteResponseModel> NodeValueWriteAsync(ConnectionModel endpoint,
            ValueWriteRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node attributes on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ReadResponseModel> NodeReadAsync(ConnectionModel endpoint,
            ReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node attributes on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WriteResponseModel> NodeWriteAsync(ConnectionModel endpoint,
            WriteRequestModel request, CancellationToken ct = default);
    }
}
