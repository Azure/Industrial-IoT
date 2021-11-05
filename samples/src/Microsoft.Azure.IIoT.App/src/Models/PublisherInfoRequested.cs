// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Models {
    using System;

    public class PublisherInfoRequested {

        public PublisherInfoRequested(PublisherInfo publisher) {
            _requestedMaxWorkers = publisher?.PublisherModel?.Configuration?.MaxWorkers;
            _requestedHeartbeatInterval = publisher?.PublisherModel?.Configuration?.HeartbeatInterval;
            _requestedJobCheckInterval = publisher?.PublisherModel?.Configuration?.JobCheckInterval;
        }

        private int? _requestedMaxWorkers;
        private TimeSpan? _requestedHeartbeatInterval;
        private TimeSpan? _requestedJobCheckInterval;

        /// <summary>
        /// MaxWorkers
        /// </summary>
        public string RequestedMaxWorkers {
            get => (_requestedMaxWorkers != null ?
                _requestedMaxWorkers.Value.ToString() : null);
            set => _requestedMaxWorkers = string.IsNullOrWhiteSpace(value) ?
                (int?)null : int.Parse(value);
        }

        /// <summary>
        /// HeartbeatInterval
        /// </summary>
        public string RequestedHeartbeatInterval {
            get => (_requestedHeartbeatInterval != null && _requestedHeartbeatInterval.Value != TimeSpan.MinValue ?
                _requestedHeartbeatInterval.Value.TotalSeconds.ToString() : null);
            set => _requestedHeartbeatInterval = string.IsNullOrWhiteSpace(value) ?
                TimeSpan.MinValue : TimeSpan.FromSeconds(Convert.ToDouble(value));
        }

        /// <summary>
        /// JobCheckInterval
        /// </summary>
        public string RequestedJobCheckInterval {
            get => (_requestedJobCheckInterval != null && _requestedJobCheckInterval.Value != TimeSpan.MinValue ?
                _requestedJobCheckInterval.Value.TotalSeconds.ToString() : null);
            set => _requestedJobCheckInterval = string.IsNullOrWhiteSpace(value) ?
                TimeSpan.MinValue : TimeSpan.FromSeconds(Convert.ToDouble(value));
        }
    }
}
