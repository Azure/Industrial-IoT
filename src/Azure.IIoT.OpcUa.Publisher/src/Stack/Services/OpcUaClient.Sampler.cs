// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed partial class OpcUaClient
    {
        /// <summary>
        /// Registers a value to read with results pushed to the provided
        /// subscription callback
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="maxAge"></param>
        /// <param name="nodeToRead"></param>
        /// <param name="subscriptionName"></param>
        /// <param name="clientHandle"></param>
        /// <returns></returns>
        internal IAsyncDisposable Sample(TimeSpan samplingRate, TimeSpan maxAge,
            ReadValueId nodeToRead, string subscriptionName, uint clientHandle)
        {
            return Sampler.Register(this, samplingRate, maxAge,
                nodeToRead, subscriptionName, clientHandle);
        }

        /// <summary>
        /// A set of client sampled values
        /// </summary>
        private sealed class Sampler : IAsyncDisposable
        {
            /// <summary>
            /// Creates the sampler
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="samplingRate"></param>
            /// <param name="maxAge"></param>
            /// <param name="subscription"></param>
            /// <param name="value"></param>
            private Sampler(OpcUaClient outer, TimeSpan samplingRate,
                TimeSpan maxAge, string subscription, SampledNodeId value)
            {
                _sampledNodes = ImmutableHashSet<SampledNodeId>.Empty.Add(value);

                _client = outer;
                _cts = new CancellationTokenSource();
                _samplingRate = samplingRate;
                _maxAge = maxAge;
                _subscription = subscription;
                _timer = new PeriodicTimer(_samplingRate);
                _sampler = RunAsync(_cts.Token);
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                try
                {
                    await _cts.CancelAsync().ConfigureAwait(false);
                    _timer.Dispose();
                    await _sampler.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    _cts.Dispose();
                }
            }

            /// <summary>
            /// Add sampler
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            private Sampler Add(SampledNodeId node)
            {
                _sampledNodes = _sampledNodes.Add(node);
                return this;
            }

            /// <summary>
            /// Remove sampler
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            private bool Remove(SampledNodeId value)
            {
                _sampledNodes = _sampledNodes.Remove(value);
                return _sampledNodes.Count == 0;
            }

            /// <summary>
            /// Run sampling of values on the periodic timer
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task RunAsync(CancellationToken ct)
            {
                var sw = Stopwatch.StartNew();
                for (var sequenceNumber = 1u; !ct.IsCancellationRequested; sequenceNumber++)
                {
                    if (sequenceNumber == 0u)
                    {
                        continue;
                    }

                    var nodesToRead = new ReadValueIdCollection(_sampledNodes.Select(n => n.InitialValue));
                    try
                    {
                        // Wait until period completed
                        if (!await _timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                        {
                            continue;
                        }

                        sw.Restart();
                        // Grab the current session
                        var session = _client._session;
                        if (session == null)
                        {
                            await SendAsync(sequenceNumber, nodesToRead, StatusCodes.BadNotConnected,
                                TimeSpan.Zero).ConfigureAwait(false);
                            continue;
                        }

                        // Ensure type system is loaded
                        await session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);

                        // Perform the read.
                        var timeout = _samplingRate.TotalMilliseconds / 2;
                        var response = await session.ReadAsync(new RequestHeader
                        {
                            Timestamp = _client._timeProvider.GetUtcNow().UtcDateTime,
                            TimeoutHint = (uint)timeout,
                            ReturnDiagnostics = 0
                        }, _maxAge.TotalMilliseconds, Opc.Ua.TimestampsToReturn.Both,
                            nodesToRead, ct).ConfigureAwait(false);

                        var values = response.Validate(response.Results,
                            r => r.StatusCode, response.DiagnosticInfos, nodesToRead);

                        if (values.ErrorInfo != null)
                        {
                            await SendAsync(sequenceNumber, nodesToRead, values.ErrorInfo.StatusCode,
                                sw.Elapsed).ConfigureAwait(false);
                            continue;
                        }

                        // Notify clients of the values
                        await SendAsync(sequenceNumber, values, sw.Elapsed).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    catch (ServiceResultException sre)
                    {
                        await SendAsync(sequenceNumber, nodesToRead,
                            sre.StatusCode, sw.Elapsed).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        var error = new ServiceResult(ex).StatusCode;
                        await SendAsync(sequenceNumber, nodesToRead,
                            error.Code, sw.Elapsed).ConfigureAwait(false);
                    }
                }
            }

            /// <summary>
            /// Notify results
            /// </summary>
            /// <param name="seq"></param>
            /// <param name="values"></param>
            /// <param name="elapsed"></param>
            private ValueTask SendAsync(uint seq, ServiceResponse<ReadValueId, DataValue> values,
                TimeSpan elapsed)
            {
                if (_client._session?.Subscriptions.FirstOrDefault(n => n.DisplayName == _subscription)
                    is not OpcUaSubscription target)
                {
                    return ValueTask.CompletedTask;
                }

                var missed = GetMissed(elapsed);
                return target.OnSubscriptionCylicReadNotificationAsync(seq,
                    _client._timeProvider.GetUtcNow().UtcDateTime, values
                    .Select(i => new SampledDataValueModel(
                        SetOverflow(i.Result, missed > 0),
                            ((SampledNodeId)i.Request.Handle).ClientHandle, missed))
                    .ToList());

                static DataValue SetOverflow(DataValue result, bool overflowBit)
                {
                    result.StatusCode.SetOverflow(overflowBit);
                    return result;
                }
            }

            /// <summary>
            /// Notify error status code
            /// </summary>
            /// <param name="seq"></param>
            /// <param name="nodesToRead"></param>
            /// <param name="statusCode"></param>
            /// <param name="elapsed"></param>
            private ValueTask SendAsync(uint seq, ReadValueIdCollection nodesToRead, uint statusCode,
                TimeSpan elapsed)
            {
                if (_client._session?.Subscriptions.FirstOrDefault(n => n.DisplayName == _subscription)
                    is not OpcUaSubscription target)
                {
                    return ValueTask.CompletedTask;
                }

                var missed = GetMissed(elapsed);
                return target.OnSubscriptionCylicReadNotificationAsync(seq,
                    _client._timeProvider.GetUtcNow().UtcDateTime, nodesToRead
                    .Select(i => new SampledDataValueModel(
                        SetOverflow(statusCode, missed > 0),
                            ((SampledNodeId)i.Handle).ClientHandle, missed))
                    .ToList());

                static DataValue SetOverflow(uint statusCode, bool overflowBit)
                {
                    var dataValue = new DataValue(statusCode);
                    dataValue.StatusCode.SetOverflow(overflowBit);
                    return dataValue;
                }
            }

            private int GetMissed(TimeSpan elapsed)
            {
                return (int)Math.Round(elapsed.TotalMilliseconds / _samplingRate.TotalMilliseconds);
            }

            /// <summary>
            /// A sampled node registered with a sampler
            /// </summary>
            private sealed class SampledNodeId : IAsyncDisposable
            {
                /// <summary>
                /// Sampler key
                /// </summary>
                public (string, TimeSpan, TimeSpan) Key { get; }

                /// <summary>
                /// Item to monito
                /// </summary>
                public ReadValueId InitialValue { get; }

                /// <summary>
                /// Client handle
                /// </summary>
                public uint ClientHandle { get; }

                /// <summary>
                /// Create node
                /// </summary>
                /// <param name="outer"></param>
                /// <param name="key"></param>
                /// <param name="item"></param>
                /// <param name="clientHandle"></param>
                public SampledNodeId(OpcUaClient outer, (string, TimeSpan, TimeSpan) key,
                    ReadValueId item, uint clientHandle)
                {
                    _outer = outer;
                    Key = key;
                    InitialValue = item;
                    ClientHandle = clientHandle;
                    item.Handle = this;
                }

                /// <inheritdoc/>
                public async ValueTask DisposeAsync()
                {
                    Sampler? sampler;
                    lock (_outer._samplers)
                    {
                        if (!_outer._samplers.TryGetValue(Key, out sampler)
                            || !sampler.Remove(this))
                        {
                            return;
                        }
                        _outer._samplers.Remove(Key);
                    }
                    await sampler.DisposeAsync().ConfigureAwait(false);
                }

                private readonly OpcUaClient _outer;
            }

            /// <summary>
            /// Register for sampling
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="samplingRate"></param>
            /// <param name="maxAge"></param>
            /// <param name="item"></param>
            /// <param name="subscriptionName"></param>
            /// <param name="clientHandle"></param>
            /// <returns></returns>
            public static IAsyncDisposable Register(OpcUaClient outer, TimeSpan samplingRate,
                TimeSpan maxAge, ReadValueId item, string subscriptionName, uint clientHandle)
            {
                if (samplingRate <= TimeSpan.Zero)
                {
                    samplingRate = TimeSpan.FromSeconds(1);
                }
                if (maxAge < TimeSpan.Zero)
                {
                    maxAge = TimeSpan.Zero;
                }
                lock (outer._samplers)
                {
                    var key = (subscriptionName, samplingRate, maxAge);
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var sampledNode = new SampledNodeId(outer, key, item, clientHandle);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    if (!outer._samplers.TryGetValue(key, out var sampler))
                    {
                        sampler = new Sampler(outer, samplingRate, maxAge,
                            subscriptionName, sampledNode);
                        outer._samplers.Add(key, sampler);
                    }
                    else
                    {
                        sampler.Add(sampledNode);
                    }
                    return sampledNode;
                }
            }

            private ImmutableHashSet<SampledNodeId> _sampledNodes;
            private readonly CancellationTokenSource _cts;
            private readonly Task _sampler;
            private readonly OpcUaClient _client;
            private readonly TimeSpan _samplingRate;
            private readonly TimeSpan _maxAge;
            private readonly string _subscription;
            private readonly PeriodicTimer _timer;
        }
    }
}
