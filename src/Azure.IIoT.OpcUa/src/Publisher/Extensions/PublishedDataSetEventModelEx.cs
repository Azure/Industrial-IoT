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
    public static class PublishedDataSetEventModelEx
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
                Triggering = model.Triggering.Clone(),
                SelectedFields = model.SelectedFields?
                    .Select(f => f.Clone())
                    .ToList(),
                ConditionHandling = model.ConditionHandling.Clone()
            });
        }

        /// <summary>
        /// Returns the unique id of the item
        /// </summary>
        /// <param name="model"></param>
        /// <param name="indexInDataSet"></param>
        /// <returns></returns>
        public static string GetUniqueId(this PublishedDataSetEventModel model, int indexInDataSet)
        {
            return model.Id
                ?? model.PublishedEventName
                ?? model.TypeDefinitionId ?? model.EventNotifier
                ?? indexInDataSet.ToString();
        }
    }
}
