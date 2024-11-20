// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Opc.Ua;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Services supported by a session
    /// </summary>
    public interface ISessionServiceSets
    {
        /// <summary>
        /// Browse service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="requestedMaxReferencesPerNode"></param>
        /// <param name="nodesToBrowse"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseResponse> BrowseAsync(RequestHeader? requestHeader,
            ViewDescription? view, uint requestedMaxReferencesPerNode,
            BrowseDescriptionCollection nodesToBrowse, CancellationToken ct);

        /// <summary>
        /// Browse next service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="continuationPoints"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseNextResponse> BrowseNextAsync(RequestHeader? requestHeader,
            bool releaseContinuationPoints, ByteStringCollection continuationPoints,
            CancellationToken ct);

        /// <summary>
        /// Translate browse path service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="browsePaths"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TranslateBrowsePathsToNodeIdsResponse> TranslateBrowsePathsToNodeIdsAsync(
            RequestHeader? requestHeader, BrowsePathCollection browsePaths,
            CancellationToken ct);

        /// <summary>
        /// Call service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="methodsToCall"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<CallResponse> CallAsync(RequestHeader? requestHeader,
            CallMethodRequestCollection methodsToCall, CancellationToken ct);

        /// <summary>
        /// Cancel service call
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="requestHandle"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<CancelResponse> CancelAsync(RequestHeader? requestHeader,
            uint requestHandle, CancellationToken ct);

        /// <summary>
        /// Read service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="maxAge"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ReadResponse> ReadAsync(RequestHeader? requestHeader, double maxAge,
            TimestampsToReturn timestampsToReturn, ReadValueIdCollection nodesToRead,
            CancellationToken ct);

        /// <summary>
        /// Write service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToWrite"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WriteResponse> WriteAsync(RequestHeader? requestHeader,
            WriteValueCollection nodesToWrite, CancellationToken ct);

        /// <summary>
        /// History read service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="historyReadDetails"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponse> HistoryReadAsync(RequestHeader? requestHeader,
            ExtensionObject historyReadDetails, TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints, HistoryReadValueIdCollection nodesToRead,
            CancellationToken ct);

        /// <summary>
        /// History update service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="historyUpdateDetails"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponse> HistoryUpdateAsync(RequestHeader? requestHeader,
            ExtensionObjectCollection historyUpdateDetails, CancellationToken ct);

        /// <summary>
        /// Register nodes service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToRegister"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<RegisterNodesResponse> RegisterNodesAsync(RequestHeader? requestHeader,
            NodeIdCollection nodesToRegister, CancellationToken ct);

        /// <summary>
        /// Unregister nodes service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToUnregister"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<UnregisterNodesResponse> UnregisterNodesAsync(RequestHeader? requestHeader,
            NodeIdCollection nodesToUnregister, CancellationToken ct);

        /// <summary>
        /// Add nodes service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToAdd"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<AddNodesResponse> AddNodesAsync(RequestHeader? requestHeader,
            AddNodesItemCollection nodesToAdd, CancellationToken ct);

        /// <summary>
        /// Add references service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="referencesToAdd"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<AddReferencesResponse> AddReferencesAsync(RequestHeader? requestHeader,
            AddReferencesItemCollection referencesToAdd, CancellationToken ct);

        /// <summary>
        /// Delete nodes service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToDelete"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DeleteNodesResponse> DeleteNodesAsync(RequestHeader? requestHeader,
            DeleteNodesItemCollection nodesToDelete, CancellationToken ct);

        /// <summary>
        /// Delete references service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="referencesToDelete"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DeleteReferencesResponse> DeleteReferencesAsync(
            RequestHeader? requestHeader, DeleteReferencesItemCollection referencesToDelete,
            CancellationToken ct);

        /// <summary>
        /// Set triggering --- TODO: Add to subscription interface
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="triggeringItemId"></param>
        /// <param name="linksToAdd"></param>
        /// <param name="linksToRemove"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SetTriggeringResponse> SetTriggeringAsync(RequestHeader? requestHeader,
            uint subscriptionId, uint triggeringItemId, UInt32Collection linksToAdd,
            UInt32Collection linksToRemove, CancellationToken ct);
    }
}
