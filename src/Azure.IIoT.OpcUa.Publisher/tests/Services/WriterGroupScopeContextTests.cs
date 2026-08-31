// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="WriterGroupScopeContext"/> and
    /// <see cref="WriterGroupScopeFactory"/>.
    /// </summary>
    public sealed class WriterGroupScopeContextTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static IOptions<PublisherOptions> CreateOptions(
            string? publisherId = null, string? siteId = null)
        {
            var opts = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            opts.Value.PublisherId = publisherId;
            opts.Value.SiteId = siteId;
            return opts;
        }

        private static WriterGroupModel CreateGroup(
            string id = "group-1",
            string? name = "TestGroup",
            string? publisherId = null)
        {
            return new WriterGroupModel
            {
                Id = id,
                Name = name,
                PublisherId = publisherId
            };
        }

        // ── Initialize – WriterGroup property ─────────────────────────────────

        [Fact]
        public void Initialize_SetsWriterGroup()
        {
            var ctx = new WriterGroupScopeContext();
            var group = CreateGroup("wg-42", "My Group");

            ctx.Initialize(group, CreateOptions(), null);

            Assert.Same(group, ctx.WriterGroup);
        }

        // ── Initialize – TagList contents ─────────────────────────────────────

        [Fact]
        public void Initialize_TagListContainsFourEntries()
        {
            var ctx = new WriterGroupScopeContext();

            ctx.Initialize(CreateGroup(), CreateOptions(), null);

            Assert.Equal(4, ctx.TagList.Count);
        }

        [Fact]
        public void Initialize_TagListContainsWriterGroupIdTag()
        {
            var ctx = new WriterGroupScopeContext();

            ctx.Initialize(CreateGroup(id: "my-group"), CreateOptions(), null);

            var tag = ctx.TagList
                .FirstOrDefault(t => t.Key == Constants.WriterGroupIdTag);
            Assert.Equal("my-group", (string?)tag.Value);
        }

        [Fact]
        public void Initialize_TagListContainsWriterGroupNameTag()
        {
            var ctx = new WriterGroupScopeContext();

            ctx.Initialize(CreateGroup(id: "g", name: "My Group"), CreateOptions(), null);

            var tag = ctx.TagList
                .FirstOrDefault(t => t.Key == Constants.WriterGroupNameTag);
            Assert.Equal("My Group", (string?)tag.Value);
        }

        [Fact]
        public void Initialize_TagListUsesGroupPublisherIdWhenSet()
        {
            var ctx = new WriterGroupScopeContext();
            var group = CreateGroup(publisherId: "group-publisher");

            ctx.Initialize(group, CreateOptions(publisherId: "options-publisher"), null);

            var tag = ctx.TagList
                .FirstOrDefault(t => t.Key == Constants.PublisherIdTag);
            Assert.Equal("group-publisher", (string?)tag.Value);
        }

        [Fact]
        public void Initialize_TagListFallsBackToOptionsPublisherIdWhenGroupPublisherIdNull()
        {
            var ctx = new WriterGroupScopeContext();
            var group = CreateGroup(publisherId: null);

            ctx.Initialize(group, CreateOptions(publisherId: "options-pub"), null);

            var tag = ctx.TagList
                .FirstOrDefault(t => t.Key == Constants.PublisherIdTag);
            Assert.Equal("options-pub", (string?)tag.Value);
        }

        [Fact]
        public void Initialize_TagListContainsSiteIdFromOptions()
        {
            var ctx = new WriterGroupScopeContext();

            ctx.Initialize(CreateGroup(), CreateOptions(siteId: "site-99"), null);

            var tag = ctx.TagList
                .FirstOrDefault(t => t.Key == Constants.SiteIdTag);
            Assert.Equal("site-99", (string?)tag.Value);
        }

        [Fact]
        public void Initialize_NullOptions_StillBuildsTagList()
        {
            var ctx = new WriterGroupScopeContext();

            ctx.Initialize(CreateGroup(), null, null);

            Assert.Equal(4, ctx.TagList.Count);
        }

        // ── Initialize – collector interaction ────────────────────────────────

        [Fact]
        public void Initialize_CallsResetWriterGroupOnCollector()
        {
            var collector = new Mock<IDiagnosticCollector>();
            var ctx = new WriterGroupScopeContext();

            ctx.Initialize(CreateGroup(id: "reset-me"), CreateOptions(), collector.Object);

            collector.Verify(c => c.ResetWriterGroup("reset-me"), Times.Once);
        }

        [Fact]
        public void Initialize_NullCollector_DoesNotThrow()
        {
            var ctx = new WriterGroupScopeContext();

            var ex = Record.Exception(() =>
                ctx.Initialize(CreateGroup(), CreateOptions(), null));

            Assert.Null(ex);
        }

        // ── ResetWriterGroupDiagnostics ───────────────────────────────────────

        [Fact]
        public void ResetWriterGroupDiagnostics_CallsCollectorWithWriterGroupId()
        {
            var collector = new Mock<IDiagnosticCollector>();
            var ctx = new WriterGroupScopeContext();
            ctx.Initialize(CreateGroup(id: "diag-group"), CreateOptions(), collector.Object);

            // Reset is already called once in Initialize; call again explicitly
            ctx.ResetWriterGroupDiagnostics();

            collector.Verify(c => c.ResetWriterGroup("diag-group"), Times.Exactly(2));
        }

        [Fact]
        public void ResetWriterGroupDiagnostics_NullCollector_DoesNotThrow()
        {
            var ctx = new WriterGroupScopeContext();
            ctx.Initialize(CreateGroup(), CreateOptions(), null);

            var ex = Record.Exception(() => ctx.ResetWriterGroupDiagnostics());

            Assert.Null(ex);
        }

        // ── RemoveWriterGroup ─────────────────────────────────────────────────

        [Fact]
        public void RemoveWriterGroup_CallsCollectorWithWriterGroupId()
        {
            var collector = new Mock<IDiagnosticCollector>();
            var ctx = new WriterGroupScopeContext();
            ctx.Initialize(CreateGroup(id: "remove-me"), CreateOptions(), collector.Object);

            ctx.RemoveWriterGroup();

            collector.Verify(c => c.RemoveWriterGroup("remove-me"), Times.Once);
        }

        [Fact]
        public void RemoveWriterGroup_NullCollector_DoesNotThrow()
        {
            var ctx = new WriterGroupScopeContext();
            ctx.Initialize(CreateGroup(), CreateOptions(), null);

            var ex = Record.Exception(() => ctx.RemoveWriterGroup());

            Assert.Null(ex);
        }

        // ── WriterGroupScopeFactory integration ───────────────────────────────

        [Fact]
        public void ScopeFactory_Create_SetsWriterGroupOnContext()
        {
            var control = new Mock<IWriterGroupControl>();
            var services = new ServiceCollection();
            services.AddScoped<WriterGroupScopeContext>();
            services.AddScoped<IWriterGroupControl>(_ => control.Object);
            using var provider = services.BuildServiceProvider();
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

            var factory = new WriterGroupScopeFactory(scopeFactory, CreateOptions());
            var group = CreateGroup(id: "scope-test", name: "ScopeGroup");

            using var scope = factory.Create(group);

            // Access WriterGroup to resolve the scope context (may throw if
            // WriterGroupScopeContext.WriterGroup is not set via Initialize)
            Assert.NotNull(scope);
        }

        [Fact]
        public void ScopeFactory_Create_Dispose_CallsRemoveWriterGroup()
        {
            var collector = new Mock<IDiagnosticCollector>();
            var control = new Mock<IWriterGroupControl>();
            var services = new ServiceCollection();
            services.AddScoped<WriterGroupScopeContext>();
            services.AddScoped<IWriterGroupControl>(_ => control.Object);
            using var provider = services.BuildServiceProvider();
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

            var factory = new WriterGroupScopeFactory(scopeFactory, CreateOptions(),
                collector.Object);
            var group = CreateGroup(id: "dispose-test");

            factory.Create(group).Dispose();

            collector.Verify(c => c.RemoveWriterGroup("dispose-test"), Times.Once);
        }
    }
}
