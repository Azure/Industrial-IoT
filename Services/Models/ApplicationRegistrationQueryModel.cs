// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Application information
    /// </summary>
    public class ApplicationRegistrationQueryModel {

        /// <summary>
        /// Type of application
        /// </summary>
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        public string ProductUri { get; set; }

        /// <summary>
        /// Name of application
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Application capabilities
        /// </summary>
        public List<string> Capabilities { get; set; }
    }
}

