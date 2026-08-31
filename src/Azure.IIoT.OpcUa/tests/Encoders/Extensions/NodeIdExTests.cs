// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using Xunit;

    public class NodeIdExTests
    {
        [Fact]
        public void DecodeNodeIdFromStringNoUri()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var result = ("s=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            result = ("s_" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeNodeIdFromStringUrlEncodedNoUri()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var result = ("s=" + expected.UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeNodeIdFromString()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "http://contosos.com/UA";
            var result = (uri + "#s=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromStringInvalidUri()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "invalidUri";
            var result = (uri + "#s=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromStringUrnUri()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "urn:contosos";
            var result = (uri + "#s=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromStringWithNamespaceIndex()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "http://contosos.com/UA";
            var result = ("ns=" + context.NamespaceUris.GetIndexOrAppend(uri) + ";s=" + expected)
                .ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromStringWithNsu()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "http://contosos.com/UA";
            var index = context.NamespaceUris.GetIndexOrAppend(uri);
            var result = ("nsu=" + uri + ";s=" + expected)
                .ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
            Assert.Equal(1, index);
        }

        [Fact]
        public void DecodeNodeIdFromStringUrlEncoded()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "http://contosos.com/UA";
            var result = (uri + "#s=" + expected.UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromIntUrl()
        {
            var context = new ServiceMessageContext();
            const string uri = "http://contosos.com#i=1";
            var result = uri.ToExpandedNodeId(context);
            Assert.Equal("http://contosos.com", result.NamespaceUri);
        }

        [Fact]
        public void ParseNodeIdUsingAbsoluteUri()
        {
            const string value = "http://contosos.com#i=1";
            Uri.TryCreate(value, UriKind.Absolute, out var uri);
            Assert.NotEqual("http://contosos.com", uri.NoQueryAndFragment().AbsoluteUri);
        }

        [Fact]
        public void DecodeNodeIdFromBufferNoUri()
        {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            var result = ("b=" + expected.ToBase64String()).ToNodeId(context);
            Assert.Equal(expected, ((ByteString)result.Identifier).ToArray());
            result = ("b_" + expected.ToBase64String()).ToNodeId(context);
            Assert.Equal(expected, ((ByteString)result.Identifier).ToArray());
        }

        [Fact]
        public void DecodeNodeIdFromBufferUrlEncodedNoUri()
        {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            var result = ("b=" + expected.ToBase64String().UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, ((ByteString)result.Identifier).ToArray());
        }

        [Fact]
        public void DecodeNodeIdFromBuffer()
        {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            const string uri = "http://contosos.com/UA";
            var result = (uri + "#b=" + expected.ToBase64String()).ToNodeId(context);
            Assert.Equal(expected, ((ByteString)result.Identifier).ToArray());
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromBufferUrlEncoded()
        {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            const string uri = "http://contosos.com/UA";
            var result = (uri + "#b=" + expected.ToBase64String().UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, ((ByteString)result.Identifier).ToArray());
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromBufferWithNamespaceIndex()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid().ToByteArray();
            const string uri = "http://contosos.com/UA";
            var result = ("ns=" + context.NamespaceUris.GetIndexOrAppend(uri) +
                ";b=" + expected.ToBase64String())
                .ToNodeId(context);
            Assert.Equal(expected, ((ByteString)result.Identifier).ToArray());
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromBufferWithNsu()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid().ToByteArray();
            const string uri = "http://contosos.com/UA";
            var index = context.NamespaceUris.GetIndexOrAppend(uri);
            var result = ("nsu=" + uri + ";b=" + expected.ToBase64String())
                .ToNodeId(context);
            Assert.Equal(expected, ((ByteString)result.Identifier).ToArray());
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
            Assert.Equal(1, index);
        }

        [Fact]
        public void DecodeNodeIdFromGuidNoUri()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            var result = ("g=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            result = ("g_" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeNodeIdFromGuidUrlEncodedNoUri()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            var result = ("g=" + expected.ToString().UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeNodeIdFromGuid()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            const string uri = "http://contosos.com/UA/";
            var result = (uri + "#g=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromGuidUrlEncoded()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            const string uri = "http://contosos.com/UA/";
            var result = (uri + "#g=" + expected.ToString().UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromGuidWithNamespaceIndex()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            const string uri = "http://contosos.com/UA";
            var result = ("ns=" + context.NamespaceUris.GetIndexOrAppend(uri) + ";g=" + expected)
                .ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromGuidWithNsu()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            const string uri = "http://contosos.com/UA";
            var index = context.NamespaceUris.GetIndexOrAppend(uri);
            var result = ("nsu=" + uri + ";g=" + expected)
                .ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
            Assert.Equal(1, index);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithString()
        {
            var context = new ServiceMessageContext();
            var expected = new NodeId("   space    tests /(%)§;#;;#;()§$\"))\"\")(§",
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithStringAndInvalidUri()
        {
            var context = new ServiceMessageContext();
            var expected = new NodeId("   space    tests /(%)§;#;;#;()§$\"))\"\")(§",
                context.NamespaceUris.GetIndexOrAppend("contoso"));

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            Assert.Equal(s1, s2);
            Assert.Contains("nsu=", s2, StringComparison.Ordinal);
            Assert.DoesNotContain("ns=", s2, StringComparison.Ordinal);
            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithStringAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var expected = new NodeId("   space    tests /(%)§;#;;#;()§$\"))\"\")(§", 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithGuid()
        {
            var context = new ServiceMessageContext();
            var expected = new NodeId(Guid.NewGuid(),
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithGuidAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var expected = new NodeId(Guid.NewGuid(), 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithInt()
        {
            var context = new ServiceMessageContext();
            var expected = new NodeId(1,
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithIntAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var expected = new NodeId(111111111, 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithBuffer()
        {
            var context = new ServiceMessageContext();
            var expected = new NodeId((ByteString)Guid.NewGuid().ToByteArray(),
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithBufferAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var expected = new NodeId((ByteString)Guid.NewGuid().ToByteArray(), 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithEmptyStringAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var input = new NodeId("", 0);

            var s1 = input.AsString(context, NamespaceFormat.Uri);
            var s2 = input.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(NodeId.Null, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithNullStringAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var input = new NodeId((string)null, 0); // == NodeId.Null

            var s1 = input.AsString(context, NamespaceFormat.Uri);
            var s2 = input.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(NodeId.Null, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNullNodeId()
        {
            var context = new ServiceMessageContext();
            var expected = NodeId.Null;

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Theory]
        [InlineData("i=42", 42)]
        [InlineData("i_42", 42)]
        [InlineData("Boolean", (int)BuiltInType.Boolean)]
        [InlineData("UInt32", (int)BuiltInType.UInt32)]
        public void DecodeNodeIdFromNumericFormsAndDataTypeNamesNoUri(
            string value, int expected)
        {
            var context = new ServiceMessageContext();

            var result = value.ToNodeId(context);

            Assert.Equal((uint)expected, result.Identifier);
            Assert.Equal(0, result.NamespaceIndex);
        }

        [Theory]
        [InlineData("x=1")]
        [InlineData("i=not-a-number")]
        [InlineData("not-a-data-type")]
        public void DecodeMalformedNodeIdUriFallsBackToNullNodeId(string value)
        {
            var context = new ServiceMessageContext();

            var result = value.ToNodeId(context);

            Assert.Equal(NodeId.Null, result);
        }

        [Theory]
        [InlineData("http://contoso.com/UA")]
        [InlineData("http://contoso.com/UA#i=1&bad=server")]
        public void DecodeBadAbsoluteNodeIdUriThrowsFormatException(string value)
        {
            var context = new ServiceMessageContext();

            Assert.Throws<FormatException>(() => value.ToNodeId(context));
        }

        [Fact]
        public void ExpandedNodeIdToNodeIdHandlesNullAndUnknownNamespaces()
        {
            var namespaces = new NamespaceTable();
            var expanded = new ExpandedNodeId("node", 0, "urn:missing", 0);

            Assert.Equal(NodeId.Null, ExpandedNodeId.Null.ToNodeId(namespaces));
            Assert.Throws<ArgumentException>(() => expanded.ToNodeId(namespaces));

            var result = expanded.ToNodeId(namespaces, allowUnknownNamespace: true);

            Assert.Equal("node", result.Identifier);
            Assert.Equal(0, result.NamespaceIndex);
        }

        [Fact]
        public void NodeIdToExpandedNodeIdRequiresNamespaceTableForNamespaceIndex()
        {
            var nodeId = new NodeId("node", 1);

            Assert.Throws<ArgumentNullException>(() => nodeId.ToExpandedNodeId(null));
        }

        [Fact]
        public void ExpandedNodeIdAsStringIncludesServerUriForUriFormat()
        {
            var context = new ServiceMessageContext();
            //
            // Index zero is the local server, for which no server uri is
            // emitted. A remote reference has to sit above it for the suffix
            // to appear at all.
            //
            context.ServerUris.GetIndexOrAppend("urn:local");
            var serverIndex = context.ServerUris.GetIndexOrAppend("urn:server");
            Assert.NotEqual(0u, serverIndex);
            var nodeId = new ExpandedNodeId("node", 0, "http://contoso.com/UA", serverIndex);

            var value = nodeId.AsString(context, NamespaceFormat.Uri);
            var result = value.ToExpandedNodeId(context);

            Assert.Equal("http://contoso.com/UA#s=node&srv=urn:server", value);
            Assert.Equal("node", result.Identifier);
            Assert.Equal("http://contoso.com/UA", result.NamespaceUri);
            Assert.Equal((uint)serverIndex, result.ServerIndex);
        }

        [Fact]
        public void ExpandedNodeIdAsStringEscapesSemicolonsInExpandedNamespaceUri()
        {
            var context = new ServiceMessageContext();
            var nodeId = new ExpandedNodeId("node", 0, "urn:has;semicolon", 0);

            var value = nodeId.AsString(context, NamespaceFormat.Expanded);

            Assert.Equal("nsu=urn:has%3bsemicolon;s=node", value);
        }

        [Fact]
        public void NodeIdAsStringUsesDataTypeNameForDefaultNamespaceNumericId()
        {
            var context = new ServiceMessageContext();
            var nodeId = new NodeId((uint)BuiltInType.Boolean, 0);

            var value = nodeId.AsString(context, NamespaceFormat.Uri);

            Assert.Equal("Boolean", value);
        }

        private static void AssertEqual(NodeId expected,
            NodeId result1, NodeId result2)
        {
            Assert.Equal(expected.Identifier, result1.Identifier);
            Assert.Equal(expected.NamespaceIndex, result1.NamespaceIndex);
            Assert.Equal(expected.Identifier, result2.Identifier);
            Assert.Equal(expected.NamespaceIndex, result2.NamespaceIndex);

            Assert.Equal(expected, result1);
            Assert.Equal(expected, result2);
            Assert.True(Utils.IsEqual(result1, result2));
        }
    }
}
