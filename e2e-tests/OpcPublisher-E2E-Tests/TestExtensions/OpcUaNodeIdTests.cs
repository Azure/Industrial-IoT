// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestExtensions
{
    using System;
    using Xunit;

    [Trait("Category", "Unit")]
    public sealed class OpcUaNodeIdTests
    {
        [Theory]
        [InlineData("ns=1;s=Machine", "urn:fixture#s=Machine")]
        [InlineData("nsu=urn:fixture;s=Machine", "urn:fixture#s=Machine")]
        [InlineData("urn:fixture#s=Machine", "urn:fixture#s=Machine")]
        [InlineData("i=2253", "http://opcfoundation.org/UA/#i=2253")]
        [InlineData("ns=0;i=2253", "http://opcfoundation.org/UA/#i=2253")]
        [InlineData("ns=1;s=Machine#a;b", "urn:fixture#s=Machine#a;b")]
        [InlineData("urn:fixture#s=Machine#a;b", "urn:fixture#s=Machine#a;b")]
        [InlineData("ns=1;i=000123", "urn:fixture#i=123")]
        [InlineData("ns=1;i=4294967295", "urn:fixture#i=4294967295")]
        [InlineData("ns=1;b=AQID", "urn:fixture#b=AQID")]
        [InlineData("ns=1;g=DEADBEEF-DEAD-BEEF-DEAD-BEEFDEADBEEF",
            "urn:fixture#g=deadbeef-dead-beef-dead-beefdeadbeef")]
        public void NormalizationPreservesNamespaceAndIdentifier(
            string nodeId, string expected)
        {
            Assert.Equal(expected, OpcUaNodeId.Normalize(nodeId, kNamespaces));
        }

        [Fact]
        public void MatchingIdentifiersInDifferentNamespacesAreNotEquivalent()
        {
            Assert.NotEqual(
                OpcUaNodeId.Normalize("ns=1;s=Machine", kNamespaces),
                OpcUaNodeId.Normalize("ns=2;s=Machine", kNamespaces));
        }

        [Fact]
        public void NamespaceUriComparisonRemainsCaseSensitive()
        {
            string[] namespaces = ["http://opcfoundation.org/UA/", "urn:Fixture", "urn:fixture"];

            Assert.NotEqual(
                OpcUaNodeId.Normalize("ns=1;s=Machine", namespaces),
                OpcUaNodeId.Normalize("ns=2;s=Machine", namespaces));
        }

        [Theory]
        [InlineData("ns=99;s=Machine")]
        [InlineData("ns=-1;s=Machine")]
        [InlineData("ns=1;invalid")]
        [InlineData("ns=1;i=-1")]
        [InlineData("ns=1;i=4294967296")]
        [InlineData("ns=1;g=invalid")]
        [InlineData("ns=1;b=!")]
        public void InvalidNodeIdsFailRatherThanCompareByIdentifier(string nodeId)
        {
            Assert.Throws<FormatException>(() =>
                OpcUaNodeId.Normalize(nodeId, kNamespaces));
        }

        private static readonly string[] kNamespaces =
            ["http://opcfoundation.org/UA/", "urn:fixture", "urn:another"];
    }
}
