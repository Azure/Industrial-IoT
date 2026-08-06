// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="EndpointDescriptionEx.IsSameAs"/>.
    /// Also serves as a regression test for the inverted <c>IsSame</c> condition
    /// that was introduced in commit 46ebdbf39 — the old code returned <c>false</c>
    /// when the security modes <em>matched</em> instead of when they did not.
    /// </summary>
    public sealed class EndpointDescriptionExTests
    {
        private const string kUrl = "opc.tcp://localhost:4840";

        // ── Matching security modes ───────────────────────────────────────────

        [Fact]
        public void IsSameAs_SignAndEncryptEndpoint_WithSignAndEncryptModel_ReturnsTrue()
        {
            var endpoint = new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt
            };
            var model = new EndpointModel
            {
                Url = kUrl,
                SecurityMode = SecurityMode.SignAndEncrypt
            };

            Assert.True(endpoint.IsSameAs(model));
        }

        [Fact]
        public void IsSameAs_NoneEndpoint_WithNoneModel_ReturnsTrue()
        {
            var endpoint = new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.None
            };
            var model = new EndpointModel
            {
                Url = kUrl,
                SecurityMode = SecurityMode.None
            };

            Assert.True(endpoint.IsSameAs(model));
        }

        [Fact]
        public void IsSameAs_SignEndpoint_WithSignModel_ReturnsTrue()
        {
            var endpoint = new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.Sign
            };
            var model = new EndpointModel
            {
                Url = kUrl,
                SecurityMode = SecurityMode.Sign
            };

            Assert.True(endpoint.IsSameAs(model));
        }

        // ── SecurityMode.Best matches any mode ───────────────────────────────

        [Fact]
        public void IsSameAs_AnySecurityModeEndpoint_WithBestModel_ReturnsTrue()
        {
            foreach (var mode in new[]
            {
                MessageSecurityMode.None,
                MessageSecurityMode.Sign,
                MessageSecurityMode.SignAndEncrypt
            })
            {
                var endpoint = new EndpointDescription { SecurityMode = mode };
                var model = new EndpointModel { Url = kUrl, SecurityMode = SecurityMode.Best };

                Assert.True(endpoint.IsSameAs(model),
                    $"Expected IsSameAs to return true for mode={mode} with SecurityMode.Best");
            }
        }

        // ── Non-matching security modes ───────────────────────────────────────

        [Fact]
        public void IsSameAs_SignEndpoint_WithSignAndEncryptModel_ReturnsFalse()
        {
            var endpoint = new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.Sign
            };
            var model = new EndpointModel
            {
                Url = kUrl,
                SecurityMode = SecurityMode.SignAndEncrypt
            };

            Assert.False(endpoint.IsSameAs(model));
        }

        [Fact]
        public void IsSameAs_NoneEndpoint_WithSignAndEncryptDefault_ReturnsFalse()
        {
            // When model.SecurityMode is null the default is SignAndEncrypt
            var endpoint = new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.None
            };
            var model = new EndpointModel
            {
                Url = kUrl,
                SecurityMode = null
            };

            Assert.False(endpoint.IsSameAs(model));
        }

        // ── Security policy filtering ─────────────────────────────────────────

        [Fact]
        public void IsSameAs_MatchingModeNoPolicy_ReturnsTrue()
        {
            var endpoint = new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            };
            var model = new EndpointModel
            {
                Url = kUrl,
                SecurityMode = SecurityMode.SignAndEncrypt,
                SecurityPolicy = null
            };

            Assert.True(endpoint.IsSameAs(model));
        }

        [Fact]
        public void IsSameAs_MatchingModeMatchingPolicy_ReturnsTrue()
        {
            var endpoint = new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            };
            var model = new EndpointModel
            {
                Url = kUrl,
                SecurityMode = SecurityMode.SignAndEncrypt,
                SecurityPolicy = SecurityPolicies.Basic256Sha256
            };

            Assert.True(endpoint.IsSameAs(model));
        }

        [Fact]
        public void IsSameAs_MatchingModeDifferentPolicy_ReturnsFalse()
        {
            var endpoint = new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            };
            var model = new EndpointModel
            {
                Url = kUrl,
                SecurityMode = SecurityMode.SignAndEncrypt,
                SecurityPolicy = SecurityPolicies.Aes128_Sha256_RsaOaep
            };

            Assert.False(endpoint.IsSameAs(model));
        }
    }
}
