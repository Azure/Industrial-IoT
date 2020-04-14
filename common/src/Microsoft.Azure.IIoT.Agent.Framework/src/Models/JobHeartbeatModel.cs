// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Job heartbeat
    /// </summary>
    public class JobHeartbeatModel {

        /// <summary>
        /// Job id
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Hash
        /// </summary>
        public string JobHash { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public JobStatus Status { get; set; }

        /// <summary>
        /// Process mode
        /// </summary>
        public ProcessMode ProcessMode { get; set; }

        /// <summary>
        /// Job state
        /// </summary>
        public VariantValue State { get; set; }
    }
}