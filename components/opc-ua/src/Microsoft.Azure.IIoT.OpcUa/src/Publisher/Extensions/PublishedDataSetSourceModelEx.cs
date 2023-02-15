// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Dataset source extensions
    /// </summary>
    public static class PublishedDataSetSourceModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublishedDataSetSourceModel Clone(this PublishedDataSetSourceModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedDataSetSourceModel {
                Connection = model.Connection.Clone(),
                PublishedEvents = model.PublishedEvents.Clone(),
                PublishedVariables = model.PublishedVariables.Clone(),
                SubscriptionSettings = model.SubscriptionSettings.Clone()
            };
        }
    }
}
