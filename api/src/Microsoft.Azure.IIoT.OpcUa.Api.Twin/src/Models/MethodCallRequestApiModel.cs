// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Call request model
    /// </summary>
    /// <remarks>
    /// A method call request can specify the targets in several ways:
    /// <ul>
    /// <li>Specify methodId and objectId node ids and leave the browse
    /// paths null.</li>
    /// <li>Specify an objectBrowsePath to a real object node from
    /// the node specified with objectId.  If objectId == null, the
    /// root node (i=84) is used. </li>
    /// <li>Specify a methodBrowsePath from the above object node
    /// to the actual method node to call on the object.  methodId
    /// remains null.</li>
    /// <li>Like previously, but specify methodId and method browse
    /// path from it to a real method node.</li>
    /// </ul>
    /// </remarks>
    [DataContract]
    public class MethodCallRequestApiModel {

        /// <summary>
        /// Method id of method to call.
        /// </summary>
        [DataMember(Name = "methodId", Order = 0,
            EmitDefaultValue = false)]
        public string MethodId { get; set; }

        /// <summary>
        /// Context of the method, i.e. an object or object type
        /// node.
        /// </summary>
        [DataMember(Name = "objectId", Order = 1,
            EmitDefaultValue = false)]
        public string ObjectId { get; set; }

        /// <summary>
        /// Arguments for the method - null means no args
        /// </summary>
        [DataMember(Name = "arguments", Order = 2,
            EmitDefaultValue = false)]
        public List<MethodCallArgumentApiModel> Arguments { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// MethodId or from a resolved objectId to the actual
        /// method node.
        /// </summary>
        [DataMember(Name = "methodBrowsePath", Order = 3,
            EmitDefaultValue = false)]
        public string[] MethodBrowsePath { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// ObjectId to the actual object or objectType node.
        /// If ObjectId == null, the root node (i=84) is used.
        /// </summary>
        [DataMember(Name = "objectBrowsePath", Order = 4,
            EmitDefaultValue = false)]
        public string[] ObjectBrowsePath { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 5,
            EmitDefaultValue = false)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
