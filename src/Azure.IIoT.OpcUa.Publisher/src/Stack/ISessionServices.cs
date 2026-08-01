// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Opc.Ua;
    using System.Threading;
    using System.Threading.Tasks;

    using System.Collections.Generic;
    /// <summary>
    /// Session services provided by the client and referenced from the
    /// session handle.
    /// </summary>
    public interface ISessionServices
    {
        /// <summary>
        /// Add nodes
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToAdd"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<AddNodesResponse> AddNodesAsync(RequestHeader requestHeader,
            List<AddNodesItem> nodesToAdd, CancellationToken ct);

        /// <summary>
        /// Add references
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="referencesToAdd"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<AddReferencesResponse> AddReferencesAsync(RequestHeader requestHeader,
            List<AddReferencesItem> referencesToAdd, CancellationToken ct);

        /// <summary>
        /// Browse first
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="requestedMaxReferencesPerNode"></param>
        /// <param name="nodesToBrowse"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<BrowseResponse> BrowseAsync(RequestHeader requestHeader,
            ViewDescription? view, uint requestedMaxReferencesPerNode,
            List<BrowseDescription> nodesToBrowse, CancellationToken ct);

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="continuationPoints"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<BrowseNextResponse> BrowseNextAsync(RequestHeader requestHeader,
            bool releaseContinuationPoints, List<ByteString> continuationPoints,
            CancellationToken ct);

        /// <summary>
        /// Call
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="methodsToCall"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<CallResponse> CallAsync(RequestHeader requestHeader,
            List<CallMethodRequest> methodsToCall, CancellationToken ct);

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToDelete"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<DeleteNodesResponse> DeleteNodesAsync(RequestHeader requestHeader,
            List<DeleteNodesItem> nodesToDelete, CancellationToken ct);

        /// <summary>
        /// Delete references
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="referencesToDelete"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<DeleteReferencesResponse> DeleteReferencesAsync(RequestHeader requestHeader,
            List<DeleteReferencesItem> referencesToDelete, CancellationToken ct);

        /// <summary>
        /// Read history
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="historyReadDetails"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<HistoryReadResponse> HistoryReadAsync(RequestHeader requestHeader,
            ExtensionObject? historyReadDetails, TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints, List<HistoryReadValueId> nodesToRead,
            CancellationToken ct);

        /// <summary>
        /// History update
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="historyUpdateDetails"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<HistoryUpdateResponse> HistoryUpdateAsync(RequestHeader requestHeader,
            List<ExtensionObject> historyUpdateDetails, CancellationToken ct);

        /// <summary>
        /// Query first
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="nodeTypes"></param>
        /// <param name="filter"></param>
        /// <param name="maxDataSetsToReturn"></param>
        /// <param name="maxReferencesToReturn"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<QueryFirstResponse> QueryFirstAsync(RequestHeader requestHeader,
            ViewDescription view, List<NodeTypeDescription> nodeTypes, ContentFilter filter,
            uint maxDataSetsToReturn, uint maxReferencesToReturn, CancellationToken ct);

        /// <summary>
        /// Query next
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="releaseContinuationPoint"></param>
        /// <param name="continuationPoint"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<QueryNextResponse> QueryNextAsync(RequestHeader requestHeader,
            bool releaseContinuationPoint, byte[] continuationPoint, CancellationToken ct);

        /// <summary>
        /// Read node
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="maxAge"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ReadResponse> ReadAsync(RequestHeader requestHeader, double maxAge,
            TimestampsToReturn timestampsToReturn, List<ReadValueId> nodesToRead,
            CancellationToken ct);

        /// <summary>
        /// Register nodes
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToRegister"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<RegisterNodesResponse> RegisterNodesAsync(RequestHeader requestHeader,
            List<NodeId> nodesToRegister, CancellationToken ct);

        /// <summary>
        /// Unregister nodes
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToUnregister"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<UnregisterNodesResponse> UnregisterNodesAsync(RequestHeader requestHeader,
            List<NodeId> nodesToUnregister, CancellationToken ct);

        /// <summary>
        /// Translate browse paths
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="browsePaths"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<TranslateBrowsePathsToNodeIdsResponse> TranslateBrowsePathsToNodeIdsAsync(
            RequestHeader requestHeader, List<BrowsePath> browsePaths,
            CancellationToken ct);

        /// <summary>
        /// Write to node
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToWrite"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<WriteResponse> WriteAsync(RequestHeader requestHeader,
            List<WriteValue> nodesToWrite, CancellationToken ct);
    }
}
