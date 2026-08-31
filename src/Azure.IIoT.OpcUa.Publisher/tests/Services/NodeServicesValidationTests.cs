// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Unit tests for argument validation in <see cref="NodeServices{T}"/> that
    /// fire before any OPC UA client call is made. These tests require no server
    /// and exercise only the guard-clause lines of each public method.
    /// </summary>
    public sealed class NodeServicesValidationTests
    {
        private static NodeServices<string> CreateSut()
        {
            return new NodeServices<string>(
                Mock.Of<IOpcUaClientManager<string>>(),
                Mock.Of<IFilterParser>(),
                NullLogger<NodeServices<string>>.Instance,
                Options.Create(new PublisherOptions()));
        }

        // ── BrowseNextAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task BrowseNextAsyncThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.BrowseNextAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task BrowseNextAsyncThrowsForEmptyContinuationTokenAsync()
        {
            var sut = CreateSut();
            var request = new BrowseNextRequestModel { ContinuationToken = "" };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.BrowseNextAsync("ep", request, CancellationToken.None));
        }

        // ── BrowsePathAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task BrowsePathAsyncThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.BrowsePathAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task BrowsePathAsyncThrowsForNullBrowsePathsAsync()
        {
            var sut = CreateSut();
            var request = new BrowsePathRequestModel { BrowsePaths = null! };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.BrowsePathAsync("ep", request, CancellationToken.None));
        }

        [Fact]
        public async Task BrowsePathAsyncThrowsForEmptyBrowsePathsAsync()
        {
            var sut = CreateSut();
            var request = new BrowsePathRequestModel
            {
                BrowsePaths = new List<IReadOnlyList<string>>()
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.BrowsePathAsync("ep", request, CancellationToken.None));
        }

        [Fact]
        public async Task BrowsePathAsyncThrowsForPathWithNullSegmentAsync()
        {
            var sut = CreateSut();
            var request = new BrowsePathRequestModel
            {
                BrowsePaths = new List<IReadOnlyList<string>> { null! }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.BrowsePathAsync("ep", request, CancellationToken.None));
        }

        // ── GetMetadataAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetMetadataAsyncThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.GetMetadataAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task GetMetadataAsyncThrowsWhenBothNodeIdAndBrowsePathMissingAsync()
        {
            var sut = CreateSut();
            var request = new NodeMetadataRequestModel { NodeId = null, BrowsePath = null };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.GetMetadataAsync("ep", request, CancellationToken.None));
        }

        // ── CompileQueryAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task CompileQueryAsyncThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.CompileQueryAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task CompileQueryAsyncThrowsForEmptyQueryAsync()
        {
            var sut = CreateSut();
            var request = new QueryCompilationRequestModel { Query = "" };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.CompileQueryAsync("ep", request, CancellationToken.None));
        }

        [Fact]
        public async Task CompileQueryAsyncThrowsForNullQueryAsync()
        {
            var sut = CreateSut();
            var request = new QueryCompilationRequestModel { Query = null! };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.CompileQueryAsync("ep", request, CancellationToken.None));
        }

        // ── GetMethodMetadataAsync ────────────────────────────────────────────

        [Fact]
        public async Task GetMethodMetadataAsyncThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.GetMethodMetadataAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task GetMethodMetadataAsyncThrowsWhenBothMethodIdAndBrowsePathMissingAsync()
        {
            var sut = CreateSut();
            var request = new MethodMetadataRequestModel
            {
                MethodId = null,
                MethodBrowsePath = null
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.GetMethodMetadataAsync("ep", request, CancellationToken.None));
        }

        // ── MethodCallAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task MethodCallAsyncThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.MethodCallAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task MethodCallAsyncThrowsWhenBothObjectIdAndBrowsePathMissingAsync()
        {
            var sut = CreateSut();
            var request = new MethodCallRequestModel
            {
                ObjectId = null,
                ObjectBrowsePath = null
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.MethodCallAsync("ep", request, CancellationToken.None));
        }

        // ── ValueReadAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task ValueReadAsyncThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.ValueReadAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task ValueReadAsyncThrowsWhenBothNodeIdAndBrowsePathMissingAsync()
        {
            var sut = CreateSut();
            var request = new ValueReadRequestModel { NodeId = null, BrowsePath = null };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.ValueReadAsync("ep", request, CancellationToken.None));
        }

        // ── ValueWriteAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task ValueWriteAsyncThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.ValueWriteAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task ValueWriteAsyncThrowsWhenValueIsNullAsync()
        {
            var sut = CreateSut();
            var request = new ValueWriteRequestModel
            {
                Value = null!,
                NodeId = "ns=1;i=1"
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.ValueWriteAsync("ep", request, CancellationToken.None));
        }

        [Fact]
        public async Task ValueWriteAsyncThrowsWhenBothNodeIdAndBrowsePathMissingAsync()
        {
            var sut = CreateSut();
            var request = new ValueWriteRequestModel
            {
                Value = JsonValue.Create(42)!,
                NodeId = null,
                BrowsePath = null
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.ValueWriteAsync("ep", request, CancellationToken.None));
        }

        // ── ReadAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task ReadAsyncThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.ReadAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task ReadAsyncThrowsWhenAttributesAreNullAsync()
        {
            var sut = CreateSut();
            var request = new ReadRequestModel { Attributes = null! };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.ReadAsync("ep", request, CancellationToken.None));
        }

        [Fact]
        public async Task ReadAsyncThrowsWhenAttributeContainsEmptyNodeIdAsync()
        {
            var sut = CreateSut();
            var request = new ReadRequestModel
            {
                Attributes = new List<AttributeReadRequestModel>
                {
                    new AttributeReadRequestModel
                    {
                        NodeId = "",
                        Attribute = NodeAttribute.Value
                    }
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.ReadAsync("ep", request, CancellationToken.None));
        }

        // ── WriteAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task WriteAsyncThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.WriteAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task WriteAsyncThrowsWhenAttributesAreNullAsync()
        {
            var sut = CreateSut();
            var request = new WriteRequestModel { Attributes = null! };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.WriteAsync("ep", request, CancellationToken.None));
        }

        [Fact]
        public async Task WriteAsyncThrowsWhenAttributeContainsEmptyNodeIdAsync()
        {
            var sut = CreateSut();
            var request = new WriteRequestModel
            {
                Attributes = new List<AttributeWriteRequestModel>
                {
                    new AttributeWriteRequestModel
                    {
                        NodeId = "",
                        Attribute = NodeAttribute.Value,
                        Value = null!
                    }
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.WriteAsync("ep", request, CancellationToken.None));
        }

        // ── HistoryGetConfigurationAsync ──────────────────────────────────────

        [Fact]
        public async Task HistoryGetConfigurationAsyncThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.HistoryGetConfigurationAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryGetConfigurationAsyncThrowsWhenNodeIdIsEmptyAsync()
        {
            var sut = CreateSut();
            var request = new HistoryConfigurationRequestModel { NodeId = "" };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryGetConfigurationAsync("ep", request, CancellationToken.None));
        }

        // ── INodeServicesInternal: HistoryReadAsync ───────────────────────────

        [Fact]
        public async Task HistoryReadInternalThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ((INodeServicesInternal<string>)sut).HistoryReadAsync<string, string>(
                    "ep",
                    null!,
                    (_, __) => default!,
                    (_, __) => default!,
                    CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadInternalThrowsWhenDetailsAreNullAsync()
        {
            var sut = CreateSut();
            var request = new HistoryReadRequestModel<string>
            {
                Details = null!,
                NodeId = "ns=1;i=1"
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                ((INodeServicesInternal<string>)sut).HistoryReadAsync<string, string>(
                    "ep", request,
                    (_, __) => default!,
                    (_, __) => string.Empty,
                    CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadInternalThrowsWhenBothNodeIdAndBrowsePathMissingAsync()
        {
            var sut = CreateSut();
            var request = new HistoryReadRequestModel<string>
            {
                Details = "some-details",
                NodeId = null,
                BrowsePath = null
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                ((INodeServicesInternal<string>)sut).HistoryReadAsync<string, string>(
                    "ep", request,
                    (_, __) => default!,
                    (_, __) => string.Empty,
                    CancellationToken.None));
        }

        // ── INodeServicesInternal: HistoryReadNextAsync ───────────────────────

        [Fact]
        public async Task HistoryReadNextInternalThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ((INodeServicesInternal<string>)sut).HistoryReadNextAsync(
                    "ep",
                    null!,
                    (_, __) => string.Empty,
                    CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadNextInternalThrowsWhenContinuationTokenIsEmptyAsync()
        {
            var sut = CreateSut();
            var request = new HistoryReadNextRequestModel { ContinuationToken = "" };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                ((INodeServicesInternal<string>)sut).HistoryReadNextAsync(
                    "ep", request,
                    (_, __) => string.Empty,
                    CancellationToken.None));
        }

        // ── INodeServicesInternal: HistoryUpdateAsync ─────────────────────────

        [Fact]
        public async Task HistoryUpdateInternalThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ((INodeServicesInternal<string>)sut).HistoryUpdateAsync<string>(
                    "ep",
                    null!,
                    (_, __, ___) => Task.FromResult(default(ExtensionObject)),
                    CancellationToken.None));
        }

        [Fact]
        public async Task HistoryUpdateInternalThrowsWhenDetailsAreNullAsync()
        {
            var sut = CreateSut();
            var request = new HistoryUpdateRequestModel<string>
            {
                Details = null!
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                ((INodeServicesInternal<string>)sut).HistoryUpdateAsync<string>(
                    "ep", request,
                    (_, __, ___) => Task.FromResult(default(ExtensionObject)),
                    CancellationToken.None));
        }

        // ── Public JsonNode wrappers (cover the delegation path) ─────────────

        [Fact]
        public async Task HistoryReadPublicThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.HistoryReadAsync("ep", (HistoryReadRequestModel<JsonNode>)null!,
                    CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadPublicThrowsWhenDetailsAreNullAsync()
        {
            var sut = CreateSut();
            var request = new HistoryReadRequestModel<JsonNode>
            {
                Details = null!,
                NodeId = "ns=1;i=1"
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadAsync("ep", request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadPublicThrowsWhenNodeIdAndBrowsePathMissingAsync()
        {
            var sut = CreateSut();
            var request = new HistoryReadRequestModel<JsonNode>
            {
                Details = JsonNode.Parse("{}")!,
                NodeId = null,
                BrowsePath = null
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadAsync("ep", request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadNextPublicThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.HistoryReadNextAsync("ep", null!, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadNextPublicThrowsWhenContinuationTokenEmptyAsync()
        {
            var sut = CreateSut();
            var request = new HistoryReadNextRequestModel { ContinuationToken = "" };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadNextAsync("ep", request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryUpdatePublicThrowsForNullRequestAsync()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sut.HistoryUpdateAsync("ep", (HistoryUpdateRequestModel<JsonNode>)null!,
                    CancellationToken.None));
        }

        [Fact]
        public async Task HistoryUpdatePublicThrowsWhenDetailsAreNullAsync()
        {
            var sut = CreateSut();
            var request = new HistoryUpdateRequestModel<JsonNode>
            {
                Details = null!
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryUpdateAsync("ep", request, CancellationToken.None));
        }
    }
}
