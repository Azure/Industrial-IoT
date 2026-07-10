// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Creates per writer group service scopes. The Autofac implementation used a
    /// child lifetime scope; on Microsoft.Extensions.DependencyInjection a child
    /// scope is created via <see cref="IServiceScopeFactory"/> and the scope
    /// specific state (the writer group model, diagnostics and metrics context) is
    /// pushed into the scoped <see cref="WriterGroupScopeContext"/> before the
    /// data flow engine (<see cref="IWriterGroupControl"/>) is resolved.
    /// </summary>
    public sealed class WriterGroupScopeFactory : IWriterGroupScopeFactory
    {
        /// <summary>
        /// Create job scope factory
        /// </summary>
        /// <param name="scopeFactory"></param>
        /// <param name="options"></param>
        /// <param name="collector"></param>
        public WriterGroupScopeFactory(IServiceScopeFactory scopeFactory,
            IOptions<PublisherOptions>? options = null,
            IDiagnosticCollector? collector = null)
        {
            _scopeFactory = scopeFactory;
            _collector = collector;
            _options = options;
        }

        /// <inheritdoc/>
        public IWriterGroupScope Create(WriterGroupModel writerGroup)
        {
            var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WriterGroupScopeContext>();
            context.Initialize(writerGroup, _options, _collector);
            return new WriterGroupScope(scope, context);
        }

        /// <summary>
        /// Scope wrapper owning the child service scope
        /// </summary>
        private sealed class WriterGroupScope : IWriterGroupScope
        {
            /// <inheritdoc/>
            public IWriterGroupControl WriterGroup
                => _scope.ServiceProvider.GetRequiredService<IWriterGroupControl>();

            /// <summary>
            /// Create scope
            /// </summary>
            /// <param name="scope"></param>
            /// <param name="context"></param>
            public WriterGroupScope(IServiceScope scope, WriterGroupScopeContext context)
            {
                _scope = scope;
                _context = context;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _context.RemoveWriterGroup();
                _scope.Dispose();
            }

            private readonly IServiceScope _scope;
            private readonly WriterGroupScopeContext _context;
        }

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDiagnosticCollector? _collector;
        private readonly IOptions<PublisherOptions>? _options;
    }

    /// <summary>
    /// Holds the per writer group scoped state (writer group model, diagnostics
    /// and metrics tag list) that the data flow services depend on.
    /// Registered as a scoped service and initialized by the
    /// <see cref="WriterGroupScopeFactory"/> right after the scope is created.
    /// </summary>
    internal sealed class WriterGroupScopeContext : IMetricsContext, IWriterGroupDiagnostics
    {
        /// <inheritdoc/>
        public TagList TagList { get; private set; }

        /// <summary>
        /// The writer group processed in this scope
        /// </summary>
        public WriterGroupModel WriterGroup { get; private set; } = null!;

        /// <summary>
        /// Initialize the scope context
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <param name="collector"></param>
        public void Initialize(WriterGroupModel writerGroup,
            IOptions<PublisherOptions>? options, IDiagnosticCollector? collector)
        {
            WriterGroup = writerGroup;
            _collector = collector;
            _writerGroupId = writerGroup.Id;

            TagList = new TagList(
            [
                new KeyValuePair<string, object?>(Publisher.Constants.SiteIdTag,
                    options?.Value.SiteId),
                new KeyValuePair<string, object?>(Publisher.Constants.PublisherIdTag,
                    writerGroup.PublisherId ?? options?.Value.PublisherId),
                new KeyValuePair<string, object?>(Publisher.Constants.WriterGroupIdTag,
                    _writerGroupId),
                new KeyValuePair<string, object?>(Publisher.Constants.WriterGroupNameTag,
                    writerGroup.Name)
            ]);

            ResetWriterGroupDiagnostics();
        }

        /// <inheritdoc/>
        public void ResetWriterGroupDiagnostics()
        {
            _collector?.ResetWriterGroup(_writerGroupId);
        }

        /// <summary>
        /// Remove the writer group diagnostics when the scope is disposed
        /// </summary>
        public void RemoveWriterGroup()
        {
            _collector?.RemoveWriterGroup(_writerGroupId);
        }

        private IDiagnosticCollector? _collector;
        private string _writerGroupId = string.Empty;
    }
}
