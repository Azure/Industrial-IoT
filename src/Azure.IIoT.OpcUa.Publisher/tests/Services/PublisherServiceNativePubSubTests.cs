// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class PublisherServiceNativePubSubTests
    {
        [Fact]
        public async Task PushesWriterGroupsIntoTheNativeHostAsync()
        {
            var host = new RecordingPubSubShadowHost();
            await using var publisher = CreatePublisher(host);
            var writerGroup = CreateWriterGroup();

            await publisher.UpdateAsync([writerGroup]);

            var pushed = Assert.Single(host.WriterGroups);
            Assert.Equal(writerGroup.Id, pushed.Id);
        }

        [Fact]
        public async Task UpdatesWithoutANativeHostAsync()
        {
            await using var publisher = CreatePublisher();

            await publisher.UpdateAsync([CreateWriterGroup()]);

            Assert.Single(publisher.WriterGroups);
        }

        [Fact]
        public async Task SuccessfulUpdateCompletesWithoutFaultingAsync()
        {
            //
            // A successful reconcile must not fall through into the failure
            // paths and complete the caller's task with an empty aggregate.
            //
            await using var publisher = CreatePublisher(new RecordingPubSubShadowHost());

            await publisher.UpdateAsync([CreateWriterGroup()]);
            await publisher.UpdateAsync([CreateWriterGroup()]);

            Assert.Single(publisher.WriterGroups);
        }

        [Fact]
        public async Task NativeHostFailureFaultsTheUpdateAsync()
        {
            await using var publisher = CreatePublisher(new ThrowingPubSubShadowHost());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => publisher.UpdateAsync([CreateWriterGroup()]));

            Assert.Equal("native host rejected the configuration", exception.Message);
        }

        private sealed class ThrowingPubSubShadowHost : IPubSubShadowHost
        {
            public ValueTask ReplaceConfigurationAsync(
                IEnumerable<WriterGroupModel> writerGroups,
                CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException(
                    "native host rejected the configuration");
            }
        }

        private static PublisherService CreatePublisher(
            IPubSubShadowHost? pubSubShadowHost = null)
        {
            return new PublisherService(new TestWriterGroupScopeFactory(),
                Options.Create(new PublisherOptions { PublisherId = "publisher" }),
                NullLogger<PublisherService>.Instance,
                pubSubShadowHost: pubSubShadowHost);
        }

        private static WriterGroupModel CreateWriterGroup()
        {
            return new WriterGroupModel
            {
                Id = "group",
                DataSetWriters =
                [
                    new DataSetWriterModel
                    {
                        Id = "writer",
                        DataSet = new PublishedDataSetModel { Name = "dataset" }
                    }
                ]
            };
        }

        private sealed class RecordingPubSubShadowHost : IPubSubShadowHost
        {
            public IReadOnlyList<WriterGroupModel> WriterGroups { get; private set; } = [];

            public ValueTask ReplaceConfigurationAsync(
                IEnumerable<WriterGroupModel> writerGroups,
                CancellationToken cancellationToken = default)
            {
                WriterGroups = writerGroups.ToArray();
                return ValueTask.CompletedTask;
            }
        }

        private sealed class TestWriterGroupScopeFactory : IWriterGroupScopeFactory
        {
            public IWriterGroupScope Create(WriterGroupModel writerGroup)
            {
                return new TestWriterGroupScope(new TestWriterGroupControl());
            }
        }

        private sealed class TestWriterGroupScope : IWriterGroupScope
        {
            public TestWriterGroupScope(IWriterGroupControl writerGroup)
            {
                WriterGroup = writerGroup;
            }

            public IWriterGroupControl WriterGroup { get; }

            public void Dispose()
            {
            }
        }

        private sealed class TestWriterGroupControl : IWriterGroupControl
        {
            public ValueTask StartAsync(CancellationToken ct)
            {
                return ValueTask.CompletedTask;
            }

            public ValueTask UpdateAsync(WriterGroupModel writerGroup,
                CancellationToken ct)
            {
                return ValueTask.CompletedTask;
            }

            public ValueTask SendKeyFrameAsync(string? dataSetWriterId,
                CancellationToken ct)
            {
                return ValueTask.CompletedTask;
            }

            public ValueTask<WriterGroupStateDiagnosticModel> GetStateAsync(
                CancellationToken ct)
            {
                return ValueTask.FromResult(new WriterGroupStateDiagnosticModel
                {
                    Id = "group",
                    DataSetWriters = []
                });
            }
        }
    }
}
