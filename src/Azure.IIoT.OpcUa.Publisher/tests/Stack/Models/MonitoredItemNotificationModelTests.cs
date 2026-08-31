// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Opc.Ua;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="MonitoredItemNotificationModel"/>.
    /// </summary>
    public sealed class MonitoredItemNotificationModelTests
    {
        // ── MessageId ─────────────────────────────────────────────────────────

        [Fact]
        public void MessageId_WithSequenceNumber_ReturnsSequenceNumber()
        {
            var model = new MonitoredItemNotificationModel
            {
                SequenceNumber = 42u
            };

            Assert.Equal(42u, model.MessageId);
        }

        [Fact]
        public void MessageId_WithNullSequenceNumber_ReturnsHashCode()
        {
            var model = new MonitoredItemNotificationModel
            {
                SequenceNumber = null
            };

            // Hash code is never zero for this purpose; whatever it is, it must match GetHashCode()
            var expected = (uint)model.GetHashCode();
            Assert.Equal(expected, model.MessageId);
        }

        [Fact]
        public void MessageId_WithSequenceNumberZero_ReturnsZero()
        {
            var model = new MonitoredItemNotificationModel
            {
                SequenceNumber = 0u
            };

            // SequenceNumber is 0 (not null) → returns 0
            Assert.Equal(0u, model.MessageId);
        }

        // ── Properties settable ───────────────────────────────────────────────

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            var value = new DataValue(new Variant(99));
            var model = new MonitoredItemNotificationModel
            {
                Id = "item-1",
                DataSetFieldName = "field-1",
                SequenceNumber = 7u,
                Overflow = 3,
                Value = value,
                Flags = MonitoredItemSourceFlags.Heartbeat
            };

            Assert.Equal("item-1", model.Id);
            Assert.Equal("field-1", model.DataSetFieldName);
            Assert.Equal(7u, model.SequenceNumber);
            Assert.Equal(3, model.Overflow);
            Assert.Equal(value, model.Value);
            Assert.Equal(MonitoredItemSourceFlags.Heartbeat, model.Flags);
        }

        // ── Record equality ───────────────────────────────────────────────────

        [Fact]
        public void TwoRecordsWithSameValues_AreEqual()
        {
            var value = new DataValue(new Variant(1));
            var a = new MonitoredItemNotificationModel
            {
                Id = "x",
                SequenceNumber = 5u,
                Value = value
            };
            var b = new MonitoredItemNotificationModel
            {
                Id = "x",
                SequenceNumber = 5u,
                Value = value
            };

            Assert.Equal(a, b);
        }

        [Fact]
        public void TwoRecordsWithDifferentId_AreNotEqual()
        {
            var a = new MonitoredItemNotificationModel { Id = "a" };
            var b = new MonitoredItemNotificationModel { Id = "b" };

            Assert.NotEqual(a, b);
        }
    }
}
