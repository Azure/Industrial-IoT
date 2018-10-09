// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Result of publish request
    /// </summary>
    public class PublishResultModel {

        /// <summary>
        /// Diagnostics data in case of error
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
