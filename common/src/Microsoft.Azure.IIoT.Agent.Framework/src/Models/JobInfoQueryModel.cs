// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    /// <summary>
    /// Job info query model
    /// </summary>
    public class JobInfoQueryModel {

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Configuration type
        /// </summary>
        public string JobConfigurationType { get; set; }

        /// <summary>
        /// Job status
        /// </summary>
        public JobStatus? Status { get; set; }
    }
}