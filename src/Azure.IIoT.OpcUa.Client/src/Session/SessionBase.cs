// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The client side interface with support for batching according
    /// to operation limits.
    /// </summary>
    public class SessionBase : Opc.Ua.Client.Obsolete.SessionClient
    {
        /// <summary>
        /// The operation limits are used to batch the service requests.
        /// </summary>
        public OperationLimits OperationLimits { get; } = new();

        /// <summary>
        /// Intializes the object with a channel and default operation limits.
        /// </summary>
        /// <param name="channel"></param>
        public SessionBase(ITransportChannel? channel = null) : base(channel)
        {
        }

        /// <inheritdoc/>
        public override async Task<BrowseResponse> BrowseAsync(RequestHeader? requestHeader,
            ViewDescription? view, uint requestedMaxReferencesPerNode,
            BrowseDescriptionCollection nodesToBrowse, CancellationToken ct)
        {
            BrowseResponse? response = null;

            var operationLimit = OperationLimits.MaxNodesPerBrowse;
            InitResponseCollections<BrowseResult, BrowseResultCollection>(
                out var results, out var diagnosticInfos, out var stringTable,
                nodesToBrowse.Count, operationLimit);
            foreach (var nodesToBrowseBatch in nodesToBrowse
                .Batch<BrowseDescription, BrowseDescriptionCollection>(operationLimit))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.BrowseAsync(requestHeader, view,
                    requestedMaxReferencesPerNode,
                    nodesToBrowseBatch, ct).ConfigureAwait(false);

                var batchResults = response.Results;
                var batchDiagnosticInfos = response.DiagnosticInfos;

                ClientBase.ValidateResponse(batchResults, nodesToBrowseBatch);
                ClientBase.ValidateDiagnosticInfos(batchDiagnosticInfos, nodesToBrowseBatch);

                AddResponses<BrowseResult, BrowseResultCollection>(
                    ref results, ref diagnosticInfos, ref stringTable, batchResults,
                    batchDiagnosticInfos, response.ResponseHeader.StringTable);
            }

            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.Results = results;
            response.DiagnosticInfos = diagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;

            return response;
        }

        /// <inheritdoc/>
        public override async Task<TranslateBrowsePathsToNodeIdsResponse> TranslateBrowsePathsToNodeIdsAsync(
            RequestHeader? requestHeader, BrowsePathCollection browsePaths, CancellationToken ct)
        {
            TranslateBrowsePathsToNodeIdsResponse? response = null;

            var operationLimit = OperationLimits.MaxNodesPerTranslateBrowsePathsToNodeIds;
            InitResponseCollections<BrowsePathResult, BrowsePathResultCollection>(
                out var results, out var diagnosticInfos, out var stringTable,
                browsePaths.Count, operationLimit);
            foreach (var batchBrowsePaths in browsePaths
                .Batch<BrowsePath, BrowsePathCollection>(operationLimit))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.TranslateBrowsePathsToNodeIdsAsync(
                    requestHeader,
                    batchBrowsePaths,
                    ct).ConfigureAwait(false);

                var batchResults = response.Results;
                var batchDiagnosticInfos = response.DiagnosticInfos;

                ClientBase.ValidateResponse(batchResults, batchBrowsePaths);
                ClientBase.ValidateDiagnosticInfos(batchDiagnosticInfos, batchBrowsePaths);

                AddResponses<BrowsePathResult, BrowsePathResultCollection>(
                    ref results, ref diagnosticInfos, ref stringTable, batchResults,
                    batchDiagnosticInfos, response.ResponseHeader.StringTable);
            }

            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.Results = results;
            response.DiagnosticInfos = diagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;

            return response;
        }

        /// <inheritdoc/>
        public override async Task<RegisterNodesResponse> RegisterNodesAsync(
            RequestHeader? requestHeader, NodeIdCollection nodesToRegister,
            CancellationToken ct)
        {
            RegisterNodesResponse? response = null;
            var registeredNodeIds = new NodeIdCollection();

            foreach (var batchNodesToRegister in nodesToRegister
                .Batch<NodeId, NodeIdCollection>(OperationLimits.MaxNodesPerRegisterNodes))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.RegisterNodesAsync(requestHeader,
                    batchNodesToRegister, ct).ConfigureAwait(false);

                var batchRegisteredNodeIds = response.RegisteredNodeIds;
                ClientBase.ValidateResponse(batchRegisteredNodeIds, batchNodesToRegister);
                registeredNodeIds.AddRange(batchRegisteredNodeIds);
            }

            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.RegisteredNodeIds = registeredNodeIds;
            return response;
        }

        /// <inheritdoc/>
        public override async Task<UnregisterNodesResponse> UnregisterNodesAsync(
            RequestHeader? requestHeader, NodeIdCollection nodesToUnregister,
            CancellationToken ct)
        {
            UnregisterNodesResponse? response = null;

            foreach (var batchNodesToUnregister in nodesToUnregister
                .Batch<NodeId, NodeIdCollection>(OperationLimits.MaxNodesPerRegisterNodes))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }
                response = await base.UnregisterNodesAsync(requestHeader,
                    batchNodesToUnregister, ct).ConfigureAwait(false);
            }
            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            return response;
        }

        /// <inheritdoc/>
        public override async Task<ReadResponse> ReadAsync(RequestHeader? requestHeader,
            double maxAge, TimestampsToReturn timestampsToReturn,
            ReadValueIdCollection nodesToRead, CancellationToken ct)
        {
            ReadResponse? response = null;

            var operationLimit = OperationLimits.MaxNodesPerRead;
            InitResponseCollections<DataValue, DataValueCollection>(
                out var results, out var diagnosticInfos, out var stringTable,
                nodesToRead.Count, operationLimit);
            foreach (var batchAttributesToRead in nodesToRead
                .Batch<ReadValueId, ReadValueIdCollection>(operationLimit))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.ReadAsync(requestHeader, maxAge, timestampsToReturn,
                    batchAttributesToRead, ct).ConfigureAwait(false);
                var batchResults = response.Results;
                var batchDiagnosticInfos = response.DiagnosticInfos;

                ClientBase.ValidateResponse(batchResults, batchAttributesToRead);
                ClientBase.ValidateDiagnosticInfos(batchDiagnosticInfos, batchAttributesToRead);

                AddResponses<DataValue, DataValueCollection>(
                    ref results, ref diagnosticInfos, ref stringTable, batchResults,
                    batchDiagnosticInfos, response.ResponseHeader.StringTable);
            }
            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.Results = results;
            response.DiagnosticInfos = diagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;

            return response;
        }

        /// <inheritdoc/>
        public override async Task<HistoryReadResponse> HistoryReadAsync(
            RequestHeader? requestHeader, ExtensionObject historyReadDetails,
            TimestampsToReturn timestampsToReturn, bool releaseContinuationPoints,
            HistoryReadValueIdCollection nodesToRead, CancellationToken ct)
        {
            HistoryReadResponse? response = null;

            var operationLimit = OperationLimits.MaxNodesPerHistoryReadData;
            if (historyReadDetails?.Body is ReadEventDetails)
            {
                operationLimit = OperationLimits.MaxNodesPerHistoryReadEvents;
            }

            InitResponseCollections<HistoryReadResult, HistoryReadResultCollection>(
                out var results, out var diagnosticInfos, out var stringTable,
                nodesToRead.Count, operationLimit);
            foreach (var batchNodesToRead in nodesToRead
                .Batch<HistoryReadValueId, HistoryReadValueIdCollection>(operationLimit))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.HistoryReadAsync(requestHeader,
                    historyReadDetails,
                    timestampsToReturn,
                    releaseContinuationPoints,
                    batchNodesToRead, ct).ConfigureAwait(false);

                var batchResults = response.Results;
                var batchDiagnosticInfos = response.DiagnosticInfos;

                ClientBase.ValidateResponse(batchResults, batchNodesToRead);
                ClientBase.ValidateDiagnosticInfos(batchDiagnosticInfos, batchNodesToRead);

                AddResponses<HistoryReadResult, HistoryReadResultCollection>(
                    ref results, ref diagnosticInfos, ref stringTable,
                    batchResults, batchDiagnosticInfos, response.ResponseHeader.StringTable);
            }

            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.Results = results;
            response.DiagnosticInfos = diagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;

            return response;
        }

        /// <inheritdoc/>
        public override async Task<WriteResponse> WriteAsync(RequestHeader? requestHeader,
            WriteValueCollection nodesToWrite, CancellationToken ct)
        {
            WriteResponse? response = null;

            var operationLimit = OperationLimits.MaxNodesPerWrite;
            InitResponseCollections<StatusCode, StatusCodeCollection>(
                out var results, out var diagnosticInfos, out var stringTable,
                nodesToWrite.Count, operationLimit);

            foreach (var batchNodesToWrite in nodesToWrite
                .Batch<WriteValue, WriteValueCollection>(operationLimit))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.WriteAsync(requestHeader,
                    batchNodesToWrite, ct).ConfigureAwait(false);

                var batchResults = response.Results;
                var batchDiagnosticInfos = response.DiagnosticInfos;

                ClientBase.ValidateResponse(batchResults, batchNodesToWrite);
                ClientBase.ValidateDiagnosticInfos(batchDiagnosticInfos, batchNodesToWrite);

                AddResponses<StatusCode, StatusCodeCollection>(
                    ref results, ref diagnosticInfos, ref stringTable, batchResults,
                    batchDiagnosticInfos, response.ResponseHeader.StringTable);
            }

            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.Results = results;
            response.DiagnosticInfos = diagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;

            return response;
        }

        /// <inheritdoc/>
        public override async Task<HistoryUpdateResponse> HistoryUpdateAsync(
            RequestHeader? requestHeader, ExtensionObjectCollection historyUpdateDetails,
            CancellationToken ct)
        {
            HistoryUpdateResponse? response = null;

            var operationLimit = OperationLimits.MaxNodesPerHistoryUpdateData;
            if (historyUpdateDetails.Count > 0 &&
                historyUpdateDetails[0].TypeId == DataTypeIds.UpdateEventDetails)
            {
                operationLimit = OperationLimits.MaxNodesPerHistoryUpdateEvents;
            }

            InitResponseCollections<HistoryUpdateResult, HistoryUpdateResultCollection>(
                out var results, out var diagnosticInfos, out var stringTable,
                historyUpdateDetails.Count, operationLimit);
            foreach (var batchHistoryUpdateDetails in historyUpdateDetails
                .Batch<ExtensionObject, ExtensionObjectCollection>(operationLimit))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.HistoryUpdateAsync(requestHeader,
                    batchHistoryUpdateDetails, ct).ConfigureAwait(false);
                var batchResults = response.Results;
                var batchDiagnosticInfos = response.DiagnosticInfos;

                ClientBase.ValidateResponse(batchResults, batchHistoryUpdateDetails);
                ClientBase.ValidateDiagnosticInfos(batchDiagnosticInfos, batchHistoryUpdateDetails);

                AddResponses<HistoryUpdateResult, HistoryUpdateResultCollection>(
                    ref results, ref diagnosticInfos, ref stringTable, batchResults,
                    batchDiagnosticInfos, response.ResponseHeader.StringTable);
            }
            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.Results = results;
            response.DiagnosticInfos = diagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;
            return response;
        }

        /// <inheritdoc/>
        public override async Task<CallResponse> CallAsync(RequestHeader? requestHeader,
            CallMethodRequestCollection methodsToCall, CancellationToken ct)
        {
            CallResponse? response = null;

            var operationLimit = OperationLimits.MaxNodesPerMethodCall;
            InitResponseCollections<CallMethodResult, CallMethodResultCollection>(
                out var results, out var diagnosticInfos, out var stringTable,
                methodsToCall.Count, operationLimit);
            foreach (var batchMethodsToCall in methodsToCall
                .Batch<CallMethodRequest, CallMethodRequestCollection>(operationLimit))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.CallAsync(requestHeader,
                    batchMethodsToCall, ct).ConfigureAwait(false);

                var batchResults = response.Results;
                var batchDiagnosticInfos = response.DiagnosticInfos;

                ClientBase.ValidateResponse(batchResults, batchMethodsToCall);
                ClientBase.ValidateDiagnosticInfos(batchDiagnosticInfos, batchMethodsToCall);

                AddResponses<CallMethodResult, CallMethodResultCollection>(
                    ref results, ref diagnosticInfos, ref stringTable, batchResults,
                    batchDiagnosticInfos, response.ResponseHeader.StringTable);
            }
            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.Results = results;
            response.DiagnosticInfos = diagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;

            return response;
        }

        /// <inheritdoc/>
        public override async Task<CreateMonitoredItemsResponse> CreateMonitoredItemsAsync(
            RequestHeader? requestHeader,
            uint subscriptionId,
            TimestampsToReturn timestampsToReturn,
            MonitoredItemCreateRequestCollection itemsToCreate,
            CancellationToken ct)
        {
            CreateMonitoredItemsResponse? response = null;

            var operationLimit = OperationLimits.MaxMonitoredItemsPerCall;
            InitResponseCollections<MonitoredItemCreateResult, MonitoredItemCreateResultCollection>(
                out var results, out var diagnosticInfos, out var stringTable,
                itemsToCreate.Count, operationLimit);
            foreach (var batchItemsToCreate in itemsToCreate
                .Batch<MonitoredItemCreateRequest, MonitoredItemCreateRequestCollection>(operationLimit))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.CreateMonitoredItemsAsync(requestHeader,
                    subscriptionId,
                    timestampsToReturn,
                    batchItemsToCreate, ct).ConfigureAwait(false);

                var batchResults = response.Results;
                var batchDiagnosticInfos = response.DiagnosticInfos;

                ClientBase.ValidateResponse(batchResults, batchItemsToCreate);
                ClientBase.ValidateDiagnosticInfos(batchDiagnosticInfos, batchItemsToCreate);

                AddResponses<MonitoredItemCreateResult, MonitoredItemCreateResultCollection>(
                    ref results, ref diagnosticInfos, ref stringTable, batchResults,
                    batchDiagnosticInfos, response.ResponseHeader.StringTable);
            }

            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.Results = results;
            response.DiagnosticInfos = diagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;

            return response;
        }

        /// <inheritdoc/>
        public override async Task<ModifyMonitoredItemsResponse> ModifyMonitoredItemsAsync(
            RequestHeader? requestHeader, uint subscriptionId, TimestampsToReturn timestampsToReturn,
            MonitoredItemModifyRequestCollection itemsToModify, CancellationToken ct)
        {
            ModifyMonitoredItemsResponse? response = null;
            var operationLimit = OperationLimits.MaxMonitoredItemsPerCall;
            InitResponseCollections<MonitoredItemModifyResult, MonitoredItemModifyResultCollection>(
                out var results, out var diagnosticInfos, out var stringTable,
                itemsToModify.Count, operationLimit);
            foreach (var batchItemsToModify in itemsToModify
                .Batch<MonitoredItemModifyRequest, MonitoredItemModifyRequestCollection>(operationLimit))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.ModifyMonitoredItemsAsync(requestHeader,
                    subscriptionId,
                    timestampsToReturn,
                    batchItemsToModify, ct).ConfigureAwait(false);

                var batchResults = response.Results;
                var batchDiagnosticInfos = response.DiagnosticInfos;

                ClientBase.ValidateResponse(batchResults, batchItemsToModify);
                ClientBase.ValidateDiagnosticInfos(batchDiagnosticInfos, batchItemsToModify);

                AddResponses<MonitoredItemModifyResult, MonitoredItemModifyResultCollection>(
                    ref results, ref diagnosticInfos, ref stringTable, batchResults,
                    batchDiagnosticInfos, response.ResponseHeader.StringTable);
            }

            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.Results = results;
            response.DiagnosticInfos = diagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;

            return response;
        }

        /// <inheritdoc/>
        public override async Task<SetMonitoringModeResponse> SetMonitoringModeAsync(
            RequestHeader? requestHeader, uint subscriptionId, MonitoringMode monitoringMode,
            UInt32Collection monitoredItemIds, CancellationToken ct)
        {
            SetMonitoringModeResponse? response = null;

            var operationLimit = OperationLimits.MaxMonitoredItemsPerCall;
            InitResponseCollections<StatusCode, StatusCodeCollection>(
                out var results, out var diagnosticInfos, out var stringTable,
                monitoredItemIds.Count, operationLimit);
            foreach (var batchMonitoredItemIds in monitoredItemIds
                .Batch<uint, UInt32Collection>(operationLimit))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.SetMonitoringModeAsync(requestHeader,
                    subscriptionId,
                    monitoringMode,
                    batchMonitoredItemIds, ct).ConfigureAwait(false);

                var batchResults = response.Results;
                var batchDiagnosticInfos = response.DiagnosticInfos;

                ClientBase.ValidateResponse(batchResults, batchMonitoredItemIds);
                ClientBase.ValidateDiagnosticInfos(batchDiagnosticInfos, batchMonitoredItemIds);

                AddResponses<StatusCode, StatusCodeCollection>(
                    ref results, ref diagnosticInfos, ref stringTable, batchResults,
                    batchDiagnosticInfos, response.ResponseHeader.StringTable);
            }
            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.Results = results;
            response.DiagnosticInfos = diagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;

            return response;
        }

        /// <inheritdoc/>
        public override async Task<SetTriggeringResponse> SetTriggeringAsync(
            RequestHeader? requestHeader, uint subscriptionId, uint triggeringItemId,
            UInt32Collection linksToAdd, UInt32Collection linksToRemove, CancellationToken ct)
        {
            SetTriggeringResponse? response = null;

            var operationLimit = OperationLimits.MaxMonitoredItemsPerCall;
            InitResponseCollections<StatusCode, StatusCodeCollection>(
                out var addResults, out var addDiagnosticInfos, out var stringTable,
                linksToAdd.Count, operationLimit);
            InitResponseCollections<StatusCode, StatusCodeCollection>(
                out var removeResults, out var removeDiagnosticInfos, out _,
                linksToRemove.Count, operationLimit);

            foreach (var batchLinksToAdd in linksToAdd
                .Batch<uint, UInt32Collection>(operationLimit))
            {
                UInt32Collection batchLinksToRemove;
                if (operationLimit == 0)
                {
                    batchLinksToRemove = linksToRemove;
                    linksToRemove = new UInt32Collection();
                }
                else if (batchLinksToAdd.Count < operationLimit)
                {
                    batchLinksToRemove = new UInt32Collection(
                        linksToRemove.Take((int)operationLimit - batchLinksToAdd.Count));
                    linksToRemove = new UInt32Collection(
                        linksToRemove.Skip(batchLinksToRemove.Count));
                }
                else
                {
                    batchLinksToRemove = new UInt32Collection();
                }

                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.SetTriggeringAsync(requestHeader,
                    subscriptionId,
                    triggeringItemId,
                    batchLinksToAdd,
                    batchLinksToRemove,
                    ct).ConfigureAwait(false);

                var batchAddResults = response.AddResults;
                var batchAddDiagnosticInfos = response.AddDiagnosticInfos;
                var batchRemoveResults = response.RemoveResults;
                var batchRemoveDiagnosticInfos = response.RemoveDiagnosticInfos;

                ClientBase.ValidateResponse(batchAddResults, batchLinksToAdd);
                ClientBase.ValidateDiagnosticInfos(batchAddDiagnosticInfos, batchLinksToAdd);
                ClientBase.ValidateResponse(batchRemoveResults, batchLinksToRemove);
                ClientBase.ValidateDiagnosticInfos(batchRemoveDiagnosticInfos, batchLinksToRemove);

                AddResponses<StatusCode, StatusCodeCollection>(
                    ref addResults, ref addDiagnosticInfos, ref stringTable,
                    batchAddResults, batchAddDiagnosticInfos,
                    response.ResponseHeader.StringTable);

                AddResponses<StatusCode, StatusCodeCollection>(
                    ref removeResults, ref removeDiagnosticInfos, ref stringTable,
                    batchRemoveResults, batchRemoveDiagnosticInfos,
                    response.ResponseHeader.StringTable);
            }

            if (linksToRemove.Count > 0)
            {
                foreach (var batchLinksToRemove in linksToRemove
                    .Batch<uint, UInt32Collection>(operationLimit))
                {
                    if (requestHeader != null)
                    {
                        requestHeader.RequestHandle = 0;
                    }

                    var batchLinksToAdd = new UInt32Collection();
                    response = await base.SetTriggeringAsync(requestHeader,
                        subscriptionId,
                        triggeringItemId,
                        batchLinksToAdd,
                        batchLinksToRemove,
                        ct).ConfigureAwait(false);

                    var batchAddResults = response.AddResults;
                    var batchAddDiagnosticInfos = response.AddDiagnosticInfos;
                    var batchRemoveResults = response.RemoveResults;
                    var batchRemoveDiagnosticInfos = response.RemoveDiagnosticInfos;

                    ClientBase.ValidateResponse(batchAddResults, batchLinksToAdd);
                    ClientBase.ValidateDiagnosticInfos(batchAddDiagnosticInfos, batchLinksToAdd);
                    ClientBase.ValidateResponse(batchRemoveResults, batchLinksToRemove);
                    ClientBase.ValidateDiagnosticInfos(batchRemoveDiagnosticInfos, batchLinksToRemove);

                    AddResponses<StatusCode, StatusCodeCollection>(
                        ref addResults, ref addDiagnosticInfos, ref stringTable,
                        batchAddResults, batchAddDiagnosticInfos, response.ResponseHeader.StringTable);

                    AddResponses<StatusCode, StatusCodeCollection>(
                        ref removeResults, ref removeDiagnosticInfos, ref stringTable,
                        batchRemoveResults, batchRemoveDiagnosticInfos,
                        response.ResponseHeader.StringTable);
                }
            }
            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.AddResults = addResults;
            response.AddDiagnosticInfos = addDiagnosticInfos;
            response.RemoveResults = removeResults;
            response.RemoveDiagnosticInfos = removeDiagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;
            return response;
        }

        /// <inheritdoc/>
        public override async Task<DeleteMonitoredItemsResponse> DeleteMonitoredItemsAsync(
            RequestHeader? requestHeader, uint subscriptionId, UInt32Collection monitoredItemIds,
            CancellationToken ct)
        {
            DeleteMonitoredItemsResponse? response = null;

            var operationLimit = OperationLimits.MaxMonitoredItemsPerCall;
            InitResponseCollections<StatusCode, StatusCodeCollection>(
                out var results, out var diagnosticInfos, out var stringTable,
                monitoredItemIds.Count, operationLimit);

            foreach (var batchMonitoredItemIds in monitoredItemIds
                .Batch<uint, UInt32Collection>(operationLimit))
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                response = await base.DeleteMonitoredItemsAsync(requestHeader,
                    subscriptionId, batchMonitoredItemIds, ct).ConfigureAwait(false);
                var batchResults = response.Results;
                var batchDiagnosticInfos = response.DiagnosticInfos;

                ClientBase.ValidateResponse(batchResults, batchMonitoredItemIds);
                ClientBase.ValidateDiagnosticInfos(batchDiagnosticInfos, batchMonitoredItemIds);

                AddResponses<StatusCode, StatusCodeCollection>(
                    ref results, ref diagnosticInfos, ref stringTable, batchResults,
                    batchDiagnosticInfos, response.ResponseHeader.StringTable);
            }

            if (response == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadNothingToDo, "Nothing to do");
            }
            response.Results = results;
            response.DiagnosticInfos = diagnosticInfos;
            response.ResponseHeader.StringTable = stringTable;

            return response;
        }

        /// <summary>
        /// Initialize the collections for a service call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="stringTable"></param>
        /// <param name="count"></param>
        /// <param name="operationLimit"></param>
        /// <remarks>
        /// Preset the result collections with null if the operation limit
        /// is sufficient or with the final size if batching is necessary.
        /// </remarks>
        private static void InitResponseCollections<T, C>(out C results,
            out DiagnosticInfoCollection diagnosticInfos,
            out StringCollection stringTable, int count, uint operationLimit)
            where C : List<T>, new()
        {
            if (count <= operationLimit)
            {
                results = new C();
                diagnosticInfos = new DiagnosticInfoCollection();
            }
            else
            {
                results = new C() { Capacity = count };
                diagnosticInfos = new DiagnosticInfoCollection(count);
            }
            stringTable = new StringCollection();
        }

        /// <summary>
        /// Add the result of a batched service call to the results.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="stringTable"></param>
        /// <param name="batchedResults"></param>
        /// <param name="batchedDiagnosticInfos"></param>
        /// <param name="batchedStringTable"></param>
        /// <remarks>
        /// Assigns the batched collection result to the result if the result
        /// collection is not initialized, adds the range to the result
        /// collections otherwise.
        /// The string table indexes are updated in the diagnostic infos if necessary.
        /// </remarks>
        private static void AddResponses<T, C>(ref C results,
            ref DiagnosticInfoCollection diagnosticInfos, ref StringCollection stringTable,
            C batchedResults, DiagnosticInfoCollection batchedDiagnosticInfos,
            StringCollection batchedStringTable) where C : List<T>
        {
            var hasDiagnosticInfos = diagnosticInfos.Count > 0;
            var hasEmptyDiagnosticInfos = diagnosticInfos.Count == 0 && results.Count > 0;
            var hasBatchDiagnosticInfos = batchedDiagnosticInfos.Count > 0;
            var correctionCount = 0;
            if (hasBatchDiagnosticInfos && hasEmptyDiagnosticInfos)
            {
                correctionCount = results.Count;
            }
            else if (!hasBatchDiagnosticInfos && hasDiagnosticInfos)
            {
                correctionCount = batchedResults.Count;
            }
            if (correctionCount > 0)
            {
                // fill missing diagnostics infos with null entries
                for (var i = 0; i < correctionCount; i++)
                {
                    diagnosticInfos.Add(null);
                }
            }
            else if (batchedStringTable.Count > 0)
            {
                // correct indexes in the string table
                var stringTableOffset = stringTable.Count;
                foreach (var diagnosticInfo in batchedDiagnosticInfos)
                {
                    UpdateDiagnosticInfoIndexes(diagnosticInfo, stringTableOffset);
                }
            }
            results.AddRange(batchedResults);
            diagnosticInfos.AddRange(batchedDiagnosticInfos);
            stringTable.AddRange(batchedStringTable);
        }

        private static void UpdateDiagnosticInfoIndexes(DiagnosticInfo diagnosticInfo,
            int stringTableOffset)
        {
            var depth = 0;
            while (diagnosticInfo != null && depth++ < DiagnosticInfo.MaxInnerDepth)
            {
                if (diagnosticInfo.LocalizedText >= 0)
                {
                    diagnosticInfo.LocalizedText += stringTableOffset;
                }
                if (diagnosticInfo.Locale >= 0)
                {
                    diagnosticInfo.Locale += stringTableOffset;
                }
                if (diagnosticInfo.NamespaceUri >= 0)
                {
                    diagnosticInfo.NamespaceUri += stringTableOffset;
                }
                if (diagnosticInfo.SymbolicId >= 0)
                {
                    diagnosticInfo.SymbolicId += stringTableOffset;
                }
                diagnosticInfo = diagnosticInfo.InnerDiagnosticInfo;
            }
        }
    }
}
