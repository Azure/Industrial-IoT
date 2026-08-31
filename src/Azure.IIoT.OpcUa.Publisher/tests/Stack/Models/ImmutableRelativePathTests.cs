// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="ImmutableRelativePath"/>.
    /// </summary>
    public sealed class ImmutableRelativePathTests
    {
        // ── Constructor ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_StoresPath()
        {
            var path = new List<string> { "Root", "Child" };
            var irp = new ImmutableRelativePath(path);

            Assert.Equal(2, irp.Path.Count);
            Assert.Equal("Root", irp.Path[0]);
            Assert.Equal("Child", irp.Path[1]);
        }

        [Fact]
        public void Constructor_EmptyPath_PathIsEmpty()
        {
            var irp = new ImmutableRelativePath([]);

            Assert.Empty(irp.Path);
        }

        // ── Create factory ────────────────────────────────────────────────────

        [Fact]
        public void Create_NullParentPath_CreatesSingleElementPath()
        {
            var irp = ImmutableRelativePath.Create(null, "Leaf");

            Assert.Single(irp.Path);
            Assert.Equal("Leaf", irp.Path[0]);
        }

        [Fact]
        public void Create_WithParentPath_AppendsBrowseName()
        {
            var parent = new List<string> { "Root", "Middle" };
            var irp = ImmutableRelativePath.Create(parent, "Leaf");

            Assert.Equal(3, irp.Path.Count);
            Assert.Equal("Root", irp.Path[0]);
            Assert.Equal("Middle", irp.Path[1]);
            Assert.Equal("Leaf", irp.Path[2]);
        }

        [Fact]
        public void Create_WithEmptyParentPath_CreatesSingleElementPath()
        {
            var irp = ImmutableRelativePath.Create([], "OnlyElement");

            Assert.Single(irp.Path);
            Assert.Equal("OnlyElement", irp.Path[0]);
        }

        // ── Equals ────────────────────────────────────────────────────────────

        [Fact]
        public void Equals_SamePath_ReturnsTrue()
        {
            var a = new ImmutableRelativePath(["Root", "Child"]);
            var b = new ImmutableRelativePath(["Root", "Child"]);

            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentLength_ReturnsFalse()
        {
            var a = new ImmutableRelativePath(["Root", "Child"]);
            var b = new ImmutableRelativePath(["Root"]);

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentContent_ReturnsFalse()
        {
            var a = new ImmutableRelativePath(["Root", "ChildA"]);
            var b = new ImmutableRelativePath(["Root", "ChildB"]);

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_EmptyPaths_ReturnsTrue()
        {
            var a = new ImmutableRelativePath([]);
            var b = new ImmutableRelativePath([]);

            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_ObjectOverload_SameContent_ReturnsTrue()
        {
            var a = new ImmutableRelativePath(["Root"]);
            object b = new ImmutableRelativePath(["Root"]);

            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_ObjectOverload_DifferentType_ReturnsFalse()
        {
            var a = new ImmutableRelativePath(["Root"]);

            Assert.False(a.Equals("Root"));
        }

        // ── GetHashCode ───────────────────────────────────────────────────────

        [Fact]
        public void GetHashCode_SameContent_ReturnsSameCode()
        {
            var a = new ImmutableRelativePath(["Alpha", "Beta"]);
            var b = new ImmutableRelativePath(["Alpha", "Beta"]);

            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void GetHashCode_IsConsistent()
        {
            var a = new ImmutableRelativePath(["Alpha"]);
            var h1 = a.GetHashCode();
            var h2 = a.GetHashCode();

            Assert.Equal(h1, h2);
        }

        [Fact]
        public void GetHashCode_EmptyPath_DoesNotThrow()
        {
            var a = new ImmutableRelativePath([]);
            var ex = Record.Exception(() => a.GetHashCode());
            Assert.Null(ex);
        }

        // ── ToString ──────────────────────────────────────────────────────────

        [Fact]
        public void ToString_MultipleElements_ConcatenatesAll()
        {
            var irp = new ImmutableRelativePath(["Alpha", "Beta", "Gamma"]);
            var str = irp.ToString();

            Assert.Equal("AlphaBetaGamma", str);
        }

        [Fact]
        public void ToString_SingleElement_ReturnsElement()
        {
            var irp = new ImmutableRelativePath(["OnlyOne"]);
            var str = irp.ToString();

            Assert.Equal("OnlyOne", str);
        }

        // ── Dictionary / set usage ────────────────────────────────────────────

        [Fact]
        public void UsedAsDictionaryKey_EqualContentLookedUpSuccessfully()
        {
            var dict = new System.Collections.Generic.Dictionary<ImmutableRelativePath, string>();
            var key = new ImmutableRelativePath(["Foo", "Bar"]);
            dict[key] = "found";

            var lookup = new ImmutableRelativePath(["Foo", "Bar"]);

            Assert.True(dict.ContainsKey(lookup));
            Assert.Equal("found", dict[lookup]);
        }
    }
}
