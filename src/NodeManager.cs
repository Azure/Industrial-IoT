
using Opc.Ua;
using Opc.Ua.Sample;
using System.Collections.Generic;
using System.Reflection;

namespace Publisher
{
    public class PublisherNodeManager : SampleNodeManager
    {
        private ushort m_namespaceIndex;
        private ushort m_typeNamespaceIndex;
        private long m_lastUsedId;

        public PublisherNodeManager(Opc.Ua.Server.IServerInternal server, ApplicationConfiguration configuration)
        : base(server)
        {
            List<string> namespaceUris = new List<string>();
            namespaceUris.Add(Namespaces.Publisher);
            namespaceUris.Add(Namespaces.Publisher + "/Instance");
            NamespaceUris = namespaceUris;

            m_typeNamespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[0]);
            m_namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[1]);

            m_lastUsedId = 0;
        }

        /// <summary>
        /// Creates a new node
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            uint id = Utils.IncrementIdentifier(ref m_lastUsedId);
            return new NodeId(id, m_namespaceIndex);
        }

        /// <summary>
        /// Loads predefined nodes file
        /// </summary>
        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();
            predefinedNodes.LoadFromBinaryResource(context, "Opc.Ua.Publisher.Publisher.PredefinedNodes.uanodes", this.GetType().GetTypeInfo().Assembly, true);
            return predefinedNodes;
        }

        /// <summary>
        /// Hooks our predefined nodes into the overal node graph of the server and
        /// initializes our state class
        /// </summary>
        protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            BaseObjectState passiveNode = predefinedNode as BaseObjectState;

            if (passiveNode == null)
            {
                return predefinedNode;
            }

            NodeId typeId = passiveNode.TypeDefinitionId;

            if (!IsNodeIdInNamespace(typeId) || typeId.IdType != IdType.Numeric)
            {
                return predefinedNode;
            }

            switch ((uint)typeId.Identifier)
            {
                case ObjectTypes.PublisherType:
                {
                    if (passiveNode is PublisherState)
                    {
                        break;
                    }

                    PublisherState activeNode = new PublisherState(passiveNode.Parent);
                    activeNode.Create(context, passiveNode);

                    // replace the node in the parent.
                    if (passiveNode.Parent != null)
                    {
                        passiveNode.Parent.ReplaceChild(context, activeNode);
                    }

                    return activeNode;
                }
            }

            return predefinedNode;
        }
    }
}
