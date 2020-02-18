// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Contains the result of the heartbeat request
    /// </summary>
    public class HeartbeatResultModel : List<HeartbeatResultEntryModel> {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public HeartbeatResultModel() {

        }

        /// <summary>
        /// Default constructor with collection initializer.
        /// </summary>
        /// <param name="entries"></param>
        public HeartbeatResultModel(IEnumerable<HeartbeatResultEntryModel> entries) : base(entries) { }
    }
}