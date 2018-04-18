// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Call Service result model
    /// </summary>
    public class MethodCallResultModel {

        /// <summary>
        /// Resulting output values of method call
        /// </summary>
        public List<string> Results { get; set; }

        /// <summary>
        /// Diagnostics in case of error
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
