// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace FileSystem
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System.Text;

    /// <summary>
    /// A class that builds NodeIds used by the FileSystem NodeManager
    /// </summary>
    public static class ModelUtils
    {
        /// <summary>
        /// The RootType for a Volume node identfier.
        /// </summary>
        public const int Volume = 0;

        /// <summary>
        /// The RootType for a Directory node identfier.
        /// </summary>
        public const int Directory = 1;

        /// <summary>
        /// The RootType for a File node identfier.
        /// </summary>
        public const int File = 2;

        /// <summary>
        /// Create id for drive
        /// </summary>
        /// <param name="path"></param>
        /// <param name="namespaceIndex"></param>
        /// <returns></returns>
        public static NodeId ConstructIdForVolume(string path, ushort namespaceIndex)
        {
            var parsedNodeId = new ParsedNodeId
            {
                RootId = path,
                NamespaceIndex = namespaceIndex,
                RootType = 0
            };
            return parsedNodeId.Construct();
        }

        /// <summary>
        /// Constructs a NodeId a file or directory.
        /// </summary>
        /// <param name="path">The directory.</param>
        /// <param name="namespaceIndex">Index of the namespace.</param>
        /// <returns>The new NodeId.</returns>
        public static NodeId ConstructIdForDirectory(string path, ushort namespaceIndex)
        {
            var parsedNodeId = new ParsedNodeId
            {
                RootId = path,
                NamespaceIndex = namespaceIndex,
                RootType = 1
            };
            return parsedNodeId.Construct();
        }

        /// <summary>
        /// Constructs a NodeId a file or directory.
        /// </summary>
        /// <param name="path">The file.</param>
        /// <param name="namespaceIndex">Index of the namespace.</param>
        /// <returns>The new NodeId.</returns>
        public static NodeId ConstructIdForFile(string path, ushort namespaceIndex)
        {
            var parsedNodeId = new ParsedNodeId
            {
                RootId = path,
                NamespaceIndex = namespaceIndex,
                RootType = 2
            };
            return parsedNodeId.Construct();
        }

        /// <summary>
        /// Constructs the node identifier for a component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="namespaceIndex">Index of the namespace.</param>
        /// <returns>The node identifier for a component.</returns>
        public static NodeId ConstructIdForComponent(NodeState component, ushort namespaceIndex)
        {
            if (component == null)
            {
                return null;
            }

            // components must be instances with a parent.

            if (component is not BaseInstanceState instance || instance.Parent == null)
            {
                return component.NodeId;
            }

            // parent must have a string identifier.

            if (instance.Parent.NodeId.Identifier is not string parentId)
            {
                return null;
            }

            var buffer = new StringBuilder();
            buffer.Append(parentId);

            // check if the parent is another component.
            var index = parentId.IndexOf('?');

            if (index < 0)
            {
                buffer.Append('?');
            }
            else
            {
                buffer.Append('/');
            }

            buffer.Append(component.SymbolicName);

            // return the node identifier.
            return new NodeId(buffer.ToString(), namespaceIndex);
        }
    }
}
