// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Models {
    using System;

    /// <summary>
    /// Endpoint with application info when discovered
    /// </summary>
    public class DiscoveryEventModel {

        /// <summary>
        /// Discovered endpoint
        /// </summary>
        public TwinRegistrationModel Endpoint { get; set; }

        /// <summary>
        /// Application to which this endpoint belongs
        /// </summary>
        public ApplicationInfoModel Application { get; set; }

        /// <summary>
        /// Timestamp of the discovery sweep
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Index in batch with timestamp
        /// </summary>
        public int Index { get; set; }
    }
}
