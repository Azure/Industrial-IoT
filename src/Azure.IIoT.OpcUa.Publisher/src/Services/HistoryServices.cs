// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implments history services on top of core services
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class HistoryServices<T> : IHistoryServices<T>
    {
        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="options"></param>
        /// <param name="services"></param>
        /// <param name="timeProvider"></param>
        public HistoryServices(IOptions<PublisherOptions> options,
            INodeServicesInternal<T> services, TimeProvider? timeProvider = null)
        {
            _services = services;
            _options = options;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteEventsDetailsModel> request,
            CancellationToken ct)
        {
            return _services.HistoryUpdateAsync(endpoint, request,
                (nodeId, details, _) =>
                {
                    if (details.EventIds == null || details.EventIds.Count == 0)
                    {
                        throw new ArgumentException("Bad events", nameof(details));
                    }
                    return Task.FromResult(new ExtensionObject(new DeleteEventDetails
                    {
                        NodeId = nodeId,
                        EventIds = new ByteStringCollection(details.EventIds)
                    }));
                }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            return _services.HistoryUpdateAsync(endpoint, request,
                (nodeId, details, _) =>
                {
                    if (details.ReqTimes == null || details.ReqTimes.Count == 0)
                    {
                        throw new ArgumentException("Bad requested times", nameof(details));
                    }
                    return Task.FromResult(new ExtensionObject(new DeleteAtTimeDetails
                    {
                        NodeId = nodeId,
                        ReqTimes = new DateTimeCollection(details.ReqTimes)
                    }));
                }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct)
        {
            return _services.HistoryUpdateAsync(endpoint, request,
                (nodeId, details, _) =>
                {
                    if (details.EndTime == null && details.StartTime == null)
                    {
                        throw new ArgumentException("Start time and end time cannot both be null",
                            nameof(details));
                    }
                    return Task.FromResult(new ExtensionObject(new DeleteRawModifiedDetails
                    {
                        NodeId = nodeId,
                        EndTime = details.EndTime ?? DateTime.MinValue,
                        StartTime = details.StartTime ?? DateTime.MinValue,
                        IsDeleteModified = true
                    }));
                }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct)
        {
            return _services.HistoryUpdateAsync(endpoint, request, (nodeId, details, _) =>
            {
                if (details.EndTime == null && details.StartTime == null)
                {
                    throw new ArgumentException("Start time and end time cannot both be null",
                        nameof(details));
                }
                return Task.FromResult(new ExtensionObject(new DeleteRawModifiedDetails
                {
                    NodeId = nodeId,
                    EndTime = details.EndTime ?? DateTime.MinValue,
                    StartTime = details.StartTime ?? DateTime.MinValue,
                    IsDeleteModified = false
                }));
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            T endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct)
        {
            return _services.HistoryReadAsync(endpoint, request, (details, session) =>
            {
                if (details.EndTime == null && details.StartTime == null)
                {
                    throw new ArgumentException("Start time and end time cannot both be null",
                        nameof(details));
                }
                if ((details.StartTime == null || details.EndTime == null) &&
                    ((details.NumEvents ?? 0) == 0))
                {
                    throw new ArgumentException("Value bound must be set",
                        nameof(details));
                }
                return new ExtensionObject(new ReadEventDetails
                {
                    EndTime = details.EndTime ?? DateTime.MinValue,
                    StartTime = details.StartTime ?? DateTime.MinValue,
                    Filter = session.Codec.Decode(details.Filter),
                    NumValuesPerNode = details.NumEvents ?? 0
                });
            }, DecodeEvents, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            T endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            return _services.HistoryReadNextAsync(endpoint, request, DecodeEvents, ct);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct)
        {
            return await _services.HistoryReadAsync(endpoint, request, (details, _) =>
            {
                if (details.EndTime == null && details.StartTime == null)
                {
                    throw new ArgumentException("Start time and end time cannot both be null",
                        nameof(details));
                }
                if ((details.StartTime == null || details.EndTime == null) &&
                    ((details.NumValues ?? 0) == 0))
                {
                    throw new ArgumentException("Value bound must be set",
                        nameof(details));
                }
                return new ExtensionObject(new ReadRawModifiedDetails
                {
                    EndTime = details.EndTime ?? DateTime.MinValue,
                    StartTime = details.StartTime ?? DateTime.MinValue,
                    IsReadModified = false,
                    ReturnBounds = details.ReturnBounds ?? false,
                    NumValuesPerNode = details.NumValues ?? 0
                });
            }, DecodeValues, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            return _services.HistoryReadAsync(endpoint, request, (details, _) =>
            {
                if (details.ReqTimes == null || details.ReqTimes.Count == 0)
                {
                    throw new ArgumentException(nameof(details.ReqTimes));
                }
                return new ExtensionObject(new ReadAtTimeDetails
                {
                    ReqTimes = new DateTimeCollection(details.ReqTimes),
                    UseSimpleBounds = details.UseSimpleBounds ?? false
                });
            }, DecodeValues, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct)
        {
            return _services.HistoryReadAsync(endpoint, request, (details, session) =>
            {
                if (details.EndTime == null && details.StartTime == null)
                {
                    throw new ArgumentException("Start time and end time cannot both be null",
                        nameof(details));
                }
                var aggregateType = details.AggregateType;
                if (aggregateType != null)
                {
                    // TODO: Should be async!
                    var capabilities = session.GetHistoryCapabilitiesAsync(
                        request.Header.GetNamespaceFormat(_options), ct).AsTask().Result;
                    if (capabilities?.AggregateFunctions != null &&
                        capabilities.AggregateFunctions.TryGetValue(aggregateType, out var id))
                    {
                        aggregateType = id;
                    }
                }
                var aggregateId = aggregateType?.ToNodeId(session.MessageContext);
                return new ExtensionObject(new ReadProcessedDetails
                {
                    EndTime = details.EndTime ?? DateTime.MinValue,
                    StartTime = details.StartTime ?? DateTime.MinValue,
                    AggregateType = aggregateId == null ? null :
                        new NodeIdCollection(aggregateId.YieldReturn()),
                    ProcessingInterval = details.ProcessingInterval == null ? 0 :
                        details.ProcessingInterval.Value.TotalMilliseconds,
                    AggregateConfiguration = details.AggregateConfiguration.ToStackModel()
                });
            }, DecodeValues, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct)
        {
            return _services.HistoryReadAsync(endpoint, request, (details, _) =>
            {
                if (details.EndTime == null && details.StartTime == null)
                {
                    throw new ArgumentException("Start time and end time cannot both be null",
                        nameof(details));
                }
                if ((details.StartTime == null || details.EndTime == null) &&
                    ((details.NumValues ?? 0) == 0))
                {
                    throw new ArgumentException("Value bound must be set",
                        nameof(details));
                }
                return new ExtensionObject(new ReadRawModifiedDetails
                {
                    EndTime = details.EndTime ?? DateTime.MinValue,
                    StartTime = details.StartTime ?? DateTime.MinValue,
                    IsReadModified = true,
                    NumValuesPerNode = details.NumValues ?? 0
                });
            }, DecodeValues, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            T endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            return _services.HistoryReadNextAsync(endpoint, request, DecodeValues, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            return HistoryUpdateEventsAsync(endpoint, request, PerformUpdateType.Replace, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            return HistoryUpdateValuesAsync(endpoint, request, PerformUpdateType.Replace, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            return HistoryUpdateEventsAsync(endpoint, request, PerformUpdateType.Insert, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            return HistoryUpdateValuesAsync(endpoint, request, PerformUpdateType.Insert, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            return HistoryUpdateEventsAsync(endpoint, request, PerformUpdateType.Update, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            return HistoryUpdateValuesAsync(endpoint, request, PerformUpdateType.Update, ct);
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var result = await HistoryReadValuesAsync(endpoint, request,
                ct).ConfigureAwait(false);
            if (result.History != null)
            {
                foreach (var item in result.History)
                {
                    yield return item;
                }
            }
            await foreach (var item in HistoryStreamRemainingValuesAsync(
                endpoint, request.Header, result.ContinuationToken, ct).ConfigureAwait(false))
            {
                yield return item;
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<HistoricValueModel> HistoryStreamModifiedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var result = await HistoryReadModifiedValuesAsync(endpoint, request,
                ct).ConfigureAwait(false);
            if (result.History != null)
            {
                foreach (var item in result.History)
                {
                    yield return item;
                }
            }

            await foreach (var item in HistoryStreamRemainingValuesAsync(
                endpoint, request.Header, result.ContinuationToken, ct).ConfigureAwait(false))
            {
                yield return item;
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAtTimesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var result = await HistoryReadValuesAtTimesAsync(endpoint, request,
                ct).ConfigureAwait(false);
            if (result.History != null)
            {
                foreach (var item in result.History)
                {
                    yield return item;
                }
            }

            await foreach (var item in HistoryStreamRemainingValuesAsync(
                endpoint, request.Header, result.ContinuationToken, ct).ConfigureAwait(false))
            {
                yield return item;
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<HistoricValueModel> HistoryStreamProcessedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var result = await HistoryReadProcessedValuesAsync(endpoint, request,
                ct).ConfigureAwait(false);
            if (result.History != null)
            {
                foreach (var item in result.History)
                {
                    yield return item;
                }
            }

            await foreach (var item in HistoryStreamRemainingValuesAsync(
                endpoint, request.Header, result.ContinuationToken, ct).ConfigureAwait(false))
            {
                yield return item;
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<HistoricEventModel> HistoryStreamEventsAsync(
            T endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var result = await HistoryReadEventsAsync(endpoint, request,
                ct).ConfigureAwait(false);
            if (result.History != null)
            {
                foreach (var item in result.History)
                {
                    yield return item;
                }
            }

            await foreach (var item in HistoryStreamRemainingEventsAsync(
                endpoint, request.Header, result.ContinuationToken, ct).ConfigureAwait(false))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Read all remaining values
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="header"></param>
        /// <param name="continuationToken"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async IAsyncEnumerable<HistoricValueModel> HistoryStreamRemainingValuesAsync(
            T connectionId, RequestHeaderModel? header, string? continuationToken,
            [EnumeratorCancellation] CancellationToken ct)
        {
            while (continuationToken != null)
            {
                var response = await HistoryReadValuesNextAsync(connectionId,
                    new HistoryReadNextRequestModel
                    {
                        ContinuationToken = continuationToken,
                        Header = header
                    }, ct).ConfigureAwait(false);
                continuationToken = response.ContinuationToken;
                if (response.History != null)
                {
                    foreach (var item in response.History)
                    {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Read all remaining events
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="header"></param>
        /// <param name="continuationToken"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async IAsyncEnumerable<HistoricEventModel> HistoryStreamRemainingEventsAsync(
            T connectionId, RequestHeaderModel? header,
            string? continuationToken, [EnumeratorCancellation] CancellationToken ct)
        {
            while (continuationToken != null)
            {
                var response = await HistoryReadEventsNextAsync(connectionId,
                    new HistoryReadNextRequestModel
                    {
                        ContinuationToken = continuationToken,
                        Header = header
                    }, ct).ConfigureAwait(false);
                continuationToken = response.ContinuationToken;
                if (response.History != null)
                {
                    foreach (var item in response.History)
                    {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Update events
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="request"></param>
        /// <param name="action"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Task<HistoryUpdateResponseModel> HistoryUpdateEventsAsync(
            T connectionId, HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            PerformUpdateType action, CancellationToken ct)
        {
            return _services.HistoryUpdateAsync(connectionId, request,
                (nodeId, details, session) =>
                {
                    if (details.Events == null || details.Events.Count == 0)
                    {
                        throw new ArgumentException("Bad events", nameof(details));
                    }
                    return Task.FromResult(new ExtensionObject(new UpdateEventDetails
                    {
                        NodeId = nodeId,
                        PerformInsertReplace = action,
                        Filter = session.Codec.Decode(details.Filter),
                        EventData = new HistoryEventFieldListCollection(details.Events
                            .Select(d => new HistoryEventFieldList
                            {
                                EventFields = new VariantCollection(d.EventFields
                                    .Select(f => session.Codec.Decode(f, BuiltInType.Variant)))
                            }))
                    }));
                }, ct);
        }

        /// <summary>
        /// Update values
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="request"></param>
        /// <param name="action"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Task<HistoryUpdateResponseModel> HistoryUpdateValuesAsync(
            T connectionId, HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            PerformUpdateType action, CancellationToken ct)
        {
            return _services.HistoryUpdateAsync(connectionId, request,
                async (nodeId, details, session) =>
                {
                    if (details.Values == null || details.Values.Count == 0)
                    {
                        throw new ArgumentException("Bad values", nameof(details));
                    }
                    var builtinType = await GetDataTypeAsync(session, request.Header,
                        nodeId, details.Values, _timeProvider, ct).ConfigureAwait(false);
                    return new ExtensionObject(new UpdateDataDetails
                    {
                        NodeId = nodeId,
                        PerformInsertReplace = action,
                        UpdateValues = new DataValueCollection(details.Values
                            .Select(d => new DataValue
                            {
                                ServerPicoseconds = d.ServerPicoseconds ?? 0,
                                SourcePicoseconds = d.SourcePicoseconds ?? 0,
                                ServerTimestamp = d.ServerTimestamp ?? DateTime.MinValue,
                                SourceTimestamp = d.SourceTimestamp ?? DateTime.MinValue,
                                StatusCode = d.Status?.StatusCode ?? StatusCodes.Good,
                                Value = session.Codec.Decode(
                                    d.Value ?? VariantValue.Null, builtinType)
                            }))
                    });
                }, ct);
        }

        /// <summary>
        /// Convert to results
        /// </summary>
        /// <param name="extensionObject"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        private static HistoricEventModel[] DecodeEvents(ExtensionObject extensionObject,
            IOpcUaSession session)
        {
            if (extensionObject.Body is HistoryEvent ev)
            {
                return ev.Events.Select(d => new HistoricEventModel
                {
                    EventFields = d.EventFields
                        .Select(v => v == Variant.Null ?
                            VariantValue.Null : session.Codec.Encode(v, out var builtInType))
                        .ToList()
                }).ToArray();
            }
            return [];
        }

        /// <summary>
        /// Gets the data type of the values and falls back
        /// to reading from node
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodeId"></param>
        /// <param name="values"></param>
        /// <param name="timeProvider"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static async Task<BuiltInType> GetDataTypeAsync(
            IOpcUaSession session, RequestHeaderModel? requestHeader,
            NodeId nodeId, IEnumerable<HistoricValueModel> values,
            TimeProvider timeProvider, CancellationToken ct)
        {
            // Get data type
            var dataTypes = values
                .Select(v => v.DataType)
                .Where(v => v != null)
                .ToList();
            var dataType = dataTypes.FirstOrDefault();
#if DEBUG
            if (dataType != null &&
                !dataTypes.All(v => string.Equals(v, dataType, StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    $"All values must have no or data type {dataType}.");
            }
#endif
            var dataTypeId = dataType.ToNodeId(session.MessageContext);
            if (NodeId.IsNull(dataTypeId))
            {
                // Read data type
                (dataTypeId, _) = await session.ReadAttributeAsync<NodeId?>(
                    requestHeader.ToRequestHeader(timeProvider), nodeId,
                    Attributes.DataType, ct).ConfigureAwait(false);
                if (NodeId.IsNull(dataTypeId))
                {
                    throw new ArgumentException(
                        $"{nodeId} does not have a data type to fall back on.");
                }
            }
            return await TypeInfo.GetBuiltInTypeAsync(dataTypeId, session.TypeTree,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Decode values
        /// </summary>
        /// <param name="extensionObject"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private static HistoricValueModel[] DecodeValues(ExtensionObject extensionObject,
            IOpcUaSession session)
        {
            if (extensionObject.Body is HistoryData data)
            {
                var modificationInfos = new List<ModificationInfoModel>();
                if (extensionObject.Body is HistoryModifiedData modified)
                {
                    if (modified.ModificationInfos.Count != data.DataValues.Count)
                    {
                        throw new FormatException(
                            "Modification infos and data value count is not the same");
                    }
                    return data.DataValues
                        .Zip(modified.ModificationInfos)
                        .Select(d => EncodeDataValue(session.Codec, d.First, d.Second))
                        .ToArray();
                }
                return data.DataValues
                    .Select(d => EncodeDataValue(session.Codec, d, null))
                    .ToArray();
            }
            return [];

            static HistoricValueModel EncodeDataValue(IVariantEncoder codec,
                DataValue dataValue, ModificationInfo? modification)
            {
                var builtInType = BuiltInType.Null;
                return new HistoricValueModel
                {
                    ServerPicoseconds = dataValue.ServerPicoseconds == 0 ? null :
                        dataValue.ServerPicoseconds,
                    SourcePicoseconds = dataValue.SourcePicoseconds == 0 ? null :
                        dataValue.SourcePicoseconds,
                    ServerTimestamp = dataValue.ServerTimestamp == DateTime.MinValue ? null :
                        dataValue.ServerTimestamp,
                    SourceTimestamp = dataValue.SourceTimestamp == DateTime.MinValue ? null :
                        dataValue.SourceTimestamp,
                    Status =  // Remove aggregate bits we are testing below.
                        (dataValue.StatusCode.Code & ~0x041F) == StatusCodes.Good ? null :
                         dataValue.StatusCode.CreateResultModel(),
                    DataLocation = dataValue.StatusCode.AggregateBits.ToDataLocation(),
                    AdditionalData = dataValue.StatusCode.AggregateBits.ToAdditionalData(),
                    ModificationInfo = modification == null ? null : new ModificationInfoModel
                    {
                        ModificationTime = modification.ModificationTime == DateTime.MinValue ?
                            null : modification.ModificationTime,
                        UpdateType = (HistoryUpdateOperation)modification.UpdateType,
                        UserName = modification.UserName
                    },
                    Value = dataValue.WrappedValue == Variant.Null ?
                        null : codec.Encode(dataValue.WrappedValue, out builtInType),
                    DataType = builtInType == BuiltInType.Null ?
                        null : builtInType.ToString()
                };
            }
        }

        private readonly INodeServicesInternal<T> _services;
        private readonly IOptions<PublisherOptions> _options;
        private readonly TimeProvider _timeProvider;
    }
}
