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
            var entry = new PublishedNodesEntryModel();
            var expected = new ServiceResponse<PublishedNodesEntryModel>
            {
                Result = new PublishedNodesEntryModel()
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
            var entry = new PublishedNodesEntryModel();
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
                Result = new PublishedNodesEntryModel()
            };

            assets.Setup(a => a.CreateOrUpdateAssetAsync(
                    It.IsAny<PublishedNodeCreateAssetRequestModel<byte[]>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PublishedNodeCreateAssetRequestModel<byte[]>, CancellationToken>(
                    (request, _) => captured = request)
                .ReturnsAsync(expected)
                .Verifiable();

            var entry = new PublishedNodesEntryModel();
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
                Entry = new PublishedNodesEntryModel()
            };
            var expected = new ServiceResultModel { StatusCode = 7 };

            assets.Setup(a => a.DeleteAssetAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.DeleteAssetAsync(request);

            Assert.Same(expected, actual);
            assets.Verify();
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
