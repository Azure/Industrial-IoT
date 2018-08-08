// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Control;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Stack;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Autofac;
    using System;

    public class TwinContainerFactory : IContainerFactory {

        /// <summary>
        /// Create twin container factory
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public TwinContainerFactory(IProtocolClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public IContainer Create() {

            // Create container for all twin level scopes...
            var builder = new ContainerBuilder();

            // Register logger singleton instance
            builder.RegisterInstance(_logger)
                .AsImplementedInterfaces().SingleInstance();

            // Register opc ua client singleton instance
            builder.RegisterInstance(_client)
                .AsImplementedInterfaces().SingleInstance();

            // Register edge host module and twin state for the lifetime of the host
            // builder.RegisterType<OpcUaPublisherServices>()
            //     .AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<PublisherServicesStub>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterModule<EdgeHostModule>();

            // Register opc ua services
            builder.RegisterType<AddressSpaceServices>()
                .AsImplementedInterfaces();
            builder.RegisterType<JsonVariantCodec>()
                .AsImplementedInterfaces();

            // Build twin container
            return builder.Build();
        }

        private readonly IProtocolClient _client;
        private readonly ILogger _logger;
    }
}
