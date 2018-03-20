// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {

    /// <summary>
    /// Application registration request
    /// </summary>
    public class ApplicationRegistrationRequestModel {

        /// <summary>
        /// Discovery url to use for registration
        /// </summary>
        public string DiscoveryUrl { get; set; }
    }
}
