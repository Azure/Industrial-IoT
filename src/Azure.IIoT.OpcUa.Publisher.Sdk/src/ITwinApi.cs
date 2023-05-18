// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents Twin api
    /// </summary>
    public interface ITwinApi
    {
        /// <summary>
        /// This call is used to test a connection by opening a session
        /// to the server identified by the connection object.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TestConnectionResponseModel> TestConnectionAsync(
            ConnectionModel connection, TestConnectionRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// This call is used to ensure a connection exists for other
        /// calls and speeds up overall operations.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ConnectResponseModel> ConnectAsync(ConnectionModel connection,
            ConnectRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get the capabilities of the server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            ConnectionModel connection, CancellationToken ct = default);

        /// <summary>
        /// Browse node on a server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseFirstResponseModel> NodeBrowseFirstAsync(ConnectionModel connection,
            BrowseFirstRequestModel request, CancellationToken ct = default);

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
        /// Get the node metadata which includes the fields
        /// and meta data of the type and can be used when constructing
        /// event filters or calling methods to pass the correct arguments.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<NodeMetadataResponseModel> GetMetadataAsync(ConnectionModel connection,
            NodeMetadataRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Compile the query string into a filter query syntax
        /// structure that can be used in other calls.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<QueryCompilationResponseModel> CompileQueryAsync(ConnectionModel connection,
            QueryCompilationRequestModel request, CancellationToken ct = default);

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
        /// Get history server capabilities
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel connection, CancellationToken ct = default);

        /// <summary>
        /// Get a node's history configuration
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            ConnectionModel connection, HistoryConfigurationRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Read node history with custom encoded extension object details
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel connection, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read history call with custom encoded extension object details
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Update using extension object details
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct = default);

        /// <summary>
        /// Disconnect connection using a connection handle that was returned
        /// by <see cref="ConnectAsync(ConnectionModel, ConnectRequestModel, CancellationToken)"/>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DisconnectAsync(ConnectionModel connection,
            DisconnectRequestModel request, CancellationToken ct = default);
    }
}
