// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Autofac;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Container builder for data set writer jobs
    /// </summary>
    public class WriterGroupScopeFactory : IWriterGroupScopeFactory
    {
        /// <summary>
        /// Create job scope factory
        /// </summary>
        /// <param name="lifetimeScope"></param>
        public WriterGroupScopeFactory(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
            lifetimeScope.TryResolve(out _collector);
        }

        /// <inheritdoc/>
        public IWriterGroupScope Create(IWriterGroupConfig config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            return new WriterGroupScope(this, config, _lifetimeScope.Resolve<IJsonSerializer>());
        }

        /// <summary>
        /// Scope wrapper
        /// </summary>
        private sealed class WriterGroupScope : IWriterGroupScope, IMetricsContext,
            IWriterGroupDiagnostics
        {
            /// <inheritdoc/>
            public IWriterGroup WriterGroup => _scope.Resolve<IWriterGroup>();

            /// <inheritdoc/>
            public TagList TagList { get; }

            /// <summary>
            /// Create scope
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="config"></param>
            /// <param name="serializer"></param>
            public WriterGroupScope(WriterGroupScopeFactory outer, IWriterGroupConfig config,
                IJsonSerializer serializer)
            {
                _writerGroup = config.WriterGroup?.WriterGroupId ?? Constants.DefaultWriterGroupId;
                _outer = outer;

                TagList = new TagList(new[] {
                    new KeyValuePair<string, object>(Constants.PublisherIdTag,
                        config.PublisherId ?? Constants.DefaultPublisherId),
                    new KeyValuePair<string, object>(Constants.WriterGroupIdTag,
                        _writerGroup),
                    new KeyValuePair<string, object>(Constants.TimeStampTag,
                        DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
                        CultureInfo.InvariantCulture))
                });

                _scope = _outer._lifetimeScope.BeginLifetimeScope(builder =>
                {
                    // Register job configuration
                    builder.RegisterInstance(config)
                        .AsImplementedInterfaces();

                    builder.RegisterInstance(this)
                        .As<IWriterGroupDiagnostics>()
                        .As<IMetricsContext>().SingleInstance();
                    builder.RegisterInstance(serializer)
                        .As<IJsonSerializer>()
                        .ExternallyOwned();

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

                ResetWriterGroupDiagnostics();
            }

            /// <inheritdoc/>
            public void ResetWriterGroupDiagnostics()
            {
                _outer._collector?.ResetWriterGroup(_writerGroup);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _outer._collector?.RemoveWriterGroup(_writerGroup);
                _scope.Dispose();
            }

            private readonly string _writerGroup;
            private readonly WriterGroupScopeFactory _outer;
            private readonly ILifetimeScope _scope;
        }

        private readonly ILifetimeScope _lifetimeScope;
        private readonly IPublisherDiagnosticCollector _collector;
    }
}
