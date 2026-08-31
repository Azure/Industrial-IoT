// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class WriterControllerTests
    {
        [Fact]
        public async Task AddOrUpdateNodeWrapsSingleNodeAsync()
        {
            var publisher = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = CreateController(publisher: publisher);
            var node = new OpcNodeModel { Id = "ns=2;s=Temperature" };

            publisher.Setup(p => p.AddOrUpdateNodesAsync("group", "writer",
                    It.Is<IReadOnlyList<OpcNodeModel>>(nodes =>
                        nodes.Count == 1 && ReferenceEquals(nodes[0], node)),
                    "after", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await controller.AddOrUpdateNodeAsync("group", "writer", node, "after");

            publisher.Verify();
        }

        [Fact]
        public async Task RemoveNodeWrapsSingleDataSetFieldIdAsync()
        {
            var publisher = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = CreateController(publisher: publisher);

            publisher.Setup(p => p.RemoveNodesAsync("group", "writer",
                    It.Is<IReadOnlyList<string>>(ids =>
                        ids.Count == 1 && ids[0] == "field"),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await controller.RemoveNodeAsync("group", "writer", "field");

            publisher.Verify();
        }

        [Fact]
        public async Task GetNodesUsesContinuationHeadersOverQueryValuesAsync()
        {
            var publisher = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = CreateController(publisher: publisher);
            var expected = new List<OpcNodeModel>
            {
                new() { Id = "ns=2;s=Pressure" }
            };
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["x-ms-continuation"] = "header-field";
            httpContext.Request.Headers["x-ms-max-item-count"] = "25";

            publisher.Setup(p => p.GetNodesAsync("group", "writer",
                    "header-field", 25, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.GetNodesAsync("group", "writer",
                "query-field", 10, httpContext.Request);

            Assert.Same(expected, actual);
            publisher.Verify();
        }

        [Fact]
        public async Task GetNodesUsesQueryValuesWhenHeadersAreAbsentAsync()
        {
            var publisher = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = CreateController(publisher: publisher);
            var expected = Array.Empty<OpcNodeModel>();

            publisher.Setup(p => p.GetNodesAsync("group", "writer",
                    "query-field", 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.GetNodesAsync("group", "writer",
                "query-field", 10, new DefaultHttpContext().Request);

            Assert.Same(expected, actual);
            publisher.Verify();
        }

        [Fact]
        public async Task GetNodesRejectsInvalidMaxItemCountHeaderAsync()
        {
            var publisher = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = CreateController(publisher: publisher);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["x-ms-max-item-count"] = "invalid";

            await Assert.ThrowsAsync<FormatException>(() => controller.GetNodesAsync(
                "group", "writer", httpRequest: httpContext.Request));

            publisher.Verify(p => p.GetNodesAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveDataSetWriterEntryPassesForceFlagAsync()
        {
            var publisher = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = CreateController(publisher: publisher);

            publisher.Setup(p => p.RemoveDataSetWriterEntryAsync("group", "writer",
                    true, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await controller.RemoveDataSetWriterEntryAsync("group", "writer", true);

            publisher.Verify();
        }

        [Fact]
        public async Task ExpandWriterUsesDefaultExpansionThatDiscardsErrorsAsync()
        {
            var configuration = new Mock<IConfigurationServices>(MockBehavior.Strict);
            var controller = CreateController(configuration: configuration);
            var entry = CreateEntry();
            var expected = new ServiceResponse<PublishedNodesEntryModel>
            {
                Result = CreateEntry()
            };

            configuration.Setup(c => c.ExpandAsync(entry,
                    It.Is<PublishedNodeExpansionModel>(model => model.DiscardErrors),
                    It.IsAny<CancellationToken>()))
                .Returns(ToAsyncEnumerable(expected))
                .Verifiable();

            var actual = await ToListAsync(controller.ExpandWriterAsync(
                new PublishedNodesEntryRequestModel<PublishedNodeExpansionModel>
                {
                    Entry = entry
                }));

            Assert.Same(expected, Assert.Single(actual));
            configuration.Verify();
        }

        [Fact]
        public async Task ExpandAndCreateUsesDefaultExpansionThatKeepsErrorsAsync()
        {
            var configuration = new Mock<IConfigurationServices>(MockBehavior.Strict);
            var controller = CreateController(configuration: configuration);
            var entry = CreateEntry();
            var expected = new ServiceResponse<PublishedNodesEntryModel>
            {
                ErrorInfo = new ServiceResultModel { StatusCode = 1 }
            };

            configuration.Setup(c => c.CreateOrUpdateAsync(entry,
                    It.Is<PublishedNodeExpansionModel>(model => !model.DiscardErrors),
                    It.IsAny<CancellationToken>()))
                .Returns(ToAsyncEnumerable(expected))
                .Verifiable();

            var actual = await ToListAsync(
                controller.ExpandAndCreateOrUpdateDataSetWriterEntriesAsync(
                    new PublishedNodesEntryRequestModel<PublishedNodeExpansionModel>
                    {
                        Entry = entry
                    }));

            Assert.Same(expected, Assert.Single(actual));
            configuration.Verify();
        }

        [Fact]
        public async Task CreateOrUpdateAssetSerializesJsonConfigurationAsync()
        {
            var assets = new Mock<IAssetConfiguration<byte[]>>(MockBehavior.Strict);
            var controller = CreateController(assets: assets);
            PublishedNodeCreateAssetRequestModel<byte[]> captured = null!;
            var expected = new ServiceResponse<PublishedNodesEntryModel>
            {
                Result = CreateEntry()
            };

            assets.Setup(a => a.CreateOrUpdateAssetAsync(
                    It.IsAny<PublishedNodeCreateAssetRequestModel<byte[]>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PublishedNodeCreateAssetRequestModel<byte[]>, CancellationToken>(
                    (request, _) => captured = request)
                .ReturnsAsync(expected)
                .Verifiable();

            var entry = CreateEntry();
            var response = await controller.CreateOrUpdateAssetAsync(
                new PublishedNodeCreateAssetRequestModel<JsonNode>
                {
                    Entry = entry,
                    Header = new RequestHeaderModel(),
                    WaitTime = TimeSpan.FromSeconds(1),
                    Configuration = JsonNode.Parse("""{"name":"asset"}""")
                });

            Assert.Same(expected, response);
            Assert.Same(entry, captured.Entry);
            Assert.Equal(TimeSpan.FromSeconds(1), captured.WaitTime);
            Assert.NotNull(captured.Header);
            Assert.Contains("\"name\":\"asset\"",
                System.Text.Encoding.UTF8.GetString(captured.Configuration));
            assets.Verify();
        }

        [Fact]
        public void GetAllAssetsRejectsMissingEntry()
        {
            var assets = new Mock<IAssetConfiguration<byte[]>>(MockBehavior.Strict);
            var controller = CreateController(assets: assets);

            Assert.Throws<ArgumentNullException>(() => controller.GetAllAssetsAsync(
                new PublishedNodesEntryRequestModel<RequestHeaderModel>
                {
                    Entry = null!
                }));
        }

        [Fact]
        public async Task DeleteAssetDelegatesToAssetServiceAsync()
        {
            var assets = new Mock<IAssetConfiguration<byte[]>>(MockBehavior.Strict);
            var controller = CreateController(assets: assets);
            var request = new PublishedNodeDeleteAssetRequestModel
            {
                Entry = CreateEntry()
            };
            var expected = new ServiceResultModel { StatusCode = 7 };

            assets.Setup(a => a.DeleteAssetAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.DeleteAssetAsync(request);

            Assert.Same(expected, actual);
            assets.Verify();
        }

        [Fact]
        public async Task CreateOrUpdateDataSetWriterEntryDelegatesToPublisherAsync()
        {
            var publisher = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = CreateController(publisher: publisher);
            var entry = CreateEntry();

            publisher.Setup(p => p.CreateOrUpdateDataSetWriterEntryAsync(
                    entry, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await controller.CreateOrUpdateDataSetWriterEntryAsync(entry);

            publisher.Verify();
        }

        [Fact]
        public async Task CreateOrUpdateDataSetWriterEntryThrowsOnNullEntryAsync()
        {
            var controller = CreateController();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                controller.CreateOrUpdateDataSetWriterEntryAsync(null!));
        }

        [Fact]
        public async Task GetDataSetWriterEntryDelegatesToPublisherAsync()
        {
            var publisher = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = CreateController(publisher: publisher);
            var expected = CreateEntry();

            publisher.Setup(p => p.GetDataSetWriterEntryAsync(
                    "group", "writer", It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.GetDataSetWriterEntryAsync("group", "writer");

            Assert.Same(expected, actual);
            publisher.Verify();
        }

        [Fact]
        public async Task GetDataSetWriterEntryThrowsOnNullGroupAsync()
        {
            var controller = CreateController();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                controller.GetDataSetWriterEntryAsync(null!, "writer"));
        }

        [Fact]
        public async Task AddOrUpdateNodesDelegatesToPublisherAsync()
        {
            var publisher = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = CreateController(publisher: publisher);
            var nodes = new List<OpcNodeModel> { new() { Id = "ns=2;s=Temp" } };

            publisher.Setup(p => p.AddOrUpdateNodesAsync(
                    "group", "writer", nodes, null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await controller.AddOrUpdateNodesAsync("group", "writer", nodes);

            publisher.Verify();
        }

        [Fact]
        public async Task AddOrUpdateNodesThrowsOnNullGroupAsync()
        {
            var controller = CreateController();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                controller.AddOrUpdateNodesAsync(null!, "writer",
                    new List<OpcNodeModel>()));
        }

        [Fact]
        public async Task RemoveNodesDelegatesToPublisherAsync()
        {
            var publisher = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = CreateController(publisher: publisher);
            var ids = new List<string> { "field1", "field2" };

            publisher.Setup(p => p.RemoveNodesAsync(
                    "group", "writer", ids, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await controller.RemoveNodesAsync("group", "writer", ids);

            publisher.Verify();
        }

        [Fact]
        public async Task RemoveNodesThrowsOnNullFieldIdsAsync()
        {
            var controller = CreateController();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                controller.RemoveNodesAsync("group", "writer", null!));
        }

        [Fact]
        public async Task GetNodeDelegatesToPublisherAsync()
        {
            var publisher = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = CreateController(publisher: publisher);
            var expected = new OpcNodeModel { Id = "ns=2;s=Temp" };

            publisher.Setup(p => p.GetNodeAsync(
                    "group", "writer", "field1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.GetNodeAsync("group", "writer", "field1");

            Assert.Same(expected, actual);
            publisher.Verify();
        }

        [Fact]
        public async Task GetNodeThrowsOnNullFieldIdAsync()
        {
            var controller = CreateController();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                controller.GetNodeAsync("group", "writer", null!));
        }

        [Fact]
        public async Task CreateOrUpdateAsset2DelegatesToAssetServiceAsync()
        {
            var assets = new Mock<IAssetConfiguration<byte[]>>(MockBehavior.Strict);
            var controller = CreateController(assets: assets);
            var request = new PublishedNodeCreateAssetRequestModel<byte[]>
            {
                Entry = CreateEntry(),
                Configuration = new byte[] { 1, 2, 3 }
            };
            var expected = new ServiceResponse<PublishedNodesEntryModel>
            {
                Result = CreateEntry()
            };

            assets.Setup(a => a.CreateOrUpdateAssetAsync(
                    request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.CreateOrUpdateAsset2Async(request);

            Assert.Same(expected, actual);
            assets.Verify();
        }

        [Fact]
        public async Task CreateOrUpdateAsset2ThrowsOnNullRequestAsync()
        {
            var controller = CreateController();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                controller.CreateOrUpdateAsset2Async(null!));
        }

        [Fact]
        public async Task GetAllAssetsDelegatesToAssetServiceAsync()
        {
            var assets = new Mock<IAssetConfiguration<byte[]>>(MockBehavior.Strict);
            var controller = CreateController(assets: assets);
            var entry = CreateEntry();
            var header = new RequestHeaderModel();
            var expected = new ServiceResponse<PublishedNodesEntryModel>
            {
                Result = CreateEntry()
            };

            assets.Setup(a => a.GetAllAssetsAsync(
                    entry, header, It.IsAny<CancellationToken>()))
                .Returns(ToAsyncEnumerable(expected))
                .Verifiable();

            var actual = await ToListAsync(controller.GetAllAssetsAsync(
                new PublishedNodesEntryRequestModel<RequestHeaderModel>
                {
                    Entry = entry,
                    Request = header
                }));

            Assert.Same(expected, Assert.Single(actual));
            assets.Verify();
        }

        [Fact]
        public void GetAllAssetsThrowsOnNullRequest()
        {
            var controller = CreateController();
            Assert.Throws<ArgumentNullException>(() =>
                controller.GetAllAssetsAsync(null!));
        }

        private static WriterController CreateController(
            Mock<IPublishedNodesServices>? publisher = null,
            Mock<IConfigurationServices>? configuration = null,
            Mock<IAssetConfiguration<byte[]>>? assets = null)
        {
            return new WriterController(
                (publisher ?? new Mock<IPublishedNodesServices>()).Object,
                (configuration ?? new Mock<IConfigurationServices>()).Object,
                (assets ?? new Mock<IAssetConfiguration<byte[]>>()).Object);
        }

        private static PublishedNodesEntryModel CreateEntry()
        {
            return new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:4840"
            };
        }

        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(T item)
        {
            yield return item;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> values)
        {
            var results = new List<T>();
            await foreach (var value in values.ConfigureAwait(false))
            {
                results.Add(value);
            }
            return results;
        }
    }
}
