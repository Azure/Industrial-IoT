// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Linq;

    /// <summary>
    /// Subscription model extensions
    /// </summary>
    public static class SubscriptionInfoModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SubscriptionInfoModel Clone(this SubscriptionInfoModel model) {
            if (model == null) {
                return null;
            }
            return new SubscriptionInfoModel {
                MessageMode = model.MessageMode,
                Subscription = model.Subscription.Clone(),
                ExtraFields = model.ExtraFields?
                    .ToDictionary(k => k.Key, v => v.Value),
                Connection = model.Connection.Clone()
            };
        }
    }
}