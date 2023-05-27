// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Diagnostics.CodeAnalysis;

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
        [return: NotNullIfNotNull(nameof(model))]
        public static SubscriptionConfigurationModel? Clone(this SubscriptionConfigurationModel? model)
        {
            if (model == null)
            {
                return null;
            }
            return new SubscriptionConfigurationModel
            {
                PublishingInterval = model.PublishingInterval,
                UseDeferredAcknoledgements = model.UseDeferredAcknoledgements,
                LifetimeCount = model.LifetimeCount,
                KeepAliveCount = model.KeepAliveCount,
                Priority = model.Priority,
                MetaData = model.MetaData.Clone(),
                ResolveDisplayName = model.ResolveDisplayName
            };
        }
    }
}
