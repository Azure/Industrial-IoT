// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Shared.Models;

    /// <summary>
    /// Subscription model extensions
    /// </summary>
    public static class SubscriptionConfigurationModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SubscriptionConfigurationModel Clone(this SubscriptionConfigurationModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new SubscriptionConfigurationModel
            {
                PublishingInterval = model.PublishingInterval,
                LifetimeCount = model.LifetimeCount,
                KeepAliveCount = model.KeepAliveCount,
                Priority = model.Priority,
                MetaData = model.MetaData.Clone(),
                ResolveDisplayName = model.ResolveDisplayName
            };
        }
    }
}
