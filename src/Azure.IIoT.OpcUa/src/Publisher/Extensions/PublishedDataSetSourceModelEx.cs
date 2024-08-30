// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Dataset source extensions
    /// </summary>
    public static class PublishedDataSetSourceModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static PublishedDataSetSourceModel? Clone(this PublishedDataSetSourceModel? model)
        {
            return model == null ? null : (model with
            {
                PublishedEvents = model.PublishedEvents.Clone(),
                PublishedVariables = model.PublishedVariables.Clone(),
                Connection = model.Connection.Clone(),
                SubscriptionSettings = model.SubscriptionSettings.Clone()
            });
        }
    }
}
