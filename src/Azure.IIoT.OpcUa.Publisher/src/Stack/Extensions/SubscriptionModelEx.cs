// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using System.Linq;

    /// <summary>
    /// Subscription model extensions
    /// </summary>
    public static class SubscriptionModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SubscriptionModel Clone(this SubscriptionModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new SubscriptionModel
            {
                Configuration = model.Configuration.Clone(),
                Id = model.Id.Clone(),
                MonitoredItems = model.MonitoredItems?
                    .Select(n => n.Clone())
                    .ToList(),
                ExtensionFields = model.ExtensionFields?
                    .ToDictionary(k => k.Key, v => v.Value)
            };
        }

        /// <summary>
        /// Clone id
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SubscriptionIdentifier Clone(this SubscriptionIdentifier model)
        {
            if (model == null)
            {
                return null;
            }
            return new SubscriptionIdentifier(model.Connection, model.Id);
        }
    }
}
