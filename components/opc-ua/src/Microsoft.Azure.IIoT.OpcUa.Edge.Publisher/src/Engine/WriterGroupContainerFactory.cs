// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Autofac;
    using Autofac.Core.Lifetime;

    /// <summary>
    /// Container builder for data set writer jobs
    /// </summary>
    public class WriterGroupContainerFactory : IWriterGroupContainerFactory {

        /// <inheritdoc/>
        public string PublisherId => _publisherId;

        /// <summary>
        /// Create job scope factory
        /// </summary>
        /// <param name="lifetimeScope"></param>
        public WriterGroupContainerFactory(LifetimeScope lifetimeScope) {
            _lifetimeScope = lifetimeScope;
            _publisherId = GetPublisherId();
        }

        /// <inheritdoc/>
        public IWriterGroup CreateWriterGroupScope(IWriterGroupConfig config) {
            return new WriterGroupScope(_lifetimeScope, config);
        }

        /// <summary>
        /// Scope wrapper
        /// </summary>
        private sealed class WriterGroupScope : IWriterGroup {

            /// <inheritdoc/>
            public IMessageTrigger Source => _lifetimeScope.Resolve<IMessageTrigger>();

            /// <summary>
            /// Create scope
            /// </summary>
            /// <param name="lifetimeScope"></param>
            /// <param name="config"></param>
            public WriterGroupScope(ILifetimeScope lifetimeScope, IWriterGroupConfig config) {
                _lifetimeScope = lifetimeScope.BeginLifetimeScope(builder => {
                    // Register job configuration
                    builder.RegisterInstance(config)
                        .AsImplementedInterfaces();

                    // Register default serializers...
                    builder.RegisterModule<NewtonSoftJsonModule>();

                    // Register processing engine - trigger, transform, sink
                    builder.RegisterType<DataFlowProcessingEngine>()
                        .AsImplementedInterfaces();
                    builder.RegisterType<WriterGroupMessageSource>()
                        .AsImplementedInterfaces();
                    builder.RegisterType<NetworkMessageEncoder>()
                        .AsImplementedInterfaces();
                    builder.RegisterType<IoTHubMessageSink>()
                        .AsImplementedInterfaces();
                });
            }

            /// <inheritdoc/>
            public void Dispose() {
                _lifetimeScope.Dispose();
            }

            private readonly ILifetimeScope _lifetimeScope;
        }

        /// <summary>
        /// Create publisher id
        /// </summary>
        private string GetPublisherId() {
            _lifetimeScope.TryResolve(out IIdentity identity);
            var site = identity?.SiteId == null ? "" : identity.SiteId + "_";
            return identity?.ModuleId == null ?
                $"{site}{identity?.DeviceId ?? "Publisher"}" :
                $"{site}{identity.DeviceId}_{identity.ModuleId}";
        }

        private readonly LifetimeScope _lifetimeScope;
        private readonly string _publisherId;
    }
}