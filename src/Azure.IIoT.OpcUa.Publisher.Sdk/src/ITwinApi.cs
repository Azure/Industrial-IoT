// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk {
    using Azure.IIoT.OpcUa.Shared.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents Twin api
    /// </summary>
    public interface ITwinApi {

        /// <summary>
        /// Connect client. Optional but can be used
        /// to test connectivity.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ConnectAsync(ConnectionModel connection,
            CancellationToken ct = default);

        /// <summary>
        /// Browse node on a server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseResponseModel> NodeBrowseFirstAsync(ConnectionModel connection,
            BrowseRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse next references on a server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseNextResponseModel> NodeBrowseNextAsync(ConnectionModel connection,
            BrowseNextRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse by path on a server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowsePathResponseModel> NodeBrowsePathAsync(ConnectionModel connection,
            BrowsePathRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Call method on a server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodCallResponseModel> NodeMethodCallAsync(ConnectionModel connection,
            MethodCallRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get meta data for method call on a server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(ConnectionModel connection,
            MethodMetadataRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node value on a server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueReadResponseModel> NodeValueReadAsync(ConnectionModel connection,
            ValueReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node value on a server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueWriteResponseModel> NodeValueWriteAsync(ConnectionModel connection,
            ValueWriteRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node attributes on a server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ReadResponseModel> NodeReadAsync(ConnectionModel connection,
            ReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node attributes on a server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WriteResponseModel> NodeWriteAsync(ConnectionModel connection,
            WriteRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Disconnect connection
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DisconnectAsync(ConnectionModel connection,
            CancellationToken ct = default);
    }
}
