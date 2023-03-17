// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Autofac;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Service client
    /// </summary>
    public sealed class ServiceClient : IDisposable
    {
        /// <summary>
        /// Twin API
        /// </summary>
        public ITwinServiceApi Twin { get; }

        /// <summary>
        /// Registry API
        /// </summary>
        public IRegistryServiceApi Registry { get; }

        /// <summary>
        /// Registry events
        /// </summary>
        public IRegistryServiceEvents Events { get; }

        /// <summary>
        /// History API
        /// </summary>
        public IHistoryServiceApi History { get; }

        /// <summary>
        /// Publisher API
        /// </summary>
        public IPublisherServiceApi Publisher { get; }

        /// <summary>
        /// Telemetry Events
        /// </summary>
        public IPublisherServiceEvents Telemetry { get; }

        /// <summary>
        /// Serializer
        /// </summary>
        public IJsonSerializer Serializer { get; }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="useMsgPack"></param>
        public ServiceClient(bool useMsgPack = false) : this(new ConfigurationBuilder()
            .AddFromDotEnvFile()
            .AddEnvironmentVariables()
            .Build(), useMsgPack)
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="useMsgPack"></param>
        public ServiceClient(IConfiguration configuration, bool useMsgPack = false)
        {
            var container = ConfigureContainer(configuration, useMsgPack);
            _scope = container.BeginLifetimeScope();
            Twin = _scope.Resolve<ITwinServiceApi>();
            Registry = _scope.Resolve<IRegistryServiceApi>();
            History = _scope.Resolve<IHistoryServiceApi>();
            Publisher = _scope.Resolve<IPublisherServiceApi>();
            Serializer = _scope.Resolve<IJsonSerializer>();
            Telemetry = _scope.Resolve<IPublisherServiceEvents>();
            Events = _scope.Resolve<IRegistryServiceEvents>();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _scope.Dispose();
        }

        /// <summary>
        /// Create container
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="useMsgPack"></param>
        /// <returns></returns>
        private static IContainer ConfigureContainer(IConfiguration configuration,
            bool useMsgPack)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(configuration)
                .AsImplementedInterfaces();
            builder.RegisterModule<ServiceSdk>();
            if (useMsgPack)
            {
                builder.AddMessagePackSerializer();
            }
            return builder.Build();
        }

        private readonly ILifetimeScope _scope;
    }
}
