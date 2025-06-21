// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;

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

        /// <summary>
        /// Max nodes per browse
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxNodesPerBrowse(this OperationLimitsModel model)
        {
            var cur = model.MaxNodesPerBrowse ?? 0;
            return Math.Min(Math.Max((int)cur, 1), kMaxBrowseNodes);
        }

        /// <summary>
        /// Max continuation points per browse
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxBrowseContinuationPoints(this OperationLimitsModel model)
        {
            var cur = model.MaxBrowseContinuationPoints ?? 0;
            return Math.Min(Math.Max((int)cur, 1), kMaxBrowseContinuationPoints);
        }

        /// <summary>
        /// Max nodes per read
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxNodesPerRead(this OperationLimitsModel model)
        {
            var cur = model.MaxNodesPerRead ?? 0;
            return Math.Min(Math.Max((int)cur, 1), kMaxReadNodes);
        }

        /// <summary>
        /// Max nodes per translate
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxNodesPerTranslatePathsToNodeIds(this OperationLimitsModel model)
        {
            var cur = model.MaxNodesPerTranslatePathsToNodeIds ?? 0;
            return Math.Min(Math.Max((int)cur, 1), kMaxNodesPerTranslate);
        }

        /// <summary>
        /// Max nodes per register
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxNodesPerRegisterNodes(this OperationLimitsModel model)
        {
            var cur = model.MaxNodesPerRegisterNodes ?? 0;
            return Math.Min(Math.Max((int)cur, 1), kMaxNodesPerRegister);
        }

        /// <summary>
        /// Max monitored items
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxMonitoredItemsPerCall(this OperationLimitsModel model)
        {
            var cur = model.MaxMonitoredItemsPerCall ?? 0;
            return Math.Min(Math.Max((int)cur, 1), kMaxMonitoredItemsPerCall);
        }

        private const int kMaxReadNodes = 10000;
        private const int kMaxNodesPerTranslate = 1000;
        private const int kMaxBrowseNodes = 10000;
        private const int kMaxNodesPerRegister = 10000;
        private const int kMaxMonitoredItemsPerCall = 10000;
        private const int kMaxBrowseContinuationPoints = 100;
    }
}
