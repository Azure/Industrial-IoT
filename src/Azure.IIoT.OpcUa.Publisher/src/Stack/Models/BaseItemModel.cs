// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    /// <summary>
    /// Base item model
    /// </summary>
    public abstract record BaseItemModel
    {
        /// <summary>
        /// Unique identifier of the item.
        /// </summary>
        public required string? Id { get; init; }

        /// <summary>
        /// Specifies the order of item. This is not the order
        /// in the subscription, consider the subscription reporting
        /// unordered list of notifications, but the monitored item
        /// notification will contain this field which then allows
        /// a consumer to order so to match this to metadata.
        /// </summary>
        public required int Order { get; init; }

        /// <summary>
        /// Name of the item that should be reported inside the
        /// monitored item notifications emitted.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Opaque context which will be added to all item monitored
        /// notifications.
        /// </summary>
        public object? Context { get; init; }
    }

    /// <summary>
    /// Extensions
    /// </summary>
    public static class BaseItemModelEx
    {
        /// <summary>
        /// Identifier for this monitored item
        /// Prio 1: Id = DataSetFieldId - if already configured
        /// Prio 2: Id = DataSetFieldName - if already configured
        /// Prio 3: NodeId as configured
        /// </summary>
        /// <param name="model"></param>
        public static string GetMonitoredItemId(this BaseMonitoredItemModel model)
        {
            return
                !string.IsNullOrEmpty(model.Id) ? model.Id :
                !string.IsNullOrEmpty(model.Name) ? model.Name :
                model.StartNodeId;
        }

        /// <summary>
        /// Name of the field in the monitored item notification
        /// Prio 1: DisplayName = DataSetFieldName - if already configured
        /// Prio 2: DisplayName = DataSetFieldId  - if already configured
        /// Prio 3: NodeId as configured
        /// </summary>
        /// <param name="model"></param>
        public static string GetFieldId(this BaseMonitoredItemModel model)
        {
            return
                !string.IsNullOrEmpty(model.Name) ? model.Name :
                !string.IsNullOrEmpty(model.Id) ? model.Id :
                model.StartNodeId;
        }

        /// <summary>
        /// Identifier for this monitored item
        /// Prio 1: Id = DataSetFieldId - if already configured
        /// Prio 2: Id = DataSetFieldName - if already configured
        /// </summary>
        /// <param name="model"></param>
        public static string GetMonitoredItemId(this BaseItemModel model)
        {
            return
                !string.IsNullOrEmpty(model.Id) ? model.Id :
                !string.IsNullOrEmpty(model.Name) ? model.Name :
                string.Empty;
        }

        /// <summary>
        /// Name of the field in the monitored item notification
        /// Prio 1: DisplayName = DataSetFieldName - if already configured
        /// Prio 2: DisplayName = DataSetFieldId  - if already configured
        /// </summary>
        /// <param name="model"></param>
        public static string GetFieldId(this BaseItemModel model)
        {
            return
                !string.IsNullOrEmpty(model.Name) ? model.Name :
                !string.IsNullOrEmpty(model.Id) ? model.Id :
                string.Empty;
        }
    }
}
