// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Autofac;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Globalization;
    using System;

    /// <summary>
    /// Container builder for data set writer jobs
    /// </summary>
    public class WriterGroupScopeFactory : IWriterGroupScopeFactory {

        /// <inheritdoc/>
        public string PublisherId => _publisherId;

        /// <summary>
        /// Create job scope factory
        /// </summary>
        /// <param name="lifetimeScope"></param>
        public WriterGroupScopeFactory(ILifetimeScope lifetimeScope) {
            _lifetimeScope = lifetimeScope;
            _publisherId = GetPublisherId();
        }

        /// <inheritdoc/>
        public IWriterGroupScope Create(IWriterGroupConfig config) {
            return new WriterGroupScope(_lifetimeScope, PublisherId, config);
        }

        /// <summary>
        /// Scope wrapper
        /// </summary>
        private sealed class WriterGroupScope : IWriterGroupScope, IMetricsContext {

            /// <inheritdoc/>
            public IWriterGroup WriterGroup => _lifetimeScope.Resolve<IWriterGroup>();

            /// <inheritdoc/>
            public TagList TagList { get; }

            /// <summary>
            /// Create scope
            /// </summary>
            /// <param name="lifetimeScope"></param>
            /// <param name="publisherId"></param>
            /// <param name="config"></param>
            public WriterGroupScope(ILifetimeScope lifetimeScope, string publisherId,
                IWriterGroupConfig config) {

                TagList = new TagList(new[] {
                    new KeyValuePair<string, object>("publisherId", publisherId),
                    new KeyValuePair<string, object>("writerGroupId", config.WriterGroup?.WriterGroupId),
                    new KeyValuePair<string, object>("timestamp_utc",
                        DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
                        CultureInfo.InvariantCulture))
                });

                _lifetimeScope = lifetimeScope.BeginLifetimeScope(builder => {
                    // Register job configuration
                    builder.RegisterInstance(config)
                        .AsImplementedInterfaces();
                    builder.RegisterInstance(this)
                        .As<IMetricsContext>().SingleInstance();

                    // Register default serializers...
                    builder.RegisterModule<NewtonSoftJsonModule>();

                    // Register data flow - source, encode, sink
                    builder.RegisterType<WriterGroupDataFlow>()
                        .AsImplementedInterfaces();
                    builder.RegisterType<WriterGroupDataSource>()
                        .AsImplementedInterfaces();
                    builder.RegisterType<NetworkMessageEncoder>()
                        .AsImplementedInterfaces();
                    builder.RegisterType<NetworkMessageSink>()
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

        private readonly ILifetimeScope _lifetimeScope;
        private readonly string _publisherId;
    }
}