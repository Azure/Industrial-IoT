// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Events extensions
    /// </summary>
    public static class PublishedDataSetEventsDataModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static PublishedDataSetEventModel? Clone(this PublishedDataSetEventModel? model)
        {
            return model == null ? null : (model with
            {
                Filter = model.Filter.Clone(),
                SelectedFields = model.SelectedFields?
                    .Select(f => f.Clone())
                    .ToList(),
                ConditionHandling = model.ConditionHandling.Clone()
            });
        }
    }
}
