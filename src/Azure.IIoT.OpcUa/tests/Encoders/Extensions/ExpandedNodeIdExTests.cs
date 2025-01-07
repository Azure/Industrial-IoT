// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using Xunit;

    public class ExpandedNodeIdExTests
    {
        [Fact]
        public void DecodeExpandedNodeIdFromStringNoUri()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var result = ("s=" + expected).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            result = ("s_" + expected).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromStringUrlEncodedNoUri()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var result = ("s=" + expected.UrlEncode()).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromString()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "http://contosos.com/UA";
            var result = (uri + "#s=" + expected).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(0, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromStringUrnNamespace()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "urn:contosos";
            var result = (uri + "#s=" + expected).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(0, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromStringInvalidUri()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "invalidUri";
            var result = (uri + "#s=" + expected).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(0, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromStringWithNamespaceIndex()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "http://contosos.com/UA";
            var result = ("ns=" + context.NamespaceUris.GetIndexOrAppend(uri) + ";s=" + expected)
                .ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromStringWithNsu()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "http://contosos.com/UA";
            var index = context.NamespaceUris.GetIndexOrAppend(uri);
            var result = ("nsu=" + uri + ";s=" + expected)
                .ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(uri, result.NamespaceUri);
            Assert.Equal(0, result.NamespaceIndex);
            Assert.Equal(1, index);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromStringWithNsuBadNamespace()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "contosos";
            var index = context.NamespaceUris.GetIndexOrAppend(uri);
            var result = ("nsu=" + uri + ";s=" + expected)
                .ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(uri, result.NamespaceUri);
            Assert.Equal(0, result.NamespaceIndex);
            Assert.Equal(1, index);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromStringWithNsuUrnNamespace()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "urn:contosos";
            var index = context.NamespaceUris.GetIndexOrAppend(uri);
            var result = ("nsu=" + uri + ";s=" + expected)
                .ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(uri, result.NamespaceUri);
            Assert.Equal(0, result.NamespaceIndex);
            Assert.Equal(1, index);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromStringUrlEncoded()
        {
            var context = new ServiceMessageContext();
            const string expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            const string uri = "http://contosos.com/UA";
            var result = (uri + "#s=" + expected.UrlEncode()).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(0, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromBufferNoUri()
        {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            var result = ("b=" + expected.ToBase64String()).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            result = ("b_" + expected.ToBase64String()).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromBufferUrlEncodedNoUri()
        {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            var result = ("b=" + expected.ToBase64String().UrlEncode()).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromBuffer()
        {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            const string uri = "http://contosos.com/UA";
            var result = (uri + "#b=" + expected.ToBase64String()).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(0, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromBufferUrlEncoded()
        {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 0, 34, 23, 255, 6, 34, 65, 0, 0, 2, 0 };
            const string uri = "http://contosos.com/UA";
            var result = (uri + "#b=" + expected.ToBase64String().UrlEncode()).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(0, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromBufferWithNamespaceIndex()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid().ToByteArray();
            const string uri = "http://contosos.com/UA";
            var result = ("ns=" + context.NamespaceUris.GetIndexOrAppend(uri) +
                ";b=" + expected.ToBase64String())
                .ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromBufferWithNsu()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid().ToByteArray();
            const string uri = "http://contosos.com/UA";
            var index = context.NamespaceUris.GetIndexOrAppend(uri);
            var result = ("nsu=" + uri + ";b=" + expected.ToBase64String())
                .ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(uri, result.NamespaceUri);
            Assert.Equal(0, result.NamespaceIndex);
            Assert.Equal(1, index);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromGuidNoUri()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            var result = ("g=" + expected).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            result = ("g_" + expected).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromGuidUrlEncodedNoUri()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            var result = ("g=" + expected.ToString().UrlEncode()).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromGuid()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            const string uri = "http://contosos.com/UA/";
            var result = (uri + "#g=" + expected).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(0, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromGuidUrlEncoded()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            const string uri = "http://contosos.com/UA/";
            var result = (uri + "#g=" + expected.ToString().UrlEncode()).ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(0, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromGuidWithNamespaceIndex()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            const string uri = "http://contosos.com/UA";
            var result = ("ns=" + context.NamespaceUris.GetIndexOrAppend(uri) + ";g=" + expected)
                .ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeExpandedNodeIdFromGuidWithNsu()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            const string uri = "http://contosos.com/UA";
            var index = context.NamespaceUris.GetIndexOrAppend(uri);
            var result = ("nsu=" + uri + ";g=" + expected)
                .ToExpandedNodeId(context);
            Assert.Equal(expected, result.Identifier);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(uri, result.NamespaceUri);
            Assert.Equal(0, result.NamespaceIndex);
            Assert.Equal(1, index);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithString()
        {
            var context = new ServiceMessageContext();
            const string uri = "http://contoso.com/UA";
            context.NamespaceUris.GetIndexOrAppend(uri);
            var expected = new ExpandedNodeId("   space    tests /(%)§;#;;#;()§$\"))\"\")(§", 0, uri, 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithStringAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var expected = new ExpandedNodeId("   space    tests /(%)§;#;;#;()§$\"))\"\")(§", 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithGuid()
        {
            var context = new ServiceMessageContext();
            const string uri = "http://contoso.com/UA";
            var expected = new ExpandedNodeId(Guid.NewGuid(), 0, uri, 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithGuidAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var expected = new ExpandedNodeId(Guid.NewGuid());

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithInt()
        {
            var context = new ServiceMessageContext();
            const string uri = "http://contoso.com/UA";
            context.NamespaceUris.GetIndexOrAppend(uri);
            var expected = new ExpandedNodeId(1u, 0, uri, 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithIntAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var expected = new ExpandedNodeId(111111111, 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithBuffer()
        {
            var context = new ServiceMessageContext();

            const string uri = "http://contoso.com/UA";
            context.NamespaceUris.GetIndexOrAppend(uri);
            var expected = new ExpandedNodeId(Guid.NewGuid().ToByteArray(), 0, uri, 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithBufferAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var expected = new ExpandedNodeId(Guid.NewGuid().ToByteArray(), 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithEmptyString()
        {
            var context = new ServiceMessageContext();
            const string uri = "http://contoso.com/UA";
            context.NamespaceUris.GetIndexOrAppend(uri);
            var expected = new ExpandedNodeId("", 0, uri, 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            AssertEqual(expected, result1, result2);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithEmptyStringAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var expected = new ExpandedNodeId("", 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            AssertEqual(ExpandedNodeId.Null, result1, result2);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithNullString()
        {
            var context = new ServiceMessageContext();
            const string uri = "http://contoso.com/UA";
            context.NamespaceUris.GetIndexOrAppend(uri);
            var expected = new ExpandedNodeId(null, 0, uri, 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            Assert.Equal(expected, result1);
            Assert.Equal(expected, result2);
            Assert.True(Utils.IsEqual(result1, result2));

            Assert.Equal(string.Empty, result1.Identifier);
            Assert.Equal(expected.NamespaceIndex, result1.NamespaceIndex);
            Assert.Equal(string.Empty, result2.Identifier);
            Assert.Equal(expected.NamespaceIndex, result2.NamespaceIndex);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeIdWithNullStringAndDefaultUri()
        {
            var context = new ServiceMessageContext();
            var expected = new ExpandedNodeId((string)null, 0);

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            Assert.Equal(ExpandedNodeId.Null, result1);
            Assert.Equal(ExpandedNodeId.Null, result2);
            Assert.True(Utils.IsEqual(result1, result2));
        }

        [Fact]
        public void EncodeDecodeNullExpandedNodeId()
        {
            var context = new ServiceMessageContext();
            var expected = ExpandedNodeId.Null;

            var s1 = expected.AsString(context, NamespaceFormat.Uri);
            var s2 = expected.AsString(context, NamespaceFormat.Expanded);

            var result1 = s1.ToExpandedNodeId(context);
            var result2 = s2.ToExpandedNodeId(context);

            Assert.Equal(expected, result1);
            Assert.Equal(expected, result2);
            Assert.True(Utils.IsEqual(result1, result2));
        }

        private static void AssertEqual(ExpandedNodeId expected,
            ExpandedNodeId result1, ExpandedNodeId result2)
        {
            Assert.Equal(expected.Identifier, result1.Identifier);
            Assert.Equal(expected.Identifier, result2.Identifier);

            Assert.Equal(expected, result1);
            Assert.Equal(expected, result2);
            Assert.True(Utils.IsEqual(result1, result2));
        }
    }
}
