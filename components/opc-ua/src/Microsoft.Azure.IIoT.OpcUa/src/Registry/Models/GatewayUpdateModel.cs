// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Edge ateway registration update request
    /// </summary>
    public class GatewayUpdateModel {

        /// <summary>
        /// Site of the Edge Gateway
        /// </summary>
        public string SiteId { get; set; }
    }
}
