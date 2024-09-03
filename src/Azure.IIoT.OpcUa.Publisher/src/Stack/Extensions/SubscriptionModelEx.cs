// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using System;

    /// <summary>
    /// Subscription model extensions
    /// </summary>
    internal static class SubscriptionModelEx
    {
        /// <summary>
        /// Returns a string that uniquely identifies the subscription based on
        /// the configuration
        /// </summary>
        /// <param name="model"></param>
        public static string CreateSubscriptionId(this SubscriptionModel model)
        {
            return $"{model.ToString().ToSha1Hash()}[P{model.Priority ?? 0}" +
               $"@{(int)(model.PublishingInterval?.TotalMilliseconds ?? 0)}]";
        }
    }
}
