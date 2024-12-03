// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Irony.Parsing
{
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Parser extensions
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Get children
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static IEnumerable<ParseTreeNode> GetChildren(
            this ParseTreeNode? node)
        {
            if (node?.ChildNodes == null)
            {
                return [];
            }
            return node.ChildNodes;
        }

        /// <summary>
        /// Get children
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IEnumerable<ParseTreeNode> GetChildren(
            this ParseTreeNode? node, string name)
        {
            if (node?.ChildNodes == null)
            {
                return [];
            }
            return node.ChildNodes.Where(
                node => node.Term?.Name == name);
        }

        /// <summary>
        /// Get child at Index
        /// </summary>
        /// <param name="node"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static ParseTreeNode? GetChild(
            this ParseTreeNode? node, int index)
        {
            if (node?.ChildNodes == null ||
                index >= node.ChildNodes.Count)
            {
                return null;
            }
            return node.ChildNodes[index];
        }

        /// <summary>
        /// Get first node in the list
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <param name="expectedIndex"></param>
        /// <returns></returns>
        public static ParseTreeNode? GetChild(this ParseTreeNode? node,
            string name, ref int expectedIndex)
        {
            if (node?.ChildNodes == null)
            {
                return null;
            }
            if (expectedIndex < node.ChildNodes.Count)
            {
                var found = node.ChildNodes[expectedIndex];
                if (found.Term.Name == name)
                {
                    expectedIndex++;
                    return found;
                }
            }
            return node.ChildNodes.Find(node => node.Term?.Name == name);
        }

        /// <summary>
        /// Get first node in the list
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <param name="expectedIndex"></param>
        /// <param name="syntaxTree"></param>
        /// <returns></returns>
        public static ParseTreeNode GetChild(this ParseTreeNode? node,
            string name, ref int expectedIndex, ParseTree syntaxTree)
        {
            var child = GetChild(node, name, ref expectedIndex);
            return child ?? throw ParserException.Create("Child node {name} not found in node.",
                syntaxTree, node);
        }

        /// <summary>
        /// Get child at Index
        /// </summary>
        /// <param name="node"></param>
        /// <param name="index"></param>
        /// <param name="syntaxTree"></param>
        /// <returns></returns>
        public static ParseTreeNode GetChild(this ParseTreeNode? node,
            int index, ParseTree syntaxTree)
        {
            return GetChild(node, index)
                ?? throw ParserException.Create($"No Child at index {index} found in node.",
                syntaxTree, node);
        }

        /// <summary>
        /// Get child token text
        /// </summary>
        /// <param name="node"></param>
        /// <param name="index"></param>
        /// <param name="syntaxTree"></param>
        /// <returns></returns>
        public static string GetChildTokenText(this ParseTreeNode? node,
            int index, ParseTree syntaxTree)
        {
            var child = GetChild(node, index, syntaxTree);
            var token = child?.FindTokenAndGetText();
            return token ?? throw ParserException.Create($"No token value in node {node} at child {index}.",
                syntaxTree,
                node, child);
        }

        /// <summary>
        /// Get child token text
        /// </summary>
        /// <param name="node"></param>
        /// <param name="index"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetChildTokenText(this ParseTreeNode? node,
            int index, string defaultValue)
        {
            var child = GetChild(node, index);
            return child?.FindTokenAndGetText() ?? defaultValue;
        }
    }
}
