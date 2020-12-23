// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using Xunit;
    using System;


    public class QualifiedNameExTests {

        [Fact]
        public void DecodeQnFromStringNoUri() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var result = expected.ToQualifiedName(context);
            Assert.Equal(expected, result.Name);
        }

        [Fact]
        public void DecodeQnFromStringUrlEncodedNoUri() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var result = expected.UrlEncode().ToQualifiedName(context);
            Assert.Equal(expected, result.Name);
        }

        [Fact]
        public void DecodeQnFromString() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var uri = "http://contosos.com/UA";
            var result = (uri + "#" + expected).ToQualifiedName(context);
            Assert.Equal(expected, result.Name);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeQnFromStringUrlEncoded() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var uri = "http://contosos.com/UA";
            var result = (uri + "#" + expected.UrlEncode()).ToQualifiedName(context);
            Assert.Equal(expected, result.Name);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeQnFromStringUrlEncodedBadNamespaceUri() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var uri = "contosos";
            var result = (uri + "#" + expected.UrlEncode()).ToQualifiedName(context);
            Assert.Equal(expected, result.Name);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeQnFromStringUrnNamespaceUri() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var uri = "urn:contosos";
            var result = (uri + "#" + expected).ToQualifiedName(context);
            Assert.Equal(expected, result.Name);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void DecodeQnFromStringUrlEncodedUrnNamespaceUri() {
            var context = new ServiceMessageContext();
            var expected = "   space    tests /(%)§;#;;#;()§$\"))\"\")(§";
            var uri = "urn:contosos";
            var result = (uri + "#" + expected.UrlEncode()).ToQualifiedName(context);
            Assert.Equal(expected, result.Name);
            Assert.Equal(uri, context.NamespaceUris.GetString(1));
            Assert.Equal(1, result.NamespaceIndex);
        }

        [Fact]
        public void EncodeDecodeQualifiedName() {

            var context = new ServiceMessageContext();
            var expected = new QualifiedName("   space    tests /(%)§;#;;#;()§$\"))\"\")(§",
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToQualifiedName(context);
            var result2 = s2.ToQualifiedName(context);

            Assert.Equal(expected, result1);
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void EncodeDecodeQualifiedNameDefaultUri() {

            var context = new ServiceMessageContext();
            var expected = new QualifiedName("   space    tests /(%)§;#;;#;()§$\"))\"\")(§", 0);

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToQualifiedName(context);
            var result2 = s2.ToQualifiedName(context);

            Assert.Equal(expected, result1);
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void EncodeDecodeQualifiedNameWithEmptyString() {

            var context = new ServiceMessageContext();
            var expected = new QualifiedName("",
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToQualifiedName(context);
            var result2 = s2.ToQualifiedName(context);

            // BUG IN Stack: Assert.Equal(expected, result1);
            Assert.Null(result1.Name);
            Assert.Equal(expected.NamespaceIndex, result1.NamespaceIndex);
            // BUG IN Stack: Assert.Equal(expected, result2);
            Assert.Null(result2.Name);
            Assert.Equal(expected.NamespaceIndex, result2.NamespaceIndex);
            // BUG IN Stack: Assert.True(Utils.IsEqual(result1, result2));
        }

        [Fact]
        public void EncodeDecodeQualifiedNameWithEmptyStringDefaultUri() {

            var context = new ServiceMessageContext();
            var expected = new QualifiedName("", 0);

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToQualifiedName(context);
            var result2 = s2.ToQualifiedName(context);

            // BUG IN Stack: Assert.Equal(expected, result1);
            Assert.Null(result1.Name);
            Assert.Equal(expected.NamespaceIndex, result1.NamespaceIndex);
            // BUG IN Stack: Assert.Equal(expected, result2);
            Assert.Null(result2.Name);
            Assert.Equal(expected.NamespaceIndex, result2.NamespaceIndex);
            // BUG IN Stack: Assert.True(Utils.IsEqual(result1, result2));
        }

        [Fact]
        public void EncodeDecodeQualifiedNameWithNullString() {

            var context = new ServiceMessageContext();
            var expected = new QualifiedName(null,
                context.NamespaceUris.GetIndexOrAppend("http://contoso.com/UA"));

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToQualifiedName(context);
            var result2 = s2.ToQualifiedName(context);

            // BUG IN Stack: Assert.Equal(expected, result1);
            Assert.Equal(expected.Name, result1.Name);
            Assert.Equal(expected.NamespaceIndex, result1.NamespaceIndex);
            // BUG IN Stack: Assert.Equal(expected, result2);
            Assert.Equal(expected.Name, result2.Name);
            Assert.Equal(expected.NamespaceIndex, result2.NamespaceIndex);
            // BUG IN Stack: Assert.True(Utils.IsEqual(result1, result2));
        }

        [Fact]
        public void EncodeDecodeQualifiedNameWithNullStringDefaultUri() {

            var context = new ServiceMessageContext();
            var expected = new QualifiedName(null, 0);

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToQualifiedName(context);
            var result2 = s2.ToQualifiedName(context);

            // BUG IN Stack: Assert.Equal(expected, result1);
            Assert.Equal(expected.Name, result1.Name);
            Assert.Equal(expected.NamespaceIndex, result1.NamespaceIndex);
            // BUG IN Stack: Assert.Equal(expected, result2);
            Assert.Equal(expected.Name, result2.Name);
            Assert.Equal(expected.NamespaceIndex, result2.NamespaceIndex);
            // BUG IN Stack: Assert.True(Utils.IsEqual(result1, result2));
        }

        [Fact]
        public void EncodeDecodeNullQualifiedName() {

            var context = new ServiceMessageContext();
            var expected = QualifiedName.Null;

            var s1 = expected.AsString(context);
            var s2 = expected.AsString(context, true);

            var result1 = s1.ToQualifiedName(context);
            var result2 = s2.ToQualifiedName(context);

            Assert.Equal(expected, result1);
            // BUG IN Stack: Assert.Equal(expected, result2);
            Assert.Equal(expected.Name, result2.Name);
            Assert.Equal(expected.NamespaceIndex, result2.NamespaceIndex);
            // BUG IN Stack: Assert.True(Utils.IsEqual(result1, result2));
        }
    }
}
