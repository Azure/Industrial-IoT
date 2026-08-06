// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="NodeIdCompat"/> and <see cref="RelativePathCompat"/>
    /// helpers in OpcUaValueTypeHelpers.cs.
    /// </summary>
    public sealed class OpcUaValueTypeHelpersTests
    {
        // ── NodeIdCompat.IsNull(NodeId) ───────────────────────────────────────

        [Fact]
        public void NodeIdCompat_IsNull_DefaultNodeId_ReturnsTrue()
        {
            var id = new NodeId();
            Assert.True(NodeIdCompat.IsNull(id));
        }

        [Fact]
        public void NodeIdCompat_IsNull_NumericZero_ReturnsTrue()
        {
            var id = new NodeId(0u);
            Assert.True(NodeIdCompat.IsNull(id));
        }

        [Fact]
        public void NodeIdCompat_IsNull_NonZeroNumeric_ReturnsFalse()
        {
            var id = new NodeId(1u);
            Assert.False(NodeIdCompat.IsNull(id));
        }

        [Fact]
        public void NodeIdCompat_IsNull_ObjectIds_ReturnsFalse()
        {
            // ObjectIds.RootFolder = 84
            Assert.False(NodeIdCompat.IsNull(ObjectIds.RootFolder));
        }

        [Fact]
        public void NodeIdCompat_IsNull_StringNodeId_ReturnsFalse()
        {
            var id = new NodeId("myNode", 1);
            Assert.False(NodeIdCompat.IsNull(id));
        }

        [Fact]
        public void NodeIdCompat_IsNull_GuidNodeId_ReturnsFalse()
        {
            var id = new NodeId(Guid.NewGuid(), 1);
            Assert.False(NodeIdCompat.IsNull(id));
        }

        // ── NodeIdCompat.IsNull(NodeId?) ──────────────────────────────────────

        [Fact]
        public void NodeIdCompat_IsNull_NullableNull_ReturnsTrue()
        {
            NodeId? id = null;
            Assert.True(NodeIdCompat.IsNull(id));
        }

        [Fact]
        public void NodeIdCompat_IsNull_NullableDefault_ReturnsTrue()
        {
            NodeId? id = new NodeId();
            Assert.True(NodeIdCompat.IsNull(id));
        }

        [Fact]
        public void NodeIdCompat_IsNull_NullableNonZero_ReturnsFalse()
        {
            NodeId? id = new NodeId(1u);
            Assert.False(NodeIdCompat.IsNull(id));
        }

        // ── NodeIdCompat.IsNull(ExpandedNodeId) ───────────────────────────────

        [Fact]
        public void NodeIdCompat_IsNull_DefaultExpandedNodeId_ReturnsTrue()
        {
            var id = new ExpandedNodeId();
            Assert.True(NodeIdCompat.IsNull(id));
        }

        [Fact]
        public void NodeIdCompat_IsNull_NonNullExpandedNodeId_ReturnsFalse()
        {
            var id = new ExpandedNodeId(1u);
            Assert.False(NodeIdCompat.IsNull(id));
        }

        // ── NodeIdCompat.IsNull(ExpandedNodeId?) ──────────────────────────────

        [Fact]
        public void NodeIdCompat_IsNull_NullableExpandedNull_ReturnsTrue()
        {
            ExpandedNodeId? id = null;
            Assert.True(NodeIdCompat.IsNull(id));
        }

        [Fact]
        public void NodeIdCompat_IsNull_NullableExpandedDefault_ReturnsTrue()
        {
            ExpandedNodeId? id = new ExpandedNodeId();
            Assert.True(NodeIdCompat.IsNull(id));
        }

        [Fact]
        public void NodeIdCompat_IsNull_NullableExpandedNonNull_ReturnsFalse()
        {
            ExpandedNodeId? id = new ExpandedNodeId(1u);
            Assert.False(NodeIdCompat.IsNull(id));
        }

        // ── RelativePathCompat.Add ────────────────────────────────────────────

        [Fact]
        public void RelativePathCompat_Add_AppendsElement()
        {
            var path = new RelativePath
            {
                Elements = new List<RelativePathElement>
                {
                    new RelativePathElement { TargetName = new QualifiedName("Existing") }
                }
            };
            var newElement = new RelativePathElement { TargetName = new QualifiedName("New") };

            path.Add(newElement);

            Assert.Equal(2, path.Elements.Count);
            Assert.Equal("New", path.Elements[1].TargetName.Name);
        }

        [Fact]
        public void RelativePathCompat_Add_ToEmptyPath_AddsElement()
        {
            var path = new RelativePath
            {
                Elements = new List<RelativePathElement>()
            };
            var el = new RelativePathElement { TargetName = new QualifiedName("Root") };

            path.Add(el);

            Assert.Equal(1, path.Elements.Count);
            Assert.Equal("Root", path.Elements[0].TargetName.Name);
        }

        // ── RelativePathCompat.Insert ─────────────────────────────────────────

        [Fact]
        public void RelativePathCompat_Insert_InsertsAtBeginning()
        {
            var path = new RelativePath
            {
                Elements = new List<RelativePathElement>
                {
                    new RelativePathElement { TargetName = new QualifiedName("Second") }
                }
            };
            var first = new RelativePathElement { TargetName = new QualifiedName("First") };

            path.Insert(0, first);

            Assert.Equal(2, path.Elements.Count);
            Assert.Equal("First", path.Elements[0].TargetName.Name);
            Assert.Equal("Second", path.Elements[1].TargetName.Name);
        }

        [Fact]
        public void RelativePathCompat_Insert_InsertsAtMiddle()
        {
            var path = new RelativePath
            {
                Elements = new List<RelativePathElement>
                {
                    new RelativePathElement { TargetName = new QualifiedName("First") },
                    new RelativePathElement { TargetName = new QualifiedName("Third") }
                }
            };
            var middle = new RelativePathElement { TargetName = new QualifiedName("Second") };

            path.Insert(1, middle);

            Assert.Equal(3, path.Elements.Count);
            Assert.Equal("First", path.Elements[0].TargetName.Name);
            Assert.Equal("Second", path.Elements[1].TargetName.Name);
            Assert.Equal("Third", path.Elements[2].TargetName.Name);
        }

        // ── RelativePathCompat.RemoveAt ───────────────────────────────────────

        [Fact]
        public void RelativePathCompat_RemoveAt_RemovesFirst()
        {
            var path = new RelativePath
            {
                Elements = new List<RelativePathElement>
                {
                    new RelativePathElement { TargetName = new QualifiedName("First") },
                    new RelativePathElement { TargetName = new QualifiedName("Second") }
                }
            };

            path.RemoveAt(0);

            Assert.Equal(1, path.Elements.Count);
            Assert.Equal("Second", path.Elements[0].TargetName.Name);
        }

        [Fact]
        public void RelativePathCompat_RemoveAt_RemovesLast()
        {
            var path = new RelativePath
            {
                Elements = new List<RelativePathElement>
                {
                    new RelativePathElement { TargetName = new QualifiedName("Only") },
                    new RelativePathElement { TargetName = new QualifiedName("Remove") }
                }
            };

            path.RemoveAt(1);

            Assert.Equal(1, path.Elements.Count);
            Assert.Equal("Only", path.Elements[0].TargetName.Name);
        }

        // ── RelativePathCompat.AddRange ───────────────────────────────────────

        [Fact]
        public void RelativePathCompat_AddRange_AppendsAllElements()
        {
            var path = new RelativePath
            {
                Elements = new List<RelativePathElement>
                {
                    new RelativePathElement { TargetName = new QualifiedName("First") }
                }
            };
            ArrayOf<RelativePathElement> range = new RelativePathElement[]
            {
                new RelativePathElement { TargetName = new QualifiedName("Second") },
                new RelativePathElement { TargetName = new QualifiedName("Third") }
            };

            path.AddRange(range);

            Assert.Equal(3, path.Elements.Count);
            Assert.Equal("First", path.Elements[0].TargetName.Name);
            Assert.Equal("Second", path.Elements[1].TargetName.Name);
            Assert.Equal("Third", path.Elements[2].TargetName.Name);
        }

        [Fact]
        public void RelativePathCompat_AddRange_EmptyRange_NoChange()
        {
            var path = new RelativePath
            {
                Elements = new List<RelativePathElement>
                {
                    new RelativePathElement { TargetName = new QualifiedName("Only") }
                }
            };
            var emptyRange = ArrayOf<RelativePathElement>.Empty;

            path.AddRange(emptyRange);

            Assert.Equal(1, path.Elements.Count);
        }
    }
}
