// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Twin registration list
    /// </summary>
    public class TwinRegistrationListModel {

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Endpoint information of the server to register
        /// </summary>
        public List<TwinRegistrationModel> Items { get; set; }
    }
}
