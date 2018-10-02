// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Opc.Ua.Gds.Server.OpcVault
{
    public class OpcVaultClientHelper
    {
        private static readonly int guidLength = Guid.Empty.ToString().Length;

        public static string GetServiceIdFromNodeId(NodeId nodeId, ushort namespaceIndex)
        {
            if (NodeId.IsNull(nodeId))
            {
                throw new ArgumentNullException(nameof(nodeId));
            }

            if (namespaceIndex != nodeId.NamespaceIndex)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }

            if (nodeId.IdType == IdType.Guid)
            {
                Guid? id = nodeId.Identifier as Guid?;
                if (id == null)
                {
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
                }
                return id.ToString();
            }
            else if (nodeId.IdType == IdType.String)
            {
                string id = nodeId.Identifier as string;
                if (id == null)
                {
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
                }
                return id;
            }
            else
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }
        }

        public static NodeId GetNodeIdFromServiceId(string nodeIdentifier, ushort namespaceIndex)
        {
            if (String.IsNullOrEmpty(nodeIdentifier))
            {
                throw new ArgumentNullException(nameof(nodeIdentifier));
            }

            if (nodeIdentifier.Length == guidLength)
            {
                try
                {
                    Guid nodeGuid = new Guid(nodeIdentifier);
                    return new NodeId(nodeGuid, namespaceIndex);
                }
                catch
                {
                    // must be string, continue...
                }
            }
            return new NodeId(nodeIdentifier, namespaceIndex);
        }

    }
}

