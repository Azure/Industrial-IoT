// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using Xunit;
    using System;

    public class NodeIdExTests {

        [Fact]
        public void DecodeNodeIdFromStringNoUri() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var result = ("s=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            result = ("s_" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeNodeIdFromStringUrlEncodedNoUri() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var result = ("s=" + expected.UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeNodeIdFromString() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var uri = "http://contosos.com/UA";
            var result = (uri + "#s=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromStringInvalidUri()
        {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var uri = "invalidUri";
            var result = (uri + "#s=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromStringUrnUri() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var uri = "urn:contosos";
            var result = (uri + "#s=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromStringWithNamespaceIndex() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var uri = "http://contosos.com/UA";
            var result = ("ns=" + context.NamespaceUris.GetIndexOrAppend(uri) + ";s=" + expected)
                .ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromStringWithNsu() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var uri = "http://contosos.com/UA";
            var index = context.NamespaceUris.GetIndexOrAppend(uri);
            var result = ("nsu=" + uri + ";s=" + expected)
                .ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
            Assert.Equal(1, index);
        }

        [Fact]
        public void DecodeNodeIdFromStringUrlEncoded() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var uri = "http://contosos.com/UA";
            var result = (uri + "#s=" + expected.UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromIntUrl() {
            var context = new ServiceMessageContext();
            var uri = "http://contosos.com#i=1";
            var result = uri.ToExpandedNodeId(context);
            Assert.Equal("http://contosos.com", result.NamespaceUri);
        }

        [Fact]
        public void ParseNodeIdUsingAbsoluteUri() {
            var value = "http://contosos.com#i=1";
            Uri.TryCreate(value, UriKind.Absolute, out var uri);
            Assert.NotEqual("http://contosos.com", uri.NoQueryAndFragment().AbsoluteUri);
        }

        [Fact]
        public void DecodeNodeIdFromBufferNoUri() {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            var result = ("b=" + expected.ToBase64String()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            result = ("b_" + expected.ToBase64String()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeNodeIdFromBufferUrlEncodedNoUri() {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            var result = ("b=" + expected.ToBase64String().UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeNodeIdFromBuffer() {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            var uri = "http://contosos.com/UA";
            var result = (uri + "#b=" + expected.ToBase64String()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromBufferUrlEncoded() {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            var uri = "http://contosos.com/UA";
            var result = (uri + "#b=" + expected.ToBase64String().UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromBufferWithNamespaceIndex() {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid().ToByteArray();
            var uri = "http://contosos.com/UA";
            var result = ("ns=" + context.NamespaceUris.GetIndexOrAppend(uri) +
                ";b=" + expected.ToBase64String())
                .ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromBufferWithNsu() {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid().ToByteArray();
            var uri = "http://contosos.com/UA";
            var index = context.NamespaceUris.GetIndexOrAppend(uri);
            var result = ("nsu=" + uri + ";b=" + expected.ToBase64String())
                .ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
            Assert.Equal(1, index);
        }

        [Fact]
        public void DecodeNodeIdFromGuidNoUri() {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            var result = ("g=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            result = ("g_" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeNodeIdFromGuidUrlEncodedNoUri() {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            var result = ("g=" + expected.ToString().UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeNodeIdFromGuid() {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            var uri = "http://contosos.com/UA/";
            var result = (uri + "#g=" + expected).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromGuidUrlEncoded() {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            var uri = "http://contosos.com/UA/";
            var result = (uri + "#g=" + expected.ToString().UrlEncode()).ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromGuidWithNamespaceIndex() {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            var uri = "http://contosos.com/UA";
            var result = ("ns=" + context.NamespaceUris.GetIndexOrAppend(uri) + ";g=" + expected)
                .ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeNodeIdFromGuidWithNsu() {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            var uri = "http://contosos.com/UA";
            var index = context.NamespaceUris.GetIndexOrAppend(uri);
            var result = ("nsu=" + uri + ";g=" + expected)
                .ToNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
            Assert.Equal(1, index);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithString() {

            var context = new ServiceMessageContext();
            var expected = new NodeId("   space    tests /(%)§;#;;#;()§$\"))\"\")(§",
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithStringAndInvalidUri() {

            var context = new ServiceMessageContext();
            var expected = new NodeId("   space    tests /(%)§;#;;#;()§$\"))\"\")(§",
                context.NamespaceUris.GetIndexOrAppend("contoso"));

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            Assert.Equal(s1, s2);
            Assert.Contains("nsu=", s2);
            Assert.DoesNotContain("ns=", s2);
            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithStringAndDefaultUri() {

            var context = new ServiceMessageContext();
            var expected = new NodeId("   space    tests /(%)§;#;;#;()§$\"))\"\")(§", 0);

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithGuid() {

            var context = new ServiceMessageContext();
            var expected = new NodeId(Guid.NewGuid(),
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithGuidAndDefaultUri() {

            var context = new ServiceMessageContext();
            var expected = new NodeId(Guid.NewGuid(), 0);

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithInt() {

            var context = new ServiceMessageContext();
            var expected = new NodeId(1,
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithIntAndDefaultUri() {

            var context = new ServiceMessageContext();
            var expected = new NodeId(111111111, 0);

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithBuffer() {

            var context = new ServiceMessageContext();
            var expected = new NodeId(Guid.NewGuid().ToByteArray(),
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithBufferAndDefaultUri() {

            var context = new ServiceMessageContext();
            var expected = new NodeId(Guid.NewGuid().ToByteArray(), 0);

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithEmptyString() {

            var context = new ServiceMessageContext();
            var expected = new NodeId("",
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithEmptyStringAndDefaultUri() {

            var context = new ServiceMessageContext();
            var input = new NodeId("", 0);

            var s1 = input.AsString(context);
            var s2 = input.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(NodeId.Null, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithNullString() {

            var context = new ServiceMessageContext();
            var expected = new NodeId((string)null,
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            Assert.Equal(expected, result1);
            Assert.Equal(expected, result2);
            Assert.True(Utils.IsEqual(result1, result2));

            Assert.Equal(string.Empty, result1.Identifier);
            Assert.Equal(expected.NamespaceIndex, result1.NamespaceIndex);
            Assert.Equal(string.Empty, result2.Identifier);
            Assert.Equal(expected.NamespaceIndex, result2.NamespaceIndex);
        }

        [Fact]
        public void EncodeDecodeNodeIdWithNullStringAndDefaultUri() {

            var context = new ServiceMessageContext();
            var input = new NodeId((string)null, 0); // == NodeId.Null

            var s1 = input.AsString(context);
            var s2 = input.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(NodeId.Null, result1, result2);
        }

        [Fact]
        public void EncodeDecodeNullNodeId() {

            var context = new ServiceMessageContext();
            var expected = NodeId.Null;

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToNodeId(context);
            var result2 = s2.ToNodeId(context);

            AssertEqual(expected, result1, result2);
        }


        private static void AssertEqual(NodeId expected,
            NodeId result1, NodeId result2) {

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
