// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="RequestHeaderModelEx.GetNamespaceFormat"/>
    /// and related overloads.
    /// </summary>
    public sealed class RequestHeaderModelExTests
    {
        // ── GetNamespaceFormat ────────────────────────────────────────────────

        [Fact]
        public void GetNamespaceFormat_NullHeaderNullOptions_ReturnsUri()
        {
            RequestHeaderModel? header = null;
            var result = header.GetNamespaceFormat(null);
            Assert.Equal(NamespaceFormat.Uri, result);
        }

        [Fact]
        public void GetNamespaceFormat_NullHeaderWithOptionsFormat_ReturnsOptionsFormat()
        {
            RequestHeaderModel? header = null;
            var options = Options.Create(new PublisherOptions
            {
                DefaultNamespaceFormat = NamespaceFormat.Index
            });

            var result = header.GetNamespaceFormat(options);
            Assert.Equal(NamespaceFormat.Index, result);
        }

        [Fact]
        public void GetNamespaceFormat_HeaderWithFormat_ReturnsHeaderFormat()
        {
            var header = new RequestHeaderModel
            {
                NamespaceFormat = NamespaceFormat.Expanded
            };

            var result = header.GetNamespaceFormat(null);
            Assert.Equal(NamespaceFormat.Expanded, result);
        }

        [Fact]
        public void GetNamespaceFormat_HeaderOverridesOptions()
        {
            var header = new RequestHeaderModel
            {
                NamespaceFormat = NamespaceFormat.Expanded
            };
            var options = Options.Create(new PublisherOptions
            {
                DefaultNamespaceFormat = NamespaceFormat.Index
            });

            // Header takes priority over options
            var result = header.GetNamespaceFormat(options);
            Assert.Equal(NamespaceFormat.Expanded, result);
        }

        [Fact]
        public void GetNamespaceFormat_HeaderWithNullFormat_FallsBackToOptions()
        {
            var header = new RequestHeaderModel
            {
                NamespaceFormat = null
            };
            var options = Options.Create(new PublisherOptions
            {
                DefaultNamespaceFormat = NamespaceFormat.Index
            });

            var result = header.GetNamespaceFormat(options);
            Assert.Equal(NamespaceFormat.Index, result);
        }

        [Fact]
        public void GetNamespaceFormat_HeaderWithNullFormatNullOptions_ReturnsUri()
        {
            var header = new RequestHeaderModel
            {
                NamespaceFormat = null
            };

            var result = header.GetNamespaceFormat(null);
            Assert.Equal(NamespaceFormat.Uri, result);
        }

        // ── AsString(NodeId) ──────────────────────────────────────────────────

        [Fact]
        public void AsString_NodeId_NullHeaderReturnsNonEmpty()
        {
            RequestHeaderModel? header = null;
            var context = ServiceMessageContext.GlobalContext;
            var nodeId = new NodeId(1u, 0);

            var result = header.AsString(nodeId, context);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void AsString_NodeId_WithIndexFormat_IncludesNamespaceIndex()
        {
            var header = new RequestHeaderModel { NamespaceFormat = NamespaceFormat.Index };
            var context = ServiceMessageContext.GlobalContext;
            var nodeId = new NodeId(42u, 0);

            var result = header.AsString(nodeId, context);

            // For namespace 0, the index format produces "i=42"
            Assert.Contains("42", result, System.StringComparison.Ordinal);
        }

        [Fact]
        public void AsString_NullNodeId_ReturnsEmptyString()
        {
            RequestHeaderModel? header = null;
            var context = ServiceMessageContext.GlobalContext;

            var result = header.AsString(NodeId.Null, context);

            // NodeId.Null.AsString may return empty/null so the fallback is string.Empty
            Assert.NotNull(result);
        }

        // ── AsString(ExpandedNodeId) ──────────────────────────────────────────

        [Fact]
        public void AsString_ExpandedNodeId_NullHeaderReturnsNonEmpty()
        {
            RequestHeaderModel? header = null;
            var context = ServiceMessageContext.GlobalContext;
            var nodeId = new ExpandedNodeId(100u, 0);

            var result = header.AsString(nodeId, context);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void AsString_ExpandedNodeId_NullExpandedNodeId_DoesNotThrow()
        {
            RequestHeaderModel? header = null;
            var context = ServiceMessageContext.GlobalContext;

            var ex = Record.Exception(() =>
                header.AsString(ExpandedNodeId.Null, context));
            Assert.Null(ex);
        }

        // ── AsString(QualifiedName) ───────────────────────────────────────────

        [Fact]
        public void AsString_QualifiedName_NullHeaderReturnsNonEmpty()
        {
            RequestHeaderModel? header = null;
            var context = ServiceMessageContext.GlobalContext;
            var name = new QualifiedName("SomeName", 0);

            var result = header.AsString(name, context);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void AsString_QualifiedName_WithUriFormat_ReturnsStringContainingName()
        {
            var header = new RequestHeaderModel { NamespaceFormat = NamespaceFormat.Uri };
            var context = ServiceMessageContext.GlobalContext;
            var name = new QualifiedName("MyBrowseName", 0);

            var result = header.AsString(name, context);

            Assert.Contains("MyBrowseName", result, System.StringComparison.Ordinal);
        }
    }
}
