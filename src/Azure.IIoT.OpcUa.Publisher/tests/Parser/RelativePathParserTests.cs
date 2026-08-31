// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Parser
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="RelativePathParser"/> — pure string parsing
    /// and formatting logic, no OPC UA server required.
    /// </summary>
    public sealed class RelativePathParserTests
    {
        // ── ToRelativePath: empty/prefix-only inputs ─────────────────────────

        [Fact]
        public void EmptyPath_ReturnsEmptyElementsAndEmptyPrefix()
        {
            var elements = "".ToRelativePath(out var prefix).ToList();

            Assert.Empty(elements);
            Assert.Equal(string.Empty, prefix);
        }

        [Fact]
        public void PrefixOnly_NoPathChars_ReturnsEmptyElementsAndExtractedPrefix()
        {
            // A bare word with no / . or < is just a prefix; no elements follow.
            var elements = "myNode".ToRelativePath(out var prefix).ToList();

            Assert.Empty(elements);
            Assert.Equal("myNode", prefix);
        }

        [Fact]
        public void PrefixFollowedBySlash_ReturnsOneHierarchicalElement()
        {
            var elements = "root/child".ToRelativePath(out var prefix).ToList();

            Assert.Equal("root", prefix);
            Assert.Single(elements);
            Assert.Equal(ReferenceTypeIds.HierarchicalReferences.ToString(),
                elements[0].ReferenceTypeId);
            Assert.Equal("child", elements[0].TargetName);
        }

        [Fact]
        public void PrefixFollowedByDot_ReturnsOneAggregatesElement()
        {
            var elements = "root.property".ToRelativePath(out var prefix).ToList();

            Assert.Equal("root", prefix);
            Assert.Single(elements);
            Assert.Equal(ReferenceTypeIds.Aggregates.ToString(),
                elements[0].ReferenceTypeId);
            Assert.Equal("property", elements[0].TargetName);
        }

        // ── ToRelativePath: slash / dot paths ────────────────────────────────

        [Fact]
        public void SlashPrefix_PrefixIsEmptyAndOneHierarchicalElement()
        {
            var elements = "/child".ToRelativePath(out var prefix).ToList();

            Assert.Equal(string.Empty, prefix);
            Assert.Single(elements);
            Assert.Equal(ReferenceTypeIds.HierarchicalReferences.ToString(),
                elements[0].ReferenceTypeId);
            Assert.Equal("child", elements[0].TargetName);
        }

        [Fact]
        public void DotPrefix_PrefixIsEmptyAndOneAggregatesElement()
        {
            var elements = ".property".ToRelativePath(out var prefix).ToList();

            Assert.Equal(string.Empty, prefix);
            Assert.Single(elements);
            Assert.Equal(ReferenceTypeIds.Aggregates.ToString(),
                elements[0].ReferenceTypeId);
            Assert.Equal("property", elements[0].TargetName);
        }

        [Fact]
        public void MultipleSegments_AllParsedInOrder()
        {
            var elements = "/parent/child".ToRelativePath(out _).ToList();

            Assert.Equal(2, elements.Count);
            Assert.All(elements, e =>
                Assert.Equal(ReferenceTypeIds.HierarchicalReferences.ToString(),
                    e.ReferenceTypeId));
            Assert.Equal("parent", elements[0].TargetName);
            Assert.Equal("child", elements[1].TargetName);
        }

        [Fact]
        public void MixedSlashDot_ParsesBothReferenceTypes()
        {
            var elements = "/parent.property".ToRelativePath(out _).ToList();

            Assert.Equal(2, elements.Count);
            Assert.Equal(ReferenceTypeIds.HierarchicalReferences.ToString(),
                elements[0].ReferenceTypeId);
            Assert.Equal("parent", elements[0].TargetName);
            Assert.Equal(ReferenceTypeIds.Aggregates.ToString(),
                elements[1].ReferenceTypeId);
            Assert.Equal("property", elements[1].TargetName);
        }

        // ── ToRelativePath: modifiers ! and # ────────────────────────────────

        [Fact]
        public void ExclamationBeforeSlash_SetsIsInverse()
        {
            var elements = "!/parent".ToRelativePath(out _).ToList();

            Assert.Single(elements);
            Assert.True(elements[0].IsInverse);
            Assert.Equal("parent", elements[0].TargetName);
        }

        [Fact]
        public void HashBeforeSlash_SetsNoSubtypes()
        {
            var elements = "#/strict".ToRelativePath(out _).ToList();

            Assert.Single(elements);
            Assert.True(elements[0].NoSubtypes);
            Assert.Equal("strict", elements[0].TargetName);
        }

        [Fact]
        public void ExclamationAndHashBeforeSlash_SetsBothFlags()
        {
            var elements = "!#/target".ToRelativePath(out _).ToList();

            Assert.Single(elements);
            Assert.True(elements[0].IsInverse);
            Assert.True(elements[0].NoSubtypes);
            Assert.Equal("target", elements[0].TargetName);
        }

        [Fact]
        public void NoModifiers_IsInverseAndNoSubtypesAreNull()
        {
            var elements = "/child".ToRelativePath(out _).ToList();

            Assert.Single(elements);
            Assert.Null(elements[0].IsInverse);
            Assert.Null(elements[0].NoSubtypes);
        }

        // ── ToRelativePath: angle-bracket reference types ────────────────────

        [Fact]
        public void BracketedKnownReferenceType_ResolvesToNodeId()
        {
            // "HasChild" is a well-known reference type with numeric node id.
            var elements = "<HasChild>target".ToRelativePath(out _).ToList();

            Assert.Single(elements);
            Assert.False(string.IsNullOrEmpty(elements[0].ReferenceTypeId));
            Assert.Equal("target", elements[0].TargetName);
        }

        [Fact]
        public void BracketedUnknownReferenceType_UsesRawString()
        {
            // An unknown reference type is preserved as-is.
            var elements = "<MyCustomRef>target".ToRelativePath(out _).ToList();

            Assert.Single(elements);
            Assert.Equal("MyCustomRef", elements[0].ReferenceTypeId);
            Assert.Equal("target", elements[0].TargetName);
        }

        [Fact]
        public void BracketedWithInverse_SetsIsInverse()
        {
            var elements = "<!HasChild>target".ToRelativePath(out _).ToList();

            Assert.Single(elements);
            Assert.True(elements[0].IsInverse);
            Assert.Equal("target", elements[0].TargetName);
        }

        [Fact]
        public void BracketedWithNoSubtypes_SetsNoSubtypes()
        {
            var elements = "<#HasChild>target".ToRelativePath(out _).ToList();

            Assert.Single(elements);
            Assert.True(elements[0].NoSubtypes);
        }

        [Fact]
        public void BracketedWithInverseAndNoSubtypes_SetsBothFlags()
        {
            var elements = "<!#HasChild>target".ToRelativePath(out _).ToList();

            Assert.Single(elements);
            Assert.True(elements[0].IsInverse);
            Assert.True(elements[0].NoSubtypes);
        }

        [Fact]
        public void BracketedRefAfterExclamation_SetsIsInverse()
        {
            var elements = "!<HasChild>target".ToRelativePath(out _).ToList();

            Assert.Single(elements);
            Assert.True(elements[0].IsInverse);
        }

        // ── ToRelativePath: error cases ───────────────────────────────────────

        [Fact]
        public void DoubleSlash_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() =>
                "//node".ToRelativePath(out _).ToList());
        }

        [Fact]
        public void DoubleDot_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() =>
                "..node".ToRelativePath(out _).ToList());
        }

        [Fact]
        public void SlashFollowedByBracket_ThrowsFormatException()
        {
            // '/' then '<' — reference type is already set
            Assert.Throws<FormatException>(() =>
                "/<HasChild>node".ToRelativePath(out _).ToList());
        }

        [Fact]
        public void EmptyBrackets_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() =>
                "<>target".ToRelativePath(out _).ToList());
        }

        [Fact]
        public void UnclosedBracket_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() =>
                "<HasChildtarget".ToRelativePath(out _).ToList());
        }

        [Fact]
        public void BracketedRefWithNestedLessThan_ThrowsFormatException()
        {
            // '<' inside the reference type portion (not escaped) → error
            Assert.Throws<FormatException>(() =>
                "<Has<Child>target".ToRelativePath(out _).ToList());
        }

        [Fact]
        public void DoubleClosingAngles_ThrowsFormatException()
        {
            // ">>" right after the closing > of the reference → error
            Assert.Throws<FormatException>(() =>
                "<HasChild>>target".ToRelativePath(out _).ToList());
        }

        [Fact]
        public void UnescapedTargetWithUnclosedBracket_ThrowsFormatException()
        {
            // '[' opened but never closed
            Assert.Throws<FormatException>(() =>
                "/[unclosed".ToRelativePath(out _).ToList());
        }

        [Fact]
        public void ExclamationWithNoBracketOrSeparator_ThrowsFormatException()
        {
            // "!node" — inverse flag set but no reference type indicator follows
            Assert.Throws<FormatException>(() =>
                "!node".ToRelativePath(out _).ToList());
        }

        // ── ToRelativePath: escaped target names ─────────────────────────────

        [Fact]
        public void EscapedTargetWithSpecialChars_ParsedCorrectly()
        {
            // Target name containing / inside [] brackets
            var elements = "/[target/with/slashes]".ToRelativePath(out _).ToList();

            Assert.Single(elements);
            Assert.Equal("target/with/slashes", elements[0].TargetName);
        }

        // ── IsAggregatesReference ─────────────────────────────────────────────

        [Fact]
        public void IsAggregatesReference_ByNodeIdString_ReturnsTrue()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "t",
                ReferenceTypeId = ReferenceTypeIds.Aggregates.ToString()
            };

            Assert.True(element.IsAggregatesReference());
        }

        [Fact]
        public void IsAggregatesReference_ByName_ReturnsTrue()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "t",
                ReferenceTypeId = nameof(ReferenceTypes.Aggregates)
            };

            Assert.True(element.IsAggregatesReference());
        }

        [Fact]
        public void IsAggregatesReference_OtherReference_ReturnsFalse()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "t",
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences.ToString()
            };

            Assert.False(element.IsAggregatesReference());
        }

        // ── IsHierarchicalReference ───────────────────────────────────────────

        [Fact]
        public void IsHierarchicalReference_ByNodeIdString_ReturnsTrue()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "t",
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences.ToString()
            };

            Assert.True(element.IsHierarchicalReference());
        }

        [Fact]
        public void IsHierarchicalReference_ByName_ReturnsTrue()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "t",
                ReferenceTypeId = nameof(ReferenceTypes.HierarchicalReferences)
            };

            Assert.True(element.IsHierarchicalReference());
        }

        [Fact]
        public void IsHierarchicalReference_OtherReference_ReturnsFalse()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "t",
                ReferenceTypeId = ReferenceTypeIds.Aggregates.ToString()
            };

            Assert.False(element.IsHierarchicalReference());
        }

        // ── AsString ─────────────────────────────────────────────────────────

        [Fact]
        public void AsString_EmptyElements_ReturnsEmptyList()
        {
            var result = Enumerable.Empty<RelativePathElementModel>().AsString();

            Assert.Empty(result);
        }

        [Fact]
        public void AsString_HierarchicalElement_StartsWithSlash()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "child",
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences.ToString()
            };

            var result = new[] { element }.AsString();

            Assert.Single(result);
            Assert.StartsWith("/", result[0]);
            Assert.Contains("child", result[0]);
        }

        [Fact]
        public void AsString_AggregatesElement_StartsWithDot()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "property",
                ReferenceTypeId = ReferenceTypeIds.Aggregates.ToString()
            };

            var result = new[] { element }.AsString();

            Assert.Single(result);
            Assert.StartsWith(".", result[0]);
            Assert.Contains("property", result[0]);
        }

        [Fact]
        public void AsString_CustomReferenceElement_WrapsInAngleBrackets()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "target",
                ReferenceTypeId = "MyCustomRef"
            };

            var result = new[] { element }.AsString();

            Assert.Single(result);
            Assert.Contains("<MyCustomRef>", result[0]);
        }

        [Fact]
        public void AsString_InverseElement_IncludesExclamation()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "parent",
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences.ToString(),
                IsInverse = true
            };

            var result = new[] { element }.AsString();

            Assert.Single(result);
            Assert.Contains("!", result[0]);
        }

        [Fact]
        public void AsString_NoSubtypesElement_IncludesHash()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "strict",
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences.ToString(),
                NoSubtypes = true
            };

            var result = new[] { element }.AsString();

            Assert.Single(result);
            Assert.Contains("#", result[0]);
        }

        [Fact]
        public void AsString_TargetWithSpecialChars_EscapesWithBrackets()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "target/with/slashes",
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences.ToString()
            };

            var result = new[] { element }.AsString();

            Assert.Single(result);
            Assert.Contains("[", result[0]);
            Assert.Contains("]", result[0]);
            Assert.Contains("target/with/slashes", result[0]);
        }

        [Fact]
        public void AsString_WithPrefix_PrependsPrefixToFirstElement()
        {
            var element = new RelativePathElementModel
            {
                TargetName = "child",
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences.ToString()
            };

            var result = new[] { element }.AsString(prefix: "myRoot");

            Assert.Single(result);
            Assert.StartsWith("myRoot", result[0]);
        }

        [Fact]
        public void AsString_KnownReferenceNodeId_UsesNameInBrackets()
        {
            // HasChild is a well-known reference type
            var element = new RelativePathElementModel
            {
                TargetName = "target",
                ReferenceTypeId = ReferenceTypeIds.HasChild.ToString()
            };

            var result = new[] { element }.AsString();

            Assert.Single(result);
            // Known NodeId should resolve to browse name in angle brackets
            Assert.Contains("<", result[0]);
            Assert.Contains("HasChild", result[0]);
        }

        // ── Round-trip consistency ────────────────────────────────────────────

        [Fact]
        public void RoundTrip_SlashPath_ProducesOriginalString()
        {
            const string input = "/parent/child";
            var elements = input.ToRelativePath(out var prefix).ToList();
            var output = elements.AsString(prefix).ToList();

            Assert.Equal(2, output.Count);
            Assert.Equal("/parent", output[0]);
            Assert.Equal("/child", output[1]);
        }

        [Fact]
        public void RoundTrip_DotPath_ProducesOriginalString()
        {
            const string input = ".property";
            var elements = input.ToRelativePath(out var prefix).ToList();
            var output = elements.AsString(prefix).ToList();

            Assert.Single(output);
            Assert.Equal(".property", output[0]);
        }

        [Fact]
        public void RoundTrip_PrefixedPath_ProducesEquivalentOutput()
        {
            const string input = "root/child.property";
            var elements = input.ToRelativePath(out var prefix).ToList();
            var output = elements.AsString(prefix).ToList();

            Assert.Equal(2, output.Count);
            Assert.StartsWith("root/", output[0]);
            Assert.StartsWith(".property", output[1]);
        }

        [Fact]
        public void RoundTrip_InverseElement_PreservesFlag()
        {
            const string input = "!/invparent";
            var elements = input.ToRelativePath(out var prefix).ToList();
            var output = elements.AsString(prefix).ToList();

            Assert.Single(output);
            Assert.Contains("!", output[0]);
        }

        [Fact]
        public void RoundTrip_NoSubtypesElement_PreservesFlag()
        {
            const string input = "#/strict";
            var elements = input.ToRelativePath(out var prefix).ToList();
            var output = elements.AsString(prefix).ToList();

            Assert.Single(output);
            Assert.Contains("#", output[0]);
        }
    }
}
