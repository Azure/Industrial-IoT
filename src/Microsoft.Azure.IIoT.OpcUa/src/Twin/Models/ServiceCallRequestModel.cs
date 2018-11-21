// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Service call request
    /// </summary>
    public class ServiceCallRequestModel {

        /// <summary>
        /// Service call request type
        /// </summary>
        public ServiceCallType Type { get; set; }

        /// <summary>
        /// Request model
        /// </summary>
        public JToken Request { get; set; }
    }
}
