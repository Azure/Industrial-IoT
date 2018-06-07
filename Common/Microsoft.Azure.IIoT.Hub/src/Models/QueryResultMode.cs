// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// List of device twins with continuation token
    /// </summary>
    public class QueryResultModel {

        /// <summary>
        /// Continuation token to use for next call or null
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Result returned
        /// </summary>
        public JArray Result { get; set; }
    }
}
