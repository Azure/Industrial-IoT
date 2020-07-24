// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Models {
    using System;

    public class PublisherInfoRequested {

        public PublisherInfoRequested(PublisherInfo publisher) {
            RequestedMaxWorkers = publisher?.PublisherModel?.Configuration?.MaxWorkers.ToString();

            TimeSpan? heartbeatInterval = publisher?.PublisherModel?.Configuration?.HeartbeatInterval;
            if (heartbeatInterval.HasValue && heartbeatInterval.Value >= TimeSpan.Zero) {
                RequestedHeartbeatInterval = heartbeatInterval.Value.TotalSeconds.ToString();
            }

            TimeSpan? jobCheckInterval = publisher?.PublisherModel?.Configuration?.JobCheckInterval;
            if (jobCheckInterval.HasValue && jobCheckInterval.Value >= TimeSpan.Zero) {
                RequestedJobCheckInterval = jobCheckInterval.Value.TotalSeconds.ToString();
            }
        }

        /// <summary>
        /// MaxWorkers
        /// </summary>
        public string RequestedMaxWorkers { get; set; }
        
        /// <summary>
        /// HeartbeatInterval
        /// </summary>
        public string RequestedHeartbeatInterval { get; set; }

        /// <summary>
        /// JobCheckInterval
        /// </summary>
        public string RequestedJobCheckInterval { get; set; }
    }
}
