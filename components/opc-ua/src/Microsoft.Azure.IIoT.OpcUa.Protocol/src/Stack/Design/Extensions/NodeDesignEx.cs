/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Design.Schema {
    using System;
    using System.Xml;

    /// <summary>
    /// Node design extensions
    /// </summary>
    public static class NodeDesignEx {

        /// <summary>
        /// Returns true if the node is a declaration.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsDeclaration(this NodeDesign node) {
            for (var cur = node; cur != null; cur = cur.Parent) {
                if (cur.IsDeclaration) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the NodeClass for the node.
        /// </summary>
        public static string GetNodeClassText(this NodeDesign node) {
            switch (node) {
                case PropertyDesign _:
                    return "Property";
                case ObjectDesign _:
                    return "Object";
                case ObjectTypeDesign _:
                    return "ObjectType";
                case DataTypeDesign _:
                    return "DataType";
                case ReferenceTypeDesign _:
                    return "ReferenceType";
                case MethodDesign _:
                    return "Method";
                case VariableDesign _:
                    return "Variable";
                case VariableTypeDesign _:
                    return "VariableType";
                case ViewDesign _:
                    return "View";
            }
            return "node";
        }

        /// <summary>
        /// Returns the NodeClass for the node.
        /// </summary>
        public static NodeClass GetNodeClass(this NodeDesign node) {
            switch (node) {
                case ObjectDesign _:
                    return NodeClass.Object;
                case ObjectTypeDesign _:
                    return NodeClass.ObjectType;
                case DataTypeDesign _:
                    return NodeClass.DataType;
                case ReferenceTypeDesign _:
                    return NodeClass.ReferenceType;
                case MethodDesign _:
                    return NodeClass.Method;
                case VariableDesign _:
                    return NodeClass.Variable;
                case VariableTypeDesign _:
                    return NodeClass.VariableType;
                case ViewDesign _:
                    return NodeClass.View;
            }
            return NodeClass.Unspecified;
        }

        /// <summary>
        /// Returns the browse path to the instance.
        /// </summary>
        public static string GetBrowsePath(this InstanceDesign instance, string basePath) {
            return CreateSymbolicId(basePath, instance.SymbolicName.Name);
        }

        /// <summary>
        /// Get symbolic id for a child of a node
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="childName"></param>
        /// <returns></returns>
        public static XmlQualifiedName GetSymbolicIdForChild(this NodeDesign parent,
            string childName) {
            if (string.IsNullOrEmpty(childName)) {
                throw new ArgumentNullException(nameof(childName));
            }
            if (parent?.SymbolicId == null) {
                throw new ArgumentNullException(nameof(parent));
            }
            return new XmlQualifiedName(CreateSymbolicId(parent.SymbolicId.Name, childName),
                parent.SymbolicId.Namespace);
        }

        /// <summary>
        /// Get symbolic id for a node that is either root or has the specified parent.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="symbolicName"></param>
        /// <returns></returns>
        public static XmlQualifiedName CreateSymbolicId(this NodeDesign parent,
            XmlQualifiedName symbolicName) {
            if (symbolicName.IsNullOrEmpty()) {
                throw new ArgumentNullException(nameof(symbolicName));
            }
            return new XmlQualifiedName(CreateSymbolicId(parent?.SymbolicId?.Name, symbolicName.Name),
                symbolicName.Namespace);
        }

        /// <summary>
        /// Create symbolic id
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="childName"></param>
        /// <returns></returns>
        public static string CreateSymbolicId(string parentId, string childName) {
            if (string.IsNullOrEmpty(childName)) {
                return parentId;
            }
            if (string.IsNullOrEmpty(parentId)) {
                return childName;
            }
            return $"{parentId}{NodeDesign.kPathChar}{childName}";
        }

        /// <summary>
        /// Get node id for node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="namespaceUris"></param>
        /// <returns></returns>
        public static NodeId GetNodeId(this NodeDesign node, NamespaceTable namespaceUris) {
            if (node == null) {
                return NodeId.Null;
            }
            if (!string.IsNullOrEmpty(node.StringId)) {
                return new NodeId(node.StringId,
                    (ushort)namespaceUris.GetIndex(node.SymbolicId.Namespace));
            }
            if (node.NumericId == 0) {
                node = node.FindInstanceInHierarchy();
                if (node.NumericId == 0) {
                    return NodeId.Null;
                }
            }
            return new NodeId(node.NumericId,
                (ushort)namespaceUris.GetIndex(node.SymbolicId.Namespace));
        }

        /// <summary>
        /// Set identifier of node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="identifier"></param>
        public static void SetIdentifier(this NodeDesign node, object identifier) {
            // set identifier for node.
            if (identifier is uint) {
                node.NumericId = (uint)identifier;
                node.NumericIdSpecified = true;
                node.StringId = null;
            }
            else {
                node.NumericId = 0;
                node.NumericIdSpecified = false;
                node.StringId = identifier as string;
            }
        }

        /// <summary>
        /// Find the related instance design in the node's hierarchy
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static NodeDesign FindInstanceInHierarchy(this NodeDesign node) {
            for (var parent = node.Parent; parent != null; parent = parent.Parent) {
                if (parent.Hierarchy != null) {
                    var browsePath = node.SymbolicId.Name;
                    if (browsePath.StartsWith(parent.SymbolicId.Name, StringComparison.Ordinal) &&
                        browsePath[parent.SymbolicId.Name.Length] == NodeDesign.kPathChar[0]) {
                        // Get relative browse path from parent
                        browsePath = browsePath.Substring(parent.SymbolicId.Name.Length + 1);
                    }
                    // Get instance using browse path
                    if (parent.Hierarchy.Nodes.TryGetValue(browsePath, out var instance)) {
                        return instance.Instance;
                    }
                }
            }
            return node;
        }
    }
}

