// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class RelativePathExTests
    {
        [Fact]
        public void EncodeDecodePath1()
        {
            var path = new string[] {
                "<!#http://contoso.com/ua#i=44>Test",
                "<!HasChild>Test",
                "<#HasChild>Test",
                "<!#HasProperty>Test",
                "<HasComponent>Test",
                "/foo",
                ".bar",
                "/!#flah",
                "!#flah",
                "xxxx"
            };

            var context = new ServiceMessageContext();
            var relative = path.ToRelativePath(context);
            var result = relative.AsString(context, NamespaceFormat.Uri);

            Assert.Equal(path, result);
        }

        [Fact]
        public void EncodeDecodePath2()
        {
            var path = new string[] {
                "<!HasChild>Test",
                "<#http://opcfoundation.org/ua#i_33>Test",
                "<#!HasProperty>Test",
                "<#!http://contoso.com/ua#i_44>Test",
                "<http://opcfoundation.org/ua#i_33>Test",
                "#foo",
                "!.bar",
                "!#/flah",
                "!/#flah",
                "!xxxx"
            };

            var context = new ServiceMessageContext();
            var relative = path.ToRelativePath(context);
            var expected = relative.AsString(context, NamespaceFormat.Uri);
            relative = expected.ToRelativePath(context);
            var result = relative.AsString(context, NamespaceFormat.Uri);

            Assert.Equal(expected, result);
        }

        // ── Null / empty path inputs ──────────────────────────────────────────

        [Fact]
        public void ToRelativePath_NullPath_ReturnsEmptyRelativePath()
        {
            var context = new ServiceMessageContext();
            var result = ((IReadOnlyList<string>?)null).ToRelativePath(context);
            Assert.Empty(result.Elements.ToArray() ?? Array.Empty<RelativePathElement>());
        }

        [Fact]
        public void ToRelativePath_EmptyList_ReturnsEmptyRelativePath()
        {
            var context = new ServiceMessageContext();
            var result = Array.Empty<string>().ToRelativePath(context);
            Assert.Empty(result.Elements.ToArray() ?? Array.Empty<RelativePathElement>());
        }

        [Fact]
        public void ToRelativePath_ListWithEmptyStrings_FiltersThemOut()
        {
            var context = new ServiceMessageContext();
            var path = new[] { "", "foo", "" };
            var result = path.ToRelativePath(context);
            Assert.Single(result.Elements.ToArray()!);
        }

        [Fact]
        public void AsString_NullRelativePath_ReturnsNull()
        {
            var context = new ServiceMessageContext();
            RelativePath? nullPath = null;
            var result = nullPath.AsString(context, NamespaceFormat.Uri);
            Assert.Null(result);
        }

        // ── Single-segment parsing ────────────────────────────────────────────

        [Fact]
        public void ParseSlash_SetsHierarchicalReferencesAndTarget()
        {
            var context = new ServiceMessageContext();
            var path = new[] { "/MyNode" };
            var result = path.ToRelativePath(context);
            var element = result.Elements[0];
            Assert.Equal(ReferenceTypeIds.HierarchicalReferences, element.ReferenceTypeId);
            Assert.Equal("MyNode", element.TargetName.Name);
            Assert.False(element.IsInverse);
            Assert.True(element.IncludeSubtypes);
        }

        [Fact]
        public void ParseDot_SetsAggregatesAndTarget()
        {
            var context = new ServiceMessageContext();
            var path = new[] { ".Child" };
            var result = path.ToRelativePath(context);
            var element = result.Elements[0];
            Assert.Equal(ReferenceTypeIds.Aggregates, element.ReferenceTypeId);
            Assert.Equal("Child", element.TargetName.Name);
        }

        [Fact]
        public void ParseBare_SetsReferencesAndTarget()
        {
            var context = new ServiceMessageContext();
            var path = new[] { "SimpleNode" };
            var result = path.ToRelativePath(context);
            var element = result.Elements[0];
            Assert.Equal(ReferenceTypeIds.References, element.ReferenceTypeId);
            Assert.Equal("SimpleNode", element.TargetName.Name);
        }

        [Fact]
        public void ParseExclamation_SetsIsInverse()
        {
            var context = new ServiceMessageContext();
            var path = new[] { "!/Parent" };
            var result = path.ToRelativePath(context);
            var element = result.Elements[0];
            Assert.True(element.IsInverse);
        }

        [Fact]
        public void ParseHash_SetsIncludeSubtypesFalse()
        {
            var context = new ServiceMessageContext();
            var path = new[] { "#/Strict" };
            var result = path.ToRelativePath(context);
            var element = result.Elements[0];
            Assert.False(element.IncludeSubtypes);
        }

        [Fact]
        public void ParseBracketedKnownReferenceType_SetsKnownNodeId()
        {
            var context = new ServiceMessageContext();
            var path = new[] { "<HasChild>ChildNode" };
            var result = path.ToRelativePath(context);
            var element = result.Elements[0];
            Assert.False(NodeIdCompat.IsNull(element.ReferenceTypeId));
        }

        // ── Format error cases ────────────────────────────────────────────────

        [Fact]
        public void ParseDuplicateSlash_ThrowsFormatException()
        {
            var context = new ServiceMessageContext();
            Assert.Throws<FormatException>(() =>
                new[] { "//node" }.ToRelativePath(context));
        }

        [Fact]
        public void ParseDuplicateDot_ThrowsFormatException()
        {
            var context = new ServiceMessageContext();
            Assert.Throws<FormatException>(() =>
                new[] { "..node" }.ToRelativePath(context));
        }

        [Fact]
        public void ParseDoubleBracket_ThrowsFormatException()
        {
            // Setting a reference with < after / (ref already set) → FormatException
            var context = new ServiceMessageContext();
            Assert.Throws<FormatException>(() =>
                new[] { "/<HasChild>node" }.ToRelativePath(context));
        }

        [Fact]
        public void ParseBracketWithoutClose_ThrowsFormatException()
        {
            var context = new ServiceMessageContext();
            Assert.Throws<FormatException>(() =>
                new[] { "<HasChildNode" }.ToRelativePath(context));
        }

        [Fact]
        public void ParseEmptyTargetName_ThrowsFormatException()
        {
            // <HasChild> with no target name after > throws FormatException
            var context = new ServiceMessageContext();
            Assert.Throws<FormatException>(() =>
                new[] { "<HasChild>" }.ToRelativePath(context));
        }

        // ── Round-trip for all namespace formats ──────────────────────────────

        [Theory]
        [InlineData(NamespaceFormat.Uri)]
        [InlineData(NamespaceFormat.Index)]
        [InlineData(NamespaceFormat.Expanded)]
        public void RoundTripSimplePath_AllNamespaceFormats(NamespaceFormat format)
        {
            var path = new[] { "/MyNode", ".Child", "Value" };
            var context = new ServiceMessageContext();
            var relative = path.ToRelativePath(context);
            var strings = relative.AsString(context, format);
            Assert.NotNull(strings);
            var roundTripped = strings.ToRelativePath(context);
            var result = roundTripped.AsString(context, format);
            Assert.Equal(strings, result);
        }

        // ── FormatRelativePathElement branches ───────────────────────────────

        [Fact]
        public void Format_HierarchicalRef_StartsWithSlash()
        {
            var context = new ServiceMessageContext();
            var path = new[] { "/MyTarget" };
            var relative = path.ToRelativePath(context);
            var result = relative.AsString(context, NamespaceFormat.Uri);
            Assert.NotNull(result);
            Assert.StartsWith("/", result[0]);
        }

        [Fact]
        public void Format_AggregatesRef_StartsWithDot()
        {
            var context = new ServiceMessageContext();
            var path = new[] { ".MyTarget" };
            var relative = path.ToRelativePath(context);
            var result = relative.AsString(context, NamespaceFormat.Uri);
            Assert.NotNull(result);
            Assert.StartsWith(".", result[0]);
        }

        [Fact]
        public void Format_IsInverse_IncludesExclamation()
        {
            var context = new ServiceMessageContext();
            var path = new[] { "!/InverseNode" };
            var relative = path.ToRelativePath(context);
            var result = relative.AsString(context, NamespaceFormat.Uri);
            Assert.NotNull(result);
            Assert.Contains("!", result[0]);
        }

        [Fact]
        public void Format_IncludeSubtypesFalse_IncludesHash()
        {
            var context = new ServiceMessageContext();
            var path = new[] { "#/StrictNode" };
            var relative = path.ToRelativePath(context);
            var result = relative.AsString(context, NamespaceFormat.Uri);
            Assert.NotNull(result);
            Assert.Contains("#", result[0]);
        }

        [Fact]
        public void Format_CustomReferenceType_WrapsInAngleBrackets()
        {
            var context = new ServiceMessageContext();
            var path = new[] { "<HasChild>BrowseTarget" };
            var relative = path.ToRelativePath(context);
            var result = relative.AsString(context, NamespaceFormat.Uri);
            Assert.NotNull(result);
            Assert.Contains("<", result[0]);
            Assert.Contains(">", result[0]);
        }
    }
}

