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
            return model == null ? null : (model with
            {
                MetaData = model.MetaData.Clone()
            });
        }
    }
}
