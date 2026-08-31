// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="OperationLimitsEx"/> — pure model extension methods.
    /// </summary>
    public class OperationLimitsExTests
    {
        // ──────────────────── Override ───────────────────────────

        [Fact]
        public void Override_NullUpdate_ReturnsSameLimits()
        {
            var limits = new OperationLimits { MaxNodesPerBrowse = 10 };
            var result = limits.Override(null);
            Assert.Equal(10u, result.MaxNodesPerBrowse);
        }

        [Fact]
        public void Override_BothZero_RemainsZero()
        {
            var limits = new OperationLimits { MaxNodesPerBrowse = 0 };
            var update = new OperationLimits { MaxNodesPerBrowse = 0 };
            var result = limits.Override(update);
            Assert.Equal(0u, result.MaxNodesPerBrowse);
        }

        [Fact]
        public void Override_CurrentZeroUpdateNonZero_TakesUpdateValue()
        {
            var limits = new OperationLimits { MaxNodesPerBrowse = 0 };
            var update = new OperationLimits { MaxNodesPerBrowse = 50 };
            var result = limits.Override(update);
            Assert.Equal(50u, result.MaxNodesPerBrowse);
        }

        [Fact]
        public void Override_CurrentNonZeroUpdateZero_KeepsCurrent()
        {
            var limits = new OperationLimits { MaxNodesPerBrowse = 100 };
            var update = new OperationLimits { MaxNodesPerBrowse = 0 };
            var result = limits.Override(update);
            Assert.Equal(100u, result.MaxNodesPerBrowse);
        }

        [Fact]
        public void Override_BothNonZero_TakesMinimum()
        {
            var limits = new OperationLimits { MaxNodesPerBrowse = 100 };
            var update = new OperationLimits { MaxNodesPerBrowse = 50 };
            var result = limits.Override(update);
            Assert.Equal(50u, result.MaxNodesPerBrowse);
        }

        [Fact]
        public void Override_UpdateSmallerMaxNodesPerRead_ReducesLimit()
        {
            var limits = new OperationLimits { MaxNodesPerRead = 200 };
            var update = new OperationLimits { MaxNodesPerRead = 100 };
            var result = limits.Override(update);
            Assert.Equal(100u, result.MaxNodesPerRead);
        }

        [Fact]
        public void Override_AllFieldsOverridden()
        {
            var limits = new OperationLimits
            {
                MaxNodesPerBrowse = 100,
                MaxNodesPerRead = 100,
                MaxNodesPerWrite = 100,
                MaxNodesPerMethodCall = 100,
                MaxNodesPerHistoryReadData = 100,
                MaxNodesPerHistoryReadEvents = 100,
                MaxNodesPerHistoryUpdateData = 100,
                MaxNodesPerHistoryUpdateEvents = 100,
                MaxNodesPerNodeManagement = 100,
                MaxNodesPerRegisterNodes = 100,
                MaxNodesPerTranslateBrowsePathsToNodeIds = 100,
                MaxMonitoredItemsPerCall = 100
            };
            var update = new OperationLimits
            {
                MaxNodesPerBrowse = 50,
                MaxNodesPerRead = 50,
                MaxNodesPerWrite = 50,
                MaxNodesPerMethodCall = 50,
                MaxNodesPerHistoryReadData = 50,
                MaxNodesPerHistoryReadEvents = 50,
                MaxNodesPerHistoryUpdateData = 50,
                MaxNodesPerHistoryUpdateEvents = 50,
                MaxNodesPerNodeManagement = 50,
                MaxNodesPerRegisterNodes = 50,
                MaxNodesPerTranslateBrowsePathsToNodeIds = 50,
                MaxMonitoredItemsPerCall = 50
            };

            var result = limits.Override(update);

            Assert.Equal(50u, result.MaxNodesPerBrowse);
            Assert.Equal(50u, result.MaxNodesPerRead);
            Assert.Equal(50u, result.MaxNodesPerWrite);
            Assert.Equal(50u, result.MaxNodesPerMethodCall);
            Assert.Equal(50u, result.MaxNodesPerHistoryReadData);
            Assert.Equal(50u, result.MaxNodesPerHistoryReadEvents);
            Assert.Equal(50u, result.MaxNodesPerHistoryUpdateData);
            Assert.Equal(50u, result.MaxNodesPerHistoryUpdateEvents);
            Assert.Equal(50u, result.MaxNodesPerNodeManagement);
            Assert.Equal(50u, result.MaxNodesPerRegisterNodes);
            Assert.Equal(50u, result.MaxNodesPerTranslateBrowsePathsToNodeIds);
            Assert.Equal(50u, result.MaxMonitoredItemsPerCall);
        }

        // ──────────────────── GetMaxNodesPerBrowse ────────────────

        [Fact]
        public void GetMaxNodesPerBrowse_Zero_ReturnsOne()
        {
            var model = new OperationLimitsModel { MaxNodesPerBrowse = 0 };
            Assert.Equal(1, model.GetMaxNodesPerBrowse());
        }

        [Fact]
        public void GetMaxNodesPerBrowse_Null_ReturnsOne()
        {
            var model = new OperationLimitsModel { MaxNodesPerBrowse = null };
            Assert.Equal(1, model.GetMaxNodesPerBrowse());
        }

        [Fact]
        public void GetMaxNodesPerBrowse_SmallValue_ReturnsValue()
        {
            var model = new OperationLimitsModel { MaxNodesPerBrowse = 500 };
            Assert.Equal(500, model.GetMaxNodesPerBrowse());
        }

        [Fact]
        public void GetMaxNodesPerBrowse_ExceedsMax_ReturnsCap()
        {
            var model = new OperationLimitsModel { MaxNodesPerBrowse = 99999 };
            var result = model.GetMaxNodesPerBrowse();
            // Cap is 10000
            Assert.Equal(10000, result);
        }

        // ──────────────────── GetMaxBrowseContinuationPoints ─────

        [Fact]
        public void GetMaxBrowseContinuationPoints_Zero_ReturnsOne()
        {
            var model = new OperationLimitsModel { MaxBrowseContinuationPoints = 0 };
            Assert.Equal(1, model.GetMaxBrowseContinuationPoints());
        }

        [Fact]
        public void GetMaxBrowseContinuationPoints_SmallValue_ReturnsValue()
        {
            var model = new OperationLimitsModel { MaxBrowseContinuationPoints = 10 };
            Assert.Equal(10, model.GetMaxBrowseContinuationPoints());
        }

        [Fact]
        public void GetMaxBrowseContinuationPoints_ExceedsMax_ReturnsCap()
        {
            var model = new OperationLimitsModel { MaxBrowseContinuationPoints = 9999 };
            var result = model.GetMaxBrowseContinuationPoints();
            // Cap is 100
            Assert.Equal(100, result);
        }

        // ──────────────────── GetMaxNodesPerRead ─────────────────

        [Fact]
        public void GetMaxNodesPerRead_Zero_ReturnsOne()
        {
            var model = new OperationLimitsModel { MaxNodesPerRead = 0 };
            Assert.Equal(1, model.GetMaxNodesPerRead());
        }

        [Fact]
        public void GetMaxNodesPerRead_ExceedsMax_ReturnsCap()
        {
            var model = new OperationLimitsModel { MaxNodesPerRead = 99999 };
            Assert.Equal(10000, model.GetMaxNodesPerRead());
        }

        // ──────────────────── GetMaxNodesPerTranslatePaths ───────

        [Fact]
        public void GetMaxNodesPerTranslatePaths_Zero_ReturnsOne()
        {
            var model = new OperationLimitsModel { MaxNodesPerTranslatePathsToNodeIds = 0 };
            Assert.Equal(1, model.GetMaxNodesPerTranslatePathsToNodeIds());
        }

        [Fact]
        public void GetMaxNodesPerTranslatePaths_ExceedsMax_ReturnsCap()
        {
            var model = new OperationLimitsModel { MaxNodesPerTranslatePathsToNodeIds = 99999 };
            Assert.Equal(1000, model.GetMaxNodesPerTranslatePathsToNodeIds());
        }

        // ──────────────────── GetMaxNodesPerRegisterNodes ────────

        [Fact]
        public void GetMaxNodesPerRegisterNodes_Zero_ReturnsOne()
        {
            var model = new OperationLimitsModel { MaxNodesPerRegisterNodes = 0 };
            Assert.Equal(1, model.GetMaxNodesPerRegisterNodes());
        }

        [Fact]
        public void GetMaxNodesPerRegisterNodes_ExceedsMax_ReturnsCap()
        {
            var model = new OperationLimitsModel { MaxNodesPerRegisterNodes = 99999 };
            Assert.Equal(10000, model.GetMaxNodesPerRegisterNodes());
        }

        // ──────────────────── GetMaxMonitoredItemsPerCall ────────

        [Fact]
        public void GetMaxMonitoredItemsPerCall_Zero_ReturnsOne()
        {
            var model = new OperationLimitsModel { MaxMonitoredItemsPerCall = 0 };
            Assert.Equal(1, model.GetMaxMonitoredItemsPerCall());
        }

        [Fact]
        public void GetMaxMonitoredItemsPerCall_ExceedsMax_ReturnsCap()
        {
            var model = new OperationLimitsModel { MaxMonitoredItemsPerCall = 99999 };
            Assert.Equal(10000, model.GetMaxMonitoredItemsPerCall());
        }

        [Fact]
        public void GetMaxMonitoredItemsPerCall_AtMax_ReturnsMax()
        {
            var model = new OperationLimitsModel { MaxMonitoredItemsPerCall = 10000 };
            Assert.Equal(10000, model.GetMaxMonitoredItemsPerCall());
        }
    }
}
