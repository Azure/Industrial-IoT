// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Autofac;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Container builder for data set writer jobs
    /// </summary>
    public class WriterGroupScopeFactory : IWriterGroupScopeFactory
    {
        /// <summary>
        /// Create job scope factory
        /// </summary>
        /// <param name="lifetimeScope"></param>
        /// <param name="serializer"></param>
        /// <param name="options"></param>
        /// <param name="collector"></param>
        public WriterGroupScopeFactory(ILifetimeScope lifetimeScope, IJsonSerializer serializer,
            IOptions<PublisherOptions>? options = null, IDiagnosticCollector? collector = null)
        {
            _lifetimeScope = lifetimeScope;
            _serializer = serializer;
            _collector = collector;
            _options = options;
        }

        /// <inheritdoc/>
        public IWriterGroupScope Create(WriterGroupModel writerGroup)
        {
            return new WriterGroupScope(this, writerGroup, _serializer);
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
            /// <param name="writerGroup"></param>
            /// <param name="serializer"></param>
            public WriterGroupScope(WriterGroupScopeFactory outer,
                WriterGroupModel writerGroup, IJsonSerializer serializer)
            {
                _outer = outer;
                _writerGroup = writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId;

                TagList = new TagList(new[]
                {
                    new KeyValuePair<string, object?>(Constants.SiteIdTag,
                        _outer._options?.Value.Site ?? Constants.DefaultSite),
                    new KeyValuePair<string, object?>(Constants.PublisherIdTag,
                        _outer._options?.Value.PublisherId ?? Constants.DefaultPublisherId),
                    new KeyValuePair<string, object?>(Constants.WriterGroupIdTag,
                        _writerGroup)
                });

                _scope = _outer._lifetimeScope.BeginLifetimeScope(builder =>
                {
                    // Register writer group for the scope
                    builder.RegisterInstance(writerGroup).As<WriterGroupModel>();

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
        private readonly IJsonSerializer _serializer;
        private readonly IDiagnosticCollector? _collector;
        private readonly IOptions<PublisherOptions>? _options;
    }
}
