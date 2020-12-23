// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Get job processing instructions from orchestrator
    /// </summary>
    public class JobRequestModel {

        /// <summary>
        /// Capabilities to match
        /// </summary>
        public Dictionary<string, string> Capabilities { get; set; }
    }
}