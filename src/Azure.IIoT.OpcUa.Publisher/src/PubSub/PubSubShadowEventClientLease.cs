// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Pins a selected transport while a configuration, native connection, or
    /// retained-metadata cleanup can still publish through it. Borrowed global
    /// clients have no owned lifetime.
    /// </summary>
    internal sealed class PubSubShadowEventClientLease : IDisposable, IAsyncDisposable
    {
        public PubSubShadowEventClientLease(IEventClient eventClient,
            IDisposable? lifetime = null, Action? released = null)
        {
            EventClient = eventClient ?? throw new ArgumentNullException(nameof(eventClient));
            _shared = lifetime is null ? null : new SharedLifetime(lifetime, released);
        }

        private PubSubShadowEventClientLease(PubSubShadowEventClientLease lease)
        {
            EventClient = lease.EventClient;
            _shared = lease._shared;
            _shared?.Acquire();
        }

        public IEventClient EventClient { get; }

        public PubSubShadowEventClientLease Acquire()
        {
            return new PubSubShadowEventClientLease(this);
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        public ValueTask DisposeAsync()
        {
            lock (_gate)
            {
                return new ValueTask(_disposeTask ??=
                    _shared?.ReleaseAsync() ?? Task.CompletedTask);
            }
        }

        internal static Task ReleaseAllAsync(
            IEnumerable<PubSubShadowEventClientLease?> leases)
        {
            return Task.WhenAll(leases.Select(lease =>
                lease?.DisposeAsync().AsTask() ?? Task.CompletedTask));
        }

        private sealed class SharedLifetime(IDisposable lifetime, Action? released)
        {
            public void Acquire()
            {
                lock (_gate)
                {
                    if (_references == 0)
                    {
                        throw new InvalidOperationException(
                            "The selected writer-group transport is closing or failed to close.");
                    }
                    _references++;
                }
            }

            public Task ReleaseAsync()
            {
                bool last;
                lock (_gate)
                {
                    last = --_references == 0;
                }
                return last ? DisposeAsync() : Task.CompletedTask;
            }

            private async Task DisposeAsync()
            {
                if (lifetime is IAsyncDisposable asyncLifetime)
                {
                    await asyncLifetime.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    lifetime.Dispose();
                }
                released?.Invoke();
            }

            private readonly Lock _gate = new();
            private int _references = 1;
        }

        private readonly Lock _gate = new();
        private readonly SharedLifetime? _shared;
        private Task? _disposeTask;
    }

    /// <summary>
    /// An owned settings snapshot, used to keep the previous configuration
    /// available throughout replacement and rollback.
    /// </summary>
    internal sealed class PubSubShadowEgressSettingsSnapshot(
        Dictionary<string, PubSubShadowEgressSettings> settings) : IDisposable, IAsyncDisposable
    {
        public Dictionary<string, PubSubShadowEgressSettings> Settings { get; } = settings;

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(PubSubShadowEventClientLease.ReleaseAllAsync(
                Settings.Values.Select(settings => settings.ClientLease)));
        }
    }
}
