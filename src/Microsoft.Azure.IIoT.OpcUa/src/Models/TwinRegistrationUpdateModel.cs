// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Endpoint registration update request
    /// </summary>
    public class TwinRegistrationUpdateModel {

        /// <summary>
        /// Identifier of the twin to update
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Whether to copy existing registration
        /// rather than replacing
        /// </summary>
        public bool? Duplicate { get; set; }

        /// <summary>
        /// Enable (=true) or disable twin
        /// </summary>
        public bool? Activate { get; set; }

        /// <summary>
        /// Authentication to change on the twin.
        /// </summary>
        public AuthenticationModel Authentication { get; set; }
    }
}
