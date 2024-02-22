// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// trigger models extensions
    /// </summary>
    public static class PublishedDataSetTriggerModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static PublishedDataSetTriggerModel? Clone(this PublishedDataSetTriggerModel? model)
        {
            return model == null ? null : (model with
            {
                PublishedVariables = model.PublishedVariables.Clone(),
                PublishedEvents = model.PublishedEvents.Clone()
            });
        }
    }
}
