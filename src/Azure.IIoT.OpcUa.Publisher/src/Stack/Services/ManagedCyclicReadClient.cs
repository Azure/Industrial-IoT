// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal readonly record struct ManagedCyclicReadRequest(
        ReadValueId Value,
        bool Register);

    internal interface IManagedCyclicReadClient : IAsyncDisposable
    {
        ValueTask<IReadOnlyList<DataValue>> ReadAsync(
            IReadOnlyList<ManagedCyclicReadRequest> requests,
            TimeSpan samplingInterval,
            TimeSpan maxAge,
            CancellationToken ct);
    }

    internal sealed class ManagedCyclicReadClient : IManagedCyclicReadClient
    {
        public ManagedCyclicReadClient(
            ManagedOpcUaSession session,
            TimeProvider timeProvider,
            ILogger<ManagedCyclicReadClient>? logger = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _logger = logger ?? NullLogger<ManagedCyclicReadClient>.Instance;
            _session.OnConnectionStateChange += OnConnectionStateChange;
        }

        public async ValueTask<IReadOnlyList<DataValue>> ReadAsync(
            IReadOnlyList<ManagedCyclicReadRequest> requests,
            TimeSpan samplingInterval,
            TimeSpan maxAge,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(requests);
            ObjectDisposedException.ThrowIf(
                Volatile.Read(ref _disposed) != 0, this);
            if (requests.Count == 0)
            {
                return [];
            }

            try
            {
                _ = await _session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var errorValues = new List<DataValue>(requests.Count);
                AddErrorValues(errorValues, requests.Count,
                    new ServiceResult(ex).StatusCode);
                return errorValues;
            }
            OperationLimitsModel? limits = null;
            if (Volatile.Read(ref _operationLimitsUnavailable) == 0)
            {
                try
                {
                    limits = await _session.GetOperationLimitsAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Interlocked.Exchange(ref _operationLimitsUnavailable, 1);
                    _logger.CyclicReadOperationLimitsUnavailable(ex);
                }
            }
            var nodesToRead = await ResolveRegisteredNodesAsync(
                requests, limits, ct).ConfigureAwait(false);
            var maxNodesPerRead = limits?.MaxNodesPerRead is > 0
                ? (int)Math.Min(limits.MaxNodesPerRead.Value, int.MaxValue)
                : nodesToRead.Count;
            var values = new List<DataValue>(nodesToRead.Count);
            for (var offset = 0; offset < nodesToRead.Count; offset += maxNodesPerRead)
            {
                var count = Math.Min(maxNodesPerRead, nodesToRead.Count - offset);
                var batch = new ReadValueIdCollection();
                for (var index = 0; index < count; index++)
                {
                    batch.Add(nodesToRead[offset + index]);
                }

                try
                {
                    var timeout = Math.Clamp(
                        samplingInterval.TotalMilliseconds / 2,
                        0,
                        uint.MaxValue);
                    ReadResponse response = await _session.Services.ReadAsync(
                        new RequestHeader
                        {
                            Timestamp = _timeProvider.GetUtcNow().UtcDateTime,
                            TimeoutHint = (uint)timeout,
                            ReturnDiagnostics = 0
                        },
                        Math.Max(0, maxAge.TotalMilliseconds),
                        Opc.Ua.TimestampsToReturn.Both,
                        batch,
                        ct).ConfigureAwait(false);
                    if (StatusCode.IsBad(response.ResponseHeader.ServiceResult) ||
                        response.Results.Count != count)
                    {
                        var status = StatusCode.IsBad(response.ResponseHeader.ServiceResult)
                            ? response.ResponseHeader.ServiceResult
                            : StatusCodes.BadUnexpectedError;
                        AddErrorValues(values, count, status);
                        continue;
                    }
                    values.AddRange(response.Results);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    AddErrorValues(values, count, new ServiceResult(ex).StatusCode);
                }
            }
            return values;
        }

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return ValueTask.CompletedTask;
            }
            _session.OnConnectionStateChange -= OnConnectionStateChange;
            _registrationGate.Dispose();
            return ValueTask.CompletedTask;
        }

        private async ValueTask<IReadOnlyList<ReadValueId>> ResolveRegisteredNodesAsync(
            IReadOnlyList<ManagedCyclicReadRequest> requests,
            OperationLimitsModel? limits,
            CancellationToken ct)
        {
            var values = requests
                .Select(request => Clone(request.Value))
                .ToArray();
            var requested = requests
                .Select((request, index) => (request, index))
                .Where(entry => entry.request.Register)
                .ToArray();
            if (requested.Length == 0)
            {
                return values;
            }

            await _registrationGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var generation = Volatile.Read(ref _registrationGeneration);
                NodeId[] missing;
                lock (_registeredNodesLock)
                {
                    missing = requested
                        .Select(entry => entry.request.Value.NodeId)
                        .Where(nodeId => !_registeredNodes.ContainsKey(nodeId))
                        .Distinct()
                        .ToArray();
                }
                var maxNodesPerRegister = limits?.MaxNodesPerRegisterNodes is > 0
                    ? (int)Math.Min(limits.MaxNodesPerRegisterNodes.Value, int.MaxValue)
                    : Math.Max(1, missing.Length);
                for (var offset = 0; offset < missing.Length;
                    offset += maxNodesPerRegister)
                {
                    var count = Math.Min(maxNodesPerRegister, missing.Length - offset);
                    var batch = new NodeIdCollection();
                    for (var index = 0; index < count; index++)
                    {
                        batch.Add(missing[offset + index]);
                    }
                    try
                    {
                        RegisterNodesResponse response =
                            await _session.Services.RegisterNodesAsync(
                                new RequestHeader
                                {
                                    Timestamp = _timeProvider.GetUtcNow().UtcDateTime,
                                    ReturnDiagnostics = 0
                                },
                                batch, ct).ConfigureAwait(false);
                        if (StatusCode.IsBad(response.ResponseHeader.ServiceResult) ||
                            response.RegisteredNodeIds.Count != count)
                        {
                            _logger.CyclicReadNodeRegistrationRejected(
                                StatusCode.IsBad(response.ResponseHeader.ServiceResult)
                                    ? response.ResponseHeader.ServiceResult
                                    : StatusCodes.BadUnexpectedError);
                            continue;
                        }
                        if (generation != Volatile.Read(ref _registrationGeneration))
                        {
                            continue;
                        }
                        lock (_registeredNodesLock)
                        {
                            if (generation != _registrationGeneration)
                            {
                                continue;
                            }
                            for (var index = 0; index < count; index++)
                            {
                                var registered = response.RegisteredNodeIds[index];
                                if (!NodeIdCompat.IsNull(registered))
                                {
                                    _registeredNodes[batch[index]] = registered;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.CyclicReadNodeRegistrationUnavailable(ex);
                    }
                }

                lock (_registeredNodesLock)
                {
                    foreach (var (request, index) in requested)
                    {
                        if (_registeredNodes.TryGetValue(
                            request.Value.NodeId, out var registered))
                        {
                            values[index] = Clone(request.Value, registered);
                        }
                    }
                }
                return values;
            }
            finally
            {
                _registrationGate.Release();
            }
        }

        private void OnConnectionStateChange(object? sender,
            EndpointConnectivityStateEventArgs e)
        {
            Interlocked.Increment(ref _registrationGeneration);
            Interlocked.Exchange(ref _operationLimitsUnavailable, 0);
            lock (_registeredNodesLock)
            {
                _registeredNodes.Clear();
            }
        }

        private static ReadValueId Clone(ReadValueId value, NodeId? nodeId = null)
        {
            return new ReadValueId
            {
                NodeId = nodeId ?? value.NodeId,
                AttributeId = value.AttributeId,
                IndexRange = value.IndexRange,
                DataEncoding = value.DataEncoding
            };
        }

        private static void AddErrorValues(
            List<DataValue> values,
            int count,
            StatusCode status)
        {
            for (var index = 0; index < count; index++)
            {
                values.Add(DataValue.FromStatusCode(status));
            }
        }

        private readonly ManagedOpcUaSession _session;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _registrationGate = new(1, 1);
        private readonly Lock _registeredNodesLock = new();
        private readonly Dictionary<NodeId, NodeId> _registeredNodes = [];
        private int _disposed;
        private int _operationLimitsUnavailable;
        private long _registrationGeneration;
    }
}
