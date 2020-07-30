// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Models {
    using System;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    
    public class ListNodeRequested {

        public ListNodeRequested(PublishedItemApiModel publishedItem) {
            if (publishedItem?.PublishingInterval.HasValue ?? false
                && publishedItem.PublishingInterval.Value != TimeSpan.MinValue) {
                RequestedPublishingInterval = publishedItem.PublishingInterval.Value.TotalMilliseconds.ToString();
            }

            if (publishedItem?.SamplingInterval.HasValue ?? false
                && publishedItem.SamplingInterval.Value != TimeSpan.MinValue) {
                RequestedSamplingInterval = publishedItem.SamplingInterval.Value.TotalMilliseconds.ToString();
            }

            if (publishedItem?.HeartbeatInterval.HasValue ?? false
                && publishedItem.HeartbeatInterval.Value != TimeSpan.MinValue) {
                RequestedHeartbeatInterval = publishedItem.HeartbeatInterval.Value.TotalSeconds.ToString();
            }
        }

        /// <summary>
        /// PublishingInterval
        /// </summary>
        public string RequestedPublishingInterval { get; set; }

        /// <summary>
        /// SamplingInterval
        /// </summary>
        public string RequestedSamplingInterval { get; set; }

        /// <summary>
        /// HeartbeatInterval
        /// </summary>
        public string RequestedHeartbeatInterval { get; set; }
    }
}
