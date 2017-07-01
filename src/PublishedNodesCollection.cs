
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Opc.Ua.Publisher
{
    [DataContract]
    public partial class NodeLookup
    {
        public NodeLookup()
        {
        }

        [DataMember]
        public Uri EndPointURL;

        [DataMember]
        public NodeId NodeID;
    }

    [CollectionDataContract]
    public partial class PublishedNodesCollection : List<NodeLookup>
    {
        public PublishedNodesCollection()
        {
        }
    }
}
