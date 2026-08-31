// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public sealed class EndpointModelExTests
    {
        [Fact]
        public void IsSameAs_SameReference_ReturnsTrue()
        {
            var endpoint = CreateEndpoint("opc.tcp://server");

            Assert.True(endpoint.IsSameAs(endpoint));
        }

        [Fact]
        public void IsSameAs_OneNull_ReturnsFalse()
        {
            var endpoint = CreateEndpoint("opc.tcp://server");

            Assert.False(endpoint.IsSameAs(null));
            Assert.False(((EndpointModel?)null).IsSameAs(endpoint));
        }

        [Fact]
        public void IsSameAs_SameSecurityAndUrls_ReturnsTrue()
        {
            var left = CreateEndpoint("opc.tcp://server") with
            {
                AlternativeUrls = new HashSet<string> { "opc.tcp://alternate" }
            };
            var right = CreateEndpoint("opc.tcp://server") with
            {
                AlternativeUrls = new HashSet<string> { "opc.tcp://alternate" }
            };

            Assert.True(left.IsSameAs(right));
        }

        [Fact]
        public void IsSameAs_DifferentSecurity_ReturnsFalse()
        {
            var left = CreateEndpoint("opc.tcp://server") with
            {
                SecurityMode = SecurityMode.Sign
            };
            var right = CreateEndpoint("opc.tcp://server") with
            {
                SecurityMode = SecurityMode.SignAndEncrypt
            };

            Assert.False(left.IsSameAs(right));
        }

        [Fact]
        public void IsSameAs_DifferentUrls_ReturnsFalse()
        {
            var left = CreateEndpoint("opc.tcp://server1");
            var right = CreateEndpoint("opc.tcp://server2");

            Assert.False(left.IsSameAs(right));
        }

        [Fact]
        public void HasSameSecurityProperties_NullSecurityPolicyActsAsWildcard()
        {
            var left = CreateEndpoint("opc.tcp://server") with
            {
                SecurityPolicy = null
            };
            var right = CreateEndpoint("opc.tcp://server") with
            {
                SecurityPolicy = "policy"
            };

            Assert.True(left.HasSameSecurityProperties(right));
        }

        [Fact]
        public void HasSameSecurityProperties_DifferentCertificate_ReturnsFalse()
        {
            var left = CreateEndpoint("opc.tcp://server") with
            {
                Certificate = "cert1"
            };
            var right = CreateEndpoint("opc.tcp://server") with
            {
                Certificate = "cert2"
            };

            Assert.False(left.HasSameSecurityProperties(right));
        }

        [Fact]
        public void HasSameSecurityProperties_DifferentNonNullSecurityPolicy_ReturnsFalse()
        {
            var left = CreateEndpoint("opc.tcp://server") with
            {
                SecurityPolicy = "policy1"
            };
            var right = CreateEndpoint("opc.tcp://server") with
            {
                SecurityPolicy = "policy2"
            };

            Assert.False(left.HasSameSecurityProperties(right));
        }

        [Fact]
        public void GetAllUrls_NullEndpoint_ReturnsEmpty()
        {
            var urls = ((EndpointModel?)null).GetAllUrls();

            Assert.Empty(urls);
        }

        [Fact]
        public void GetAllUrls_ReturnsPrimaryThenAlternatives()
        {
            var endpoint = CreateEndpoint("opc.tcp://server") with
            {
                AlternativeUrls = new HashSet<string> { "opc.tcp://alternate" }
            };

            var urls = endpoint.GetAllUrls().ToList();

            Assert.Equal(["opc.tcp://server", "opc.tcp://alternate"], urls);
        }

        [Fact]
        public void CreateConsistentHash_SameEndpointValues_ReturnsSameHash()
        {
            var left = CreateEndpoint("opc.tcp://server");
            var right = CreateEndpoint("opc.tcp://server");

            Assert.Equal(left.CreateConsistentHash(), right.CreateConsistentHash());
        }

        [Fact]
        public void UnionWith_NullEndpoint_DoesNotChangeModel()
        {
            var endpoint = CreateEndpoint("opc.tcp://server");

            endpoint.UnionWith(null);

            Assert.Equal("opc.tcp://server", endpoint.Url);
            Assert.Null(endpoint.AlternativeUrls);
        }

        [Fact]
        public void UnionWith_ModelHasUrl_AddsEndpointUrlAsAlternative()
        {
            var endpoint = CreateEndpoint("opc.tcp://server");
            var other = CreateEndpoint("opc.tcp://alternate");

            endpoint.UnionWith(other);

            Assert.Equal("opc.tcp://server", endpoint.Url);
            Assert.Contains("opc.tcp://alternate", endpoint.AlternativeUrls!);
        }

        [Fact]
        public void UnionWith_ModelWithoutUrl_UsesEndpointUrl()
        {
            var endpoint = CreateEndpoint(null!);
            var other = CreateEndpoint("opc.tcp://server");

            endpoint.UnionWith(other);

            Assert.Equal("opc.tcp://server", endpoint.Url);
        }

        [Fact]
        public void Clone_NullEndpoint_ReturnsNull()
        {
            Assert.Null(((EndpointModel?)null).Clone());
        }

        [Fact]
        public void Clone_CopiesAlternativeUrlSet()
        {
            var endpoint = CreateEndpoint("opc.tcp://server") with
            {
                AlternativeUrls = new HashSet<string> { "opc.tcp://alternate" }
            };

            var clone = endpoint.Clone();

            Assert.NotNull(clone);
            Assert.NotSame(endpoint, clone);
            Assert.NotSame(endpoint.AlternativeUrls, clone.AlternativeUrls);
            Assert.Equal(endpoint.AlternativeUrls, clone.AlternativeUrls);
        }

        private static EndpointModel CreateEndpoint(string url)
        {
            return new EndpointModel
            {
                Url = url,
                SecurityMode = SecurityMode.None,
                SecurityPolicy = "policy"
            };
        }
    }
}
