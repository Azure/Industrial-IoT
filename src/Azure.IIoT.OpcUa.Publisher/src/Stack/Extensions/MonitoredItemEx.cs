// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Linq;

    /// <summary>
    /// Base monitored item extensions
    /// </summary>
    internal static class MonitoredItemEx
    {
        /// <summary>
        /// Set defaults from configuration
        /// </summary>
        /// <param name="item"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static BaseMonitoredItemModel SetDefaults(this BaseMonitoredItemModel item,
            OpcUaSubscriptionOptions options)
        {
            switch (item)
            {
                case MonitoredAddressSpaceModel mas:
                    return mas with
                    {
                        RebrowsePeriod = mas.RebrowsePeriod
                            ?? options.DefaultRebrowsePeriod
                            ?? TimeSpan.FromHours(12),
                        //
                        // see https://reference.opcfoundation.org/v104/Core/docs/Part4/7.16/
                        // 0 the Server returns the default queue size for Event Notifications
                        // as revisedQueueSize for event monitored items.
                        //
                        QueueSize = item.QueueSize
                            ?? options.DefaultQueueSize
                            ?? 0,
                        DiscardNew = item.DiscardNew
                            ?? options.DefaultDiscardNew,
                        MonitoringMode = item.MonitoringMode
                            ?? MonitoringMode.Reporting,
                        FetchDataSetFieldName = item.FetchDataSetFieldName
                            ?? options.ResolveDisplayName,
                        AutoSetQueueSize = item.AutoSetQueueSize
                            ?? options.AutoSetQueueSizes,
                        TriggeredItems = item.TriggeredItems?
                            .Select(ti => ti.SetDefaults(options))
                            .ToList(),
                    };
                case DataMonitoredItemModel dmi:
                    return dmi with
                    {
                        SamplingInterval = dmi.SamplingInterval
                            ?? options.DefaultSamplingInterval,
                        HeartbeatBehavior = dmi.HeartbeatBehavior
                            ?? options.DefaultHeartbeatBehavior,
                        HeartbeatInterval = dmi.HeartbeatInterval
                            ?? options.DefaultHeartbeatInterval,
                        SkipFirst = dmi.SkipFirst
                            ?? options.DefaultSkipFirst,

                        //
                        // see https://reference.opcfoundation.org/v104/Core/docs/Part4/7.16/
                        // 0 or 1 the Server returns the default queue size which shall be 1
                        // as revisedQueueSize for data monitored items. The queue has a single
                        // entry, effectively disabling queuing. This is the default behavior
                        // since beginning of publisher time.
                        //
                        QueueSize = item.QueueSize
                            ?? options.DefaultQueueSize
                            ?? 1,
                        DiscardNew = item.DiscardNew
                            ?? options.DefaultDiscardNew,
                        FetchDataSetFieldName = item.FetchDataSetFieldName
                            ?? options.ResolveDisplayName,
                        AutoSetQueueSize = item.AutoSetQueueSize
                            ?? options.AutoSetQueueSizes,
                        TriggeredItems = item.TriggeredItems?
                            .Select(ti => ti.SetDefaults(options))
                            .ToList(),

                        SamplingUsingCyclicRead = dmi.SamplingUsingCyclicRead
                            ?? options.DefaultSamplingUsingCyclicRead,
                        CyclicReadMaxAge = dmi.CyclicReadMaxAge
                            ?? options.DefaultCyclicReadMaxAge,

                        DataChangeFilter = dmi.DataChangeFilter.SetDefaults(options)
                    };
                case EventMonitoredItemModel emi:
                    return emi with
                    {
                        //
                        // see https://reference.opcfoundation.org/v104/Core/docs/Part4/7.16/
                        // 0 the Server returns the default queue size for Event Notifications
                        // as revisedQueueSize for event monitored items.
                        //
                        QueueSize = item.QueueSize
                            ?? options.DefaultQueueSize
                            ?? 0,
                        DiscardNew = item.DiscardNew
                            ?? options.DefaultDiscardNew,
                        FetchDataSetFieldName = item.FetchDataSetFieldName
                            ?? options.ResolveDisplayName,
                        AutoSetQueueSize = item.AutoSetQueueSize
                            ?? options.AutoSetQueueSizes,
                        TriggeredItems = item.TriggeredItems?
                            .Select(ti => ti.SetDefaults(options))
                            .ToList(),
                    };
                default:
                    return item;
            }
        }

        /// <summary>
        /// Set default data change filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static DataChangeFilterModel? SetDefaults(
            this DataChangeFilterModel? filter, OpcUaSubscriptionOptions options)
        {
            if (filter == null &&
                options.DefaultDataChangeTrigger == null)
            {
                return null;
            }
            filter ??= new DataChangeFilterModel();
            return filter with
            {
                DataChangeTrigger = filter.DataChangeTrigger
                    ?? options.DefaultDataChangeTrigger
                    ?? DataChangeTriggerType.StatusValue,
            };
        }
    }
}
