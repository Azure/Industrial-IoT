// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Node method call service request
    /// </summary>
    public class MethodCallRequestModel {

        /// <summary>
        /// Method id of method to call.
        /// </summary>
        public string MethodId { get; set; }

        /// <summary>
        /// Context of the method, i.e. an object or object type
        /// node.  If null then the method is called in the context
        /// of the inverse HasComponent reference of the MethodId
        /// if it exists.
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// Input Arguments
        /// </summary>
        public List<MethodCallArgumentModel> Arguments { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// MethodId or from a resolved objectId to the actual
        /// method node.
        /// </summary>
        public string[] MethodBrowsePath { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// ObjectId to the actual object or objectType node.
        /// If ObjectId is null, the root node (i=84) is used
        /// </summary>
        public string[] ObjectBrowsePath { get; set; }

        /// <summary>
        /// Optional header
        /// </summary>
        public RequestHeaderModel Header { get; set; }
    }
}
