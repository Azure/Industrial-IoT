// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;

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
                Priority = model.Priority,
                MetaData = model.MetaData.Clone(),
                ResolveDisplayName = model.ResolveDisplayName
            };
        }

        /// <summary>
        /// Compare items
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this SubscriptionConfigurationModel model,
            SubscriptionConfigurationModel other) {
            if (model == null && other == null) {
                return true;
            }
            if (model == null || other == null) {
                return false;
            }
            if (model.PublishingInterval != other.PublishingInterval) {
                return false;
            }
            if (model.LifetimeCount != other.LifetimeCount) {
                return false;
            }
            if (model.KeepAliveCount != other.KeepAliveCount) {
                return false;
            }
            if (model.Priority != other.Priority) {
                return false;
            }
            if (!model.MetaData.IsSameAs(other.MetaData)) {
                return false;
            }
            if (model.ResolveDisplayName != other.ResolveDisplayName) {
                return false;
            }
            return true;
        }
    }
}