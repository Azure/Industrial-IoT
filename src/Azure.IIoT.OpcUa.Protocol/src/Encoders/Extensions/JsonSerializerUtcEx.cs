// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using Opc.Ua;
    using System;
    using System.Globalization;

    /// <summary>
    /// Json serializer settings extensions
    /// </summary>
    public static class JsonSerializerUtcEx {

        /// <summary>
        /// Constant for OpcUa JSON encoded DateTime.MinValue.
        /// </summary>
        public static string OpcUaDateTimeMinValue = "0001-01-01T00:00:00Z";

        /// <summary>
        /// Constant for OpcUa JSON encoded DateTime.MaxValue.
        /// </summary>
        public static string OpcUaDateTimeMaxValue = "9999-12-31T23:59:59Z";

        /// <summary>
        /// DateTime value of: “9999-12-31T23:59:59Z”
        /// </summary>
        private static readonly DateTime kDateTimeMaxJsonValue = new DateTime((long)3155378975990000000);

        /// <summary>
        /// Convert to OpcUa JSON Encoded Utc string.
        /// </summary>
        public static string ToOpcUaJsonEncodedTime(this DateTime dateTime) {
            if (dateTime <= DateTime.MinValue) {
                return OpcUaDateTimeMinValue;
            }
            else if (dateTime >= kDateTimeMaxJsonValue) {
                return OpcUaDateTimeMaxValue;
            }
            else {
                return dateTime.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
                    CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Convert DataValue timestamps to OpcUa Encoded Utc.
        /// </summary>
        public static DataValue ToOpcUaUniversalTime(this DataValue dataValue) {
            dataValue.SourceTimestamp = dataValue.SourceTimestamp.ToOpcUaUniversalTime();
            dataValue.ServerTimestamp = dataValue.ServerTimestamp.ToOpcUaUniversalTime();
            return dataValue;
        }

        /// <summary>
        /// Converter from OpcUa encoded Utc to DateTime.
        /// The result is DateTime.MinValue, DateTime.MaxValue or
        /// the Utc kind.
        /// </summary>
        public static DateTime ToOpcUaUniversalTime(this DateTime dateTime) {
            if (dateTime <= DateTime.MinValue) {
                return DateTime.MinValue;
            }
            else if (dateTime >= kDateTimeMaxJsonValue) {
                return DateTime.MaxValue;
            }
            else {
                if (dateTime.Kind != DateTimeKind.Utc)                     {
                    return dateTime.ToUniversalTime();
                }
            }
            return dateTime;
        }
    }
}
