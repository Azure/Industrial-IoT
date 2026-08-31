// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="ConnectionIdentifier"/>, <see cref="EndpointIdentifier"/>,
    /// and <see cref="ImmutableRelativePath"/>.
    /// </summary>
    public sealed class StackModelIdentifierTests
    {
        private static EndpointModel CreateEndpoint(string url = "opc.tcp://localhost:4840") =>
            new EndpointModel { Url = url };

        private static ConnectionModel CreateConnection(string url = "opc.tcp://localhost:4840") =>
            new ConnectionModel { Endpoint = CreateEndpoint(url) };

        // ── ConnectionIdentifier ───────────────────────────────────────────────

        [Fact]
        public void ConnectionIdentifier_NullConnection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ConnectionIdentifier((ConnectionModel)null!));
        }

        [Fact]
        public void ConnectionIdentifier_CreatesFromEndpoint()
        {
            var endpoint = CreateEndpoint();
            var id = new ConnectionIdentifier(endpoint);
            Assert.NotNull(id.Connection);
            Assert.Equal(endpoint.Url, id.Connection.Endpoint!.Url);
        }

        [Fact]
        public void ConnectionIdentifier_NullEndpoint_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ConnectionIdentifier((EndpointModel)null!));
        }

        [Fact]
        public void ConnectionIdentifier_SameUrl_AreEqual()
        {
            var id1 = new ConnectionIdentifier(CreateConnection());
            var id2 = new ConnectionIdentifier(CreateConnection());
            Assert.Equal(id1, id2);
        }

        [Fact]
        public void ConnectionIdentifier_DifferentUrl_AreNotEqual()
        {
            var id1 = new ConnectionIdentifier(CreateConnection("opc.tcp://a:4840"));
            var id2 = new ConnectionIdentifier(CreateConnection("opc.tcp://b:4840"));
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void ConnectionIdentifier_SameUrl_HaveSameHashCode()
        {
            var id1 = new ConnectionIdentifier(CreateConnection());
            var id2 = new ConnectionIdentifier(CreateConnection());
            Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
        }

        [Fact]
        public void ConnectionIdentifier_ToString_ReturnsNonEmpty()
        {
            var id = new ConnectionIdentifier(CreateConnection());
            Assert.NotEmpty(id.ToString());
        }

        [Fact]
        public void ConnectionIdentifier_Equals_NullObject_ReturnsFalse()
        {
            var id = new ConnectionIdentifier(CreateConnection());
            Assert.False(id.Equals(null));
        }

        [Fact]
        public void ConnectionIdentifier_Equals_WrongType_ReturnsFalse()
        {
            var id = new ConnectionIdentifier(CreateConnection());
            Assert.False(id.Equals(42));
        }

        [Fact]
        public void ConnectionIdentifier_Equals_StringMatch_ReturnsTrue()
        {
            var id = new ConnectionIdentifier(CreateConnection());
            var str = id.ToString();
            Assert.True(id.Equals(str));
        }

        [Fact]
        public void ConnectionIdentifier_OperatorEquals_SameUrl()
        {
            var id1 = new ConnectionIdentifier(CreateConnection());
            var id2 = new ConnectionIdentifier(CreateConnection());
            Assert.True(id1 == id2);
        }

        [Fact]
        public void ConnectionIdentifier_OperatorNotEquals_DifferentUrl()
        {
            var id1 = new ConnectionIdentifier(CreateConnection("opc.tcp://a:4840"));
            var id2 = new ConnectionIdentifier(CreateConnection("opc.tcp://b:4840"));
            Assert.True(id1 != id2);
        }

        // ── EndpointIdentifier ────────────────────────────────────────────────

        [Fact]
        public void EndpointIdentifier_NullEndpoint_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new EndpointIdentifier(null!));
        }

        [Fact]
        public void EndpointIdentifier_SameUrl_AreEqual()
        {
            var id1 = new EndpointIdentifier(CreateEndpoint());
            var id2 = new EndpointIdentifier(CreateEndpoint());
            Assert.Equal(id1, id2);
        }

        [Fact]
        public void EndpointIdentifier_DifferentUrl_AreNotEqual()
        {
            var id1 = new EndpointIdentifier(CreateEndpoint("opc.tcp://a:4840"));
            var id2 = new EndpointIdentifier(CreateEndpoint("opc.tcp://b:4840"));
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void EndpointIdentifier_SameUrl_HaveSameHashCode()
        {
            var id1 = new EndpointIdentifier(CreateEndpoint());
            var id2 = new EndpointIdentifier(CreateEndpoint());
            Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
        }

        [Fact]
        public void EndpointIdentifier_ToString_ReturnsNonEmpty()
        {
            var id = new EndpointIdentifier(CreateEndpoint());
            Assert.NotEmpty(id.ToString());
        }

        [Fact]
        public void EndpointIdentifier_Equals_NullObject_ReturnsFalse()
        {
            var id = new EndpointIdentifier(CreateEndpoint());
            Assert.False(id.Equals(null));
        }

        [Fact]
        public void EndpointIdentifier_Equals_StringMatch_ReturnsTrue()
        {
            var id = new EndpointIdentifier(CreateEndpoint());
            var str = id.ToString();
            Assert.True(id.Equals(str));
        }

        // ── ImmutableRelativePath ─────────────────────────────────────────────

        [Fact]
        public void ImmutableRelativePath_EmptyPath_HasEmptyPathList()
        {
            var path = new ImmutableRelativePath([]);
            Assert.Empty(path.Path);
        }

        [Fact]
        public void ImmutableRelativePath_SingleSegment_HasOneElement()
        {
            var path = new ImmutableRelativePath(["a"]);
            Assert.Single(path.Path);
            Assert.Equal("a", path.Path[0]);
        }

        [Fact]
        public void ImmutableRelativePath_EqualPaths_AreEqual()
        {
            var path1 = new ImmutableRelativePath(["a", "b", "c"]);
            var path2 = new ImmutableRelativePath(["a", "b", "c"]);
            Assert.Equal(path1, path2);
        }

        [Fact]
        public void ImmutableRelativePath_DifferentPaths_AreNotEqual()
        {
            var path1 = new ImmutableRelativePath(["a", "b"]);
            var path2 = new ImmutableRelativePath(["a", "c"]);
            Assert.NotEqual(path1, path2);
        }

        [Fact]
        public void ImmutableRelativePath_DifferentLengths_AreNotEqual()
        {
            var path1 = new ImmutableRelativePath(["a", "b"]);
            var path2 = new ImmutableRelativePath(["a"]);
            Assert.NotEqual(path1, path2);
        }

        [Fact]
        public void ImmutableRelativePath_SamePaths_HaveSameHashCode()
        {
            var path1 = new ImmutableRelativePath(["a", "b"]);
            var path2 = new ImmutableRelativePath(["a", "b"]);
            Assert.Equal(path1.GetHashCode(), path2.GetHashCode());
        }

        [Fact]
        public void ImmutableRelativePath_Create_AddsSegmentToParent()
        {
            var parent = new List<string> { "a", "b" };
            var path = ImmutableRelativePath.Create(parent, "c");
            Assert.Equal(3, path.Path.Count);
            Assert.Equal("c", path.Path[2]);
        }

        [Fact]
        public void ImmutableRelativePath_Create_NullParent_CreatesWithJustSegment()
        {
            var path = ImmutableRelativePath.Create(null, "root");
            Assert.Single(path.Path);
            Assert.Equal("root", path.Path[0]);
        }

        [Fact]
        public void ImmutableRelativePath_Equals_WrongType_ReturnsFalse()
        {
            var path = new ImmutableRelativePath(["a"]);
            Assert.False(path.Equals("a"));
        }

        [Fact]
        public void ImmutableRelativePath_EmptyPath_ToString_Throws()
        {
            var path = new ImmutableRelativePath([]);
            // Aggregate over empty sequence throws InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => path.ToString());
        }

        [Fact]
        public void ImmutableRelativePath_SingleSegment_ToString_ReturnsSegment()
        {
            var path = new ImmutableRelativePath(["hello"]);
            Assert.Equal("hello", path.ToString());
        }

        [Fact]
        public void ImmutableRelativePath_MultipleSegments_ToString_ConcatenatesAll()
        {
            var path = new ImmutableRelativePath(["a", "b", "c"]);
            Assert.Equal("abc", path.ToString());
        }
    }
}
