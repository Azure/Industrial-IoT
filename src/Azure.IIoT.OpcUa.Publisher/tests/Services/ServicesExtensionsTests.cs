// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for the internal <see cref="Extensions.ToServiceRelativePath"/> helper
    /// and its private <c>ParsePathElement</c> implementation.
    /// </summary>
    public sealed class ServicesExtensionsTests
    {
        private static readonly IServiceMessageContext _ctx = ServiceMessageContext.GlobalContext;

        // ── ToServiceRelativePath null / empty ────────────────────────────────

        [Fact]
        public void ToServiceRelativePath_NullPath_ReturnsEmptyPath()
        {
            var result = ((IReadOnlyList<string>?)null).ToServiceRelativePath(_ctx);

            Assert.NotNull(result);
            Assert.True(result.Elements.IsNull || result.Elements.Count == 0);
        }

        [Fact]
        public void ToServiceRelativePath_EmptyPath_ReturnsEmptyPath()
        {
            var result = new List<string>().ToServiceRelativePath(_ctx);

            Assert.NotNull(result);
            Assert.True(result.Elements.IsNull || result.Elements.Count == 0);
        }

        [Fact]
        public void ToServiceRelativePath_PathWithEmptyString_FiltersEmptyElement()
        {
            var path = new List<string> { "" };

            var result = path.ToServiceRelativePath(_ctx);

            Assert.NotNull(result);
            Assert.True(result.Elements.IsNull || result.Elements.Count == 0);
        }

        // ── ParsePathElement — slash prefix → HierarchicalReferences ─────────

        [Fact]
        public void ToServiceRelativePath_SlashPrefix_UsesHierarchicalReferences()
        {
            var path = new List<string> { "/MyNode" };

            var result = path.ToServiceRelativePath(_ctx);

            var el = result.Elements[0];
            Assert.Equal(ReferenceTypeIds.HierarchicalReferences, el.ReferenceTypeId);
            Assert.True(el.IncludeSubtypes);
            Assert.False(el.IsInverse);
            Assert.Equal("MyNode", el.TargetName.Name);
        }

        // ── ParsePathElement — dot prefix → Aggregates ───────────────────────

        [Fact]
        public void ToServiceRelativePath_DotPrefix_UsesAggregatesReference()
        {
            var path = new List<string> { ".PropertyName" };

            var result = path.ToServiceRelativePath(_ctx);

            var el = result.Elements[0];
            Assert.Equal(ReferenceTypeIds.Aggregates, el.ReferenceTypeId);
            Assert.Equal("PropertyName", el.TargetName.Name);
        }

        // ── ParsePathElement — no prefix → References ─────────────────────────

        [Fact]
        public void ToServiceRelativePath_NoPrefix_UsesReferences()
        {
            var path = new List<string> { "ChildNode" };

            var result = path.ToServiceRelativePath(_ctx);

            var el = result.Elements[0];
            Assert.Equal(ReferenceTypeIds.References, el.ReferenceTypeId);
            Assert.Equal("ChildNode", el.TargetName.Name);
        }

        // ── ParsePathElement — ! prefix → IsInverse ──────────────────────────

        [Fact]
        public void ToServiceRelativePath_BangPrefix_SetsIsInverse()
        {
            var path = new List<string> { "!ChildNode" };

            var result = path.ToServiceRelativePath(_ctx);

            var el = result.Elements[0];
            Assert.True(el.IsInverse);
            Assert.Equal(ReferenceTypeIds.References, el.ReferenceTypeId);
            Assert.Equal("ChildNode", el.TargetName.Name);
        }

        [Fact]
        public void ToServiceRelativePath_BangSlash_SetsIsInverseAndHierarchical()
        {
            var path = new List<string> { "!/ChildNode" };

            var result = path.ToServiceRelativePath(_ctx);

            var el = result.Elements[0];
            Assert.True(el.IsInverse);
            Assert.Equal(ReferenceTypeIds.HierarchicalReferences, el.ReferenceTypeId);
        }

        // ── ParsePathElement — # prefix → IncludeSubtypes=false ──────────────

        [Fact]
        public void ToServiceRelativePath_HashPrefix_SetsIncludeSubtypesFalse()
        {
            var path = new List<string> { "#ChildNode" };

            var result = path.ToServiceRelativePath(_ctx);

            var el = result.Elements[0];
            Assert.False(el.IncludeSubtypes);
            Assert.Equal(ReferenceTypeIds.References, el.ReferenceTypeId);
        }

        [Fact]
        public void ToServiceRelativePath_BangHash_SetsBothFlags()
        {
            var path = new List<string> { "!#ChildNode" };

            var result = path.ToServiceRelativePath(_ctx);

            var el = result.Elements[0];
            Assert.True(el.IsInverse);
            Assert.False(el.IncludeSubtypes);
        }

        // ── ParsePathElement — <...> prefix → custom reference type ──────────

        [Fact]
        public void ToServiceRelativePath_AngleBracketWithKnownName_ResolvesReference()
        {
            var path = new List<string> { "<HierarchicalReferences>ChildNode" };

            var result = path.ToServiceRelativePath(_ctx);

            var el = result.Elements[0];
            Assert.False(NodeIdCompat.IsNull(el.ReferenceTypeId));
            Assert.Equal("ChildNode", el.TargetName.Name);
        }

        [Fact]
        public void ToServiceRelativePath_AngleBracketWithNodeId_UsesNodeId()
        {
            var path = new List<string> { "<i=33>ChildNode" };

            var result = path.ToServiceRelativePath(_ctx);

            var el = result.Elements[0];
            Assert.False(NodeIdCompat.IsNull(el.ReferenceTypeId));
            Assert.Equal("ChildNode", el.TargetName.Name);
        }

        [Fact]
        public void ToServiceRelativePath_AngleBracketWithModifiers_Applies()
        {
            var path = new List<string> { "<!#HierarchicalReferences>ChildNode" };

            var result = path.ToServiceRelativePath(_ctx);

            var el = result.Elements[0];
            Assert.True(el.IsInverse);
            Assert.False(el.IncludeSubtypes);
            Assert.Equal("ChildNode", el.TargetName.Name);
        }

        // ── ParsePathElement — format errors ──────────────────────────────────

        [Fact]
        public void ToServiceRelativePath_UnclosedAngleBracket_ThrowsFormatException()
        {
            var path = new List<string> { "<HierarchicalReferences" };

            Assert.Throws<FormatException>(() =>
                path.ToServiceRelativePath(_ctx));
        }

        [Fact]
        public void ToServiceRelativePath_EmptyTargetAfterSlash_ThrowsFormatException()
        {
            var path = new List<string> { "/" };

            Assert.Throws<FormatException>(() =>
                path.ToServiceRelativePath(_ctx));
        }

        [Fact]
        public void ToServiceRelativePath_EmptyTargetAfterDot_ThrowsFormatException()
        {
            var path = new List<string> { "." };

            Assert.Throws<FormatException>(() =>
                path.ToServiceRelativePath(_ctx));
        }

        [Fact]
        public void ToServiceRelativePath_EmptyTargetAfterAngle_ThrowsFormatException()
        {
            var path = new List<string> { "<i=33>" };

            Assert.Throws<FormatException>(() =>
                path.ToServiceRelativePath(_ctx));
        }

        // ── Multiple elements ─────────────────────────────────────────────────

        [Fact]
        public void ToServiceRelativePath_MultipleElements_ReturnsAll()
        {
            var path = new List<string> { "/Parent", ".Child" };

            var result = path.ToServiceRelativePath(_ctx);

            Assert.Equal(2, result.Elements.Count);
            Assert.Equal(ReferenceTypeIds.HierarchicalReferences,
                result.Elements[0].ReferenceTypeId);
            Assert.Equal(ReferenceTypeIds.Aggregates, result.Elements[1].ReferenceTypeId);
        }

        // ── ResolveBrowsePathToNodeAsync — empty / null paths ─────────────────

        [Fact]
        public async Task ResolveBrowsePathToNodeAsync_NullPaths_ReturnsRootIdAsync()
        {
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            var rootId = new NodeId(2253u); // Server

            var result = await session.Object.ResolveBrowsePathToNodeAsync(
                null, rootId, null!, "param", TimeProvider.System);

            Assert.Equal(rootId, result);
        }

        [Fact]
        public async Task ResolveBrowsePathToNodeAsync_EmptyPaths_ReturnsRootIdAsync()
        {
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            var rootId = new NodeId(2253u); // Server

            var result = await session.Object.ResolveBrowsePathToNodeAsync(
                null, rootId, Array.Empty<string>(), "param", TimeProvider.System);

            Assert.Equal(rootId, result);
        }

        // ── ResolveNodeIdAsync — empty / null browsePath ──────────────────────

        [Fact]
        public async Task ResolveNodeIdAsync_NullBrowsePath_ReturnsConvertedRootIdAsync()
        {
            var session = new Mock<IOpcUaSession>(MockBehavior.Loose);
            session.Setup(s => s.MessageContext).Returns(ServiceMessageContext.GlobalContext);
            var nodeIdStr = "ns=2;i=42";

            var result = await session.Object.ResolveNodeIdAsync(
                null, nodeIdStr, null, "param", TimeProvider.System);

            Assert.Equal(new NodeId(42u, 2), result);
        }

        [Fact]
        public async Task ResolveNodeIdAsync_EmptyBrowsePath_ReturnsConvertedRootIdAsync()
        {
            var session = new Mock<IOpcUaSession>(MockBehavior.Loose);
            session.Setup(s => s.MessageContext).Returns(ServiceMessageContext.GlobalContext);
            var nodeIdStr = "ns=2;i=42";

            var result = await session.Object.ResolveNodeIdAsync(
                null, nodeIdStr, new List<string>(), "param", TimeProvider.System);

            Assert.Equal(new NodeId(42u, 2), result);
        }

        [Fact]
        public async Task ResolveNodeIdAsync_NullRootIdAndNullBrowsePath_ReturnsNullNodeIdAsync()
        {
            var session = new Mock<IOpcUaSession>(MockBehavior.Loose);
            session.Setup(s => s.MessageContext).Returns(ServiceMessageContext.GlobalContext);

            var result = await session.Object.ResolveNodeIdAsync(
                null, null, null, "param", TimeProvider.System);

            Assert.True(NodeIdCompat.IsNull(result));
        }
    }
}
