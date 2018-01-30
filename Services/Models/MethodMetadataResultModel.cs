// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models {
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Result of method metadata query
    /// </summary>
    public class MethodMetadataResultModel {

        /// <summary>
        /// Input arguments
        /// </summary>
        public List<MethodArgumentModel> InputArguments { get; set; }

        /// <summary>
        /// Output arguments
        /// </summary>
        public List<MethodArgumentModel> OutputArguments { get; set; }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
