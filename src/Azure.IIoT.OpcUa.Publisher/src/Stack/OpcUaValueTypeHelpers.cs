// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using System.Collections.Generic;

    // What is left here is not dead weight. The generated XxxCollection types
    // this file used to resurrect are gone - call sites use List<T>, which
    // still converts implicitly to the 2.0 ArrayOf<T>. These two remain
    // because removing them costs more than it saves:
    //
    // NodeIdCompat is ten lines standing in front of sixty-six call sites, and
    // the substitution is not uniform: a NodeId reads x.IsNull, a NodeId? has
    // to check the null first. Getting one of those wrong buys a null
    // reference at runtime in exchange for ten lines.
    //
    // RelativePathCompat backs a backtracking browse-path search in SessionEx
    // that removes, inserts and rewrites elements as it walks. Rebuilding it
    // around the immutable collection is a rewrite of the algorithm, not a
    // mechanical substitution.

    /// <summary> Null-check helpers for the value-type node ids. </summary>
    public static class NodeIdCompat
    {
        /// <summary> True if the node id is null. </summary>
        public static bool IsNull(NodeId nodeId) => nodeId.IsNull;
        /// <summary> True if the node id is null. </summary>
        public static bool IsNull(NodeId? nodeId) => nodeId is null || nodeId.Value.IsNull;
        /// <summary> True if the node id is null. </summary>
        public static bool IsNull(ExpandedNodeId nodeId) => nodeId.IsNull;
        /// <summary> True if the node id is null. </summary>
        public static bool IsNull(ExpandedNodeId? nodeId) => nodeId is null || nodeId.Value.IsNull;
    }

    // TODO(Phase 4b/5): In 2.0 RelativePath.Elements is an immutable ArrayOf<T>
    // with no Add/Insert/RemoveAt. These helpers restore the classic in-place
    // mutation shape used by the browse-path resolver by rebuilding the list.
    /// <summary> Mutation helpers for RelativePath. </summary>
    public static class RelativePathCompat
    {
        /// <summary> Append an element. </summary>
        public static void Add(this RelativePath path, RelativePathElement element)
        {
            List<RelativePathElement> elements = [.. path.Elements];
            elements.Add(element);
            path.Elements = elements;
        }

        /// <summary> Insert an element. </summary>
        public static void Insert(this RelativePath path, int index,
            RelativePathElement element)
        {
            List<RelativePathElement> elements = [.. path.Elements];
            elements.Insert(index, element);
            path.Elements = elements;
        }

        /// <summary> Remove an element at index. </summary>
        public static void RemoveAt(this RelativePath path, int index)
        {
            List<RelativePathElement> elements = [.. path.Elements];
            elements.RemoveAt(index);
            path.Elements = elements;
        }

        /// <summary> Append a range of elements. </summary>
        public static void AddRange(this RelativePath path,
            ArrayOf<RelativePathElement> range)
        {
            List<RelativePathElement> elements = [.. path.Elements];
            elements.AddRange([.. range]);
            path.Elements = elements;
        }
    }

}
