// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Linq;

    /// <summary>
    /// Subscription model extensions
    /// </summary>
    public static class SubscriptionModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SubscriptionModel Clone(this SubscriptionModel model) {
            if (model == null) {
                return null;
            }
            return new SubscriptionModel {
                Id = model.Id,
                PublishingInterval = model.PublishingInterval,
                LifeTimeCount = model.LifeTimeCount,
                MaxKeepAliveCount = model.MaxKeepAliveCount,
                MaxNotificationsPerPublish = model.MaxNotificationsPerPublish,
                Priority = model.Priority,
                PublishingDisabled = model.PublishingDisabled,
                MonitoredItems = model.MonitoredItems?
                    .Select(n => n.Clone())
                    .ToList()
            };
        }
    }
}