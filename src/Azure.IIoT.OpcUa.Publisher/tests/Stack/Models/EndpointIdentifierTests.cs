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
    /// Tests for <see cref="EndpointIdentifier"/>.
    /// </summary>
    public sealed class EndpointIdentifierTests
    {
        private static EndpointModel MakeEndpoint(string url = "opc.tcp://host:4840") =>
            new EndpointModel { Url = url };

        // ── Constructor ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_NullEndpointModel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new EndpointIdentifier(null!));
        }

        [Fact]
        public void Constructor_ValidEndpointModel_StoresEndpoint()
        {
            var endpoint = MakeEndpoint("opc.tcp://host:4840");
            var id = new EndpointIdentifier(endpoint);

            Assert.NotNull(id.Endpoint);
            Assert.Equal("opc.tcp://host:4840", id.Endpoint.Url);
        }

        [Fact]
        public void Constructor_ValidEndpointModel_ClonesEndpoint()
        {
            var endpoint = MakeEndpoint();
            var id = new EndpointIdentifier(endpoint);

            Assert.NotSame(endpoint, id.Endpoint);
        }

        // ── Equals ────────────────────────────────────────────────────────────

        [Fact]
        public void Equals_SameUrl_ReturnsTrue()
        {
            var id1 = new EndpointIdentifier(MakeEndpoint("opc.tcp://host:4840"));
            var id2 = new EndpointIdentifier(MakeEndpoint("opc.tcp://host:4840"));

            Assert.True(id1.Equals(id2));
        }

        [Fact]
        public void Equals_DifferentUrl_ReturnsFalse()
        {
            var id1 = new EndpointIdentifier(MakeEndpoint("opc.tcp://host1:4840"));
            var id2 = new EndpointIdentifier(MakeEndpoint("opc.tcp://host2:4840"));

            Assert.False(id1.Equals(id2));
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            var id = new EndpointIdentifier(MakeEndpoint());

            Assert.False(id.Equals(null));
        }

        [Fact]
        public void Equals_NonEndpointIdentifier_ReturnsFalse()
        {
            var id = new EndpointIdentifier(MakeEndpoint());

            Assert.False(id.Equals(42));
        }

        [Fact]
        public void Equals_StringMatchingToString_ReturnsTrue()
        {
            var id = new EndpointIdentifier(MakeEndpoint("opc.tcp://host:4840"));
            var str = id.ToString();

            Assert.True(id.Equals(str));
        }

        [Fact]
        public void Equals_StringNotMatchingToString_ReturnsFalse()
        {
            var id = new EndpointIdentifier(MakeEndpoint("opc.tcp://host:4840"));

            Assert.False(id.Equals("definitely-not-the-hash"));
        }

        // ── GetHashCode ───────────────────────────────────────────────────────

        [Fact]
        public void GetHashCode_SameUrl_ReturnsSameCode()
        {
            var id1 = new EndpointIdentifier(MakeEndpoint("opc.tcp://host:4840"));
            var id2 = new EndpointIdentifier(MakeEndpoint("opc.tcp://host:4840"));

            Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_IsConsistent()
        {
            var id = new EndpointIdentifier(MakeEndpoint());
            var h1 = id.GetHashCode();
            var h2 = id.GetHashCode();

            Assert.Equal(h1, h2);
        }

        // ── ToString ──────────────────────────────────────────────────────────

        [Fact]
        public void ToString_ReturnsNonNullNonEmpty()
        {
            var id = new EndpointIdentifier(MakeEndpoint("opc.tcp://host:4840"));
            var str = id.ToString();

            Assert.NotNull(str);
            Assert.NotEmpty(str);
        }

        [Fact]
        public void ToString_SameUrl_ReturnsSameString()
        {
            var id1 = new EndpointIdentifier(MakeEndpoint("opc.tcp://host:4840"));
            var id2 = new EndpointIdentifier(MakeEndpoint("opc.tcp://host:4840"));

            Assert.Equal(id1.ToString(), id2.ToString());
        }

        // ── Dictionary usage ──────────────────────────────────────────────────

        [Fact]
        public void UsedAsDictionaryKey_SameUrlFoundAfterLookup()
        {
            var dict = new Dictionary<EndpointIdentifier, int>();
            var key1 = new EndpointIdentifier(MakeEndpoint("opc.tcp://host:4840"));
            dict[key1] = 99;

            var key2 = new EndpointIdentifier(MakeEndpoint("opc.tcp://host:4840"));

            Assert.True(dict.ContainsKey(key2));
            Assert.Equal(99, dict[key2]);
        }
    }
}
