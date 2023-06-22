// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using System.Diagnostics.CodeAnalysis;
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
        [return: NotNullIfNotNull(nameof(model))]
        public static SubscriptionModel? Clone(this SubscriptionModel? model)
        {
            return model == null ? null : (model with
            {
                Configuration = model.Configuration.Clone(),
                Id = model.Id.Clone(),
                MonitoredItems = model.MonitoredItems?
                    .ToList(),
                ExtensionFields = model.ExtensionFields?
                    .ToDictionary(k => k.Key, v => v.Value)
            });
        }

        /// <summary>
        /// Clone id
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static SubscriptionIdentifier? Clone(this SubscriptionIdentifier? model)
        {
            if (model is null)
            {
                return null;
            }
            return new SubscriptionIdentifier(model.Connection, model.Id);
        }
    }
}
