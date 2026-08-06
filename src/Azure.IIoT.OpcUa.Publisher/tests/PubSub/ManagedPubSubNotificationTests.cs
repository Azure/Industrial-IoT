// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using Opc.Ua;
    using Opc.Ua.PubSub.DataSets;
    using Opc.Ua.PubSub.Encoding;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="ManagedPubSubNotification"/> and related model types.
    /// </summary>
    public sealed class ManagedPubSubNotificationTests
    {
        // ── Constructor: DataValue overload ──────────────────────────────────

        [Fact]
        public void DataValueCtor_ValidInputs_SetsAllProperties()
        {
            var ts = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
            var value = new DataValue(new Variant(42), StatusCodes.Good, ts.UtcDateTime);

            var notification = new ManagedPubSubNotification("ds", "field", ts, value);

            Assert.Equal("ds", notification.DataSetName);
            Assert.Equal("field", notification.FieldName);
            Assert.Equal(ManagedPubSubNotificationKind.Data, notification.Kind);
            Assert.Equal(ts, notification.Timestamp);
            Assert.Single(notification.Fields);
        }

        [Fact]
        public void DataValueCtor_EmptyFieldName_ThrowsArgumentException()
        {
            var ts = DateTimeOffset.UtcNow;
            var value = new DataValue();

            Assert.Throws<ArgumentException>(() =>
                new ManagedPubSubNotification("ds", "", ts, value));
        }

        [Fact]
        public void DataValueCtor_WhitespaceFieldName_ThrowsArgumentException()
        {
            var value = new DataValue();

            Assert.Throws<ArgumentException>(() =>
                new ManagedPubSubNotification("ds", "   ", DateTimeOffset.UtcNow, value));
        }

        [Fact]
        public void DataValueCtor_EmptyDataSetName_ThrowsArgumentException()
        {
            var value = new DataValue();

            Assert.Throws<ArgumentException>(() =>
                new ManagedPubSubNotification("", "field", DateTimeOffset.UtcNow, value));
        }

        [Fact]
        public void DataValueCtor_ExplicitKind_SetsKind()
        {
            var value = new DataValue();

            var notification = new ManagedPubSubNotification(
                "ds", "field", DateTimeOffset.UtcNow, value,
                ManagedPubSubNotificationKind.Extension);

            Assert.Equal(ManagedPubSubNotificationKind.Extension, notification.Kind);
        }

        // ── Constructor: multi-field overload ────────────────────────────────

        [Fact]
        public void MultiFieldCtor_ValidInputs_SetsAllProperties()
        {
            var ts = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
            var fields = new List<ManagedPubSubField>
            {
                new ManagedPubSubField("field1", new DataValue(new Variant("hello"))),
                new ManagedPubSubField("field2", new DataValue(new Variant(99)))
            };

            var notification = new ManagedPubSubNotification(
                "ds", ts, ManagedPubSubNotificationKind.Event, fields);

            Assert.Equal("ds", notification.DataSetName);
            Assert.Equal(ManagedPubSubNotificationKind.Event, notification.Kind);
            Assert.Equal(ts, notification.Timestamp);
            Assert.Equal(2, notification.Fields.Count);
            Assert.Equal("field1", notification.FieldName);
        }

        [Fact]
        public void MultiFieldCtor_NullFields_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ManagedPubSubNotification(
                    "ds", DateTimeOffset.UtcNow, ManagedPubSubNotificationKind.Data,
                    null!));
        }

        [Fact]
        public void MultiFieldCtor_EmptyFields_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new ManagedPubSubNotification(
                    "ds", DateTimeOffset.UtcNow, ManagedPubSubNotificationKind.Data,
                    Array.Empty<ManagedPubSubField>()));
        }

        [Fact]
        public void MultiFieldCtor_EmptyDataSetName_ThrowsArgumentException()
        {
            var fields = new List<ManagedPubSubField>
            {
                new ManagedPubSubField("f", new DataValue())
            };

            Assert.Throws<ArgumentException>(() =>
                new ManagedPubSubNotification(
                    "", DateTimeOffset.UtcNow, ManagedPubSubNotificationKind.Data, fields));
        }

        [Fact]
        public void MultiFieldCtor_ExplicitFrame_SetsFrame()
        {
            var fields = new List<ManagedPubSubField>
            {
                new ManagedPubSubField("f", new DataValue())
            };

            var notification = new ManagedPubSubNotification(
                "ds", DateTimeOffset.UtcNow, ManagedPubSubNotificationKind.Data, fields,
                PubSubDataSetMessageType.DeltaFrame);

            Assert.Equal(PubSubDataSetMessageType.DeltaFrame, notification.Frame);
        }

        // ── Constructor: byte payload overload ───────────────────────────────

        [Fact]
        public void BytePayloadCtor_ValidPayload_WrapsAsByteStringDataValue()
        {
            var ts = DateTimeOffset.UtcNow;
            var payload = new byte[] { 1, 2, 3 };

            var notification = new ManagedPubSubNotification("ds", "field", ts, payload);

            Assert.Equal("ds", notification.DataSetName);
            Assert.Equal("field", notification.FieldName);
            Assert.Equal(ManagedPubSubNotificationKind.Data, notification.Kind);
            // Payload is wrapped in a DataValue with byte array Variant
            var valueBytes = notification.Value.Value as byte[];
            Assert.NotNull(valueBytes);
            Assert.Equal(payload, valueBytes);
        }

        [Fact]
        public void BytePayloadCtor_EmptyFieldName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new ManagedPubSubNotification("ds", "", DateTimeOffset.UtcNow,
                    ReadOnlySpan<byte>.Empty));
        }

        // ── Constructor: MetaData overload ───────────────────────────────────

        [Fact]
        public void MetaDataCtor_ValidInputs_SetsAllProperties()
        {
            var metaData = new DataSetMetaDataType { Name = "my-dataset" };

            var notification = new ManagedPubSubNotification("ds", metaData);

            Assert.Equal("ds", notification.DataSetName);
            Assert.Equal(ManagedPubSubNotificationKind.MetaData, notification.Kind);
            Assert.Same(metaData, notification.ResolvedMetaData);
        }

        [Fact]
        public void MetaDataCtor_NullMetaData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ManagedPubSubNotification("ds", (DataSetMetaDataType)null!));
        }

        [Fact]
        public void MetaDataCtor_EmptyDataSetName_ThrowsArgumentException()
        {
            var metaData = new DataSetMetaDataType();

            Assert.Throws<ArgumentException>(() =>
                new ManagedPubSubNotification("", metaData));
        }

        // ── KeepAlive factory ────────────────────────────────────────────────

        [Fact]
        public void KeepAlive_ValidInputs_SetsKindAndTimestamp()
        {
            var ts = new DateTimeOffset(2024, 6, 15, 8, 0, 0, TimeSpan.Zero);

            var notification = ManagedPubSubNotification.KeepAlive("ds", ts);

            Assert.Equal("ds", notification.DataSetName);
            Assert.Equal(ManagedPubSubNotificationKind.KeepAlive, notification.Kind);
            Assert.Equal(PubSubDataSetMessageType.KeepAlive, notification.Frame);
            Assert.Equal(ts, notification.Timestamp);
        }

        [Fact]
        public void KeepAlive_HasNoFields()
        {
            var notification = ManagedPubSubNotification.KeepAlive("ds", DateTimeOffset.UtcNow);

            Assert.Empty(notification.Fields);
            Assert.Equal(string.Empty, notification.FieldName);
        }

        // ── Properties ───────────────────────────────────────────────────────

        [Fact]
        public void Value_SingleField_ReturnsFirstFieldValue()
        {
            var expected = new DataValue(new Variant(77));
            var notification = new ManagedPubSubNotification(
                "ds", "f", DateTimeOffset.UtcNow, expected);

            Assert.Equal(expected.Value, notification.Value.Value);
        }

        [Fact]
        public void FieldName_EmptyFields_ReturnsEmptyString()
        {
            var notification = ManagedPubSubNotification.KeepAlive("ds", DateTimeOffset.UtcNow);

            Assert.Equal(string.Empty, notification.FieldName);
        }

        [Fact]
        public void ResolvedMetaData_NonMetaDataNotification_ReturnsNull()
        {
            var notification = new ManagedPubSubNotification(
                "ds", "f", DateTimeOffset.UtcNow, new DataValue());

            Assert.Null(notification.ResolvedMetaData);
        }

        // ── Clone ─────────────────────────────────────────────────────────────

        [Fact]
        public void Clone_DataNotification_ProducesEquivalentCopy()
        {
            var original = new ManagedPubSubNotification(
                "ds", "field", DateTimeOffset.UtcNow, new DataValue(new Variant(1)));

            var clone = original.Clone();

            Assert.Equal(original.DataSetName, clone.DataSetName);
            Assert.Equal(original.Kind, clone.Kind);
            Assert.Equal(original.FieldName, clone.FieldName);
            Assert.NotSame(original, clone);
        }

        [Fact]
        public void Clone_KeepAlive_ProducesKeepAlive()
        {
            var original = ManagedPubSubNotification.KeepAlive("ds", DateTimeOffset.UtcNow);

            var clone = original.Clone();

            Assert.Equal(ManagedPubSubNotificationKind.KeepAlive, clone.Kind);
            Assert.Equal(original.DataSetName, clone.DataSetName);
        }

        [Fact]
        public void Clone_MetaDataNotification_ProducesEquivalentCopy()
        {
            var metaData = new DataSetMetaDataType { Name = "meta" };
            var original = new ManagedPubSubNotification("ds", metaData);

            var clone = original.Clone();

            Assert.Equal(original.DataSetName, clone.DataSetName);
            Assert.Equal(ManagedPubSubNotificationKind.MetaData, clone.Kind);
            Assert.Same(original.ResolvedMetaData, clone.ResolvedMetaData);
        }

        // ── Internal barrier ─────────────────────────────────────────────────

        [Fact]
        public void CreateBarrier_IsBarrierIsTrue()
        {
            var barrier = new TaskCompletionSource();
            var notification = ManagedPubSubNotification.CreateBarrier("ds", barrier);

            Assert.True(notification.IsBarrier);
        }

        [Fact]
        public void NonBarrier_IsBarrierIsFalse()
        {
            var notification = new ManagedPubSubNotification(
                "ds", "f", DateTimeOffset.UtcNow, new DataValue());

            Assert.False(notification.IsBarrier);
        }

        [Fact]
        public void CompleteBarrier_CompletesTheTaskCompletionSource()
        {
            var barrier = new TaskCompletionSource();
            var notification = ManagedPubSubNotification.CreateBarrier("ds", barrier);

            notification.CompleteBarrier();

            Assert.True(barrier.Task.IsCompletedSuccessfully);
        }

        [Fact]
        public void Clone_BarrierNotification_ClonesWithSameBarrier()
        {
            var barrier = new TaskCompletionSource();
            var original = ManagedPubSubNotification.CreateBarrier("ds", barrier);

            var clone = original.Clone();

            Assert.True(clone.IsBarrier);
        }

        // ── ManagedPubSubField ────────────────────────────────────────────────

        [Fact]
        public void ManagedPubSubField_StoresNameAndValue()
        {
            var value = new DataValue(new Variant("hello"));
            var field = new ManagedPubSubField("myField", value);

            Assert.Equal("myField", field.Name);
            Assert.Equal("hello", field.Value.Value as string);
        }

        [Fact]
        public void ManagedPubSubField_EqualityByNameAndValue()
        {
            var f1 = new ManagedPubSubField("f", new DataValue(new Variant(1)));
            var f2 = new ManagedPubSubField("f", new DataValue(new Variant(1)));

            Assert.Equal(f1, f2);
        }
    }
}
