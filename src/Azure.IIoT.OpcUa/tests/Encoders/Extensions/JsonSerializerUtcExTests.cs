// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Opc.Ua;
    using System;
    using Xunit;

    public sealed class JsonSerializerUtcExTests
    {
        [Fact]
        public void ToOpcUaJsonEncodedTime_MinValue_ReturnsOpcUaMinimum()
        {
            var result = DateTime.MinValue.ToOpcUaJsonEncodedTime();

            Assert.Equal(JsonSerializerUtcEx.OpcUaDateTimeMinValue, result);
        }

        [Fact]
        public void ToOpcUaJsonEncodedTime_MaxJsonValueOrGreater_ReturnsOpcUaMaximum()
        {
            var result = DateTime.MaxValue.ToOpcUaJsonEncodedTime();

            Assert.Equal(JsonSerializerUtcEx.OpcUaDateTimeMaxValue, result);
        }

        [Fact]
        public void ToOpcUaJsonEncodedTime_LocalTime_ConvertsToUniversalTime()
        {
            var local = new DateTime(2026, 8, 3, 12, 34, 56, DateTimeKind.Local);

            var result = local.ToOpcUaJsonEncodedTime();

            Assert.Equal(local.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
                System.Globalization.CultureInfo.InvariantCulture), result);
        }

        [Fact]
        public void ToOpcUaUniversalTime_MinValue_ReturnsMinValue()
        {
            var result = DateTime.MinValue.ToOpcUaUniversalTime();

            Assert.Equal(DateTime.MinValue, result);
        }

        [Fact]
        public void ToOpcUaUniversalTime_MaxValue_ReturnsMaxValue()
        {
            var result = DateTime.MaxValue.ToOpcUaUniversalTime();

            Assert.Equal(DateTime.MaxValue, result);
        }

        [Fact]
        public void ToOpcUaUniversalTime_LocalTime_ReturnsUtcTime()
        {
            var local = new DateTime(2026, 8, 3, 12, 34, 56, DateTimeKind.Local);

            var result = local.ToOpcUaUniversalTime();

            Assert.Equal(DateTimeKind.Utc, result.Kind);
            Assert.Equal(local.ToUniversalTime(), result);
        }

        [Fact]
        public void ToOpcUaUniversalTime_UtcTime_ReturnsSameValue()
        {
            var utc = new DateTime(2026, 8, 3, 12, 34, 56, DateTimeKind.Utc);

            var result = utc.ToOpcUaUniversalTime();

            Assert.Equal(utc, result);
        }

        [Fact]
        public void ToOpcUaUniversalTime_DataValue_ConvertsSourceAndServerTimestamps()
        {
            var source = new DateTime(2026, 8, 3, 12, 34, 56, DateTimeKind.Local);
            var server = new DateTime(2026, 8, 3, 13, 34, 56, DateTimeKind.Local);
            var dataValue = new DataValue(new Variant(1))
                .WithSourceTimestamp(source)
                .WithServerTimestamp(server);

            var result = dataValue.ToOpcUaUniversalTime();

            Assert.Equal(source.ToUniversalTime(), result.SourceTimestamp.ToDateTime());
            Assert.Equal(server.ToUniversalTime(), result.ServerTimestamp.ToDateTime());
        }
    }
}
