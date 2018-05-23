// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Node method call service request
    /// </summary>
    public class MethodCallRequestModel {

        /// <summary>
        /// Object scope
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// Method to call
        /// </summary>
        public string MethodId { get; set; }

        /// <summary>
        /// Arguments
        /// </summary>
        public List<MethodArgumentModel> InputArguments { get; set; }
    }
}
