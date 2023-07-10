// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// OPC UA Client based on official ua client reference sample.
    /// </summary>
    internal sealed class OpcUaSession : IOpcUaSession, ISessionServices,
        ISessionAccessor, IDisposable
    {
        /// <inheritdoc/>
        public IVariantEncoder Codec { get; }

        /// <inheritdoc/>
        public ISessionServices Services => this;

        /// <inheritdoc/>
        public ITypeTable TypeTree => Session.TypeTree;

        /// <inheritdoc/>
        public INodeCache NodeCache => Session.NodeCache;

        /// <inheritdoc/>
        public IServiceMessageContext MessageContext => Session.MessageContext;

        /// <inheritdoc/>
        public ISystemContext SystemContext => Session.SystemContext;

        /// <summary>
        /// The underlying session
        /// </summary>
        internal ISession Session { get; }

        /// <summary>
        /// Type system has loaded
        /// </summary>
        internal bool IsTypeSystemLoaded => _complexTypeSystem?.IsCompleted ?? false;

        /// <summary>
        /// Create session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="keepAlive"></param>
        /// <param name="keepAliveInterval"></param>
        /// <param name="operationTimeout"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="errorHandler"></param>
        /// <param name="ackHandler"></param>
        /// <param name="preloadComplexTypeSystem"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public OpcUaSession(ISession session, KeepAliveEventHandler keepAlive,
            TimeSpan keepAliveInterval, TimeSpan operationTimeout,
            IJsonSerializer serializer, ILogger<OpcUaSession> logger,
            PublishErrorEventHandler? errorHandler = null,
            PublishSequenceNumbersToAcknowledgeEventHandler? ackHandler = null,
            bool preloadComplexTypeSystem = true)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _keepAlive = keepAlive ??
                throw new ArgumentNullException(nameof(keepAlive));
            Session = session ??
                throw new ArgumentNullException(nameof(session));

            // support transfer
            Session.DeleteSubscriptionsOnClose = false;
            Session.TransferSubscriptionsOnReconnect = true;
            Session.MinPublishRequestCount = 3;
            Session.KeepAliveInterval = (int)keepAliveInterval.TotalMilliseconds;
            Session.OperationTimeout = (int)operationTimeout.TotalMilliseconds;

            _authenticationToken = (NodeId?)typeof(ClientBase).GetProperty(
                "AuthenticationToken",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)?.GetValue(session)
                ?? NodeId.Null;

            Codec = new JsonVariantEncoder(session.MessageContext, serializer);
            if (errorHandler != null)
            {
                Session.PublishError += errorHandler;
                _errorHandler = errorHandler;
            }
            if (ackHandler != null)
            {
                Session.PublishSequenceNumbersToAcknowledge += ackHandler;
                _ackHandler = ackHandler;
            }
            Session.KeepAlive += keepAlive;

            _cts = new CancellationTokenSource();
            _complexTypeSystem = preloadComplexTypeSystem ?
                LoadComplexTypeSystemAsync() : null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _cts.Cancel();
                Session.KeepAlive -= _keepAlive;
                if (_ackHandler != null)
                {
                    Session.PublishSequenceNumbersToAcknowledge -= _ackHandler;
                }
                if (_errorHandler != null)
                {
                    Session.PublishError -= _errorHandler;
                }

                Session.Dispose();
                _logger.LogDebug("Session {Name} disposed.", Session.SessionName);
            }
            finally
            {
                _activitySource.Dispose();
                _cts.Dispose();
            }
        }

        /// <inheritdoc/>
        public bool TryGetSession([NotNullWhen(true)] out ISession? session)
        {
            session = Session;
            return true;
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Session.SessionName;
        }

        /// <inheritdoc/>
        public async ValueTask<OperationLimitsModel> GetOperationLimitsAsync(
            CancellationToken ct = default)
        {
            if (_limits != null)
            {
                return _limits;
            }
            _limits = await FetchOperationLimitsAsync(new RequestHeader(),
                ct).ConfigureAwait(false);
            return _limits ?? new OperationLimitsModel();
        }

        /// <inheritdoc/>
        public async ValueTask<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            NamespaceFormat namespaceFormat, CancellationToken ct = default)
        {
            if (_server != null && namespaceFormat == NamespaceFormat.Uri)
            {
                return _server;
            }
            if (_limits == null)
            {
                _limits = await FetchOperationLimitsAsync(new RequestHeader(),
                    ct).ConfigureAwait(false);
            }
            var server = await FetchServerCapabilitiesAsync(new RequestHeader(),
                namespaceFormat, ct).ConfigureAwait(false);
            if (namespaceFormat == NamespaceFormat.Uri)
            {
                _server = server;
            }
            return server ?? new ServerCapabilitiesModel
            {
                OperationLimits = _limits ?? new OperationLimitsModel()
            };
        }

        /// <inheritdoc/>
        public async ValueTask<HistoryServerCapabilitiesModel> GetHistoryCapabilitiesAsync(
            NamespaceFormat namespaceFormat, CancellationToken ct = default)
        {
            if (_history != null && namespaceFormat == NamespaceFormat.Uri)
            {
                return _history;
            }
            var history = await FetchHistoryCapabilitiesAsync(new RequestHeader(),
                namespaceFormat, ct).ConfigureAwait(false);
            if (namespaceFormat == NamespaceFormat.Uri)
            {
                _history = history;
            }
            return history ?? new HistoryServerCapabilitiesModel();
        }

        /// <inheritdoc/>
        public async ValueTask<ComplexTypeSystem?> GetComplexTypeSystemAsync(CancellationToken ct)
        {
            try
            {
                Debug.Assert(_complexTypeSystem != null);
                return await _complexTypeSystem.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Throw any cancellation token exception
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to get complex type system for client {Client}.", this);

                // Try again. TODO: Throttle using a timer or so...
                _complexTypeSystem = LoadComplexTypeSystemAsync();
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<AddNodesResponse> AddNodesAsync(RequestHeader requestHeader,
            AddNodesItemCollection nodesToAdd, CancellationToken ct)
        {
            using var activity = Begin<AddNodesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new AddNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToAdd = nodesToAdd
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<AddReferencesResponse> AddReferencesAsync(
            RequestHeader requestHeader, AddReferencesItemCollection referencesToAdd,
            CancellationToken ct)
        {
            using var activity = Begin<AddReferencesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new AddReferencesRequest
            {
                RequestHeader = requestHeader,
                ReferencesToAdd = referencesToAdd
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<DeleteNodesResponse> DeleteNodesAsync(
            RequestHeader requestHeader, DeleteNodesItemCollection nodesToDelete,
            CancellationToken ct)
        {
            using var activity = Begin<DeleteNodesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new DeleteNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToDelete = nodesToDelete
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<DeleteReferencesResponse> DeleteReferencesAsync(
            RequestHeader requestHeader, DeleteReferencesItemCollection referencesToDelete,
            CancellationToken ct)
        {
            using var activity = Begin<DeleteReferencesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new DeleteReferencesRequest
            {
                RequestHeader = requestHeader,
                ReferencesToDelete = referencesToDelete
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<BrowseResponse> BrowseAsync(
            RequestHeader requestHeader, ViewDescription? view,
            uint requestedMaxReferencesPerNode,
            BrowseDescriptionCollection nodesToBrowse, CancellationToken ct)
        {
            using var activity = Begin<BrowseResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new BrowseRequest
            {
                RequestHeader = requestHeader,
                View = view,
                RequestedMaxReferencesPerNode = requestedMaxReferencesPerNode,
                NodesToBrowse = nodesToBrowse
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponse> BrowseNextAsync(
            RequestHeader requestHeader, bool releaseContinuationPoints,
            ByteStringCollection continuationPoints, CancellationToken ct)
        {
            using var activity = Begin<BrowseNextResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new BrowseNextRequest
            {
                RequestHeader = requestHeader,
                ReleaseContinuationPoints = releaseContinuationPoints,
                ContinuationPoints = continuationPoints
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<TranslateBrowsePathsToNodeIdsResponse> TranslateBrowsePathsToNodeIdsAsync(
            RequestHeader requestHeader, BrowsePathCollection browsePaths,
            CancellationToken ct)
        {
            using var activity = Begin<TranslateBrowsePathsToNodeIdsResponse>(
                requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new TranslateBrowsePathsToNodeIdsRequest
            {
                RequestHeader = requestHeader,
                BrowsePaths = browsePaths
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<RegisterNodesResponse> RegisterNodesAsync(
            RequestHeader requestHeader, NodeIdCollection nodesToRegister,
            CancellationToken ct)
        {
            using var activity = Begin<RegisterNodesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new RegisterNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToRegister = nodesToRegister
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<UnregisterNodesResponse> UnregisterNodesAsync(
            RequestHeader requestHeader, NodeIdCollection nodesToUnregister,
            CancellationToken ct)
        {
            using var activity = Begin<UnregisterNodesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new UnregisterNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToUnregister = nodesToUnregister
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<QueryFirstResponse> QueryFirstAsync(
            RequestHeader requestHeader, ViewDescription view,
            NodeTypeDescriptionCollection nodeTypes, ContentFilter filter,
            uint maxDataSetsToReturn, uint maxReferencesToReturn,
            CancellationToken ct)
        {
            using var activity = Begin<QueryFirstResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new QueryFirstRequest
            {
                RequestHeader = requestHeader,
                View = view,
                NodeTypes = nodeTypes,
                Filter = filter,
                MaxDataSetsToReturn = maxDataSetsToReturn,
                MaxReferencesToReturn = maxReferencesToReturn
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<QueryNextResponse> QueryNextAsync(
            RequestHeader requestHeader, bool releaseContinuationPoint,
            byte[] continuationPoint, CancellationToken ct)
        {
            using var activity = Begin<QueryNextResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new QueryNextRequest
            {
                RequestHeader = requestHeader,
                ReleaseContinuationPoint = releaseContinuationPoint,
                ContinuationPoint = continuationPoint
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<ReadResponse> ReadAsync(RequestHeader requestHeader,
            double maxAge, Opc.Ua.TimestampsToReturn timestampsToReturn,
            ReadValueIdCollection nodesToRead, CancellationToken ct)
        {
            using var activity = Begin<ReadResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new ReadRequest
            {
                RequestHeader = requestHeader,
                MaxAge = maxAge,
                TimestampsToReturn = timestampsToReturn,
                NodesToRead = nodesToRead
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponse> HistoryReadAsync(
            RequestHeader requestHeader, ExtensionObject? historyReadDetails,
            Opc.Ua.TimestampsToReturn timestampsToReturn, bool releaseContinuationPoints,
            HistoryReadValueIdCollection nodesToRead, CancellationToken ct)
        {
            using var activity = Begin<HistoryReadResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new HistoryReadRequest
            {
                RequestHeader = requestHeader,
                HistoryReadDetails = historyReadDetails,
                TimestampsToReturn = timestampsToReturn,
                ReleaseContinuationPoints = releaseContinuationPoints,
                NodesToRead = nodesToRead
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<WriteResponse> WriteAsync(RequestHeader requestHeader,
            WriteValueCollection nodesToWrite, CancellationToken ct)
        {
            using var activity = Begin<WriteResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new WriteRequest
            {
                RequestHeader = requestHeader,
                NodesToWrite = nodesToWrite
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponse> HistoryUpdateAsync(
            RequestHeader requestHeader, ExtensionObjectCollection historyUpdateDetails,
            CancellationToken ct)
        {
            using var activity = Begin<HistoryUpdateResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new HistoryUpdateRequest
            {
                RequestHeader = requestHeader,
                HistoryUpdateDetails = historyUpdateDetails
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async Task<CallResponse> CallAsync(RequestHeader requestHeader,
            CallMethodRequestCollection methodsToCall, CancellationToken ct)
        {
            using var activity = Begin<CallResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new CallRequest
            {
                RequestHeader = requestHeader,
                MethodsToCall = methodsToCall
            };
            var response = await Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        public async ValueTask CloseAsync(CancellationToken ct)
        {
            try
            {
                await Session.CloseAsync(ct).ConfigureAwait(false);

                _logger.LogDebug("Successfully closed session {Session}.", this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close session {Session}.", this);
            }
        }

        /// <summary>
        /// Read operation limits
        /// </summary>
        /// <param name="header"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<OperationLimitsModel?> FetchOperationLimitsAsync(RequestHeader header,
            CancellationToken ct)
        {
            // Fetch limits into the session using the new api
            var maxNodesPerRead = Validate32(Session.OperationLimits.MaxNodesPerRead);

            // Read once more to ensure we have all we need and also correctly show what is not provided.
            var nodes = new[] {
                Variables.Server_ServerCapabilities_MaxArrayLength,
                Variables.Server_ServerCapabilities_MaxBrowseContinuationPoints,
                Variables.Server_ServerCapabilities_MaxByteStringLength,
                Variables.Server_ServerCapabilities_MaxHistoryContinuationPoints,
                Variables.Server_ServerCapabilities_MaxQueryContinuationPoints,
                Variables.Server_ServerCapabilities_MaxStringLength,
                Variables.Server_ServerCapabilities_MinSupportedSampleRate,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryReadData,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryReadEvents,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerWrite,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryUpdateData,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryUpdateEvents,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerMethodCall,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerBrowse,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerRegisterNodes,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerTranslateBrowsePathsToNodeIds,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerNodeManagement,
                Variables.Server_ServerCapabilities_OperationLimits_MaxMonitoredItemsPerCall
            };

            var values = Enumerable.Empty<DataValue>();
            foreach (var chunk in nodes.Batch(Math.Max(1, (int)(maxNodesPerRead ?? 0))))
            {
                // Group the reads
                var requests = new ReadValueIdCollection(chunk
                    .Select(n => new ReadValueId
                    {
                        NodeId = n,
                        AttributeId = Attributes.Value
                    }));
                var response = await Session.ReadAsync(header, 0,
                    Opc.Ua.TimestampsToReturn.Both, requests, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, d => d.StatusCode,
                    response.DiagnosticInfos, requests);
                results.ThrowIfError();
                values = values.Concat(results.Select(r => r.Result));
            }

            var value = values.ToList();
            return new OperationLimitsModel
            {
                MaxArrayLength =
                    Validate32(value[0].GetValueOrDefault<uint?>()),
                MaxBrowseContinuationPoints =
                    Validate16(value[1].GetValueOrDefault<ushort?>()),
                MaxByteStringLength =
                    Validate32(value[2].GetValueOrDefault<uint?>()),
                MaxHistoryContinuationPoints =
                    Validate16(value[3].GetValueOrDefault<ushort?>()),
                MaxQueryContinuationPoints =
                    Validate16(value[4].GetValueOrDefault<ushort?>()),
                MaxStringLength =
                    Validate32(value[5].GetValueOrDefault<uint?>()),
                MinSupportedSampleRate =
                    value[6].GetValueOrDefault<double?>(),
                MaxNodesPerHistoryReadData =
                    Validate32(value[7].GetValueOrDefault<uint?>()),
                MaxNodesPerHistoryReadEvents =
                    Validate32(value[8].GetValueOrDefault<uint?>()),
                MaxNodesPerWrite =
                    Validate32(value[9].GetValueOrDefault<uint?>()),
                MaxNodesPerHistoryUpdateData =
                    Validate32(value[10].GetValueOrDefault<uint?>()),
                MaxNodesPerHistoryUpdateEvents =
                    Validate32(value[11].GetValueOrDefault<uint?>()),
                MaxNodesPerMethodCall =
                    Validate32(value[12].GetValueOrDefault<uint?>()),
                MaxNodesPerBrowse =
                    Validate32(value[13].GetValueOrDefault<uint?>()),
                MaxNodesPerRegisterNodes =
                    Validate32(value[14].GetValueOrDefault<uint?>()),
                MaxNodesPerTranslatePathsToNodeIds =
                    Validate32(value[15].GetValueOrDefault<uint?>()),
                MaxNodesPerNodeManagement =
                    Validate32(value[16].GetValueOrDefault<uint?>()),
                MaxMonitoredItemsPerCall =
                    Validate32(value[17].GetValueOrDefault<uint?>()),
                MaxNodesPerRead = maxNodesPerRead
            };

            static uint? Validate32(uint? v) => v == null ? null :
                v is > 0 and < int.MaxValue ? v : int.MaxValue;
            static ushort? Validate16(ushort? v) => v == null ? null :
                v > 0 ? v : ushort.MaxValue;
        }

        /// <summary>
        /// Read the server capabilities if available
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<ServerCapabilitiesModel?> FetchServerCapabilitiesAsync(
            RequestHeader requestHeader, NamespaceFormat namespaceFormat, CancellationToken ct)
        {
            // load the defaults for the historical capabilities object.
            var config =
                new ServerCapabilitiesState(null);
            config.ServerProfileArray =
                new PropertyState<string[]>(config);
            config.LocaleIdArray =
                new PropertyState<string[]>(config);
            config.SoftwareCertificates =
                new PropertyState<SignedSoftwareCertificate[]>(config);
            config.ModellingRules =
                new FolderState(config);
            config.AggregateFunctions =
                new FolderState(config);
            config.Create(SystemContext, null,
                BrowseNames.ServerCapabilities, null, false);

            var relativePath = new RelativePath();
            relativePath.Elements.Add(new RelativePathElement
            {
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                IsInverse = false,
                IncludeSubtypes = false,
                TargetName = BrowseNames.ServerCapabilities
            });
            var errorInfo = await this.ReadNodeStateAsync(requestHeader, config,
                Objects.Server, relativePath, ct).ConfigureAwait(false);
            if (errorInfo != null)
            {
                return null;
            }

            var functions = new List<BaseInstanceState>();
            config.AggregateFunctions.GetChildren(SystemContext, functions);
            var aggregateFunctions = functions.OfType<BaseObjectState>().ToDictionary(
                c => c.BrowseName.AsString(MessageContext, namespaceFormat),
                c => c.NodeId.AsString(MessageContext, namespaceFormat) ?? string.Empty);
            var rules = new List<BaseInstanceState>();
            config.ModellingRules.GetChildren(SystemContext, rules);
            var modellingRules = rules.OfType<BaseObjectState>().ToDictionary(
                c => c.BrowseName.AsString(MessageContext, namespaceFormat),
                c => c.NodeId.AsString(MessageContext, namespaceFormat) ?? string.Empty);
            return new ServerCapabilitiesModel
            {
                OperationLimits = _limits ?? new OperationLimitsModel(),
                ModellingRules =
                    modellingRules.Count == 0 ? null : modellingRules,
                SupportedLocales =
                    config.LocaleIdArray.GetValueOrDefault(
                        v => v == null || v.Length == 0 ? null : v),
                ServerProfiles =
                    config.ServerProfileArray.GetValueOrDefault(
                        v => v == null || v.Length == 0 ? null : v),
                AggregateFunctions =
                    aggregateFunctions.Count == 0 ? null : aggregateFunctions
            };
        }

        /// <summary>
        /// Read the history server capabilities if available
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<HistoryServerCapabilitiesModel?> FetchHistoryCapabilitiesAsync(
            RequestHeader requestHeader, NamespaceFormat namespaceFormat, CancellationToken ct)
        {
            // load the defaults for the historical capabilities object.
            var config =
                new HistoryServerCapabilitiesState(null);
            config.AccessHistoryDataCapability =
                new PropertyState<bool>(config);
            config.AccessHistoryEventsCapability =
                new PropertyState<bool>(config);
            config.MaxReturnDataValues =
                new PropertyState<uint>(config);
            config.MaxReturnEventValues =
                new PropertyState<uint>(config);
            config.InsertDataCapability =
                new PropertyState<bool>(config);
            config.ReplaceDataCapability =
                new PropertyState<bool>(config);
            config.UpdateDataCapability =
                new PropertyState<bool>(config);
            config.DeleteRawCapability =
                new PropertyState<bool>(config);
            config.DeleteAtTimeCapability =
                new PropertyState<bool>(config);
            config.InsertEventCapability =
                new PropertyState<bool>(config);
            config.ReplaceEventCapability =
                new PropertyState<bool>(config);
            config.UpdateEventCapability =
                new PropertyState<bool>(config);
            config.DeleteEventCapability =
                new PropertyState<bool>(config);
            config.InsertAnnotationCapability =
                new PropertyState<bool>(config);
            config.ServerTimestampSupported =
                new PropertyState<bool>(config);
            config.AggregateFunctions =
                new FolderState(config);
            config.Create(SystemContext, null,
                BrowseNames.HistoryServerCapabilities, null, false);

            var relativePath = new RelativePath();
            relativePath.Elements.Add(new RelativePathElement
            {
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                IsInverse = false,
                IncludeSubtypes = false,
                TargetName = BrowseNames.HistoryServerCapabilities
            });
            var errorInfo = await this.ReadNodeStateAsync(requestHeader, config,
                Objects.Server_ServerCapabilities, relativePath, ct).ConfigureAwait(false);
            if (errorInfo != null)
            {
                return null;
            }
            var supportsValues =
              config.AccessHistoryDataCapability.GetValueOrDefault() ?? false;
            var supportsEvents =
                config.AccessHistoryEventsCapability.GetValueOrDefault() ?? false;
            Dictionary<string, string>? aggregateFunctions = null;
            if (supportsEvents || supportsValues)
            {
                var children = new List<BaseInstanceState>();
                config.AggregateFunctions.GetChildren(SystemContext, children);
                aggregateFunctions = children.OfType<BaseObjectState>().ToDictionary(
                    c => c.BrowseName.AsString(MessageContext, namespaceFormat),
                    c => c.NodeId.AsString(MessageContext, namespaceFormat) ?? string.Empty);
            }
            return new HistoryServerCapabilitiesModel
            {
                AccessHistoryDataCapability =
                    supportsValues,
                AccessHistoryEventsCapability =
                    supportsEvents,
                MaxReturnDataValues =
                    config.MaxReturnDataValues.GetValueOrDefault(
                        v => !supportsValues ? null : v == 0 ? uint.MaxValue : v),
                MaxReturnEventValues =
                    config.MaxReturnEventValues.GetValueOrDefault(
                        v => !supportsEvents ? null : v == 0 ? uint.MaxValue : v),
                InsertDataCapability =
                    config.InsertDataCapability.GetValueOrDefault(),
                ReplaceDataCapability =
                    config.ReplaceDataCapability.GetValueOrDefault(),
                UpdateDataCapability =
                    config.UpdateDataCapability.GetValueOrDefault(),
                DeleteRawCapability =
                    config.DeleteRawCapability.GetValueOrDefault(),
                DeleteAtTimeCapability =
                    config.DeleteAtTimeCapability.GetValueOrDefault(),
                InsertEventCapability =
                    config.InsertEventCapability.GetValueOrDefault(),
                ReplaceEventCapability =
                    config.ReplaceEventCapability.GetValueOrDefault(),
                UpdateEventCapability =
                    config.UpdateEventCapability.GetValueOrDefault(),
                DeleteEventCapability =
                    config.DeleteEventCapability.GetValueOrDefault(),
                InsertAnnotationCapability =
                    config.InsertAnnotationCapability.GetValueOrDefault(),
                ServerTimestampSupported =
                    config.ServerTimestampSupported.GetValueOrDefault(),
                AggregateFunctions = aggregateFunctions == null ||
                    aggregateFunctions.Count == 0 ? null : aggregateFunctions
            };
        }

        /// <summary>
        /// Load complex type system
        /// </summary>
        /// <returns></returns>
        private Task<ComplexTypeSystem> LoadComplexTypeSystemAsync()
        {
            return Task.Run(async () =>
            {
                if (Session?.Connected == true)
                {
                    var complexTypeSystem = new ComplexTypeSystem(Session);
                    await complexTypeSystem.Load().ConfigureAwait(false);
                    _logger.LogInformation(
                        "Complex type system loaded into client {Client}.", this);
                    return complexTypeSystem;
                }
                throw new ServiceResultException(StatusCodes.BadNotConnected);
            }, _cts.Token);
        }

        /// <summary>
        /// Begin request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="header"></param>
        /// <returns></returns>
        private SessionActivity<T> Begin<T>(RequestHeader header)
            where T : IServiceResponse, new()
        {
            var activity = new SessionActivity<T>(this, typeof(T).Name[0..^8]);
            if (!Session.Connected)
            {
                var error = new T();
                error.ResponseHeader.ServiceResult = StatusCodes.BadNotConnected;
                error.ResponseHeader.Timestamp = DateTime.UtcNow;
                var text = error.ResponseHeader.StringTable.Count;
                error.ResponseHeader.StringTable.Add("Session not connected.");
                var locale = error.ResponseHeader.StringTable.Count;
                error.ResponseHeader.StringTable.Add("en-US");
                var symbol = error.ResponseHeader.StringTable.Count;
                error.ResponseHeader.StringTable.Add("BadNotConnected");
                error.ResponseHeader.ServiceDiagnostics = new DiagnosticInfo
                {
                    SymbolicId = symbol,
                    Locale = locale,
                    LocalizedText = text
                };
                activity.Error = error;
            }
            else
            {
                header.RequestHandle = Session.NewRequestHandle();
                header.AuthenticationToken = _authenticationToken;
                header.Timestamp = DateTime.UtcNow;
            }
            return activity;
        }

        /// <summary>
        /// Session activity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private sealed class SessionActivity<T> : IDisposable
            where T : IServiceResponse, new()
        {
            /// <summary>
            /// Any error to convey
            /// </summary>
            public T? Error { get; set; }

            /// <inheritdoc/>
            public SessionActivity(OpcUaSession outer, string activity)
            {
                _activity = outer._activitySource.StartActivity(activity);

                if (outer._logger.IsEnabled(LogLevel.Debug))
                {
                    _logScope = new LogScope(activity, Stopwatch.StartNew(),
                        outer._logger);
                    _logScope.logger.LogDebug("Session activity {Activity} started...",
                        _logScope.name);
                }
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _activity?.Dispose();

                if (_logScope != null)
                {
                    _logScope.logger.LogDebug(
                        "Session activity {Activity} completed in {Elapsed} with {Status}.",
                        _logScope.name, _logScope.sw.Elapsed,
                            Error?.ResponseHeader?.ServiceResult == null ? "Good" :
                     StatusCodes.GetBrowseName(Error.ResponseHeader.ServiceResult.CodeBits));
                }
            }

            /// <summary>
            /// Gets the response
            /// </summary>
            /// <param name="response"></param>
            /// <exception cref="ServiceResultException"></exception>
            /// <returns></returns>
            public T ValidateResponse(IServiceResponse response)
            {
                if (response?.ResponseHeader == null)
                {
                    // Throw - this is likely an issue in the transport.
                    throw new ServiceResultException(StatusCodes.BadUnknownResponse);
                }
                if (response is not T result)
                {
                    // Received a response, but not the type we expected.
                    // Promote to expected Type.
                    result = new T();

                    result.ResponseHeader.ServiceResult =
                        response.ResponseHeader.ServiceResult;
                    result.ResponseHeader.StringTable =
                        response.ResponseHeader.StringTable;
                    result.ResponseHeader.AdditionalHeader =
                        response.ResponseHeader.AdditionalHeader;
                    result.ResponseHeader.RequestHandle =
                        response.ResponseHeader.RequestHandle;
                    result.ResponseHeader.ServiceDiagnostics =
                        response.ResponseHeader.ServiceDiagnostics;
                    result.ResponseHeader.Timestamp =
                        response.ResponseHeader.Timestamp;
                }

                if (StatusCode.IsBad(result.ResponseHeader.ServiceResult))
                {
                    Error = result;
                }
                return result;
            }

            private sealed record class LogScope(string name, Stopwatch sw, ILogger logger);
            private readonly Activity? _activity;
            private readonly LogScope? _logScope;
        }

        private ServerCapabilitiesModel? _server;
        private OperationLimitsModel? _limits;
        private HistoryServerCapabilitiesModel? _history;
        private Task<ComplexTypeSystem>? _complexTypeSystem;
        private readonly CancellationTokenSource _cts;
        private readonly NodeId _authenticationToken;
        private readonly KeepAliveEventHandler _keepAlive;
        private readonly ILogger _logger;
        private readonly PublishErrorEventHandler? _errorHandler;
        private readonly PublishSequenceNumbersToAcknowledgeEventHandler? _ackHandler;
        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
    }
}
