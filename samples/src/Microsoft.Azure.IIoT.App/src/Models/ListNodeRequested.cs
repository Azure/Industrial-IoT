// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;

namespace Microsoft.Azure.IIoT.App.Models {

    public class ListNodeRequested {

        public ListNodeRequested(PublishedItemApiModel publishedItem) {
            if (publishedItem?.PublishingInterval != null) {
                RequestedPublishingInterval = publishedItem.PublishingInterval.Value.TotalMilliseconds.ToString();
            }

            if (publishedItem?.SamplingInterval != null)
            {
                RequestedSamplingInterval = publishedItem.SamplingInterval.Value.TotalMilliseconds.ToString();
            }

            if (publishedItem?.HeartbeatInterval != null)
            {
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
