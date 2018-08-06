// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// For manual discovery requests
    /// </summary>
    public class DiscoveryResultModel {

        /// <summary>
        /// Discovered endpoint
        /// </summary>
        public List<ApplicationRegistrationModel> Found { get; set; }

        /// <summary>
        /// Timestamp of the discovery sweep
        /// </summary>
        public DateTime TimeStamp { get; set; }
    }
}
