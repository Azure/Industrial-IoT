// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.PubSub
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class PubSubTestServiceCollectionEx
    {
        public static IServiceCollection AddIsolatedPubSubShadowHost(
            this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddSingleton<IPubSubIdentityRegistryStore, PubSubTestIdentityStore>();
            return services.AddPubSubShadowHost();
        }

        public static IServiceCollection AddIsolatedPubSubShadowEgressHost(
            this IServiceCollection services, IEventClient eventClient,
            Action<PubSubShadowEgressOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddSingleton<IPubSubIdentityRegistryStore, PubSubTestIdentityStore>();
            return services.AddPubSubShadowEgressHost(eventClient, configure);
        }
    }

    internal sealed class PubSubTestIdentityStore : IPubSubIdentityRegistryStore
    {
        public ValueTask<PubSubIdentityRegistrySnapshot> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                return new ValueTask<PubSubIdentityRegistrySnapshot>(Clone(_snapshot));
            }
        }

        public ValueTask SaveAsync(PubSubIdentityRegistrySnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                _snapshot = Clone(snapshot);
            }
            return default;
        }

        private static PubSubIdentityRegistrySnapshot Clone(
            PubSubIdentityRegistrySnapshot snapshot)
        {
            return new PubSubIdentityRegistrySnapshot
            {
                Entries = snapshot.Entries.ConvertAll(entry =>
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = entry.Scope,
                        Id = entry.Id,
                        Value = entry.Value
                    })
            };
        }

        private readonly Lock _gate = new();
        private PubSubIdentityRegistrySnapshot _snapshot = new();
    }
}
