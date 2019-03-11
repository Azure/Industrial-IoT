// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {

    /// <summary>
    /// Vertex and edge labels defining the elements of the address
    /// space graph.
    /// </summary>
    public static class AddressSpaceElementNames {

        /// <summary>
        /// Source vertex
        /// </summary>
        public const string Source = nameof(Source);

        /// <summary>
        /// Any vertex originates from a source except for source.
        /// </summary>
        public const string originatesFrom = nameof(originatesFrom);

        /// <summary>
        /// View node vertex
        /// </summary>
        public const string View = nameof(View);

        /// <summary>
        /// Method node vertex
        /// </summary>
        public const string Method = nameof(Method);

        /// <summary>
        /// Variable node vertex
        /// </summary>
        public const string Variable = nameof(Variable);

        /// <summary>
        /// Property node vertex
        /// </summary>
        public const string Property = nameof(Property);

        /// <summary>
        /// Variable type node vertex
        /// </summary>
        public const string VariableType = nameof(VariableType);

        /// <summary>
        /// Property type node vertex
        /// </summary>
        public const string PropertyType = nameof(PropertyType);

        /// <summary>
        /// Object node vertex
        /// </summary>
        public const string Object = nameof(Object);

        /// <summary>
        /// Object type node vertex
        /// </summary>
        public const string ObjectType = nameof(ObjectType);

        /// <summary>
        /// Links a node to its type node
        /// </summary>
        public const string ofType = nameof(ofType);

        /// <summary>
        /// Data type Node vertex
        /// </summary>
        public const string DataType = nameof(DataType);

        /// <summary>
        /// ReferenceType Node vertex
        /// </summary>
        public const string ReferenceType = nameof(ReferenceType);

        /// <summary>
        /// Links a target node from a reference
        /// </summary>
        public const string to = nameof(to);

        /// <summary>
        /// Reference node vertex
        /// </summary>
        public const string Reference = nameof(Reference);

        /// <summary>
        /// Links a source node to a reference
        /// </summary>
        public const string from = nameof(from);

        /// <summary>
        /// Links a node to a permission
        /// </summary>
        public const string rolePermission = nameof(rolePermission);

        /// <summary>
        /// Unknown node vertex type
        /// </summary>
        public const string Unknown = nameof(Unknown);
    }
}
