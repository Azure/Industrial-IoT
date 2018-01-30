// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models {

    /// <summary>
    /// Result of an endpoint registration
    /// </summary>
    public class ServerRegistrationResultModel {

        /// <summary>
        /// New id endpoint was registered under
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The connection string of the new identity.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}
