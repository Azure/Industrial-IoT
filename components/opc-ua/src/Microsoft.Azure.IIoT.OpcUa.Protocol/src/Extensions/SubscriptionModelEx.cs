// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
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
                Configuration = model.Configuration.Clone(),
                Id = model.Id,
                MonitoredItems = model.MonitoredItems?
                    .Select(n => n.Clone())
                    .ToList(),
                ExtensionFields = model.ExtensionFields?
                    .ToDictionary(k => k.Key, v => v.Value),
                Connection = model.Connection.Clone()
            };
        }
    }
}