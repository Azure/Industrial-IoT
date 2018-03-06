// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models {
    using System.Collections.Generic;

    /// <summary>
    /// List of device twins with continuation token
    /// </summary>
    public class TwinListModel {

        /// <summary>
        /// Continuation token to use for next call or null
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Items returned
        /// </summary>
        public List<TwinModel> Items { get; set; }
    }
}
