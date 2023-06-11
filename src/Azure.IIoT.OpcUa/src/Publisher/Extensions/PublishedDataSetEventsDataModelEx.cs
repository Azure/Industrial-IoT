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
            if (model == null)
            {
                return null;
            }
            return new PublishedDataSetEventModel
            {
                Id = model.Id,
                MonitoringMode = model.MonitoringMode,
                DiscardNew = model.DiscardNew,
                EventNotifier = model.EventNotifier,
                PublishedEventName = model.PublishedEventName,
                ReadEventNameFromNode = model.ReadEventNameFromNode,
                WhereClause = model.WhereClause.Clone(),
                QueueSize = model.QueueSize,
                BrowsePath = model.BrowsePath,
                SelectClauses = model.SelectClauses?
                    .Select(f => f.Clone())
                    .ToList(),
                ConditionHandling = model.ConditionHandling.Clone(),
                TypeDefinitionId = model.TypeDefinitionId
            };
        }
    }
}
