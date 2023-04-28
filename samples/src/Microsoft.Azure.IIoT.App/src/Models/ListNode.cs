// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Models
{
    using global::Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public class ListNode
    {
        public string Id { get; set; }
        public NodeClass NodeClass { get; set; }
        public NodeAccessLevel AccessLevel { get; set; }

        public string NextParentId { get; set; }
        public string ParentName { get; set; }
        public bool Children { get; set; }

        public string NodeName { get; set; }
        public string DiscovererId { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public string Status { get; set; }
        public string Timestamp { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> ParentIdList { get; set; }

        public ListNode()
        {
            ParentIdList = new List<string>();
        }
        public PublishedItemModel PublishedItem { get; set; }

        public bool Publishing { get; set; }

        public bool TryUpdateData(ListNodeRequested input)
        {
            try
            {
                PublishedItem.PublishingInterval = string.IsNullOrWhiteSpace(input.RequestedPublishingInterval) ?
                    TimeSpan.MinValue : TimeSpan.FromMilliseconds(Convert.ToDouble(input.RequestedPublishingInterval, CultureInfo.InvariantCulture));

                PublishedItem.SamplingInterval = string.IsNullOrWhiteSpace(input.RequestedSamplingInterval) ?
                    TimeSpan.MinValue : TimeSpan.FromMilliseconds(Convert.ToDouble(input.RequestedSamplingInterval, CultureInfo.InvariantCulture));

                PublishedItem.HeartbeatInterval = string.IsNullOrWhiteSpace(input.RequestedHeartbeatInterval) ?
                    TimeSpan.MinValue : TimeSpan.FromSeconds(Convert.ToDouble(input.RequestedHeartbeatInterval, CultureInfo.InvariantCulture));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
