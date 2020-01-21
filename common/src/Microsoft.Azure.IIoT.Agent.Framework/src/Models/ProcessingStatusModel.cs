// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// processing status
    /// </summary>
    public class ProcessingStatusModel {

        /// <summary>
        /// Last known heartbeat
        /// </summary>
        public DateTime? LastKnownHeartbeat { get; set; }

        /// <summary>
        /// Last known state
        /// </summary>
        public JToken LastKnownState { get; set; }

        /// <summary>
        /// Processing mode
        /// </summary>
        public ProcessMode? ProcessMode { get; set; }
    }
}