// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Xunit;

    /// <summary>
    /// Tests for the priority logic on <see cref="BaseMonitoredItemModel.Id"/> and
    /// <see cref="BaseMonitoredItemModel.DisplayName"/>.
    /// Uses <see cref="DataMonitoredItemModel"/> (the simplest concrete subtype).
    /// </summary>
    public sealed class BaseMonitoredItemModelTests
    {
        // ── Id priority logic ─────────────────────────────────────────────────

        [Fact]
        public void Id_WithDataSetFieldId_ReturnsDataSetFieldId()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                DataSetFieldId = "field-id-1",
                DataSetFieldName = "field-name-1"
            };

            Assert.Equal("field-id-1", model.Id);
        }

        [Fact]
        public void Id_WithNoDataSetFieldId_ReturnsDataSetFieldName()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                DataSetFieldId = null,
                DataSetFieldName = "field-name-1"
            };

            Assert.Equal("field-name-1", model.Id);
        }

        [Fact]
        public void Id_WithNoFieldIdOrName_ReturnsStartNodeId()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                DataSetFieldId = null,
                DataSetFieldName = null
            };

            Assert.Equal("i=2256", model.Id);
        }

        [Fact]
        public void Id_WithEmptyDataSetFieldId_FallsThroughToFieldName()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                DataSetFieldId = string.Empty,
                DataSetFieldName = "fallback-name"
            };

            Assert.Equal("fallback-name", model.Id);
        }

        [Fact]
        public void Id_WithEmptyFieldIdAndName_ReturnsStartNodeId()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "ns=2;s=Temperature",
                DataSetFieldId = string.Empty,
                DataSetFieldName = string.Empty
            };

            Assert.Equal("ns=2;s=Temperature", model.Id);
        }

        // ── DisplayName priority logic ────────────────────────────────────────

        [Fact]
        public void DisplayName_WithDataSetFieldName_ReturnsDataSetFieldName()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                DataSetFieldName = "display-name-1",
                DataSetFieldId = "field-id-1"
            };

            Assert.Equal("display-name-1", model.DisplayName);
        }

        [Fact]
        public void DisplayName_WithNoFieldName_ReturnsDataSetFieldId()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                DataSetFieldName = null,
                DataSetFieldId = "field-id-1"
            };

            Assert.Equal("field-id-1", model.DisplayName);
        }

        [Fact]
        public void DisplayName_WithNoFieldNameOrId_ReturnsStartNodeId()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "ns=3;s=Pressure",
                DataSetFieldName = null,
                DataSetFieldId = null
            };

            Assert.Equal("ns=3;s=Pressure", model.DisplayName);
        }

        [Fact]
        public void DisplayName_WithEmptyDataSetFieldName_FallsThroughToFieldId()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "i=1000",
                DataSetFieldName = string.Empty,
                DataSetFieldId = "fallback-id"
            };

            Assert.Equal("fallback-id", model.DisplayName);
        }

        [Fact]
        public void DisplayName_WithEmptyFieldNameAndId_ReturnsStartNodeId()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "i=9999",
                DataSetFieldName = string.Empty,
                DataSetFieldId = string.Empty
            };

            Assert.Equal("i=9999", model.DisplayName);
        }

        // ── Combined scenarios ────────────────────────────────────────────────

        [Fact]
        public void Id_FieldIdTakesPriorityOverFieldName()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "i=1",
                DataSetFieldId = "id-wins",
                DataSetFieldName = "name-loses"
            };

            Assert.Equal("id-wins", model.Id);
        }

        [Fact]
        public void DisplayName_FieldNameTakesPriorityOverFieldId()
        {
            var model = new DataMonitoredItemModel
            {
                StartNodeId = "i=1",
                DataSetFieldName = "name-wins",
                DataSetFieldId = "id-loses"
            };

            Assert.Equal("name-wins", model.DisplayName);
        }
    }
}
