// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Server onboarding request
    /// </summary>
    public class ServerRegistrationRequestModel {

        /// <summary>
        /// Discovery url to use for registration
        /// </summary>
        public string DiscoveryUrl { get; set; }

        /// <summary>
        /// Only retrieve information for the provided locales
        /// </summary>
        public List<string> Locales { get; set; }

        /// <summary>
        /// User defined registration id
        /// </summary>
        public string RegistrationId { get; set; }

        /// <summary>
        /// Callback to invoke once registration finishes
        /// </summary>
        public CallbackModel Callback { get; set; }

        /// <summary>
        /// Upon discovery, activate all twins with this filter.
        /// </summary>
        public TwinActivationFilterModel ActivationFilter { get; set; }
    }
}
