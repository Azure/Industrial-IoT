// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {

    /// <summary>
    /// Result of an twin registration
    /// </summary>
    public class TwinRegistrationResultModel {

        /// <summary>
        /// New id twin was registered under
        /// </summary>
        public string Id { get; set; }
    }
}
