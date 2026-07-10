// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients;
    using Azure.IIoT.OpcUa.Core.Storage;
    using Azure.IIoT.OpcUa.Core.Storage.Services;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// <see cref="IServiceCollection"/> registrations for the in-repo
    /// <c>Azure.IIoT.OpcUa.Core</c> messaging and storage implementations that
    /// replace the corresponding Furly.Extensions services.
    /// </summary>
    public static class CoreServiceCollectionEx
    {
        /// <summary>
        /// Add the in-memory <see cref="IKeyValueStore"/> fallback.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddMemoryKeyValueStore(
            this IServiceCollection services)
        {
            return services.AddSingletonAsImplementedInterfaces<MemoryKVStore>();
        }

        /// <summary>
        /// Add the null <see cref="IEventClient"/> fallback.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddNullEventClient(
            this IServiceCollection services)
        {
            return services.AddAs<NullEventClient>(ServiceLifetime.Transient,
                typeof(IEventClient));
        }
    }
}
