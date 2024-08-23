// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    internal abstract partial class OpcUaMonitoredItem
    {
        /// <summary>
        /// Cyclic read items are part of a subscription but disabled. They
        /// execute through the client sampler periodically (at the configured
        /// sampling rate).
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal sealed class CyclicRead : DataChange
        {
            /// <summary>
            /// Create cyclic read item
            /// </summary>
            /// <param name="client"></param>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            /// <param name="timeProvider"></param>
            public CyclicRead(IOpcUaClient client, DataMonitoredItemModel template,
                ILogger<CyclicRead> logger, TimeProvider timeProvider)
                : base(template with
                {
                    // Always ensure item is disabled
                    MonitoringMode = Publisher.Models.MonitoringMode.Disabled
                }, logger, timeProvider)
            {
                _client = client;

                LastReceivedValue = new MonitoredItemNotification
                {
                    Value = new DataValue(StatusCodes.GoodNoData)
                };
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            private CyclicRead(CyclicRead item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                _client = item._client;
                if (item._sampling)
                {
                    EnsureSamplerRunning();
                }
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new CyclicRead(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                // Cleanup
                var sampler = _sampler;
                lock (_lock)
                {
                    _disposed = true;
                    _sampler = null;
                }
                sampler?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                base.Dispose(disposing);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not CyclicRead cyclicRead)
                {
                    return false;
                }
                if (_client != cyclicRead._client)
                {
                    return false;
                }
                if ((Template.SamplingInterval ?? TimeSpan.FromSeconds(1)) !=
                    (cyclicRead.Template.SamplingInterval ?? TimeSpan.FromSeconds(1)))
                {
                    return false;
                }
                return base.Equals(obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * -1521134295) +
                    _client.GetHashCode();
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<TimeSpan>.Default.GetHashCode(
                        Template.SamplingInterval ?? TimeSpan.FromSeconds(1));
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"Cyclic read '{Template.StartNodeId}' every {Template.SamplingInterval
                    ?? TimeSpan.FromSeconds(1)}";
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                out bool metadataChanged)
            {
                if (item is not CyclicRead)
                {
                    metadataChanged = false;
                    return false;
                }
                return base.MergeWith(item, session, out metadataChanged);
            }

            /// <inheritdoc/>
            public override Func<CancellationToken, Task>? FinalizeMonitoringModeChange => async _ =>
            {
                if (!AttachedToSubscription)
                {
                    // Disabling sampling
                    await StopSamplerAsync().ConfigureAwait(false);
                }
                else
                {
                    Debug.Assert(MonitoringMode == MonitoringMode.Disabled);
                    EnsureSamplerRunning();
                }
            };

            /// <summary>
            /// Ensure sampler is started
            /// </summary>
            private void EnsureSamplerRunning()
            {
                Debug.Assert(AttachedToSubscription);
                lock (_lock)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    if (_sampler == null)
                    {
                        _sampling = true;
                        _sampler = _client.Sample(TimeSpan.FromMilliseconds(SamplingInterval),
                            new ReadValueId
                            {
                                AttributeId = AttributeId,
                                IndexRange = IndexRange,
                                NodeId = ResolvedNodeId
                            },
                            Subscription.DisplayName, ClientHandle);
                        _logger.LogDebug("Item {Item} successfully registered with sampler.",
                            this);
                    }
                }
            }

            /// <summary>
            /// Stop sampling
            /// </summary>
            /// <returns></returns>
            private async Task StopSamplerAsync()
            {
                var sampler = _sampler;
                lock (_lock)
                {
                    _sampler = null;
                    _sampling = false;
                }
                if (sampler != null)
                {
                    await sampler.DisposeAsync().ConfigureAwait(false);
                    _logger.LogDebug("Item {Item} unregistered from sampler.", this);
                }
            }

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(uint sequenceNumber, DateTimeOffset timestamp,
                IEncodeable encodeablePayload, IList<MonitoredItemNotificationModel> notifications)
            {
                if (!Valid || encodeablePayload is not SampledDataValueModel cyclicReadNotification)
                {
                    return false;
                }

                LastReceivedValue = cyclicReadNotification;
                LastReceivedTime = TimeProvider.GetUtcNow();
                notifications.Add(ToMonitoredItemNotification(sequenceNumber,
                    cyclicReadNotification.Value, cyclicReadNotification.Overflow));
                return true;
            }

            private readonly IOpcUaClient _client;
            private IAsyncDisposable? _sampler;
            private bool _sampling;
            private readonly object _lock = new ();
            private bool _disposed;
        }
    }
}
