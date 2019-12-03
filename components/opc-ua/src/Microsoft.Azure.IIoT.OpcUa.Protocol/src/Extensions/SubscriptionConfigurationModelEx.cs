// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {

    /// <summary>
    /// Subscription model extensions
    /// </summary>
    public static class SubscriptionConfigurationModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SubscriptionConfigurationModel Clone(this SubscriptionConfigurationModel model) {
            if (model == null) {
                return null;
            }
            return new SubscriptionConfigurationModel {
                PublishingInterval = model.PublishingInterval,
                LifetimeCount = model.LifetimeCount,
                KeepAliveCount = model.KeepAliveCount,
                MaxNotificationsPerPublish = model.MaxNotificationsPerPublish,
                Priority = model.Priority
            };
        }
    }
}