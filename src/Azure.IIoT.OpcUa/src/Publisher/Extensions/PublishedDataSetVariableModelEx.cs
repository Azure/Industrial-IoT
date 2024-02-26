// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Events extensions
    /// </summary>
    public static class PublishedDataSetVariableModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static PublishedDataSetVariableModel? Clone(this PublishedDataSetVariableModel? model)
        {
            return model == null ? null : (model with
            {
                MetaDataProperties = model.MetaDataProperties?.ToList(),
                Triggering = model.Triggering.Clone(),
                SubstituteValue = model.SubstituteValue?.Copy()
            });
        }

        /// <summary>
        /// Returns the unique id of the item
        /// </summary>
        /// <param name="model"></param>
        /// <param name="indexInDataSet"></param>
        /// <returns></returns>
        public static string GetUniqueId(this PublishedDataSetVariableModel model, int indexInDataSet)
        {
            return model.Id
                ?? model.PublishedVariableDisplayName
                ?? model.PublishedVariableNodeId
                ?? indexInDataSet.ToString();
        }
    }
}
