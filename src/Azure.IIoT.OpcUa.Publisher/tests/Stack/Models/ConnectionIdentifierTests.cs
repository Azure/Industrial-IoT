// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="ConnectionIdentifier"/>.
    /// </summary>
    public sealed class ConnectionIdentifierTests
    {
        private static ConnectionModel MakeConnection(string url = "opc.tcp://host:4840") =>
            new ConnectionModel
            {
                Endpoint = new EndpointModel { Url = url }
            };

        // ── Constructor ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_NullConnectionModel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ConnectionIdentifier((ConnectionModel)null!));
        }

        [Fact]
        public void Constructor_ValidConnectionModel_StoresConnection()
        {
            var connection = MakeConnection();
            var id = new ConnectionIdentifier(connection);

            Assert.NotNull(id.Connection);
            Assert.Equal(connection.Endpoint!.Url, id.Connection.Endpoint!.Url);
        }

        [Fact]
        public void Constructor_ValidConnectionModel_ClonesConnection()
        {
            var connection = MakeConnection();
            var id = new ConnectionIdentifier(connection);

            // Modifying the original after construction should not affect the identifier
            Assert.NotSame(connection, id.Connection);
        }

        [Fact]
        public void Constructor_NullEndpointModel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ConnectionIdentifier((EndpointModel)null!));
        }

        [Fact]
        public void Constructor_ValidEndpointModel_WrapsInConnectionModel()
        {
            var endpoint = new EndpointModel { Url = "opc.tcp://host:4840" };
            var id = new ConnectionIdentifier(endpoint);

            Assert.NotNull(id.Connection.Endpoint);
            Assert.Equal("opc.tcp://host:4840", id.Connection.Endpoint.Url);
        }

        // ── Equals ────────────────────────────────────────────────────────────

        [Fact]
        public void Equals_SameUrl_ReturnsTrue()
        {
            var id1 = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));
            var id2 = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));

            Assert.True(id1.Equals(id2));
        }

        [Fact]
        public void Equals_DifferentUrl_ReturnsFalse()
        {
            var id1 = new ConnectionIdentifier(MakeConnection("opc.tcp://host1:4840"));
            var id2 = new ConnectionIdentifier(MakeConnection("opc.tcp://host2:4840"));

            Assert.False(id1.Equals(id2));
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            var id1 = new ConnectionIdentifier(MakeConnection());

            Assert.False(id1.Equals(null));
        }

        [Fact]
        public void Equals_NonConnectionIdentifier_ReturnsFalse()
        {
            var id1 = new ConnectionIdentifier(MakeConnection());

            Assert.False(id1.Equals("not an identifier"));
        }

        [Fact]
        public void Equals_StringMatchingToString_ReturnsTrue()
        {
            var id1 = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));
            var str = id1.ToString();

            Assert.True(id1.Equals(str));
        }

        [Fact]
        public void Equals_StringNotMatchingToString_ReturnsFalse()
        {
            var id1 = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));

            Assert.False(id1.Equals("opc.tcp://totally-different:9999"));
        }

        // ── Operators ─────────────────────────────────────────────────────────

        [Fact]
        public void EqualityOperator_SameUrl_ReturnsTrue()
        {
            var id1 = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));
            var id2 = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));

            Assert.True(id1 == id2);
        }

        [Fact]
        public void InequalityOperator_DifferentUrl_ReturnsTrue()
        {
            var id1 = new ConnectionIdentifier(MakeConnection("opc.tcp://host1:4840"));
            var id2 = new ConnectionIdentifier(MakeConnection("opc.tcp://host2:4840"));

            Assert.True(id1 != id2);
        }

        // ── GetHashCode ───────────────────────────────────────────────────────

        [Fact]
        public void GetHashCode_SameUrl_ReturnsSameCode()
        {
            var id1 = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));
            var id2 = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));

            Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_IsConsistent()
        {
            var id = new ConnectionIdentifier(MakeConnection());
            var first = id.GetHashCode();
            var second = id.GetHashCode();

            Assert.Equal(first, second);
        }

        // ── ToString ──────────────────────────────────────────────────────────

        [Fact]
        public void ToString_WithUrl_ReturnsNonEmpty()
        {
            var id = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));
            var str = id.ToString();

            Assert.NotNull(str);
            Assert.NotEmpty(str);
        }

        [Fact]
        public void ToString_WithUrl_ContainsUrl()
        {
            var id = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));
            var str = id.ToString();

            Assert.Contains("opc.tcp://host:4840", str, StringComparison.Ordinal);
        }

        [Fact]
        public void ToString_ConnectionWithNoUrl_ReturnsBadConnectionFallback()
        {
            var id = new ConnectionIdentifier(
                new ConnectionModel { Endpoint = new EndpointModel { Url = "" } });
            var str = id.ToString();

            Assert.Equal("Bad connection", str);
        }

        // ── Dictionary usage ──────────────────────────────────────────────────

        [Fact]
        public void UsedAsDictionaryKey_SameUrlFoundAfterLookup()
        {
            var dict = new Dictionary<ConnectionIdentifier, string>();
            var key = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));
            dict[key] = "value";

            var lookup = new ConnectionIdentifier(MakeConnection("opc.tcp://host:4840"));

            Assert.True(dict.ContainsKey(lookup));
            Assert.Equal("value", dict[lookup]);
        }
    }
}
