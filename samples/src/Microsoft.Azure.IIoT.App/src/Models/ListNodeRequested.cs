// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Models {
    using System;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;

    public class ListNodeRequested {

        public ListNodeRequested(PublishedItemApiModel publishedItem) {
            _requestedPublishingInterval = publishedItem?.PublishingInterval;
            _requestedSamplingInterval = publishedItem?.SamplingInterval;
            _requestedHeartbeatInterval = publishedItem?.HeartbeatInterval;
        }

        private TimeSpan? _requestedPublishingInterval;

        private TimeSpan? _requestedSamplingInterval;

        private TimeSpan? _requestedHeartbeatInterval;

        /// <summary>
        /// PublishingInterval
        /// </summary>
        public string RequestedPublishingInterval {
            get => (_requestedPublishingInterval != null && _requestedPublishingInterval.Value != TimeSpan.MinValue ?
                _requestedPublishingInterval.Value.TotalMilliseconds.ToString() : null);
            set => _requestedPublishingInterval = string.IsNullOrWhiteSpace(value) ?
                TimeSpan.MinValue : TimeSpan.FromMilliseconds(Convert.ToDouble(value));
        }

        /// <summary>
        /// SamplingInterval
        /// </summary>
        public string RequestedSamplingInterval {
            get => (_requestedSamplingInterval != null && _requestedSamplingInterval.Value != TimeSpan.MinValue ?
                _requestedSamplingInterval.Value.TotalMilliseconds.ToString() : null);
            set => _requestedSamplingInterval = string.IsNullOrWhiteSpace(value) ?
                TimeSpan.MinValue : TimeSpan.FromMilliseconds(Convert.ToDouble(value));
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
    }
}
