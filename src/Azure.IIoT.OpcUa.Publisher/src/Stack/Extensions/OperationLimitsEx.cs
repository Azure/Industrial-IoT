// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Numerics;

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
        public static void Override(this Limits limits, Limits? update)
        {
            if (update == null)
            {
                return;
            }

            limits.MaxMonitoredItemsPerCall
                = Override(limits.MaxMonitoredItemsPerCall, update.MaxMonitoredItemsPerCall);
            limits.MaxNodesPerBrowse
                = Override(limits.MaxNodesPerBrowse, update.MaxNodesPerBrowse);
            limits.MaxNodesPerHistoryReadData
                = Override(limits.MaxNodesPerHistoryReadData, update.MaxNodesPerHistoryReadData);
            limits.MaxNodesPerHistoryReadEvents
                = Override(limits.MaxNodesPerHistoryReadEvents, update.MaxNodesPerHistoryReadEvents);
            limits.MaxNodesPerHistoryUpdateData
                = Override(limits.MaxNodesPerHistoryUpdateData, update.MaxNodesPerHistoryUpdateData);
            limits.MaxNodesPerHistoryUpdateEvents
                = Override(limits.MaxNodesPerHistoryUpdateEvents, update.MaxNodesPerHistoryUpdateEvents);
            limits.MaxNodesPerMethodCall
                = Override(limits.MaxNodesPerMethodCall, update.MaxNodesPerMethodCall);
            limits.MaxNodesPerNodeManagement
                = Override(limits.MaxNodesPerNodeManagement, update.MaxNodesPerNodeManagement);
            limits.MaxNodesPerRead
                = Override(limits.MaxNodesPerRead, update.MaxNodesPerRead);
            limits.MaxNodesPerRegisterNodes
                = Override(limits.MaxNodesPerRegisterNodes, update.MaxNodesPerRegisterNodes);
            limits.MaxNodesPerTranslateBrowsePathsToNodeIds
                = Override(limits.MaxNodesPerTranslateBrowsePathsToNodeIds, update.MaxNodesPerTranslateBrowsePathsToNodeIds);
            limits.MaxNodesPerWrite
                = Override(limits.MaxNodesPerWrite, update.MaxNodesPerWrite);

            static uint Override(uint a, uint b) => b == 0u ? a : b < a ? b : a;
        }

        /// <summary>
        /// Max nodes per browse
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxNodesPerBrowse(this Limits model)
        {
            var cur = model.MaxNodesPerBrowse;
            return Math.Min(Math.Max((int)cur, 1), kMaxBrowseNodes);
        }

        /// <summary>
        /// Max continuation points per browse
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxBrowseContinuationPoints(this Limits model)
        {
            var cur = model.MaxBrowseContinuationPoints;
            return Math.Min(Math.Max((int)cur, 1), kMaxBrowseContinuationPoints);
        }

        /// <summary>
        /// Max nodes per read
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxNodesPerRead(this Limits model)
        {
            var cur = model.MaxNodesPerRead;
            return Math.Min(Math.Max((int)cur, 1), kMaxReadNodes);
        }

        /// <summary>
        /// Max nodes per translate
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxNodesPerTranslatePathsToNodeIds(this Limits model)
        {
            var cur = model.MaxNodesPerTranslatePathsToNodeIds;
            return Math.Min(Math.Max((int)cur, 1), kMaxNodesPerTranslate);
        }

        /// <summary>
        /// Max nodes per register
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxNodesPerRegisterNodes(this Limits model)
        {
            var cur = model.MaxNodesPerRegisterNodes;
            return Math.Min(Math.Max((int)cur, 1), kMaxNodesPerRegister);
        }

        /// <summary>
        /// Max monitored items
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int GetMaxMonitoredItemsPerCall(this Limits model)
        {
            var cur = model.MaxMonitoredItemsPerCall;
            return Math.Min(Math.Max((int)cur, 1), kMaxMonitoredItemsPerCall);
        }

        /// <summary>
        /// Convert limits
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public static OperationLimitsModel ToServiceModel(this Limits l)
        {
            return new OperationLimitsModel
            {
                MaxArrayLength = Null(l.MaxArrayLength),
                MaxBrowseContinuationPoints = Null(l.MaxBrowseContinuationPoints),
                MaxByteStringLength = Null(l.MaxByteStringLength),
                MaxHistoryContinuationPoints = Null(l.MaxHistoryContinuationPoints),
                MaxQueryContinuationPoints = Null(l.MaxQueryContinuationPoints),
                MaxStringLength = Null(l.MaxStringLength),
                MinSupportedSampleRate = Null(l.MinSupportedSampleRate),
                MaxNodesPerHistoryReadData = Null(l.MaxNodesPerHistoryReadData),
                MaxNodesPerHistoryReadEvents = Null(l.MaxNodesPerHistoryReadEvents),
                MaxNodesPerWrite = Null(l.MaxNodesPerWrite),
                MaxNodesPerRead = Null(l.MaxNodesPerRead),
                MaxNodesPerHistoryUpdateData = Null(l.MaxNodesPerHistoryUpdateData),
                MaxNodesPerHistoryUpdateEvents = Null(l.MaxNodesPerHistoryUpdateEvents),
                MaxNodesPerMethodCall = Null(l.MaxNodesPerMethodCall),
                MaxNodesPerBrowse = Null(l.MaxNodesPerBrowse),
                MaxNodesPerRegisterNodes = Null(l.MaxNodesPerRegisterNodes),
                MaxNodesPerTranslatePathsToNodeIds = Null(l.MaxNodesPerTranslatePathsToNodeIds),
                MaxNodesPerNodeManagement = Null(l.MaxNodesPerNodeManagement),
                MaxMonitoredItemsPerCall = Null(l.MaxMonitoredItemsPerCall),
            };
            static T? Null<T>(T v) where T : struct, INumberBase<T> => T.IsZero(v) ? null : v;
        }


        private const int kMaxReadNodes = 10000;
        private const int kMaxNodesPerTranslate = 1000;
        private const int kMaxBrowseNodes = 10000;
        private const int kMaxNodesPerRegister = 10000;
        private const int kMaxMonitoredItemsPerCall = 10000;
        private const int kMaxBrowseContinuationPoints = 100;
    }
}
