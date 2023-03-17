// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;

    /// <summary>
    /// Event monitored item
    /// </summary>
    public sealed record class EventMonitoredItemModel : BaseMonitoredItemModel
    {
        /// <summary>
        /// Event filter
        /// </summary>
        public EventFilterModel EventFilter { get; init; }

        /// <summary>
        /// Condition handling settings
        /// </summary>
        public ConditionHandlingOptionsModel ConditionHandling { get; init; }
    }
}
