// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Compute {

    /// <summary>
    /// Image in marketplace
    /// </summary>
    public class VirtualMachineImage {

        /// <summary>
        /// Publisher
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// Offer
        /// </summary>
        public string Offer { get; set; }

        /// <summary>
        /// Sku
        /// </summary>
        public string Sku { get; set; }

        /// <summary>
        /// Sku
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Whether the offer is linux or windows.
        /// </summary>
        public bool IsLinux { get; set; }
    }
}
