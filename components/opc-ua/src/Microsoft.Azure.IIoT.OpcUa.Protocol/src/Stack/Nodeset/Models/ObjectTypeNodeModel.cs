// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using System.Runtime.Serialization;

    /// <summary>
    /// The base class for all object type nodes.
    /// </summary>
    [DataContract(Name = "ObjectType")]
    public class ObjectTypeNodeModel : TypeNodeModel {

        /// <summary>
        /// Create object type.
        /// </summary>
        public ObjectTypeNodeModel() :
            base(NodeClass.ObjectType) {
        }
    }
}
