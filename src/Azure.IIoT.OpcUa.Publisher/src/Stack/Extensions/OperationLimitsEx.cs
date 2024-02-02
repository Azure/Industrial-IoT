// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using Opc.Ua;

    /// <summary>
    /// Operation limits extensions
    /// </summary>
    public static class OperationLimitsEx
    {
        /// <summary>
        /// Update limits
        /// </summary>
        /// <param name="limits"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        public static OperationLimits Override(this OperationLimits limits, OperationLimits? update)
        {
            if (update == null)
            {
                return limits;
            }
            return new OperationLimits
            {
                MaxMonitoredItemsPerCall
                    = Override(limits.MaxMonitoredItemsPerCall, update.MaxMonitoredItemsPerCall),
                MaxNodesPerBrowse
                    = Override(limits.MaxNodesPerBrowse, update.MaxNodesPerBrowse),
                MaxNodesPerHistoryReadData
                    = Override(limits.MaxNodesPerHistoryReadData, update.MaxNodesPerHistoryReadData),
                MaxNodesPerHistoryReadEvents
                    = Override(limits.MaxNodesPerHistoryReadEvents, update.MaxNodesPerHistoryReadEvents),
                MaxNodesPerHistoryUpdateData
                    = Override(limits.MaxNodesPerHistoryUpdateData, update.MaxNodesPerHistoryUpdateData),
                MaxNodesPerHistoryUpdateEvents
                    = Override(limits.MaxNodesPerHistoryUpdateEvents, update.MaxNodesPerHistoryUpdateEvents),
                MaxNodesPerMethodCall
                    = Override(limits.MaxNodesPerMethodCall, update.MaxNodesPerMethodCall),
                MaxNodesPerNodeManagement
                    = Override(limits.MaxNodesPerNodeManagement, update.MaxNodesPerNodeManagement),
                MaxNodesPerRead
                    = Override(limits.MaxNodesPerRead, update.MaxNodesPerRead),
                MaxNodesPerRegisterNodes
                    = Override(limits.MaxNodesPerRegisterNodes, update.MaxNodesPerRegisterNodes),
                MaxNodesPerTranslateBrowsePathsToNodeIds
                    = Override(limits.MaxNodesPerTranslateBrowsePathsToNodeIds, update.MaxNodesPerTranslateBrowsePathsToNodeIds),
                MaxNodesPerWrite
                    = Override(limits.MaxNodesPerWrite, update.MaxNodesPerWrite)
            };

            static uint Override(uint a, uint b) => b == 0u ? a : b < a ? b : a;
        }
    }
}
